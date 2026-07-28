using EscalaLixo.Api.Dtos;
using EscalaLixo.Modelos;
using EscalaLixo.Servicos;

namespace EscalaLixo.Api.Mapping;

internal static class ApiMapper
{
    private static readonly Dictionary<string, string> EnParaPt = new(StringComparer.OrdinalIgnoreCase)
    {
        ["monday"] = "segunda-feira",
        ["tuesday"] = "terça-feira",
        ["wednesday"] = "quarta-feira",
        ["thursday"] = "quinta-feira",
        ["friday"] = "sexta-feira",
        ["waiting"] = ServicoValidacaoEscala.ChaveFilaEspera,
        ["blocked"] = ServicoValidacaoEscala.ChaveBloqueados,
    };

    private static readonly Dictionary<string, string> PtParaEn = EnParaPt
        .ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.OrdinalIgnoreCase);

    private static readonly string[] AllDaysEn =
        ["monday", "tuesday", "wednesday", "thursday", "friday"];

    public static string DiaEnParaPt(string diaEn) =>
        EnParaPt.TryGetValue(diaEn.Trim(), out var pt) ? pt : diaEn;

    public static string DiaPtParaEn(string diaPt)
    {
        var t = diaPt.Trim();
        if (ServicoValidacaoEscala.EhFilaEsperaPublico(t))
            return "waiting";
        if (ServicoValidacaoEscala.EhBloqueadoPublico(t))
            return "blocked";
        if (PtParaEn.TryGetValue(t, out var en))
            return en;
        return t;
    }

    public static string IdParaNome(string id, IReadOnlyList<Colaborador> colaboradores) =>
        colaboradores.FirstOrDefault(c => c.ObterIdPublico() == id)?.Nome
        ?? colaboradores.FirstOrDefault(c => string.Equals(c.Id.ToString(), id, StringComparison.OrdinalIgnoreCase))?.Nome
        ?? id;

    public static ApiEmployeeDto ParaEmployeeDto(Colaborador c) => new()
    {
        Id = c.ObterIdPublico(),
        Name = c.Nome,
        Role = c.Cargo,
        DiscordUser = c.UsuarioDiscord,
        Color = c.Cor,
        OnVacation = c.DeFerias,
        Absent = c.Ausente,
        Notes = c.Observacoes,
        PhotoUrl = c.FotoUrl,
    };

    public static Colaborador ParaColaborador(ApiColaboradorDto dto) => new()
    {
        Nome = dto.Name.Trim(),
        UsuarioDiscord = dto.DiscordUser?.Trim() ?? "",
        Cargo = string.IsNullOrWhiteSpace(dto.Role) ? "" : dto.Role.Trim(),
        Cor = dto.Color ?? "#FFC300",
        FotoUrl = dto.PhotoUrl,
        DeFerias = dto.OnVacation,
        Ausente = dto.Absent,
        Observacoes = dto.Notes,
    };

    public static ApiEstadoDto ParaEstado(
        IReadOnlyList<Colaborador> colaboradores,
        EscalaSemana? escala,
        ResultadoValidacaoEscala? validacao = null,
        string? hashEscala = null)
    {
        var employees = colaboradores.Select(ParaEmployeeDto).ToList();
        var idMap = employees.ToDictionary(e => e.Name, e => e.Id, StringComparer.OrdinalIgnoreCase);

        ApiScheduleDto? schedule = null;
        if (escala is not null)
        {
            var dayMap = escala.Dias.ToDictionary(
                d => DiaPtParaEn(d.DiaDaSemana),
                d => d.Nomes
                    .Where(n => idMap.ContainsKey(n.Trim()))
                    .Select(n => idMap[n.Trim()])
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);

            schedule = new ApiScheduleDto
            {
                Id = ColaboradorIds.DerivarDeNome(escala.InicioDaSemana),
                Title = $"Escala — {escala.InicioDaSemana}",
                WeekStart = escala.InicioDaSemana,
                Days = AllDaysEn.Select(dayEn => new ApiDayDto
                {
                    Day = dayEn,
                    EmployeeIds = dayMap.TryGetValue(dayEn, out var ids) ? ids : [],
                }).ToList(),
                WaitingQueue = escala.FilaEspera
                    .Where(n => idMap.ContainsKey(n.Trim()))
                    .Select(n => idMap[n.Trim()])
                    .ToList(),
                BlockedQueue = escala.Bloqueados
                    .Where(n => idMap.ContainsKey(n.Trim()))
                    .Select(n => idMap[n.Trim()])
                    .ToList(),
                UpdatedAt = DateTime.UtcNow.ToString("o"),
            };
        }

        return new ApiEstadoDto
        {
            Employees = employees,
            Schedule = schedule,
            Validation = validacao is null
                ? null
                : new ApiValidationDto
                {
                    Valid = validacao.Valido,
                    Errors = validacao.Erros,
                    Warnings = validacao.Avisos,
                },
            ScheduleHash = hashEscala,
        };
    }

    public static ApiEscalaHistoricoResumoDto ParaHistoricoResumo(EscalaHistoricoResumo item) => new()
    {
        Id = item.Id.ToString(),
        WeekStart = item.InicioSemana,
        ArchivedAt = item.ArquivadoEm.ToString("o"),
        Motivo = item.Motivo,
        AssignedCount = item.TotalEscalados,
    };

    public static ApiEscalaHistoricoDetalheDto ParaHistoricoDetalhe(
        IReadOnlyList<Colaborador> colaboradores,
        EscalaHistoricoCompleta historico)
    {
        var nomesHistorico = ColetarNomesEscala(historico.Escala);
        var extras = nomesHistorico
            .Where(nome => !colaboradores.Any(c =>
                string.Equals(c.Nome.Trim(), nome, StringComparison.OrdinalIgnoreCase)))
            .Select(nome => new Colaborador { Nome = nome, Cor = "#6b7280" })
            .ToList();

        var merged = colaboradores.Concat(extras).ToList();
        var estado = ParaEstado(merged, historico.Escala);

        return new ApiEscalaHistoricoDetalheDto
        {
            Id = historico.Id.ToString(),
            WeekStart = historico.Escala.InicioDaSemana,
            ArchivedAt = historico.ArquivadoEm.ToString("o"),
            Motivo = historico.Motivo,
            Employees = estado.Employees,
            Schedule = estado.Schedule,
        };
    }

    private static HashSet<string> ColetarNomesEscala(EscalaSemana escala)
    {
        var nomes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dia in escala.Dias)
        foreach (var nome in dia.Nomes)
            if (!string.IsNullOrWhiteSpace(nome))
                nomes.Add(nome.Trim());

        foreach (var nome in escala.FilaEspera.Concat(escala.Bloqueados))
            if (!string.IsNullOrWhiteSpace(nome))
                nomes.Add(nome.Trim());

        return nomes;
    }

    public static ApiRotacaoDto ParaRotacaoDto(RotacaoPainel painel) => new()
    {
        Summary = new ApiRotacaoResumoDto
        {
            TotalColaboradores = painel.Resumo.TotalColaboradores,
            VagasPorSemana = painel.Resumo.VagasPorSemana,
            DeForaProxima = painel.Resumo.DeForaProxima,
            RepetidosNecessarios = painel.Resumo.RepetidosNecessarios,
            LimiteSemanasConsecutivas = painel.Resumo.LimiteSemanasConsecutivas,
        },
        Employees = painel.Colaboradores.Select(c => new ApiRotacaoColaboradorDto
        {
            Id = c.Id,
            Name = c.Nome,
            Color = c.Cor,
            Streak = c.SequenciaConsecutiva,
            Status = c.StatusAtual,
            CanRepeatNext = c.PodeRepetirProxima,
            BlockedRepeat = c.BloqueadoRepeticao,
        }).ToList(),
        NextWeek = new ApiRotacaoProximaDto
        {
            NewEntries = painel.ProximaSemana.NovosEntram,
            WaitingOutside = painel.ProximaSemana.NovosDeFora,
            RepeatNeeded = painel.ProximaSemana.RepetidosNecessarios,
            CanRepeat = painel.ProximaSemana.PodemRepetir,
            BlockedRepeat = painel.ProximaSemana.BloqueadosRepeticao,
        },
        Simulation = painel.Simulacao.Select(s => new ApiRotacaoSemanaDto
        {
            Index = s.Indice,
            WeekStart = s.InicioSemana,
            OutsideCount = s.DeFora,
            RepeatNeeded = s.RepetidosNecessarios,
            NewNames = s.Novos,
            RepeatNames = s.Repetidos,
            AssignedNames = s.Escalados,
            StreaksAfter = s.SequenciasApos,
        }).ToList(),
    };
}
