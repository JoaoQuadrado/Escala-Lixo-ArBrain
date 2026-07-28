using EscalaLixo.Api.Dtos;
using EscalaLixo.Modelos;

namespace EscalaLixo.Api.Infrastructure;

internal static class ConfigMapper
{
    public static ApiConfigDto ParaDto(ConfiguracaoApp cfg, string fonte)
    {
        return new ApiConfigDto
        {
            WebhookDiscord = "",
            WebhookConfigured = !string.IsNullOrWhiteSpace(cfg.WebhookDiscord),
            TokenBotDiscord = "",
            TokenBotConfigured = !string.IsNullOrWhiteSpace(cfg.TokenBotDiscord),
            IdServidorDiscord = cfg.IdServidorDiscord,
            UrlGifPreviaSemanal = "",
            UrlGifDiario = "",
            GifPreviaConfigured = cfg.GifPreviaId is not null
                || cfg.TemGifPreviaArquivo
                || !string.IsNullOrWhiteSpace(cfg.UrlGifPreviaSemanal),
            GifDiarioConfigured = cfg.GifDiarioId is not null
                || cfg.GifPreviaId is not null
                || cfg.TemGifDiarioArquivo
                || cfg.TemGifPreviaArquivo
                || !string.IsNullOrWhiteSpace(cfg.UrlGifDiario)
                || !string.IsNullOrWhiteSpace(cfg.UrlGifPreviaSemanal),
            GifPreviaId = cfg.GifPreviaId?.ToString(),
            GifDiarioId = cfg.GifDiarioId?.ToString(),
            ModeloMensagemDiaria = cfg.ModeloMensagemDiaria,
            IntervaloVerificacaoMinutos = cfg.IntervaloVerificacaoMinutos,
            HoraNotificacaoPadrao = cfg.HoraNotificacaoPadrao,
            HoraPreviaSemanal = cfg.HoraPreviaSemanal,
            HoraLembreteDiario = cfg.HoraLembreteDiario,
            PastaDados = "",
            ColaboradoresFonte = "postgres",
            PostgresConfigured = true,
            CaminhoConfiguracao = fonte,
        };
    }
}
