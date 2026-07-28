namespace EscalaLixo.Modelos;

public sealed class EscalaHistoricoResumo
{
    public Guid Id { get; set; }
    public string InicioSemana { get; set; } = "";
    public string Motivo { get; set; } = "";
    public DateTimeOffset ArquivadoEm { get; set; }
    public int TotalEscalados { get; set; }
}

public sealed class EscalaHistoricoCompleta
{
    public Guid Id { get; set; }
    public EscalaSemana Escala { get; set; } = new();
    public string Motivo { get; set; } = "";
    public DateTimeOffset ArquivadoEm { get; set; }
}
