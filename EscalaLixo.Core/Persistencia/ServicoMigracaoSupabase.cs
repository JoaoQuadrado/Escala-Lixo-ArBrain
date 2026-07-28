using Npgsql;

namespace EscalaLixo.Servicos;

public sealed class ServicoMigracaoSupabase
{
    private readonly string _connectionString;
    private readonly string _pastaMigrations;

    public ServicoMigracaoSupabase(string connectionString, string pastaMigrations)
    {
        _connectionString = connectionString;
        _pastaMigrations = pastaMigrations;
    }

    public async Task<IReadOnlyList<string>> AplicarPendentesAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(_pastaMigrations))
            throw new DirectoryNotFoundException($"Pasta de migrations não encontrada: {_pastaMigrations}");

        var arquivos = Directory.GetFiles(_pastaMigrations, "*.sql")
            .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
            .ToList();

        var aplicadas = new List<string>();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);

        await GarantirTabelaControloAsync(conn, ct).ConfigureAwait(false);

        foreach (var caminho in arquivos)
        {
            var id = Path.GetFileName(caminho);
            if (await JaAplicadaAsync(conn, id, ct).ConfigureAwait(false))
                continue;

            var sql = await File.ReadAllTextAsync(caminho, ct).ConfigureAwait(false);
            await using var tx = await conn.BeginTransactionAsync(ct).ConfigureAwait(false);
            try
            {
                await using (var cmd = new NpgsqlCommand(sql, conn, tx))
                    await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

                await using (var reg = new NpgsqlCommand(
                    "INSERT INTO public.schema_migrations (id) VALUES (@id) ON CONFLICT DO NOTHING",
                    conn, tx))
                {
                    reg.Parameters.AddWithValue("id", id);
                    await reg.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }

                await tx.CommitAsync(ct).ConfigureAwait(false);
                aplicadas.Add(id);
            }
            catch
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                throw;
            }
        }

        return aplicadas;
    }

    public async Task<DbStatusSupabase> ObterStatusAsync(CancellationToken ct = default)
    {
        var status = new DbStatusSupabase { Connected = false };

        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct).ConfigureAwait(false);
            status.Connected = true;

            await GarantirTabelaControloAsync(conn, ct).ConfigureAwait(false);

            status.MigrationsAplicadas = await ListarMigrationsAsync(conn, ct).ConfigureAwait(false);
            status.Tabelas = await ListarTabelasAsync(conn, ct).ConfigureAwait(false);

            if (status.Tabelas.Contains("colaboradores"))
            {
                await using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM public.colaboradores", conn);
                status.Colaboradores = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false));
            }
        }
        catch (Exception ex)
        {
            status.Erro = ex.Message;
        }

        return status;
    }

    private static async Task GarantirTabelaControloAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS public.schema_migrations (
              id text PRIMARY KEY,
              applied_at timestamptz NOT NULL DEFAULT now()
            );
            """;
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static async Task<bool> JaAplicadaAsync(NpgsqlConnection conn, string id, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT 1 FROM public.schema_migrations WHERE id = @id LIMIT 1",
            conn);
        cmd.Parameters.AddWithValue("id", id);
        return await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false) is not null;
    }

    private static async Task<List<string>> ListarMigrationsAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT id FROM public.schema_migrations ORDER BY id",
            conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var lista = new List<string>();
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
            lista.Add(reader.GetString(0));
        return lista;
    }

    private static async Task<List<string>> ListarTabelasAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        const string sql = """
            SELECT table_name FROM information_schema.tables
            WHERE table_schema = 'public'
              AND table_name IN (
                'colaboradores', 'escala_ativa', 'escala_posicoes', 'historico_pares',
                'escala_historico', 'configuracao_app', 'biblioteca_gifs', 'schema_migrations'
              )
            ORDER BY table_name
            """;
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var lista = new List<string>();
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
            lista.Add(reader.GetString(0));
        return lista;
    }
}

public sealed class DbStatusSupabase
{
    public bool Connected { get; set; }
    public string? Erro { get; set; }
    public int Colaboradores { get; set; }
    public List<string> MigrationsAplicadas { get; set; } = [];
    public List<string> Tabelas { get; set; } = [];

    public bool Pronto =>
        Connected &&
        Tabelas.Contains("colaboradores") &&
        Tabelas.Contains("escala_ativa") &&
        Tabelas.Contains("escala_posicoes") &&
        Tabelas.Contains("historico_pares");
}
