using EscalaLixo.Api.Infrastructure;
using EscalaLixo.Servicos;

namespace EscalaLixo.Api.Endpoints;

internal static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app, string? connectionString)
    {
        app.MapGet("/api/health", (ServicoRepositorioArquivos repo) =>
            Results.Ok(new
            {
                status = "ok",
                pastaDados = repo.PastaBase,
                colaboradoresFonte = "postgres",
                escalaFonte = "postgres",
                postgresConfigurado = true,
                postgresMotivo = (string?)null,
            }));
    }
}
