using EscalaLixo.Modelos;

namespace EscalaLixo.Servicos;

public sealed class ServicoBibliotecaGifs
{
    private readonly RepositorioBibliotecaGifs _gifs;
    private readonly RepositorioConfiguracaoAppPostgres _config;
    private readonly SemaphoreSlim _bloqueio = new(1, 1);

    public ServicoBibliotecaGifs(RepositorioBibliotecaGifs gifs, RepositorioConfiguracaoAppPostgres config)
    {
        _gifs = gifs;
        _config = config;
    }

    public async Task<IReadOnlyList<BibliotecaGifResumo>> ListarAsync(CancellationToken ct = default)
    {
        await _bloqueio.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await _gifs.ListarAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _bloqueio.Release();
        }
    }

    public async Task<BibliotecaGif?> ObterAsync(Guid id, CancellationToken ct = default)
    {
        await _bloqueio.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await _gifs.ObterAsync(id, ct).ConfigureAwait(false);
        }
        finally
        {
            _bloqueio.Release();
        }
    }

    public async Task<(Guid Id, ConfiguracaoApp Config)> AdicionarAsync(
        string nome,
        byte[] dados,
        string mime,
        CancellationToken ct = default)
    {
        await _bloqueio.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var id = await _gifs.InserirAsync(nome, dados, mime, ct).ConfigureAwait(false);
            var cfg = await _config.LerAsync(ct).ConfigureAwait(false) ?? new ConfiguracaoApp();

            if (cfg.GifPreviaId is null)
                cfg.GifPreviaId = id;

            await _config.SalvarAsync(cfg, ct).ConfigureAwait(false);
            return (id, cfg);
        }
        finally
        {
            _bloqueio.Release();
        }
    }

    public async Task RemoverAsync(Guid id, CancellationToken ct = default)
    {
        await _bloqueio.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await _gifs.RemoverAsync(id, ct).ConfigureAwait(false);
        }
        finally
        {
            _bloqueio.Release();
        }
    }

    public async Task<ConfiguracaoApp> SelecionarPreviaAsync(Guid? id, CancellationToken ct = default)
    {
        await _bloqueio.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (id is Guid gid && !await _gifs.ExisteAsync(gid, ct).ConfigureAwait(false))
                throw new InvalidOperationException("GIF não encontrado na biblioteca.");

            var cfg = await _config.LerAsync(ct).ConfigureAwait(false) ?? new ConfiguracaoApp();
            cfg.GifPreviaId = id;
            await _config.SalvarAsync(cfg, ct).ConfigureAwait(false);
            return cfg;
        }
        finally
        {
            _bloqueio.Release();
        }
    }

    public async Task<ConfiguracaoApp> SelecionarDiarioAsync(Guid? id, CancellationToken ct = default)
    {
        await _bloqueio.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (id is Guid gid && !await _gifs.ExisteAsync(gid, ct).ConfigureAwait(false))
                throw new InvalidOperationException("GIF não encontrado na biblioteca.");

            var cfg = await _config.LerAsync(ct).ConfigureAwait(false) ?? new ConfiguracaoApp();
            cfg.GifDiarioId = id;
            await _config.SalvarAsync(cfg, ct).ConfigureAwait(false);
            return cfg;
        }
        finally
        {
            _bloqueio.Release();
        }
    }

    public async Task HidratarGifsAsync(ConfiguracaoApp cfg, CancellationToken ct = default)
    {
        if (cfg.GifPreviaId is Guid previaId)
        {
            var gif = await _gifs.ObterAsync(previaId, ct).ConfigureAwait(false);
            if (gif is not null)
            {
                cfg.GifPreviaSemanal = gif.Dados;
                cfg.GifPreviaMime = gif.Mime;
            }
        }

        if (cfg.GifDiarioId is Guid diarioId)
        {
            var gif = await _gifs.ObterAsync(diarioId, ct).ConfigureAwait(false);
            if (gif is not null)
            {
                cfg.GifDiario = gif.Dados;
                cfg.GifDiarioMime = gif.Mime;
            }
        }
    }
}
