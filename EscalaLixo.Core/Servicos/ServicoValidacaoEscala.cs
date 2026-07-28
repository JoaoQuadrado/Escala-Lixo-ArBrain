using EscalaLixo.Modelos;

namespace EscalaLixo.Servicos;

public sealed class ResultadoValidacaoEscala
{
    public bool Valido => Erros.Count == 0;
    public List<string> Erros { get; init; } = [];
    public List<string> Avisos { get; init; } = [];

    public static ResultadoValidacaoEscala Ok(IEnumerable<string>? avisos = null) => new()
    {
        Avisos = avisos?.ToList() ?? []
    };

    public static ResultadoValidacaoEscala Falha(params string[] erros) => new()
    {
        Erros = erros.ToList()
    };
}

/// <summary>
/// Validação rigorosa da escala semanal — impede duplicatas e estados inconsistentes.
/// </summary>
public sealed class ServicoValidacaoEscala
{
    public const int MaximoPorDiaUtil = 2;

    private static readonly HashSet<string> DiasUteis = new(StringComparer.OrdinalIgnoreCase)
    {
        "segunda-feira", "terça-feira", "quarta-feira", "quinta-feira", "sexta-feira"
    };

    private static readonly HashSet<string> FimDeSemana = new(StringComparer.OrdinalIgnoreCase)
    {
        "sábado", "sabado", "domingo", "saturday", "sunday"
    };

    public const string ChaveFilaEspera = "fila_espera";
    public const string ChaveBloqueados = "bloqueados";

    /// <summary>
    /// Valida a escala completa antes de gravar.
    /// </summary>
    public ResultadoValidacaoEscala Validar(
        EscalaSemana escala,
        IReadOnlyList<Colaborador> colaboradores)
    {
        var erros = new List<string>();
        var avisos = new List<string>();

        if (string.IsNullOrWhiteSpace(escala.InicioDaSemana))
            erros.Add("A data de início da semana (inicio_semana) é obrigatória.");

        var nomesCadastrados = new HashSet<string>(
            colaboradores.Select(c => c.Nome.Trim()),
            StringComparer.OrdinalIgnoreCase);

        // Mapa: nome -> dias em que aparece (detecção de duplicata semanal)
        var presencaSemanal = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var dia in escala.Dias)
        {
            var diaLabel = string.IsNullOrWhiteSpace(dia.DiaDaSemana)
                ? "(dia sem nome)"
                : dia.DiaDaSemana.Trim();

            if (FimDeSemana.Contains(diaLabel))
                continue;

            // Duplicatas no mesmo dia
            var vistosNoDia = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var nomesValidosNoDia = new List<string>();

            foreach (var raw in dia.Nomes)
            {
                var nome = raw?.Trim() ?? "";
                if (string.IsNullOrEmpty(nome))
                {
                    erros.Add($"Dia \"{DiasSemanaPt.ParaExibicao(diaLabel)}\": entrada vazia na lista de nomes.");
                    continue;
                }

                if (!vistosNoDia.Add(nome))
                {
                    erros.Add(
                        $"Duplicata no mesmo dia: \"{nome}\" aparece mais de uma vez em {DiasSemanaPt.ParaExibicao(diaLabel)}.");
                    continue;
                }

                if (!nomesCadastrados.Contains(nome))
                {
                    erros.Add(
                        $"Colaborador desconhecido: \"{nome}\" em {DiasSemanaPt.ParaExibicao(diaLabel)} não está cadastrado.");
                    continue;
                }

                nomesValidosNoDia.Add(nome);

                if (!presencaSemanal.TryGetValue(nome, out var dias))
                {
                    dias = [];
                    presencaSemanal[nome] = dias;
                }

                dias.Add(diaLabel);
            }

            var ehDiaUtil = DiasUteis.Contains(diaLabel) ||
                            DiasSemanaPt.CorrespondeAoDia(diaLabel, DayOfWeek.Monday) ||
                            DiasSemanaPt.CorrespondeAoDia(diaLabel, DayOfWeek.Tuesday) ||
                            DiasSemanaPt.CorrespondeAoDia(diaLabel, DayOfWeek.Wednesday) ||
                            DiasSemanaPt.CorrespondeAoDia(diaLabel, DayOfWeek.Thursday) ||
                            DiasSemanaPt.CorrespondeAoDia(diaLabel, DayOfWeek.Friday);

            if (ehDiaUtil && nomesValidosNoDia.Count > MaximoPorDiaUtil)
            {
                erros.Add(
                    $"Dia \"{DiasSemanaPt.ParaExibicao(diaLabel)}\": máximo de {MaximoPorDiaUtil} pessoas (dupla). " +
                    $"Encontradas {nomesValidosNoDia.Count}: {string.Join(", ", nomesValidosNoDia)}.");
            }
            else if (ehDiaUtil && nomesValidosNoDia.Count == 1)
            {
                avisos.Add(
                    $"{DiasSemanaPt.ParaExibicao(diaLabel)}: falta 1 pessoa para completar a dupla.");
            }
            else if (ehDiaUtil && nomesValidosNoDia.Count == 0)
            {
                avisos.Add(
                    $"{DiasSemanaPt.ParaExibicao(diaLabel)}: ninguém escalado ainda.");
            }
        }

        // Fila de espera
        var vistosFila = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in escala.FilaEspera)
        {
            var nome = raw?.Trim() ?? "";
            if (string.IsNullOrEmpty(nome))
            {
                erros.Add("Fila de espera: entrada vazia na lista de nomes.");
                continue;
            }

            if (!vistosFila.Add(nome))
            {
                erros.Add($"Duplicata na fila de espera: \"{nome}\" aparece mais de uma vez.");
                continue;
            }

            if (!nomesCadastrados.Contains(nome))
            {
                erros.Add($"Colaborador desconhecido: \"{nome}\" na fila de espera não está cadastrado.");
                continue;
            }

            if (!presencaSemanal.TryGetValue(nome, out var dias))
            {
                dias = [];
                presencaSemanal[nome] = dias;
            }

            dias.Add("fila de espera");
        }

        // Bloqueados
        var vistosBloqueados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in escala.Bloqueados)
        {
            var nome = raw?.Trim() ?? "";
            if (string.IsNullOrEmpty(nome))
            {
                erros.Add("Lista de bloqueados: entrada vazia.");
                continue;
            }

            if (!vistosBloqueados.Add(nome))
            {
                erros.Add($"Duplicata na lista de bloqueados: \"{nome}\" aparece mais de uma vez.");
                continue;
            }

            if (!nomesCadastrados.Contains(nome))
            {
                erros.Add($"Colaborador desconhecido: \"{nome}\" na lista de bloqueados não está cadastrado.");
                continue;
            }

            if (!presencaSemanal.TryGetValue(nome, out var dias))
            {
                dias = [];
                presencaSemanal[nome] = dias;
            }

            dias.Add("bloqueados");
        }

        // Duplicata na mesma semana (pessoa em mais de um lugar)
        foreach (var (nome, dias) in presencaSemanal)
        {
            var emDiasUteis = dias.Count(d =>
                !string.Equals(d, "fila de espera", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(d, "bloqueados", StringComparison.OrdinalIgnoreCase));

            if (emDiasUteis > 1)
            {
                erros.Add(
                    $"Duplicata na semana: \"{nome}\" está escalado em {emDiasUteis} dias " +
                    $"({string.Join(", ", dias.Where(d => !string.Equals(d, "fila de espera", StringComparison.OrdinalIgnoreCase) && !string.Equals(d, "bloqueados", StringComparison.OrdinalIgnoreCase)).Select(DiasSemanaPt.ParaExibicao))}). " +
                    "Cada pessoa só pode aparecer uma vez por semana.");
            }

            if (emDiasUteis >= 1 && dias.Any(d =>
                    string.Equals(d, "fila de espera", StringComparison.OrdinalIgnoreCase)))
            {
                erros.Add(
                    $"\"{nome}\" não pode estar na fila de espera e escalado num dia útil ao mesmo tempo.");
            }

            if (emDiasUteis >= 1 && dias.Any(d =>
                    string.Equals(d, "bloqueados", StringComparison.OrdinalIgnoreCase)))
            {
                erros.Add(
                    $"\"{nome}\" não pode estar bloqueado e escalado num dia útil ao mesmo tempo.");
            }

            if (dias.Any(d => string.Equals(d, "fila de espera", StringComparison.OrdinalIgnoreCase)) &&
                dias.Any(d => string.Equals(d, "bloqueados", StringComparison.OrdinalIgnoreCase)))
            {
                erros.Add($"\"{nome}\" não pode estar na fila de espera e bloqueado ao mesmo tempo.");
            }
        }

        return new ResultadoValidacaoEscala { Erros = erros, Avisos = avisos };
    }

    /// <summary>
    /// Valida um movimento antes de aplicar (drag-and-drop).
    /// </summary>
    public ResultadoValidacaoEscala ValidarMovimento(
        EscalaSemana escala,
        IReadOnlyList<Colaborador> colaboradores,
        string nomeColaborador,
        string diaOrigemPt,
        string diaDestinoPt)
    {
        var nome = nomeColaborador.Trim();
        if (string.IsNullOrEmpty(nome))
            return ResultadoValidacaoEscala.Falha("Nome do colaborador é obrigatório.");

        var cadastrado = colaboradores.Any(c =>
            string.Equals(c.Nome.Trim(), nome, StringComparison.OrdinalIgnoreCase));
        if (!cadastrado)
            return ResultadoValidacaoEscala.Falha($"Colaborador \"{nome}\" não está cadastrado.");

        if (string.Equals(diaOrigemPt, diaDestinoPt, StringComparison.OrdinalIgnoreCase))
            return ResultadoValidacaoEscala.Ok();

        var origemFila = EhFilaEspera(diaOrigemPt);
        var origemBloqueado = EhBloqueado(diaOrigemPt);
        var destinoFila = EhFilaEspera(diaDestinoPt);
        var destinoBloqueado = EhBloqueado(diaDestinoPt);

        // Verificar se já está em outro dia (além da origem)
        foreach (var dia in escala.Dias)
        {
            if (!origemFila && !origemBloqueado &&
                string.Equals(dia.DiaDaSemana, diaOrigemPt, StringComparison.OrdinalIgnoreCase))
                continue;

            if (dia.Nomes.Any(n => string.Equals(n.Trim(), nome, StringComparison.OrdinalIgnoreCase)))
            {
                return ResultadoValidacaoEscala.Falha(
                    $"Não é possível mover \"{nome}\" para {RotuloDestino(diaDestinoPt)}: " +
                    $"já está escalado em {DiasSemanaPt.ParaExibicao(dia.DiaDaSemana)} nesta semana.");
            }
        }

        if (!origemBloqueado && escala.Bloqueados.Any(n =>
                string.Equals(n.Trim(), nome, StringComparison.OrdinalIgnoreCase)))
        {
            if (!destinoBloqueado)
            {
                return ResultadoValidacaoEscala.Falha(
                    $"Não é possível mover \"{nome}\": está na lista de bloqueados.");
            }
        }

        if (origemFila && destinoBloqueado)
            return ResultadoValidacaoEscala.Ok();

        if (origemBloqueado && destinoFila)
            return ResultadoValidacaoEscala.Ok();

        if (origemFila && !destinoFila && !destinoBloqueado)
        {
            foreach (var dia in escala.Dias)
            {
                if (dia.Nomes.Any(n => string.Equals(n.Trim(), nome, StringComparison.OrdinalIgnoreCase)))
                {
                    return ResultadoValidacaoEscala.Falha(
                        $"Não é possível mover \"{nome}\" da fila: já está em {DiasSemanaPt.ParaExibicao(dia.DiaDaSemana)}.");
                }
            }
        }

        if (destinoFila || destinoBloqueado)
            return ResultadoValidacaoEscala.Ok();

        var diaDestino = escala.Dias.FirstOrDefault(d =>
            string.Equals(d.DiaDaSemana, diaDestinoPt, StringComparison.OrdinalIgnoreCase));

        if (diaDestino is not null)
        {
            var countDestino = diaDestino.Nomes.Count(n => !string.IsNullOrWhiteSpace(n));
            var jaNoDestino = diaDestino.Nomes.Any(n =>
                string.Equals(n.Trim(), nome, StringComparison.OrdinalIgnoreCase));

            if (!jaNoDestino && DiasUteis.Contains(diaDestinoPt) && countDestino >= MaximoPorDiaUtil)
            {
                return ResultadoValidacaoEscala.Falha(
                    $"Dia \"{DiasSemanaPt.ParaExibicao(diaDestinoPt)}\" já tem {MaximoPorDiaUtil} pessoas (dupla completa).");
            }
        }

        return ResultadoValidacaoEscala.Ok();
    }

    /// <summary>Valida troca de posição entre dois colaboradores (drag sobre outro card).</summary>
    public ResultadoValidacaoEscala ValidarTroca(
        EscalaSemana escala,
        IReadOnlyList<Colaborador> colaboradores,
        string nomeA,
        string diaAPt,
        string nomeB,
        string diaBPt)
    {
        var a = nomeA.Trim();
        var b = nomeB.Trim();
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return ResultadoValidacaoEscala.Falha("Colaboradores inválidos para troca.");
        if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
            return ResultadoValidacaoEscala.Falha("Selecione dois colaboradores diferentes.");

        foreach (var nome in new[] { a, b })
        {
            if (!colaboradores.Any(c => string.Equals(c.Nome.Trim(), nome, StringComparison.OrdinalIgnoreCase)))
                return ResultadoValidacaoEscala.Falha($"Colaborador \"{nome}\" não está cadastrado.");
        }

        if (!ColaboradorEstaNoSlot(escala, a, diaAPt))
            return ResultadoValidacaoEscala.Falha($"\"{a}\" não está em {RotuloDestino(diaAPt)}.");
        if (!ColaboradorEstaNoSlot(escala, b, diaBPt))
            return ResultadoValidacaoEscala.Falha($"\"{b}\" não está em {RotuloDestino(diaBPt)}.");

        var aBloqueado = escala.Bloqueados.Any(n => string.Equals(n.Trim(), a, StringComparison.OrdinalIgnoreCase));
        var bBloqueado = escala.Bloqueados.Any(n => string.Equals(n.Trim(), b, StringComparison.OrdinalIgnoreCase));

        if (aBloqueado && !EhBloqueado(diaBPt))
            return ResultadoValidacaoEscala.Falha($"\"{a}\" está bloqueado e só pode trocar com alguém na lista de bloqueados.");
        if (bBloqueado && !EhBloqueado(diaAPt))
            return ResultadoValidacaoEscala.Falha($"\"{b}\" está bloqueado e só pode trocar com alguém na lista de bloqueados.");

        return ResultadoValidacaoEscala.Ok();
    }

    private static bool ColaboradorEstaNoSlot(EscalaSemana escala, string nome, string diaPt)
    {
        if (EhFilaEspera(diaPt))
            return escala.FilaEspera.Any(n => string.Equals(n.Trim(), nome, StringComparison.OrdinalIgnoreCase));
        if (EhBloqueado(diaPt))
            return escala.Bloqueados.Any(n => string.Equals(n.Trim(), nome, StringComparison.OrdinalIgnoreCase));

        var dia = escala.Dias.FirstOrDefault(d =>
            string.Equals(d.DiaDaSemana, diaPt, StringComparison.OrdinalIgnoreCase));
        return dia?.Nomes.Any(n => string.Equals(n.Trim(), nome, StringComparison.OrdinalIgnoreCase)) == true;
    }

    private static bool EhFilaEspera(string dia) =>
        string.Equals(dia.Trim(), ChaveFilaEspera, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(dia.Trim(), "waiting", StringComparison.OrdinalIgnoreCase);

    private static bool EhBloqueado(string dia) =>
        string.Equals(dia.Trim(), ChaveBloqueados, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(dia.Trim(), "blocked", StringComparison.OrdinalIgnoreCase);

    public static bool EhFilaEsperaPublico(string dia) => EhFilaEspera(dia);

    public static bool EhBloqueadoPublico(string dia) => EhBloqueado(dia);

    private static string RotuloDestino(string diaDestinoPt)
    {
        if (EhFilaEspera(diaDestinoPt)) return "fila de espera";
        if (EhBloqueado(diaDestinoPt)) return "lista de bloqueados";
        return DiasSemanaPt.ParaExibicao(diaDestinoPt);
    }

    /// <summary>
    /// Remove duplicatas mantendo a primeira ocorrência e normaliza nomes.
    /// </summary>
    public EscalaSemana Sanitizar(EscalaSemana escala)
    {
        var vistosSemana = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var filaExtra = new List<string>();

        foreach (var dia in escala.Dias.ToList())
        {
            var diaLabel = dia.DiaDaSemana?.Trim() ?? "";
            if (FimDeSemana.Contains(diaLabel))
            {
                filaExtra.AddRange(dia.Nomes.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()));
                escala.Dias.Remove(dia);
                continue;
            }

            var limpos = new List<string>();
            var vistosDia = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var raw in dia.Nomes)
            {
                var nome = raw?.Trim() ?? "";
                if (string.IsNullOrEmpty(nome))
                    continue;
                if (!vistosDia.Add(nome))
                    continue;
                if (!vistosSemana.Add(nome))
                    continue;
                limpos.Add(nome);
            }

            dia.Nomes = limpos;
        }

        var filaLimpa = new List<string>();
        var vistosFila = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in escala.FilaEspera.Concat(filaExtra))
        {
            var nome = raw?.Trim() ?? "";
            if (string.IsNullOrEmpty(nome))
                continue;
            if (vistosSemana.Contains(nome))
                continue;
            if (!vistosFila.Add(nome))
                continue;
            filaLimpa.Add(nome);
        }

        escala.FilaEspera = filaLimpa;

        var bloqueadosLimpos = new List<string>();
        var vistosBloqueados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in escala.Bloqueados)
        {
            var nome = raw?.Trim() ?? "";
            if (string.IsNullOrEmpty(nome))
                continue;
            if (vistosSemana.Contains(nome))
                continue;
            if (!vistosBloqueados.Add(nome))
                continue;
            bloqueadosLimpos.Add(nome);
        }

        escala.Bloqueados = bloqueadosLimpos;
        return escala;
    }
}
