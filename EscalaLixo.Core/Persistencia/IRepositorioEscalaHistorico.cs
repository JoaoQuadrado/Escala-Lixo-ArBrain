using EscalaLixo.Modelos;

namespace EscalaLixo.Servicos;

public interface IRepositorioEscalaHistorico
{
    bool UsaPostgres { get; }

    /// <summary>PostgreSQL com tabela escala_posicoes — movimento atómico no banco.</summary>
    bool SuportaMovimentoAtomico { get; }

    Task<EscalaSemana?> LerEscalaAsync(CancellationToken ct = default);

    Task<string> ObterHashEscalaAsync(CancellationToken ct = default);

    Task SalvarEscalaAsync(EscalaSemana escala, CancellationToken ct = default);

    Task MoverColaboradorAtomicoAsync(
        Guid colaboradorId,
        string slotDestino,
        int ordem,
        CancellationToken ct = default);

    Task TrocarColaboradoresAtomicoAsync(
        Guid colaboradorIdA,
        Guid colaboradorIdB,
        CancellationToken ct = default);

    Task<HistoricoPares> LerHistoricoAsync(CancellationToken ct = default);

    Task SalvarHistoricoAsync(HistoricoPares historico, CancellationToken ct = default);

    Task ArquivarEscalaAsync(EscalaSemana escala, string motivo, CancellationToken ct = default);

    Task<List<EscalaHistoricoResumo>> ListarHistoricoEscalasAsync(int limite = 50, CancellationToken ct = default);

    Task<EscalaHistoricoCompleta?> LerHistoricoEscalaAsync(Guid id, CancellationToken ct = default);
}
