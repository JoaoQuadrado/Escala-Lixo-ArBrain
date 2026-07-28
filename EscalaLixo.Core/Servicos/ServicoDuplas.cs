using EscalaLixo.Modelos;

namespace EscalaLixo.Servicos;

public sealed class ServicoDuplas
{
    private const int VagasPorSemana = 10;

    /// <summary>
    /// Quem já tem esta sequência ou mais <strong>não</strong> entra como repetido, exceto se não houver gente suficiente
    /// (evita 3.ª semana seguida quando há alternativa).
    /// </summary>
    private const int SemanasConsecutivasLimiteParaEvitarSobrecarga = 2;

    /// <summary>
    /// Escolhe quem entra na escala da semana: prioriza quem <strong>não</strong> participou na semana anterior;
    /// se faltar gente, completa com quem já tinha escala, preferindo quem tem <strong>menor</strong> sequência consecutiva
    /// (quem já foi muitas semanas seguidas fica de fora primeiro).
    /// </summary>
    public IReadOnlyList<Colaborador> SelecionarParticipantesParaSemana(
        IReadOnlyList<Colaborador> todos,
        HistoricoPares historico,
        IReadOnlySet<string> participantesSemanaAnterior,
        int vagasPorSemana = VagasPorSemana,
        Random? rng = null)
    {
        rng ??= Random.Shared;
        if (todos.Count == 0 || vagasPorSemana <= 0)
            return Array.Empty<Colaborador>();

        vagasPorSemana = Math.Min(vagasPorSemana, VagasPorSemana);

        if (todos.Count <= vagasPorSemana)
            return todos.ToList();

        var fora = todos.Where(c => !participantesSemanaAnterior.Contains(c.Nome)).ToList();
        var dentro = todos.Where(c => participantesSemanaAnterior.Contains(c.Nome)).ToList();

        if (fora.Count >= vagasPorSemana)
            return fora.OrderBy(_ => rng.Next()).Take(vagasPorSemana).ToList();

        var k = vagasPorSemana - fora.Count;
        var dentroOrdenados = dentro
            .OrderBy(c => historico.ObterSequenciaConsecutiva(c.Nome))
            .ThenBy(_ => rng.Next())
            .ToList();

        var elegiveisRepetir = dentroOrdenados
            .Where(c => historico.ObterSequenciaConsecutiva(c.Nome) < SemanasConsecutivasLimiteParaEvitarSobrecarga)
            .ToList();
        var sobrecarregados = dentroOrdenados
            .Where(c => historico.ObterSequenciaConsecutiva(c.Nome) >= SemanasConsecutivasLimiteParaEvitarSobrecarga)
            .ToList();

        var extras = new List<Colaborador>();
        extras.AddRange(elegiveisRepetir.Take(k));
        if (extras.Count < k)
            extras.AddRange(sobrecarregados.Take(k - extras.Count));

        return fora.Concat(extras).ToList();
    }

    /// <summary>
    /// Confere se o número de repetidos bate com o mínimo necessário e regista avisos se alguém já vinha sobrecarregado.
    /// </summary>
    public static void RegistrarValidacaoEquilibrio(
        IReadOnlyList<Colaborador> selecionados,
        IReadOnlySet<string> participantesSemanaAnterior,
        HistoricoPares historico,
        IReadOnlyList<Colaborador> todos,
        Action<string> log)
    {
        if (todos.Count <= VagasPorSemana)
            return;

        var foraCount = todos.Count(c => !participantesSemanaAnterior.Contains(c.Nome));
        var precisamRepetir = Math.Max(0, VagasPorSemana - foraCount);
        var repetidos = selecionados.Count(c => participantesSemanaAnterior.Contains(c.Nome));

        if (repetidos != precisamRepetir)
        {
            log(
                $"Validação equilíbrio: esperavam-se {precisamRepetir} repetidos da semana anterior (vagas {VagasPorSemana} − {foraCount} de fora); lista tem {repetidos}.");
        }

        foreach (var c in selecionados)
        {
            if (!participantesSemanaAnterior.Contains(c.Nome))
                continue;

            var s = historico.ObterSequenciaConsecutiva(c.Nome);
            if (s >= SemanasConsecutivasLimiteParaEvitarSobrecarga)
            {
                log(
                    $"Equilíbrio: {c.Nome} já tinha {s} semana(s) consecutiva(s); entrou na vaga só por falta de colaboradores com sequência menor.");
            }
        }
    }

    /// <summary>
    /// Uma dupla por dia útil (2 pessoas). Cada pessoa entra no máximo uma vez na semana.
    /// Quem sobra no sorteio fica sem escala nesta semana (não há trios).
    /// Rodízio justo: menor histórico de par juntos, com embaralhamento.
    /// </summary>
    public EscalaSemana GerarSemana(
        IReadOnlyList<Colaborador> colaboradores,
        HistoricoPares historico,
        DateTime segundaSemana,
        int indicePrimeiroDia = 0,
        Random? rng = null)
    {
        rng ??= Random.Shared;
        var dias = DiasSemanaPt.OrdemSemana
            .Select(d => new AtribuicaoDia { DiaDaSemana = d, Nomes = new List<string>() })
            .ToList();

        if (colaboradores.Count == 0)
        {
            return new EscalaSemana
            {
                InicioDaSemana = segundaSemana.ToString("yyyy-MM-dd"),
                Dias = dias
            };
        }

        var pool = colaboradores.OrderBy(_ => rng.Next()).ToList();

        var indiceDia = Math.Clamp(indicePrimeiroDia, 0, 4);
        while (pool.Count >= 2 && indiceDia < 5)
        {
            var a = pool[0];
            pool.RemoveAt(0);

            var candidatos = pool.ToList();
            var minCont = candidatos.Min(c => historico.ObterContagem(a.Nome, c.Nome));
            var melhores = candidatos.Where(c => historico.ObterContagem(a.Nome, c.Nome) == minCont).ToList();
            var melhor = melhores[rng.Next(melhores.Count)];
            pool.Remove(melhor);

            dias[indiceDia].Nomes.Add(a.Nome);
            dias[indiceDia].Nomes.Add(melhor.Nome);
            indiceDia++;
        }

        return new EscalaSemana
        {
            InicioDaSemana = segundaSemana.ToString("yyyy-MM-dd"),
            Dias = dias
        };
    }

    public void AplicarIncrementosHistorico(HistoricoPares historico, EscalaSemana semana)
    {
        foreach (var dia in semana.Dias)
        {
            var nomes = dia.Nomes;
            if (nomes.Count != 2)
                continue;

            historico.Incrementar(nomes[0], nomes[1]);
        }
    }

    /// <summary>
    /// Garante que quem está em Missão cumprida não entra nos dias/fila e permanece bloqueado na mesma semana.
    /// </summary>
    public void AplicarMissaoCumpridaPosGeracao(
        EscalaSemana semana,
        IReadOnlyList<Colaborador> todos,
        IReadOnlySet<string> participantesAnteriores,
        IReadOnlySet<string> missaoCumpridaAtiva,
        HistoricoPares historico)
    {
        if (missaoCumpridaAtiva.Count > 0)
        {
            foreach (var dia in semana.Dias)
            {
                dia.Nomes = dia.Nomes
                    .Where(n => !string.IsNullOrWhiteSpace(n) &&
                                !missaoCumpridaAtiva.Contains(n.Trim()))
                    .ToList();
            }

            semana.Bloqueados = missaoCumpridaAtiva
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        else
        {
            semana.Bloqueados = [];
        }

        var escalados = semana.Dias
            .SelectMany(d => d.Nomes)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        semana.FilaEspera = todos
            .Where(c =>
            {
                var nome = c.Nome.Trim();
                return !escalados.Contains(nome) && !missaoCumpridaAtiva.Contains(nome);
            })
            .OrderBy(c => participantesAnteriores.Contains(c.Nome.Trim()) ? 1 : 0)
            .ThenBy(c => historico.ObterSequenciaConsecutiva(c.Nome.Trim()))
            .ThenBy(c => c.Nome.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(c => c.Nome.Trim())
            .ToList();
    }
}
