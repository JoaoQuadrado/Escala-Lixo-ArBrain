using EscalaLixo.Api.Mapping;
using EscalaLixo.Servicos;

namespace EscalaLixo.Api.Endpoints;

internal static class EstadoEndpoints
{
    public static void MapEstadoEndpoints(this WebApplication app)
    {
        app.MapGet("/api/estado", async (
            ServicoRepositorioArquivos repo,
            ServicoValidacaoEscala validacao,
            CancellationToken ct) =>
        {
            var lista = await repo.LerColaboradoresAsync(ct);
            var escala = await repo.LerEscalaAsync(ct);
            var hash = await repo.ObterHashEscalaAsync(ct);

            ResultadoValidacaoEscala? val = null;
            if (escala is not null)
                val = validacao.Validar(escala, lista);

            return Results.Ok(ApiMapper.ParaEstado(lista, escala, val, hash));
        });
    }
}
