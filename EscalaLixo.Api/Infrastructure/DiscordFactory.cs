using EscalaLixo.Modelos;
using EscalaLixo.Servicos;

namespace EscalaLixo.Api.Infrastructure;

internal static class DiscordFactory
{
    public static ServicoDiscord? Criar(ConfiguracaoApp cfg)
    {
        if (string.IsNullOrWhiteSpace(cfg.WebhookDiscord))
            return null;

        ServicoResolucaoUsuarioDiscord? resolucao = null;
        if (!string.IsNullOrWhiteSpace(cfg.TokenBotDiscord) && !string.IsNullOrWhiteSpace(cfg.IdServidorDiscord))
        {
            try
            {
                resolucao = new ServicoResolucaoUsuarioDiscord(cfg.TokenBotDiscord, cfg.IdServidorDiscord, _ => { });
            }
            catch
            {
                // menções por apelido indisponíveis
            }
        }

        return new ServicoDiscord(
            cfg.WebhookDiscord,
            resolucao,
            GifConfigHelper.ResolverPrevia(cfg),
            GifConfigHelper.ResolverDiarioProprio(cfg),
            GifConfigHelper.ResolverUrlPrevia(cfg),
            GifConfigHelper.ResolverUrlDiario(cfg),
            cfg.ModeloMensagemDiaria);
    }
}
