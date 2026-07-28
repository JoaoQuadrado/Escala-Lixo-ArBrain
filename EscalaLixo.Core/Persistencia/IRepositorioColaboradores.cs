using EscalaLixo.Modelos;

namespace EscalaLixo.Servicos;

public interface IRepositorioColaboradores
{
    Task<List<Colaborador>> ListarAsync(CancellationToken ct = default);
    Task<Colaborador?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<Colaborador> CriarAsync(Colaborador colaborador, CancellationToken ct = default);
    Task<Colaborador> AtualizarAsync(Colaborador colaborador, CancellationToken ct = default);
    Task ExcluirAsync(Guid id, CancellationToken ct = default);
    Task SalvarTodosAsync(IReadOnlyList<Colaborador> colaboradores, CancellationToken ct = default);
    bool Disponivel { get; }
}
