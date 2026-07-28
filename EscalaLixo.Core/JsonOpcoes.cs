using System.Text.Encodings.Web;
using System.Text.Json;

namespace EscalaLixo;

public static class JsonOpcoes
{
    public static JsonSerializerOptions Ler { get; } = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Indentado, UTF-8 sem fugir acentos para \uXXXX.</summary>
    public static JsonSerializerOptions Gravar { get; } = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}
