using EscalaLixo.Api.Dtos;
using EscalaLixo.Api.Infrastructure;
using EscalaLixo.Modelos;
using EscalaLixo.Servicos;

namespace EscalaLixo.Api.Endpoints;

internal static class ConfigEndpoints
{
    public static void MapConfigEndpoints(this WebApplication app)
    {
        app.MapGet("/api/config", async (ServicoConfiguracaoApp configApp, CancellationToken ct) =>
        {
            var cfg = await configApp.LerAsync(ct);
            return Results.Ok(ConfigMapper.ParaDto(cfg, configApp.Fonte));
        });

        app.MapPut("/api/config", async (
            ApiConfigSaveDto body,
            ServicoConfiguracaoApp configApp,
            CancellationToken ct) =>
        {
            var entrada = new ConfiguracaoApp
            {
                WebhookDiscord = body.WebhookDiscord,
                TokenBotDiscord = body.TokenBotDiscord,
                IdServidorDiscord = body.IdServidorDiscord,
                UrlGifPreviaSemanal = body.UrlGifPreviaSemanal,
                UrlGifDiario = body.UrlGifDiario,
                ModeloMensagemDiaria = body.ModeloMensagemDiaria,
                IntervaloVerificacaoMinutos = body.IntervaloVerificacaoMinutos,
                HoraNotificacaoPadrao = body.HoraNotificacaoPadrao,
                HoraPreviaSemanal = body.HoraPreviaSemanal,
                HoraLembreteDiario = body.HoraLembreteDiario,
            };

            var salvo = await configApp.SalvarAsync(entrada, ct);
            return Results.Ok(ConfigMapper.ParaDto(salvo, configApp.Fonte));
        });

        app.MapGet("/api/config/gif/previa", async (
            ServicoConfiguracaoApp configApp,
            ServicoBibliotecaGifs biblioteca,
            CancellationToken ct) =>
        {
            var cfg = await configApp.LerAsync(ct);
            await biblioteca.HidratarGifsAsync(cfg, ct);
            if (!cfg.TemGifPreviaArquivo)
                return Results.NotFound();

            return Results.File(cfg.GifPreviaSemanal!, cfg.GifPreviaMime);
        });

        app.MapGet("/api/config/gif/dia", async (
            ServicoConfiguracaoApp configApp,
            ServicoBibliotecaGifs biblioteca,
            CancellationToken ct) =>
        {
            var cfg = await configApp.LerAsync(ct);
            await biblioteca.HidratarGifsAsync(cfg, ct);

            if (cfg.TemGifDiarioArquivo)
                return Results.File(cfg.GifDiario!, cfg.GifDiarioMime);

            if (cfg.TemGifPreviaArquivo)
                return Results.File(cfg.GifPreviaSemanal!, cfg.GifPreviaMime);

            return Results.NotFound();
        });
    }
}
