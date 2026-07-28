using System.Text.Json.Serialization;

namespace EscalaLixo.Modelos;

public sealed class Colaborador
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("nome")]
    public string Nome { get; set; } = "";

    [JsonPropertyName("usuario_discord")]
    public string UsuarioDiscord { get; set; } = "";

    [JsonPropertyName("cargo")]
    public string Cargo { get; set; } = "Auxiliar";

    [JsonPropertyName("cor")]
    public string Cor { get; set; } = "#FFC300";

    [JsonPropertyName("foto_url")]
    public string? FotoUrl { get; set; }

    [JsonPropertyName("de_ferias")]
    public bool DeFerias { get; set; }

    [JsonPropertyName("ausente")]
    public bool Ausente { get; set; }

    [JsonPropertyName("observacoes")]
    public string? Observacoes { get; set; }

    public string ObterIdPublico() =>
        Id != Guid.Empty ? Id.ToString() : ColaboradorIds.DerivarDeNome(Nome);

    public string MarcacaoParaResolver() => SemArroba(UsuarioDiscord);

    private static string SemArroba(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return "";
        s = s.Trim();
        if (s.Length > 0 && s[0] == '@')
            return s[1..].Trim();
        return s;
    }
}

public static class ColaboradorIds
{
    public static string DerivarDeNome(string nome)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(nome.Trim().ToLowerInvariant()));
        return Convert.ToHexString(hash)[..12].ToLowerInvariant();
    }
}
