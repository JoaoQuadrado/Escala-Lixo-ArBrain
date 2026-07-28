using EscalaLixo.Modelos;
using Npgsql;

namespace EscalaLixo.Servicos;

public sealed class RepositorioBibliotecaGifs
{
    private readonly string _connectionString;

    public RepositorioBibliotecaGifs(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string é obrigatória.");
        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<BibliotecaGifResumo>> ListarAsync(CancellationToken ct = default)
    {
        await using var conn = await AbrirAsync(ct).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(
            """
            SELECT id, nome, mime, created_at
            FROM public.biblioteca_gifs
            ORDER BY created_at DESC
            """,
            conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var lista = new List<BibliotecaGifResumo>();
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            lista.Add(new BibliotecaGifResumo
            {
                Id = reader.GetGuid(0),
                Nome = reader.GetString(1),
                Mime = reader.GetString(2),
                CriadoEm = reader.GetFieldValue<DateTimeOffset>(3),
            });
        }
        return lista;
    }

    public async Task<BibliotecaGif?> ObterAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = await AbrirAsync(ct).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(
            """
            SELECT id, nome, dados, mime, created_at
            FROM public.biblioteca_gifs
            WHERE id = @id
            """,
            conn);
        cmd.Parameters.AddWithValue("id", id);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
            return null;

        return new BibliotecaGif
        {
            Id = reader.GetGuid(0),
            Nome = reader.GetString(1),
            Dados = reader.GetFieldValue<byte[]>(2),
            Mime = reader.GetString(3),
            CriadoEm = reader.GetFieldValue<DateTimeOffset>(4),
        };
    }

    public async Task<bool> ExisteAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = await AbrirAsync(ct).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(
            "SELECT 1 FROM public.biblioteca_gifs WHERE id = @id",
            conn);
        cmd.Parameters.AddWithValue("id", id);
        var result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
        return result is not null;
    }

    public async Task<Guid> InserirAsync(string nome, byte[] dados, string mime, CancellationToken ct = default)
    {
        await using var conn = await AbrirAsync(ct).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO public.biblioteca_gifs (nome, dados, mime)
            VALUES (@nome, @dados, @mime)
            RETURNING id
            """,
            conn);
        cmd.Parameters.AddWithValue("nome", nome.Trim());
        cmd.Parameters.AddWithValue("dados", dados);
        cmd.Parameters.AddWithValue("mime", mime.Trim());
        var id = (Guid)(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false))!;
        return id;
    }

    public async Task RemoverAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = await AbrirAsync(ct).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(
            "DELETE FROM public.biblioteca_gifs WHERE id = @id",
            conn);
        cmd.Parameters.AddWithValue("id", id);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private async Task<NpgsqlConnection> AbrirAsync(CancellationToken ct)
    {
        var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        return conn;
    }
}
