using EscalaLixo.Modelos;

namespace EscalaLixo.Servicos;

/// <summary>
/// Nomes de dias úteis em pt-BR (formato usado no JSON da escala).
/// </summary>
public static class DiasSemanaPt
{
    public static readonly string[] OrdemSemana =
    {
        "segunda-feira",
        "terça-feira",
        "quarta-feira",
        "quinta-feira",
        "sexta-feira"
    };

    /// <summary>Índice 0–4 do primeiro dia útil a preencher (hoje, se for dia útil).</summary>
    public static int IndicePrimeiroDiaUtilRestante(DateTime hoje) => hoje.DayOfWeek switch
    {
        DayOfWeek.Monday => 0,
        DayOfWeek.Tuesday => 1,
        DayOfWeek.Wednesday => 2,
        DayOfWeek.Thursday => 3,
        DayOfWeek.Friday => 4,
        _ => 0,
    };

    public static bool EhFimDeSemana(DateTime data) =>
        data.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    public static string DoDayOfWeek(DayOfWeek d) => d switch
    {
        DayOfWeek.Monday => "segunda-feira",
        DayOfWeek.Tuesday => "terça-feira",
        DayOfWeek.Wednesday => "quarta-feira",
        DayOfWeek.Thursday => "quinta-feira",
        DayOfWeek.Friday => "sexta-feira",
        _ => ""
    };

    /// <summary>
    /// Corresponde o valor guardado no JSON ao dia atual (aceita legado em inglês: Monday, …).
    /// </summary>
    public static bool CorrespondeAoDia(string? valorNoJson, DayOfWeek diaAtual)
    {
        if (string.IsNullOrWhiteSpace(valorNoJson))
            return false;

        var esperado = DoDayOfWeek(diaAtual);
        if (string.Equals(valorNoJson.Trim(), esperado, StringComparison.OrdinalIgnoreCase))
            return true;

        return LegadoInglesParaDayOfWeek(valorNoJson.Trim()) == diaAtual;
    }

    private static DayOfWeek? LegadoInglesParaDayOfWeek(string s) => s switch
    {
        "Monday" => DayOfWeek.Monday,
        "Tuesday" => DayOfWeek.Tuesday,
        "Wednesday" => DayOfWeek.Wednesday,
        "Thursday" => DayOfWeek.Thursday,
        "Friday" => DayOfWeek.Friday,
        _ => null
    };

    /// <summary>
    /// Substitui Monday, Tuesday, … por segunda-feira, terça-feira, … no modelo em memória.
    /// </summary>
    public static bool MigrarDiasSemanaSeIngles(EscalaSemana semana)
    {
        var alterou = false;
        foreach (var d in semana.Dias)
        {
            if (LegadoInglesParaDayOfWeek(d.DiaDaSemana) is not { } dow)
                continue;

            var pt = DoDayOfWeek(dow);
            if (!string.Equals(d.DiaDaSemana, pt, StringComparison.Ordinal))
            {
                d.DiaDaSemana = pt;
                alterou = true;
            }
        }

        return alterou;
    }

    public static string Emoji(string diaSemana)
    {
        var n = diaSemana.Trim();
        if (string.Equals(n, "Monday", StringComparison.OrdinalIgnoreCase)) n = "segunda-feira";
        if (string.Equals(n, "Tuesday", StringComparison.OrdinalIgnoreCase)) n = "terça-feira";
        if (string.Equals(n, "Wednesday", StringComparison.OrdinalIgnoreCase)) n = "quarta-feira";
        if (string.Equals(n, "Thursday", StringComparison.OrdinalIgnoreCase)) n = "quinta-feira";
        if (string.Equals(n, "Friday", StringComparison.OrdinalIgnoreCase)) n = "sexta-feira";

        return n.ToLowerInvariant() switch
        {
            "segunda-feira" => "🟡",
            "terça-feira" => "🟠",
            "quarta-feira" => "🟢",
            "quinta-feira" => "🔵",
            "sexta-feira" => "🟣",
            _ => "⬜"
        };
    }

    /// <summary>Primeira letra maiúscula: "segunda-feira" → "Segunda-feira".</summary>
    public static string Capitalizar(string diaSemana)
    {
        if (string.IsNullOrEmpty(diaSemana))
            return diaSemana;
        var s = diaSemana.Trim();
        if (s.Length == 1)
            return s.ToUpperInvariant();
        return char.ToUpperInvariant(s[0]) + s[1..];
    }

    /// <summary>Nome para Discord: aceita JSON novo (pt) ou legado (Monday, …).</summary>
    public static string ParaExibicao(string? valorNoJson)
    {
        if (string.IsNullOrWhiteSpace(valorNoJson))
            return "";
        var t = valorNoJson.Trim();
        if (LegadoInglesParaDayOfWeek(t) is { } dow)
            return Capitalizar(DoDayOfWeek(dow));
        return Capitalizar(t);
    }
}
