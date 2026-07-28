using EscalaLixo.Api.Infrastructure;
using EscalaLixo.Modelos;
using EscalaLixo.Servicos;

namespace EscalaLixo.Api.Servicos;

/// <summary>
/// Verifica periodicamente os horários configurados e envia lembrete diário / prévia semanal ao Discord.
/// </summary>
public sealed class ServicoAgendamentoDiscord : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ServicoAgendamentoDiscord> _logger;
    private DateOnly? _ultimoLembreteDiario;
    private DateOnly? _ultimaPreviaSemanal;

    public ServicoAgendamentoDiscord(
        IServiceScopeFactory scopeFactory,
        ILogger<ServicoAgendamentoDiscord> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Agendamento Discord iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var intervaloMin = 60;
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var configApp = scope.ServiceProvider.GetRequiredService<ServicoConfiguracaoApp>();
                var cfg = await configApp.LerAsync(stoppingToken).ConfigureAwait(false);
                intervaloMin = Math.Clamp(cfg.IntervaloVerificacaoMinutos, 1, 1440);

                await VerificarEnviosAsync(scope.ServiceProvider, cfg, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no ciclo de agendamento Discord.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(intervaloMin), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task VerificarEnviosAsync(
        IServiceProvider services,
        ConfiguracaoApp cfg,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cfg.WebhookDiscord))
            return;

        var agora = DateTime.Now;
        var hoje = DateOnly.FromDateTime(agora);

        if (agora.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday
            && agora.Hour >= cfg.HoraLembreteDiario
            && _ultimoLembreteDiario != hoje)
        {
            var ok = await TentarEnviarDiaAsync(services, ct).ConfigureAwait(false);
            if (ok)
            {
                _ultimoLembreteDiario = hoje;
                _logger.LogInformation("Lembrete diário enviado automaticamente às {Hora:00}:00.", cfg.HoraLembreteDiario);
            }
        }

        if (agora.DayOfWeek == DayOfWeek.Monday
            && agora.Hour >= cfg.HoraPreviaSemanal
            && _ultimaPreviaSemanal != hoje)
        {
            var ok = await TentarEnviarPreviaAsync(services, ct).ConfigureAwait(false);
            if (ok)
            {
                _ultimaPreviaSemanal = hoje;
                _logger.LogInformation("Prévia semanal enviada automaticamente (segunda às {Hora:00}:00).", cfg.HoraPreviaSemanal);
            }
        }
    }

    private async Task<bool> TentarEnviarDiaAsync(IServiceProvider services, CancellationToken ct)
    {
        var repo = services.GetRequiredService<ServicoRepositorioArquivos>();
        var validacao = services.GetRequiredService<ServicoValidacaoEscala>();
        var configApp = services.GetRequiredService<ServicoConfiguracaoApp>();
        var biblioteca = services.GetRequiredService<ServicoBibliotecaGifs>();

        var cfg = await configApp.LerAsync(ct).ConfigureAwait(false);
        await biblioteca.HidratarGifsAsync(cfg, ct).ConfigureAwait(false);
        var discord = DiscordFactory.Criar(cfg);
        if (discord is null)
            return false;

        var lista = await repo.LerColaboradoresAsync(ct).ConfigureAwait(false);
        var escala = await repo.LerEscalaAsync(ct).ConfigureAwait(false);
        if (escala is null)
            return false;

        var hoje = DateTime.Now.DayOfWeek;
        if (hoje is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return false;

        var dia = escala.Dias.FirstOrDefault(d => DiasSemanaPt.CorrespondeAoDia(d.DiaDaSemana, hoje));
        if (dia is null || dia.Nomes.Count == 0)
            return false;

        if (!validacao.Validar(escala, lista).Valido)
            return false;

        using (discord)
        {
            await discord.NotificarDiaAsync(dia, lista, ct).ConfigureAwait(false);
        }

        return true;
    }

    private async Task<bool> TentarEnviarPreviaAsync(IServiceProvider services, CancellationToken ct)
    {
        var repo = services.GetRequiredService<ServicoRepositorioArquivos>();
        var validacao = services.GetRequiredService<ServicoValidacaoEscala>();
        var configApp = services.GetRequiredService<ServicoConfiguracaoApp>();
        var biblioteca = services.GetRequiredService<ServicoBibliotecaGifs>();

        var cfg = await configApp.LerAsync(ct).ConfigureAwait(false);
        await biblioteca.HidratarGifsAsync(cfg, ct).ConfigureAwait(false);
        var discord = DiscordFactory.Criar(cfg);
        if (discord is null)
            return false;

        var lista = await repo.LerColaboradoresAsync(ct).ConfigureAwait(false);
        var escala = await repo.LerEscalaAsync(ct).ConfigureAwait(false);
        if (escala is null)
            return false;

        if (!validacao.Validar(escala, lista).Valido)
            return false;

        using (discord)
        {
            await discord.NotificarPreviaSemanaAsync(escala, lista, ct).ConfigureAwait(false);
        }

        return true;
    }
}
