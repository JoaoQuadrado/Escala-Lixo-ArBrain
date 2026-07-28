using EscalaLixo.Api.Dtos;
using EscalaLixo.Api.Infrastructure;
using EscalaLixo.Modelos;
using EscalaLixo.Servicos;

namespace EscalaLixo.Api.Endpoints;

internal static class GifEndpoints
{
    private const long MaxGifBytes = 8 * 1024 * 1024;

    private static readonly HashSet<string> MimesPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/gif", "image/png", "image/jpeg", "image/webp",
    };

    public static void MapGifEndpoints(this WebApplication app)
    {
        app.MapGet("/api/gifs", async (ServicoBibliotecaGifs biblioteca, CancellationToken ct) =>
        {
            var lista = await biblioteca.ListarAsync(ct);
            return Results.Ok(lista.Select(g => new ApiGifResumoDto
            {
                Id = g.Id.ToString(),
                Nome = g.Nome,
                Mime = g.Mime,
                CriadoEm = g.CriadoEm,
            }));
        });

        app.MapPost("/api/gifs", async (
            HttpRequest request,
            ServicoBibliotecaGifs biblioteca,
            ServicoConfiguracaoApp configApp,
            CancellationToken ct) =>
        {
            var resultado = await LerArquivoGifAsync(request, ct);
            if (resultado.Erro is not null)
                return Results.BadRequest(new ApiErrorDto { Message = resultado.Erro });

            var nome = resultado.Nome ?? "GIF";
            var (_, cfg) = await biblioteca.AdicionarAsync(nome, resultado.Dados!, resultado.Mime!, ct);
            return Results.Ok(ConfigMapper.ParaDto(cfg, configApp.Fonte));
        });

        app.MapGet("/api/gifs/{id:guid}", async (Guid id, ServicoBibliotecaGifs biblioteca, CancellationToken ct) =>
        {
            var gif = await biblioteca.ObterAsync(id, ct);
            return gif is null ? Results.NotFound() : Results.File(gif.Dados, gif.Mime);
        });

        app.MapDelete("/api/gifs/{id:guid}", async (
            Guid id,
            ServicoBibliotecaGifs biblioteca,
            ServicoConfiguracaoApp configApp,
            CancellationToken ct) =>
        {
            await biblioteca.RemoverAsync(id, ct);
            var cfg = await configApp.LerAsync(ct);
            return Results.Ok(ConfigMapper.ParaDto(cfg, configApp.Fonte));
        });

        app.MapPut("/api/config/gif-selecao", async (
            ApiGifSelecaoDto body,
            ServicoBibliotecaGifs biblioteca,
            ServicoConfiguracaoApp configApp,
            CancellationToken ct) =>
        {
            try
            {
                var cfg = await configApp.LerAsync(ct);

                if (body.GifPreviaId is not null)
                {
                    var previaId = string.IsNullOrWhiteSpace(body.GifPreviaId)
                        ? (Guid?)null
                        : Guid.Parse(body.GifPreviaId);
                    cfg = await biblioteca.SelecionarPreviaAsync(previaId, ct);
                }

                if (body.GifDiarioId is not null)
                {
                    var diarioId = string.IsNullOrWhiteSpace(body.GifDiarioId)
                        ? (Guid?)null
                        : Guid.Parse(body.GifDiarioId);
                    cfg = await biblioteca.SelecionarDiarioAsync(diarioId, ct);
                }

                return Results.Ok(ConfigMapper.ParaDto(cfg, configApp.Fonte));
            }
            catch (Exception ex) when (ex is FormatException or InvalidOperationException)
            {
                return Results.BadRequest(new ApiErrorDto { Message = ex.Message });
            }
        });
    }

    internal static async Task<(byte[]? Dados, string? Mime, string? Nome, string? Erro)> LerArquivoGifAsync(
        HttpRequest request,
        CancellationToken ct)
    {
        if (!request.HasFormContentType)
            return (null, null, null, "Envie o arquivo como multipart/form-data.");

        var form = await request.ReadFormAsync(ct);
        var arquivo = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
        if (arquivo is null || arquivo.Length == 0)
            return (null, null, null, "Nenhum arquivo enviado.");

        if (arquivo.Length > MaxGifBytes)
            return (null, null, null, "Arquivo muito grande (máximo 8 MB).");

        var mime = ResolverMime(arquivo);
        if (!MimesPermitidos.Contains(mime))
            return (null, null, null, "Formato não suportado. Use GIF, PNG, JPEG ou WebP.");

        var nome = form["nome"].ToString();
        if (string.IsNullOrWhiteSpace(nome))
            nome = Path.GetFileNameWithoutExtension(arquivo.FileName);
        if (string.IsNullOrWhiteSpace(nome))
            nome = "GIF";

        await using var stream = arquivo.OpenReadStream();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        return (ms.ToArray(), mime, nome, null);
    }

    private static string ResolverMime(IFormFile arquivo)
    {
        var mime = arquivo.ContentType?.Split(';')[0].Trim() ?? "";
        if (MimesPermitidos.Contains(mime))
            return mime;

        var ext = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        return ext switch
        {
            ".gif" => "image/gif",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => string.IsNullOrWhiteSpace(mime) ? "image/gif" : mime,
        };
    }
}
