using EscalaLixo.Modelos;
using Microsoft.Extensions.Configuration;

namespace EscalaLixo.Servicos;

/// <summary>Leitura/gravação de configurações em public.configuracao_app (PostgreSQL).</summary>
public sealed class ServicoConfiguracaoApp : IDisposable
{
    private readonly RepositorioConfiguracaoAppPostgres _repo;
    private readonly string _pastaDados;
    private readonly IConfiguration _configuration;
    private readonly SemaphoreSlim _bloqueio = new(1, 1);

    public ServicoConfiguracaoApp(string connectionString, string pastaDados, IConfiguration configuration)
        : this(new RepositorioConfiguracaoAppPostgres(connectionString), pastaDados, configuration)
    {
    }

    public ServicoConfiguracaoApp(RepositorioConfiguracaoAppPostgres repo, string pastaDados, IConfiguration configuration)
    {
        _repo = repo;
        _pastaDados = pastaDados;
        _configuration = configuration;
        Directory.CreateDirectory(_pastaDados);
    }

    public string Fonte => "postgres:configuracao_app";

    public async Task<ConfiguracaoApp> LerAsync(CancellationToken ct = default)
    {
        await _bloqueio.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var db = await _repo.LerAsync(ct).ConfigureAwait(false);
            if (db is not null)
                return db;

            var importada = LerFallbackLocal();
            await _repo.SalvarAsync(importada, ct).ConfigureAwait(false);
            return importada;
        }
        finally
        {
            _bloqueio.Release();
        }
    }

    public ConfiguracaoApp Ler() => LerAsync().GetAwaiter().GetResult();

    public async Task<ConfiguracaoApp> SalvarAsync(ConfiguracaoApp entrada, CancellationToken ct = default)
    {
        await _bloqueio.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var atual = await _repo.LerAsync(ct).ConfigureAwait(false) ?? LerFallbackLocal();
            var merged = MesclarSecrets(atual, entrada);

            if (merged.IntervaloVerificacaoMinutos < 1)
                merged.IntervaloVerificacaoMinutos = 60;

            merged.HoraNotificacaoPadrao = ClampHora(merged.HoraNotificacaoPadrao, 8);
            merged.HoraPreviaSemanal = ClampHora(merged.HoraPreviaSemanal, merged.HoraNotificacaoPadrao);
            merged.HoraLembreteDiario = ClampHora(merged.HoraLembreteDiario, merged.HoraNotificacaoPadrao);

            await _repo.SalvarAsync(merged, ct).ConfigureAwait(false);
            return merged;
        }
        finally
        {
            _bloqueio.Release();
        }
    }

    private ConfiguracaoApp LerFallbackLocal()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(_pastaDados)
            .AddConfiguration(_configuration);

        var jsonLocal = Path.Combine(_pastaDados, NomesDosArquivos.ArquivoConfiguracaoApp);
        if (File.Exists(jsonLocal))
            builder.AddJsonFile(jsonLocal, optional: true, reloadOnChange: false);

        return MapearDeConfiguration(builder.Build());
    }

    private static int ClampHora(int valor, int fallback) =>
        valor is >= 0 and <= 23 ? valor : fallback;

    private static ConfiguracaoApp MesclarSecrets(ConfiguracaoApp atual, ConfiguracaoApp entrada) => new()
    {
        WebhookDiscord = ManterSecretSeVazio(entrada.WebhookDiscord, atual.WebhookDiscord),
        TokenBotDiscord = ManterSecretSeVazio(entrada.TokenBotDiscord, atual.TokenBotDiscord),
        IdServidorDiscord = string.IsNullOrWhiteSpace(entrada.IdServidorDiscord)
            ? atual.IdServidorDiscord
            : entrada.IdServidorDiscord.Trim(),
        UrlGifPreviaSemanal = entrada.UrlGifPreviaSemanal?.Trim() ?? "",
        UrlGifDiario = entrada.UrlGifDiario?.Trim() ?? "",
        GifPreviaSemanal = atual.GifPreviaSemanal,
        GifPreviaMime = atual.GifPreviaMime,
        GifDiario = atual.GifDiario,
        GifDiarioMime = atual.GifDiarioMime,
        GifPreviaId = atual.GifPreviaId,
        GifDiarioId = atual.GifDiarioId,
        ModeloMensagemDiaria = entrada.ModeloMensagemDiaria ?? "",
        IntervaloVerificacaoMinutos = entrada.IntervaloVerificacaoMinutos,
        HoraNotificacaoPadrao = entrada.HoraNotificacaoPadrao,
        HoraPreviaSemanal = entrada.HoraPreviaSemanal,
        HoraLembreteDiario = entrada.HoraLembreteDiario,
    };

    private static string ManterSecretSeVazio(string novo, string atual) =>
        string.IsNullOrWhiteSpace(novo) ? atual : novo.Trim();

    public static ConfiguracaoApp MapearDeConfiguration(IConfiguration cfg) => new()
    {
        WebhookDiscord = ConfiguracaoLeitura.Texto(cfg, ChavesConfiguracao.WebhookDiscord, ChavesConfiguracao.Legado.DiscordWebhookUrl),
        TokenBotDiscord = ConfiguracaoLeitura.Texto(cfg, ChavesConfiguracao.TokenBotDiscord, ChavesConfiguracao.Legado.DiscordBotToken),
        IdServidorDiscord = ConfiguracaoLeitura.Texto(cfg, ChavesConfiguracao.IdServidorDiscord, ChavesConfiguracao.Legado.DiscordGuildId),
        UrlGifPreviaSemanal = ConfiguracaoLeitura.Texto(cfg, ChavesConfiguracao.UrlGifPreviaSemanal, ChavesConfiguracao.Legado.DiscordGifUrl),
        UrlGifDiario = ConfiguracaoLeitura.Texto(cfg, ChavesConfiguracao.UrlGifDiario, ChavesConfiguracao.Legado.DiscordDailyGifUrl),
        ModeloMensagemDiaria = ConfiguracaoLeitura.Texto(cfg, ChavesConfiguracao.ModeloMensagemDiaria, ChavesConfiguracao.Legado.DiscordDailyMessage),
        IntervaloVerificacaoMinutos = int.TryParse(cfg[ChavesConfiguracao.IntervaloVerificacaoMinutos], out var iv) && iv > 0
            ? iv
            : int.TryParse(cfg[ChavesConfiguracao.Legado.CheckIntervalMinutes], out iv) && iv > 0 ? iv : 60,
        HoraNotificacaoPadrao = ConfiguracaoLeitura.Hora0a23(cfg, 8, ChavesConfiguracao.HoraNotificacaoPadrao, ChavesConfiguracao.Legado.NotifyHour),
        HoraPreviaSemanal = ConfiguracaoLeitura.Hora0a23(cfg, 8, ChavesConfiguracao.HoraPreviaSemanal, ChavesConfiguracao.Legado.NotifyHourWeeklyPreview, ChavesConfiguracao.HoraNotificacaoPadrao),
        HoraLembreteDiario = ConfiguracaoLeitura.Hora0a23(cfg, 8, ChavesConfiguracao.HoraLembreteDiario, ChavesConfiguracao.Legado.NotifyHourDaily, ChavesConfiguracao.HoraNotificacaoPadrao),
    };

    public void Dispose() => _bloqueio.Dispose();
}
