using EscalaLixo.Api.Dtos;
using EscalaLixo.Api.Infrastructure;
using EscalaLixo.Servicos;

namespace EscalaLixo.Api.Endpoints;

internal static class DiscordEndpoints
{
    public static void MapDiscordEndpoints(this WebApplication app)
    {
        app.MapPost("/api/discord/dia", async (
            ServicoConfiguracaoApp configApp,
            ServicoBibliotecaGifs biblioteca,
            ServicoRepositorioArquivos repo,
            ServicoValidacaoEscala validacao,
            CancellationToken ct) =>
        {
            var cfg = await configApp.LerAsync(ct);
            await biblioteca.HidratarGifsAsync(cfg, ct);
            var discord = DiscordFactory.Criar(cfg);
            if (discord is null)
                return Results.BadRequest(new ApiErrorDto { Message = "Webhook Discord não configurado." });

            var lista = await repo.LerColaboradoresAsync(ct);
            var escala = await repo.LerEscalaAsync(ct);
            if (escala is null)
                return Results.NotFound(new ApiErrorDto { Message = "Nenhuma escala encontrada." });

            var hoje = DateTime.Now.DayOfWeek;
            if (hoje is DayOfWeek.Saturday or DayOfWeek.Sunday)
                return Results.BadRequest(new ApiErrorDto { Message = "Lembrete diário só de segunda a sexta." });

            var dia = escala.Dias.FirstOrDefault(d => DiasSemanaPt.CorrespondeAoDia(d.DiaDaSemana, hoje));
            if (dia is null || dia.Nomes.Count == 0)
                return Results.NotFound(new ApiErrorDto { Message = "Ninguém escalado para hoje." });

            var val = validacao.Validar(escala, lista);
            if (!val.Valido)
            {
                return Results.BadRequest(new ApiErrorDto
                {
                    Message = "Escala inválida — corrija antes de enviar ao Discord.",
                    Validation = new ApiValidationDto { Valid = false, Errors = val.Erros, Warnings = val.Avisos },
                });
            }

            using (discord)
            {
                await discord.NotificarDiaAsync(dia, lista, ct);
            }

            return Results.Ok(new { message = "Lembrete do dia enviado ao Discord." });
        });

        app.MapPost("/api/discord/previa", async (
            ServicoConfiguracaoApp configApp,
            ServicoBibliotecaGifs biblioteca,
            ServicoRepositorioArquivos repo,
            ServicoValidacaoEscala validacao,
            CancellationToken ct) =>
        {
            var cfg = await configApp.LerAsync(ct);
            await biblioteca.HidratarGifsAsync(cfg, ct);
            var discord = DiscordFactory.Criar(cfg);
            if (discord is null)
                return Results.BadRequest(new ApiErrorDto { Message = "Webhook Discord não configurado." });

            var lista = await repo.LerColaboradoresAsync(ct);
            var escala = await repo.LerEscalaAsync(ct);
            if (escala is null)
                return Results.NotFound(new ApiErrorDto { Message = "Nenhuma escala encontrada." });

            var val = validacao.Validar(escala, lista);
            if (!val.Valido)
            {
                return Results.BadRequest(new ApiErrorDto
                {
                    Message = "Escala inválida — corrija antes de enviar ao Discord.",
                    Validation = new ApiValidationDto { Valid = false, Errors = val.Erros, Warnings = val.Avisos },
                });
            }

            using (discord)
            {
                await discord.NotificarPreviaSemanaAsync(escala, lista, ct);
            }

            return Results.Ok(new { message = "Prévia semanal enviada ao Discord." });
        });
    }
}
