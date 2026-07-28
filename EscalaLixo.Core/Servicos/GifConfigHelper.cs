using EscalaLixo.Modelos;

namespace EscalaLixo.Servicos;

public static class GifConfigHelper
{
    public static GifAnexo? ResolverPrevia(ConfiguracaoApp cfg)
    {
        if (cfg.TemGifPreviaArquivo)
            return CriarAnexo(cfg.GifPreviaSemanal!, cfg.GifPreviaMime, "previa");

        return null;
    }

    public static GifAnexo? ResolverDiarioProprio(ConfiguracaoApp cfg)
    {
        if (cfg.TemGifDiarioArquivo)
            return CriarAnexo(cfg.GifDiario!, cfg.GifDiarioMime, "diario");

        return null;
    }

    public static GifAnexo? ResolverDiario(ConfiguracaoApp cfg)
    {
        if (cfg.TemGifDiarioArquivo)
            return CriarAnexo(cfg.GifDiario!, cfg.GifDiarioMime, "diario");

        if (cfg.TemGifPreviaArquivo)
            return CriarAnexo(cfg.GifPreviaSemanal!, cfg.GifPreviaMime, "previa");

        return null;
    }

    public static string? ResolverUrlPrevia(ConfiguracaoApp cfg) =>
        cfg.TemGifPreviaArquivo ? null : VazioParaNull(cfg.UrlGifPreviaSemanal);

    public static string? ResolverUrlDiario(ConfiguracaoApp cfg)
    {
        if (cfg.TemGifDiarioArquivo || cfg.TemGifPreviaArquivo)
            return null;

        var diario = VazioParaNull(cfg.UrlGifDiario);
        return diario ?? VazioParaNull(cfg.UrlGifPreviaSemanal);
    }

    private static GifAnexo CriarAnexo(byte[] dados, string mime, string prefixo)
    {
        var ext = mime switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/webp" => ".webp",
            _ => ".gif",
        };
        return new GifAnexo(dados, mime, $"{prefixo}{ext}");
    }

    private static string? VazioParaNull(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
