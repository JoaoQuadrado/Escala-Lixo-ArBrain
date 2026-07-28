using EscalaLixo.Api.Dtos;
using EscalaLixo.Api.Infrastructure;
using EscalaLixo.Servicos;

namespace EscalaLixo.Api.Endpoints;

internal static class DatabaseEndpoints
{
    public static void MapDatabaseEndpoints(
        this WebApplication app,
        string connectionString,
        string pastaMigrations)
    {
        app.MapGet("/api/db/status", async (CancellationToken ct) =>
        {
            var migracao = new ServicoMigracaoSupabase(connectionString, pastaMigrations);
            return Results.Ok(await migracao.ObterStatusAsync(ct));
        });

        app.MapPost("/api/db/migrate", async (CancellationToken ct) =>
        {
            try
            {
                var migracao = new ServicoMigracaoSupabase(connectionString, pastaMigrations);
                var aplicadas = await migracao.AplicarPendentesAsync(ct);
                var status = await migracao.ObterStatusAsync(ct);
                return Results.Ok(new
                {
                    message = aplicadas.Count > 0
                        ? $"Migrations aplicadas: {string.Join(", ", aplicadas)}"
                        : "Nenhuma migration pendente.",
                    aplicadas,
                    status,
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, title: "Falha ao aplicar migrations", statusCode: 500);
            }
        });
    }
}
