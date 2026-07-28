using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EscalaLixo.Servicos;

/// <summary>
/// Com token de bot + ID do servidor, lista membros e resolve apelido → ID para &lt;@id&gt;.
/// O webhook sozinho não converte texto em menção.
/// </summary>
public sealed class ServicoResolucaoUsuarioDiscord : IDisposable
{
    private readonly HttpClient _http = new();
    private readonly string _token;
    private readonly ulong _guildId;
    private readonly Action<string> _registrar;

    private Dictionary<string, string>? _apelidoParaId;
    private DateTime _cacheAte = DateTime.MinValue;
    private static readonly TimeSpan DuracaoCache = TimeSpan.FromHours(6);

    private static readonly JsonSerializerOptions JsonDiscord = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ServicoResolucaoUsuarioDiscord(string botToken, string guildId, Action<string> registrar)
    {
        _token = botToken.Trim();
        _registrar = registrar;
        if (!ulong.TryParse(guildId.Trim(), out var gid))
            throw new ArgumentException("IdServidorDiscord (appsettings) deve ser numérico.", nameof(guildId));
        _guildId = gid;

        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", _token);
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("DiscordBot (https://github.com/EscalaLixo, 1.0)");
    }

    /// <summary>
    /// Se for só dígitos (snowflake), devolve como está; senão procura apelido/nome no cache do servidor.
    /// </summary>
    public async Task<string?> ResolverIdAsync(string entrada, CancellationToken ct = default)
    {
        entrada = entrada.Trim();
        if (entrada.StartsWith('@'))
            entrada = entrada[1..].Trim();

        if (ServicoDiscordHelpers.IdDiscordValido(entrada))
            return entrada.Trim();

        await GarantirCacheAsync(ct).ConfigureAwait(false);
        if (_apelidoParaId is null)
            return null;

        var chave = entrada.ToLowerInvariant();
        return _apelidoParaId.TryGetValue(chave, out var id) ? id : null;
    }

    private async Task GarantirCacheAsync(CancellationToken ct)
    {
        if (_apelidoParaId is not null && DateTime.UtcNow < _cacheAte)
            return;

        _apelidoParaId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            string? after = null;
            while (true)
            {
                var url = $"https://discord.com/api/v10/guilds/{_guildId}/members?limit=1000";
                if (!string.IsNullOrEmpty(after))
                    url += $"&after={after}";

                using var resp = await _http.GetAsync(url, ct).ConfigureAwait(false);
                var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

                if (!resp.IsSuccessStatusCode)
                {
                    _registrar($"Discord (membros): {(int)resp.StatusCode} — {body}");
                    _cacheAte = DateTime.UtcNow.AddMinutes(15);
                    return;
                }

                var lista = JsonSerializer.Deserialize<List<MembroDiscordDto>>(body, JsonDiscord);
                if (lista is null || lista.Count == 0)
                    break;

                foreach (var m in lista)
                {
                    if (m.User?.Id is null)
                        continue;
                    var id = m.User.Id;
                    void Add(string? texto)
                    {
                        if (string.IsNullOrWhiteSpace(texto))
                            return;
                        var k = texto.Trim().ToLowerInvariant();
                        _apelidoParaId[k] = id;
                    }

                    Add(m.User.Username);
                    Add(m.User.GlobalName);
                    Add(m.Nick);
                }

                if (lista.Count < 1000)
                    break;

                after = lista[^1].User?.Id;
                if (after is null)
                    break;
            }

            _cacheAte = DateTime.UtcNow + DuracaoCache;
            _registrar($"Discord: cache de membros atualizado ({_apelidoParaId.Count} chaves).");
        }
        catch (Exception ex)
        {
            _registrar($"Discord (membros): {ex.Message}");
            _cacheAte = DateTime.UtcNow.AddMinutes(15);
        }
    }

    public void InvalidarCache() => _cacheAte = DateTime.MinValue;

    public void Dispose() => _http.Dispose();

    private sealed class MembroDiscordDto
    {
        public UsuarioDiscordDto? User { get; set; }
        public string? Nick { get; set; }
    }

    private sealed class UsuarioDiscordDto
    {
        public string? Id { get; set; }
        public string? Username { get; set; }

        [JsonPropertyName("global_name")]
        public string? GlobalName { get; set; }
    }
}
