using System.Linq;
using System.Text.Json.Serialization;

namespace EscalaLixo.Modelos;

public sealed class HistoricoPares
{
    [JsonPropertyName("contagens_pares")]
    public Dictionary<string, int> ContagensDosPares { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Semanas consecutivas em que a pessoa participou (até à última escala gravada). Usado para equilibrar repetições.</summary>
    [JsonPropertyName("sequencias_consecutivas")]
    public Dictionary<string, int> SequenciasConsecutivas { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static string CriarChave(string a, string b)
    {
        var x = string.CompareOrdinal(a, b) <= 0 ? a : b;
        var y = string.CompareOrdinal(a, b) <= 0 ? b : a;
        return $"{x}|{y}";
    }

    public int ObterContagem(string a, string b)
    {
        var key = CriarChave(a, b);
        return ContagensDosPares.TryGetValue(key, out var n) ? n : 0;
    }

    public void Incrementar(string a, string b)
    {
        var key = CriarChave(a, b);
        ContagensDosPares[key] = ContagensDosPares.TryGetValue(key, out var n) ? n + 1 : 1;
    }

    public int ObterSequenciaConsecutiva(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return 0;
        return SequenciasConsecutivas.TryGetValue(nome.Trim(), out var n) ? n : 0;
    }

    /// <summary>Atualiza sequências após gravar a nova escala (quem repetiu incrementa; quem ficou de fora perde a entrada).</summary>
    public void AtualizarSequenciasConsecutivas(
        IReadOnlySet<string> participantesSemanaAnterior,
        EscalaSemana semanaNova)
    {
        var participantesNovo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dia in semanaNova.Dias)
        {
            foreach (var n in dia.Nomes)
            {
                if (!string.IsNullOrWhiteSpace(n))
                    participantesNovo.Add(n.Trim());
            }
        }

        var streakAntes = SequenciasConsecutivas.ToDictionary(
            static kv => kv.Key,
            static kv => kv.Value,
            StringComparer.OrdinalIgnoreCase);

        foreach (var nome in participantesNovo)
        {
            if (participantesSemanaAnterior.Contains(nome))
                SequenciasConsecutivas[nome] = (streakAntes.TryGetValue(nome, out var s) ? s : 0) + 1;
            else
                SequenciasConsecutivas[nome] = 1;
        }

        foreach (var k in SequenciasConsecutivas.Keys.ToList())
        {
            if (!participantesNovo.Contains(k))
                SequenciasConsecutivas.Remove(k);
        }
    }
}
