using EscalaLixo.Api.Dtos;
using EscalaLixo.Api.Infrastructure;
using EscalaLixo.Api.Mapping;
using EscalaLixo.Servicos;

namespace EscalaLixo.Api.Endpoints;

internal static class ColaboradoresEndpoints
{
    public static void MapColaboradoresEndpoints(this WebApplication app)
    {
        app.MapPost("/api/colaboradores", async (
            ApiColaboradorDto body,
            IRepositorioColaboradores colaboradores,
            ServicoRepositorioArquivos repo,
            CancellationToken ct) =>
        {
            try
            {
                var criado = await colaboradores.CriarAsync(ApiMapper.ParaColaborador(body), ct);
                var lista = await repo.LerColaboradoresAsync(ct);
                var escala = await repo.LerEscalaAsync(ct);
                var hash = await repo.ObterHashEscalaAsync(ct);
                return Results.Created($"/api/colaboradores/{criado.Id}", ApiMapper.ParaEstado(lista, escala, null, hash));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ApiErrorDto { Message = ex.Message });
            }
        });

        app.MapPut("/api/colaboradores/{id:guid}", async (
            Guid id,
            ApiColaboradorDto body,
            IRepositorioColaboradores colaboradores,
            ServicoRepositorioArquivos repo,
            CancellationToken ct) =>
        {
            try
            {
                var col = ApiMapper.ParaColaborador(body);
                col.Id = id;
                await colaboradores.AtualizarAsync(col, ct);
                var lista = await repo.LerColaboradoresAsync(ct);
                var escala = await repo.LerEscalaAsync(ct);
                var hash = await repo.ObterHashEscalaAsync(ct);
                return Results.Ok(ApiMapper.ParaEstado(lista, escala, null, hash));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ApiErrorDto { Message = ex.Message });
            }
        });

        app.MapDelete("/api/colaboradores/{id:guid}", async (
            Guid id,
            IRepositorioColaboradores colaboradores,
            ServicoRepositorioArquivos repo,
            CancellationToken ct) =>
        {
            await colaboradores.ExcluirAsync(id, ct);
            var lista = await repo.LerColaboradoresAsync(ct);
            var escala = await repo.LerEscalaAsync(ct);
            var hash = await repo.ObterHashEscalaAsync(ct);
            return Results.Ok(ApiMapper.ParaEstado(lista, escala, null, hash));
        });
    }
}
