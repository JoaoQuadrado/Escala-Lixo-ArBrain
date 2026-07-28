using System.Text.Json.Serialization;

namespace EscalaLixo.Modelos;

public sealed class AtribuicaoDia
{
    [JsonPropertyName("dia_semana")]
    public string DiaDaSemana { get; set; } = "";

    [JsonPropertyName("nomes")]
    public List<string> Nomes { get; set; } = new();
}

public sealed class EscalaSemana
{
    [JsonPropertyName("inicio_semana")]
    public string InicioDaSemana { get; set; } = "";

    [JsonPropertyName("dias")]
    public List<AtribuicaoDia> Dias { get; set; } = new();

    [JsonPropertyName("fila_espera")]
    public List<string> FilaEspera { get; set; } = new();

    [JsonPropertyName("bloqueados")]
    public List<string> Bloqueados { get; set; } = new();

    public bool TemConteudo() =>
        Dias.Any(d => d.Nomes.Any(n => !string.IsNullOrWhiteSpace(n)))
        || FilaEspera.Any(n => !string.IsNullOrWhiteSpace(n))
        || Bloqueados.Any(n => !string.IsNullOrWhiteSpace(n));
}
