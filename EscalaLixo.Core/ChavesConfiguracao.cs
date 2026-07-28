namespace EscalaLixo;

/// <summary>Nomes das chaves em appsettings.json (português).</summary>
public static class ChavesConfiguracao
{
    public const string WebhookDiscord = "WebhookDiscord";
    public const string TokenBotDiscord = "TokenBotDiscord";
    public const string IdServidorDiscord = "IdServidorDiscord";
    public const string UrlGifPreviaSemanal = "UrlGifPreviaSemanal";
    public const string UrlGifDiario = "UrlGifDiario";
    public const string ModeloMensagemDiaria = "ModeloMensagemDiaria";
    public const string IntervaloVerificacaoMinutos = "IntervaloVerificacaoMinutos";
    public const string HoraNotificacaoPadrao = "HoraNotificacaoPadrao";
    public const string HoraPreviaSemanal = "HoraPreviaSemanal";
    public const string HoraLembreteDiario = "HoraLembreteDiario";

    /// <summary>Chaves antigas (inglês) — ainda lidas se a chave nova não existir.</summary>
    public static class Legado
    {
        public const string DiscordWebhookUrl = "DiscordWebhookUrl";
        public const string DiscordBotToken = "DiscordBotToken";
        public const string DiscordGuildId = "DiscordGuildId";
        public const string DiscordGifUrl = "DiscordGifUrl";
        public const string DiscordDailyGifUrl = "DiscordDailyGifUrl";
        public const string DiscordDailyMessage = "DiscordDailyMessage";
        public const string CheckIntervalMinutes = "CheckIntervalMinutes";
        public const string NotifyHour = "NotifyHour";
        public const string NotifyHourWeeklyPreview = "NotifyHourWeeklyPreview";
        public const string NotifyHourDaily = "NotifyHourDaily";
    }
}
