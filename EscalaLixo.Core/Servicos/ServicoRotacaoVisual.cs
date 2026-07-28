using EscalaLixo.Modelos;

namespace EscalaLixo.Servicos;

public sealed class ServicoRotacaoVisual
{
    public const int VagasPorSemana = 10;
    public const int LimiteSemanasConsecutivas = 2;

    private readonly ServicoDuplas _duplas = new();

    public RotacaoPainel MontarPainel(
        IReadOnlyList<Colaborador> todos,
        HistoricoPares historico,
        EscalaSemana? escalaAtual,
        int semanasSimuladas = 4,
        int seed = 42)
    {
        semanasSimuladas = Math.Clamp(semanasSimuladas, 1, 8);

        var participantesAtuais = ExtrairParticipantes(escalaAtual);
        var resumo = MontarResumo(todos, participantesAtuais);
        var colaboradores = MontarColaboradores(todos, historico, escalaAtual, participantesAtuais);
        var proxima = AnalisarProximaSemana(todos, historico, participantesAtuais);
        var simulacao = SimularSemanas(todos, historico, escalaAtual, semanasSimuladas, seed);

        return new RotacaoPainel
        {
            Resumo = resumo,
            Colaboradores = colaboradores,
            ProximaSemana = proxima,
            Simulacao = simulacao,
        };
    }

    private static RotacaoResumo MontarResumo(
        IReadOnlyList<Colaborador> todos,
        HashSet<string> participantesAtuais)
    {
        var deFora = todos.Count(c => !participantesAtuais.Contains(c.Nome.Trim()));
        var repetidosNecessarios = Math.Max(0, VagasPorSemana - deFora);

        return new RotacaoResumo
        {
            TotalColaboradores = todos.Count,
            VagasPorSemana = VagasPorSemana,
            DeForaProxima = deFora,
            RepetidosNecessarios = repetidosNecessarios,
            LimiteSemanasConsecutivas = LimiteSemanasConsecutivas,
        };
    }

    private static List<RotacaoColaborador> MontarColaboradores(
        IReadOnlyList<Colaborador> todos,
        HistoricoPares historico,
        EscalaSemana? escalaAtual,
        HashSet<string> participantesAtuais)
    {
        var escalados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var fila = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var missao = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (escalaAtual is not null)
        {
            foreach (var dia in escalaAtual.Dias)
            foreach (var n in dia.Nomes)
                if (!string.IsNullOrWhiteSpace(n))
                    escalados.Add(n.Trim());

            foreach (var n in escalaAtual.FilaEspera)
                if (!string.IsNullOrWhiteSpace(n))
                    fila.Add(n.Trim());

            foreach (var n in escalaAtual.Bloqueados)
                if (!string.IsNullOrWhiteSpace(n))
                    missao.Add(n.Trim());
        }

        return todos
            .Select(c =>
            {
                var nome = c.Nome.Trim();
                var streak = historico.ObterSequenciaConsecutiva(nome);
                var status = escalados.Contains(nome)
                    ? "escalado"
                    : missao.Contains(nome)
                        ? "missao"
                        : fila.Contains(nome)
                            ? "fila"
                            : participantesAtuais.Count > 0
                                ? "fora"
                                : "sem_escala";

                return new RotacaoColaborador
                {
                    Id = c.ObterIdPublico(),
                    Nome = nome,
                    Cor = c.Cor,
                    SequenciaConsecutiva = streak,
                    StatusAtual = status,
                    PodeRepetirProxima = participantesAtuais.Contains(nome)
                        && streak < LimiteSemanasConsecutivas,
                    BloqueadoRepeticao = participantesAtuais.Contains(nome)
                        && streak >= LimiteSemanasConsecutivas,
                };
            })
            .OrderBy(c => c.Nome, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static RotacaoProximaSemana AnalisarProximaSemana(
        IReadOnlyList<Colaborador> todos,
        HistoricoPares historico,
        HashSet<string> participantesAnteriores)
    {
        var deFora = todos
            .Where(c => !participantesAnteriores.Contains(c.Nome.Trim()))
            .Select(c => c.Nome.Trim())
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var repetidosNecessarios = Math.Max(0, VagasPorSemana - deFora.Count);

        var candidatosRepetir = todos
            .Where(c => participantesAnteriores.Contains(c.Nome.Trim()))
            .Select(c => new
            {
                Nome = c.Nome.Trim(),
                Streak = historico.ObterSequenciaConsecutiva(c.Nome),
            })
            .OrderBy(x => x.Streak)
            .ThenBy(x => x.Nome, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var podemRepetir = candidatosRepetir
            .Where(x => x.Streak < LimiteSemanasConsecutivas)
            .Select(x => x.Nome)
            .ToList();

        var bloqueadosRepeticao = candidatosRepetir
            .Where(x => x.Streak >= LimiteSemanasConsecutivas)
            .Select(x => x.Nome)
            .ToList();

        return new RotacaoProximaSemana
        {
            NovosEntram = deFora.Take(VagasPorSemana).ToList(),
            NovosDeFora = deFora,
            RepetidosNecessarios = repetidosNecessarios,
            PodemRepetir = podemRepetir,
            BloqueadosRepeticao = bloqueadosRepeticao,
        };
    }

    private List<RotacaoSemanaSimulada> SimularSemanas(
        IReadOnlyList<Colaborador> todos,
        HistoricoPares historico,
        EscalaSemana? escalaAtual,
        int semanas,
        int seed)
    {
        var histSim = ClonarHistorico(historico);
        var rng = new Random(seed);
        var participantesAnteriores = ExtrairParticipantes(escalaAtual);
        var segundaBase = ObterSegundaBase(escalaAtual);
        var resultado = new List<RotacaoSemanaSimulada>();

        for (var i = 0; i < semanas; i++)
        {
            var segunda = segundaBase.AddDays(7 * (i + 1));
            var selecionados = _duplas.SelecionarParticipantesParaSemana(
                todos, histSim, participantesAnteriores, VagasPorSemana, rng);

            var nomesSelecionados = selecionados.Select(c => c.Nome.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var novos = selecionados
                .Where(c => !participantesAnteriores.Contains(c.Nome.Trim()))
                .Select(c => c.Nome.Trim())
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var repetidos = selecionados
                .Where(c => participantesAnteriores.Contains(c.Nome.Trim()))
                .Select(c => c.Nome.Trim())
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var semana = _duplas.GerarSemana(selecionados, histSim, segunda, 0, rng);
            _duplas.AplicarIncrementosHistorico(histSim, semana);
            histSim.AtualizarSequenciasConsecutivas(participantesAnteriores, semana);

            var deFora = todos.Count(c => !participantesAnteriores.Contains(c.Nome.Trim()));

            resultado.Add(new RotacaoSemanaSimulada
            {
                Indice = i + 1,
                InicioSemana = segunda.ToString("yyyy-MM-dd"),
                DeFora = deFora,
                RepetidosNecessarios = Math.Max(0, VagasPorSemana - deFora),
                Novos = novos,
                Repetidos = repetidos,
                Escalados = nomesSelecionados.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList(),
                SequenciasApos = selecionados.ToDictionary(
                    c => c.Nome.Trim(),
                    c => histSim.ObterSequenciaConsecutiva(c.Nome),
                    StringComparer.OrdinalIgnoreCase),
            });

            participantesAnteriores = ExtrairParticipantes(semana);
        }

        return resultado;
    }

    private static HashSet<string> ExtrairParticipantes(EscalaSemana? escala)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (escala is null)
            return set;

        foreach (var dia in escala.Dias)
        foreach (var n in dia.Nomes)
            if (!string.IsNullOrWhiteSpace(n))
                set.Add(n.Trim());

        foreach (var n in escala.FilaEspera)
            if (!string.IsNullOrWhiteSpace(n))
                set.Add(n.Trim());

        foreach (var n in escala.Bloqueados)
            if (!string.IsNullOrWhiteSpace(n))
                set.Add(n.Trim());

        return set;
    }

    private static DateTime ObterSegundaBase(EscalaSemana? escalaAtual)
    {
        if (escalaAtual is not null
            && DateOnly.TryParse(escalaAtual.InicioDaSemana, out var inicio))
        {
            return inicio.ToDateTime(TimeOnly.MinValue);
        }

        var hoje = DateTime.Today;
        var diff = (7 + (hoje.DayOfWeek - DayOfWeek.Monday)) % 7;
        return hoje.AddDays(-diff);
    }

    private static HistoricoPares ClonarHistorico(HistoricoPares origem) => new()
    {
        ContagensDosPares = origem.ContagensDosPares.ToDictionary(
            static kv => kv.Key,
            static kv => kv.Value,
            StringComparer.Ordinal),
        SequenciasConsecutivas = origem.SequenciasConsecutivas.ToDictionary(
            static kv => kv.Key,
            static kv => kv.Value,
            StringComparer.OrdinalIgnoreCase),
    };
}

public sealed class RotacaoPainel
{
    public RotacaoResumo Resumo { get; set; } = new();
    public List<RotacaoColaborador> Colaboradores { get; set; } = [];
    public RotacaoProximaSemana ProximaSemana { get; set; } = new();
    public List<RotacaoSemanaSimulada> Simulacao { get; set; } = [];
}

public sealed class RotacaoResumo
{
    public int TotalColaboradores { get; set; }
    public int VagasPorSemana { get; set; }
    public int DeForaProxima { get; set; }
    public int RepetidosNecessarios { get; set; }
    public int LimiteSemanasConsecutivas { get; set; }
}

public sealed class RotacaoColaborador
{
    public string Id { get; set; } = "";
    public string Nome { get; set; } = "";
    public string Cor { get; set; } = "";
    public int SequenciaConsecutiva { get; set; }
    public string StatusAtual { get; set; } = "";
    public bool PodeRepetirProxima { get; set; }
    public bool BloqueadoRepeticao { get; set; }
}

public sealed class RotacaoProximaSemana
{
    public List<string> NovosEntram { get; set; } = [];
    public List<string> NovosDeFora { get; set; } = [];
    public int RepetidosNecessarios { get; set; }
    public List<string> PodemRepetir { get; set; } = [];
    public List<string> BloqueadosRepeticao { get; set; } = [];
}

public sealed class RotacaoSemanaSimulada
{
    public int Indice { get; set; }
    public string InicioSemana { get; set; } = "";
    public int DeFora { get; set; }
    public int RepetidosNecessarios { get; set; }
    public List<string> Novos { get; set; } = [];
    public List<string> Repetidos { get; set; } = [];
    public List<string> Escalados { get; set; } = [];
    public Dictionary<string, int> SequenciasApos { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
