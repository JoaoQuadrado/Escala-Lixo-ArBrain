namespace EscalaLixo.Api.Dtos;

public sealed class ApiEstadoDto
{
    public List<ApiEmployeeDto> Employees { get; set; } = [];
    public ApiScheduleDto? Schedule { get; set; }
    public ApiValidationDto? Validation { get; set; }
    public string? ScheduleHash { get; set; }
}

public sealed class ApiEmployeeDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "Auxiliar";
    public string? DiscordUser { get; set; }
    public string? PhotoUrl { get; set; }
    public string Color { get; set; } = "#FFC300";
    public bool OnVacation { get; set; }
    public bool Absent { get; set; }
    public string? Notes { get; set; }
}

public sealed class ApiScheduleDto
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string WeekStart { get; set; } = "";
    public List<ApiDayDto> Days { get; set; } = [];
    public List<string> WaitingQueue { get; set; } = [];
    public List<string> BlockedQueue { get; set; } = [];
    public string? UpdatedAt { get; set; }
}

public sealed class ApiDayDto
{
    public string Day { get; set; } = "";
    public List<string> EmployeeIds { get; set; } = [];
}

public sealed class ApiValidationDto
{
    public bool Valid { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public sealed class ApiMoveDto
{
    public string EmployeeId { get; set; } = "";
    public string FromDay { get; set; } = "";
    public string ToDay { get; set; } = "";
    public int? ToIndex { get; set; }
    public string? ExpectedHash { get; set; }
}

public sealed class ApiSwapDto
{
    public string EmployeeIdA { get; set; } = "";
    public string FromDayA { get; set; } = "";
    public string EmployeeIdB { get; set; } = "";
    public string FromDayB { get; set; } = "";
    public string? ExpectedHash { get; set; }
}

public sealed class ApiColaboradorDto
{
    public string Name { get; set; } = "";
    public string? DiscordUser { get; set; }
    public string Role { get; set; } = "Auxiliar";
    public string Color { get; set; } = "#FFC300";
    public string? PhotoUrl { get; set; }
    public bool OnVacation { get; set; }
    public bool Absent { get; set; }
    public string? Notes { get; set; }
}

public sealed class ApiErrorDto
{
    public string Message { get; set; } = "";
    public ApiValidationDto? Validation { get; set; }
}

public sealed class ApiConfigDto
{
    public string WebhookDiscord { get; set; } = "";
    public bool WebhookConfigured { get; set; }
    public string TokenBotDiscord { get; set; } = "";
    public bool TokenBotConfigured { get; set; }
    public string IdServidorDiscord { get; set; } = "";
    public string UrlGifPreviaSemanal { get; set; } = "";
    public string UrlGifDiario { get; set; } = "";
    public bool GifPreviaConfigured { get; set; }
    public bool GifDiarioConfigured { get; set; }
    public string? GifPreviaId { get; set; }
    public string? GifDiarioId { get; set; }
    public string ModeloMensagemDiaria { get; set; } = "";
    public int IntervaloVerificacaoMinutos { get; set; } = 60;
    public int HoraNotificacaoPadrao { get; set; } = 8;
    public int HoraPreviaSemanal { get; set; } = 8;
    public int HoraLembreteDiario { get; set; } = 17;
    public string PastaDados { get; set; } = "";
    public string ColaboradoresFonte { get; set; } = "";
    public bool PostgresConfigured { get; set; }
    public string CaminhoConfiguracao { get; set; } = "";
}

public sealed class ApiConfigSaveDto
{
    public string WebhookDiscord { get; set; } = "";
    public string TokenBotDiscord { get; set; } = "";
    public string IdServidorDiscord { get; set; } = "";
    public string UrlGifPreviaSemanal { get; set; } = "";
    public string UrlGifDiario { get; set; } = "";
    public string ModeloMensagemDiaria { get; set; } = "";
    public int IntervaloVerificacaoMinutos { get; set; } = 60;
    public int HoraNotificacaoPadrao { get; set; } = 8;
    public int HoraPreviaSemanal { get; set; } = 8;
    public int HoraLembreteDiario { get; set; } = 17;
}

public sealed class ApiGifResumoDto
{
    public string Id { get; set; } = "";
    public string Nome { get; set; } = "";
    public string Mime { get; set; } = "";
    public DateTimeOffset CriadoEm { get; set; }
}

public sealed class ApiGifSelecaoDto
{
    public string? GifPreviaId { get; set; }
    public string? GifDiarioId { get; set; }
}

public sealed class ApiEscalaHistoricoResumoDto
{
    public string Id { get; set; } = "";
    public string WeekStart { get; set; } = "";
    public string ArchivedAt { get; set; } = "";
    public string Motivo { get; set; } = "";
    public int AssignedCount { get; set; }
}

public sealed class ApiEscalaHistoricoDetalheDto
{
    public string Id { get; set; } = "";
    public string WeekStart { get; set; } = "";
    public string ArchivedAt { get; set; } = "";
    public string Motivo { get; set; } = "";
    public List<ApiEmployeeDto> Employees { get; set; } = [];
    public ApiScheduleDto? Schedule { get; set; }
}

public sealed class ApiRotacaoResumoDto
{
    public int TotalColaboradores { get; set; }
    public int VagasPorSemana { get; set; }
    public int DeForaProxima { get; set; }
    public int RepetidosNecessarios { get; set; }
    public int LimiteSemanasConsecutivas { get; set; }
}

public sealed class ApiRotacaoColaboradorDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Color { get; set; } = "";
    public int Streak { get; set; }
    public string Status { get; set; } = "";
    public bool CanRepeatNext { get; set; }
    public bool BlockedRepeat { get; set; }
}

public sealed class ApiRotacaoProximaDto
{
    public List<string> NewEntries { get; set; } = [];
    public List<string> WaitingOutside { get; set; } = [];
    public int RepeatNeeded { get; set; }
    public List<string> CanRepeat { get; set; } = [];
    public List<string> BlockedRepeat { get; set; } = [];
}

public sealed class ApiRotacaoSemanaDto
{
    public int Index { get; set; }
    public string WeekStart { get; set; } = "";
    public int OutsideCount { get; set; }
    public int RepeatNeeded { get; set; }
    public List<string> NewNames { get; set; } = [];
    public List<string> RepeatNames { get; set; } = [];
    public List<string> AssignedNames { get; set; } = [];
    public Dictionary<string, int> StreaksAfter { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ApiRotacaoDto
{
    public ApiRotacaoResumoDto Summary { get; set; } = new();
    public List<ApiRotacaoColaboradorDto> Employees { get; set; } = [];
    public ApiRotacaoProximaDto NextWeek { get; set; } = new();
    public List<ApiRotacaoSemanaDto> Simulation { get; set; } = [];
}
