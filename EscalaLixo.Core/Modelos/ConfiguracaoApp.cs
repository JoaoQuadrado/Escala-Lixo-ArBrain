using System.Text.Json.Serialization;

namespace EscalaLixo.Modelos;

/// <summary>
/// Configurações editáveis pelo utilizador (Discord, GIFs, agendamento).
/// Persistidas em public.configuracao_app (PostgreSQL).
/// </summary>
public sealed class ConfiguracaoApp
{
    [JsonPropertyName("WebhookDiscord")]
    public string WebhookDiscord { get; set; } = "";

    [JsonPropertyName("TokenBotDiscord")]
    public string TokenBotDiscord { get; set; } = "";

    [JsonPropertyName("IdServidorDiscord")]
    public string IdServidorDiscord { get; set; } = "";

    [JsonPropertyName("UrlGifPreviaSemanal")]
    public string UrlGifPreviaSemanal { get; set; } = "";

    [JsonPropertyName("UrlGifDiario")]
    public string UrlGifDiario { get; set; } = "";

    [JsonIgnore]
    public byte[]? GifPreviaSemanal { get; set; }

    [JsonIgnore]
    public string GifPreviaMime { get; set; } = "";

    [JsonIgnore]
    public byte[]? GifDiario { get; set; }

    [JsonIgnore]
    public string GifDiarioMime { get; set; } = "";

    public bool TemGifPreviaArquivo => GifPreviaSemanal is { Length: > 0 };

    public bool TemGifDiarioArquivo => GifDiario is { Length: > 0 };

    [JsonIgnore]
    public Guid? GifPreviaId { get; set; }

    [JsonIgnore]
    public Guid? GifDiarioId { get; set; }

    [JsonPropertyName("ModeloMensagemDiaria")]
    public string ModeloMensagemDiaria { get; set; } = "";

    [JsonPropertyName("IntervaloVerificacaoMinutos")]
    public int IntervaloVerificacaoMinutos { get; set; } = 60;

    [JsonPropertyName("HoraNotificacaoPadrao")]
    public int HoraNotificacaoPadrao { get; set; } = 8;

    [JsonPropertyName("HoraPreviaSemanal")]
    public int HoraPreviaSemanal { get; set; } = 8;

    [JsonPropertyName("HoraLembreteDiario")]
    public int HoraLembreteDiario { get; set; } = 17;
}
