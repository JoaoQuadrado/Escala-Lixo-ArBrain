using EscalaLixo.Api.Dtos;
using EscalaLixo.Api.Mapping;
using EscalaLixo.Modelos;
using EscalaLixo.Servicos;

namespace EscalaLixo.Api.Endpoints;

internal static class EscalaEndpoints
{
    public static void MapEscalaEndpoints(this WebApplication app)
    {
        app.MapPost("/api/escala/mover", async (
            ApiMoveDto body,
            ServicoRepositorioArquivos repo,
            CancellationToken ct) =>
        {
            var lista = await repo.LerColaboradoresAsync(ct);
            var escala = await repo.LerEscalaAsync(ct);

            if (escala is null)
                return Results.NotFound(new ApiErrorDto { Message = "Nenhuma escala encontrada." });

            if (!string.IsNullOrEmpty(body.ExpectedHash))
            {
                var hashAtual = await repo.ObterHashEscalaAsync(ct);
                if (hashAtual != body.ExpectedHash)
                {
                    return Results.Conflict(new ApiErrorDto
                    {
                        Message = "A escala foi alterada por outro processo. Recarregue e tente novamente.",
                    });
                }
            }

            var nome = ApiMapper.IdParaNome(body.EmployeeId, lista);
            var diaOrigem = ApiMapper.DiaEnParaPt(body.FromDay);
            var diaDestino = ApiMapper.DiaEnParaPt(body.ToDay);

            var (escalaAtualizada, resultado) = await repo.MoverColaboradorAsync(
                escala, lista, nome, diaOrigem, diaDestino, body.ToIndex, ct);

            if (!resultado.Valido)
            {
                return Results.BadRequest(new ApiErrorDto
                {
                    Message = resultado.Erros.FirstOrDefault() ?? "Movimento inválido.",
                    Validation = new ApiValidationDto
                    {
                        Valid = false,
                        Errors = resultado.Erros,
                        Warnings = resultado.Avisos,
                    },
                });
            }

            var hash = await repo.ObterHashEscalaAsync(ct);
            return Results.Ok(ApiMapper.ParaEstado(lista, escalaAtualizada, resultado, hash));
        });

        app.MapPost("/api/escala/trocar", async (
            ApiSwapDto body,
            ServicoRepositorioArquivos repo,
            CancellationToken ct) =>
        {
            var lista = await repo.LerColaboradoresAsync(ct);
            var escala = await repo.LerEscalaAsync(ct);

            if (escala is null)
                return Results.NotFound(new ApiErrorDto { Message = "Nenhuma escala encontrada." });

            if (!string.IsNullOrEmpty(body.ExpectedHash))
            {
                var hashAtual = await repo.ObterHashEscalaAsync(ct);
                if (hashAtual != body.ExpectedHash)
                {
                    return Results.Conflict(new ApiErrorDto
                    {
                        Message = "A escala foi alterada por outro processo. Recarregue e tente novamente.",
                    });
                }
            }

            var nomeA = ApiMapper.IdParaNome(body.EmployeeIdA, lista);
            var nomeB = ApiMapper.IdParaNome(body.EmployeeIdB, lista);
            var diaA = ApiMapper.DiaEnParaPt(body.FromDayA);
            var diaB = ApiMapper.DiaEnParaPt(body.FromDayB);

            var (escalaAtualizada, resultado) = await repo.TrocarColaboradoresAsync(
                escala, lista, nomeA, diaA, nomeB, diaB, ct);

            if (!resultado.Valido)
            {
                return Results.BadRequest(new ApiErrorDto
                {
                    Message = resultado.Erros.FirstOrDefault() ?? "Troca inválida.",
                    Validation = new ApiValidationDto
                    {
                        Valid = false,
                        Errors = resultado.Erros,
                        Warnings = resultado.Avisos,
                    },
                });
            }

            var hash = await repo.ObterHashEscalaAsync(ct);
            return Results.Ok(ApiMapper.ParaEstado(lista, escalaAtualizada, resultado, hash));
        });

        app.MapPost("/api/escala/gerar", async (
            ServicoRepositorioArquivos repo,
            ServicoDuplas duplas,
            CancellationToken ct) =>
        {
            var lista = await repo.LerColaboradoresAsync(ct);
            if (lista.Count == 0)
                return Results.BadRequest(new ApiErrorDto { Message = "Nenhum colaborador cadastrado." });

            var historico = await repo.LerHistoricoAsync(ct);
            var escalaAnterior = await repo.LerEscalaAsync(ct);

            var participantesAnteriores = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var missaoCumprida = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (escalaAnterior is not null)
            {
                foreach (var dia in escalaAnterior.Dias)
                foreach (var n in dia.Nomes)
                    if (!string.IsNullOrWhiteSpace(n))
                        participantesAnteriores.Add(n.Trim());

                foreach (var n in escalaAnterior.Bloqueados)
                {
                    if (string.IsNullOrWhiteSpace(n))
                        continue;
                    var nome = n.Trim();
                    participantesAnteriores.Add(nome);
                    missaoCumprida.Add(nome);
                }
            }

            var hoje = DateTime.Today;
            var segunda = hoje;
            var diff = (7 + (segunda.DayOfWeek - DayOfWeek.Monday)) % 7;
            segunda = segunda.Date.AddDays(-diff);

            var indicePrimeiroDia = DiasSemanaPt.IndicePrimeiroDiaUtilRestante(hoje);
            if (DiasSemanaPt.EhFimDeSemana(hoje))
            {
                segunda = segunda.AddDays(7);
                indicePrimeiroDia = 0;
            }

            var inicioSemanaStr = segunda.ToString("yyyy-MM-dd");
            var mesmaSemana = escalaAnterior is not null
                && string.Equals(escalaAnterior.InicioDaSemana.Trim(), inicioSemanaStr, StringComparison.Ordinal);

            var missaoCumpridaAtiva = mesmaSemana
                ? missaoCumprida
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var elegiveis = lista
                .Where(c => !missaoCumpridaAtiva.Contains(c.Nome.Trim()))
                .ToList();

            var diasRestantes = 5 - indicePrimeiroDia;
            var vagasNecessarias = diasRestantes * ServicoValidacaoEscala.MaximoPorDiaUtil;

            var selecionados = duplas.SelecionarParticipantesParaSemana(
                elegiveis, historico, participantesAnteriores, vagasNecessarias);

            var semana = duplas.GerarSemana(selecionados, historico, segunda, indicePrimeiroDia);

            duplas.AplicarMissaoCumpridaPosGeracao(
                semana, lista, participantesAnteriores, missaoCumpridaAtiva, historico);

            duplas.AplicarIncrementosHistorico(historico, semana);
            historico.AtualizarSequenciasConsecutivas(participantesAnteriores, semana);

            if (escalaAnterior is not null && escalaAnterior.TemConteudo())
            {
                var motivo = mesmaSemana ? "regeneracao" : "nova_semana";
                await repo.ArquivarEscalaAsync(escalaAnterior, motivo, ct);
            }

            await repo.SalvarHistoricoAsync(historico, ct);

            var resultado = await repo.SalvarEscalaAsync(semana, lista, sanitizar: false, ct);
            if (!resultado.Valido)
            {
                return Results.BadRequest(new ApiErrorDto
                {
                    Message = "Escala gerada é inválida.",
                    Validation = new ApiValidationDto { Valid = false, Errors = resultado.Erros },
                });
            }

            var hash = await repo.ObterHashEscalaAsync(ct);
            return Results.Ok(ApiMapper.ParaEstado(lista, semana, resultado, hash));
        });

        app.MapGet("/api/escala/historico", async (
            ServicoRepositorioArquivos repo,
            int? limit,
            CancellationToken ct) =>
        {
            var lista = await repo.ListarHistoricoEscalasAsync(limit ?? 50, ct);
            return Results.Ok(lista.Select(ApiMapper.ParaHistoricoResumo).ToList());
        });

        app.MapGet("/api/escala/historico/{id:guid}", async (
            Guid id,
            ServicoRepositorioArquivos repo,
            CancellationToken ct) =>
        {
            var item = await repo.LerHistoricoEscalaAsync(id, ct);
            if (item is null)
                return Results.NotFound(new ApiErrorDto { Message = "Registro de histórico não encontrado." });

            var colaboradores = await repo.LerColaboradoresAsync(ct);
            return Results.Ok(ApiMapper.ParaHistoricoDetalhe(colaboradores, item));
        });
    }
}
