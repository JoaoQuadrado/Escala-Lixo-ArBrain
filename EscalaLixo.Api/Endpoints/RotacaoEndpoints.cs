using EscalaLixo.Api.Dtos;
using EscalaLixo.Api.Mapping;
using EscalaLixo.Servicos;

namespace EscalaLixo.Api.Endpoints;

internal static class RotacaoEndpoints
{
    public static void MapRotacaoEndpoints(this WebApplication app)
    {
        app.MapGet("/api/escala/rotacao", async (
            ServicoRepositorioArquivos repo,
            ServicoRotacaoVisual rotacao,
            int? semanas,
            CancellationToken ct) =>
        {
            var lista = await repo.LerColaboradoresAsync(ct);
            var historico = await repo.LerHistoricoAsync(ct);
            var escala = await repo.LerEscalaAsync(ct);

            var painel = rotacao.MontarPainel(lista, historico, escala, semanas ?? 4);
            return Results.Ok(ApiMapper.ParaRotacaoDto(painel));
        });
    }
}
