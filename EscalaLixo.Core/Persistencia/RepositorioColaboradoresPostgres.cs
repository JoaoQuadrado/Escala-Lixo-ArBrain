using EscalaLixo.Modelos;
using Npgsql;

namespace EscalaLixo.Servicos;

/// <summary>Colaboradores via PostgreSQL direto (Supabase pooler).</summary>
public sealed class RepositorioColaboradoresPostgres : IRepositorioColaboradores
{
    private readonly string _connectionString;

    private const string SelectColumns = """
        id, nome, usuario_discord, cargo, cor, foto_url, de_ferias, ausente, observacoes
        """;

    public RepositorioColaboradoresPostgres(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string é obrigatória.");
        _connectionString = connectionString;
    }

    public bool Disponivel => true;

    public async Task<List<Colaborador>> ListarAsync(CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);

        await using var cmd = new NpgsqlCommand(
            $"SELECT {SelectColumns} FROM public.colaboradores ORDER BY nome ASC",
            conn);

        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var lista = new List<Colaborador>();
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
            lista.Add(LerLinha(reader));
        return lista;
    }

    public async Task<Colaborador?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);

        await using var cmd = new NpgsqlCommand(
            $"SELECT {SelectColumns} FROM public.colaboradores WHERE id = @id LIMIT 1",
            conn);
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        return await reader.ReadAsync(ct).ConfigureAwait(false) ? LerLinha(reader) : null;
    }

    public async Task<Colaborador> CriarAsync(Colaborador colaborador, CancellationToken ct = default)
    {
        if (colaborador.Id == Guid.Empty)
            colaborador.Id = Guid.NewGuid();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);

        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO public.colaboradores
              (id, nome, usuario_discord, cargo, cor, foto_url, de_ferias, ausente, observacoes)
            VALUES
              (@id, @nome, @usuario_discord, @cargo, @cor, @foto_url, @de_ferias, @ausente, @observacoes)
            RETURNING id, nome, usuario_discord, cargo, cor, foto_url, de_ferias, ausente, observacoes
            """,
            conn);

        PreencherParametros(cmd, colaborador);

        try
        {
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            if (await reader.ReadAsync(ct).ConfigureAwait(false))
                return LerLinha(reader);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new InvalidOperationException(
                $"Já existe um colaborador com o nome \"{colaborador.Nome}\".", ex);
        }

        return colaborador;
    }

    public async Task<Colaborador> AtualizarAsync(Colaborador colaborador, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);

        await using var cmd = new NpgsqlCommand(
            """
            UPDATE public.colaboradores SET
              nome = @nome,
              usuario_discord = @usuario_discord,
              cargo = @cargo,
              cor = @cor,
              foto_url = @foto_url,
              de_ferias = @de_ferias,
              ausente = @ausente,
              observacoes = @observacoes
            WHERE id = @id
            RETURNING id, nome, usuario_discord, cargo, cor, foto_url, de_ferias, ausente, observacoes
            """,
            conn);

        PreencherParametros(cmd, colaborador);

        try
        {
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            if (await reader.ReadAsync(ct).ConfigureAwait(false))
                return LerLinha(reader);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new InvalidOperationException(
                $"Já existe um colaborador com o nome \"{colaborador.Nome}\".", ex);
        }

        throw new InvalidOperationException("Colaborador não encontrado.");
    }

    public async Task ExcluirAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);

        await using var cmd = new NpgsqlCommand(
            "DELETE FROM public.colaboradores WHERE id = @id",
            conn);
        cmd.Parameters.AddWithValue("id", id);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task SalvarTodosAsync(IReadOnlyList<Colaborador> colaboradores, CancellationToken ct = default)
    {
        ValidarDuplicatas(colaboradores);

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var tx = await conn.BeginTransactionAsync(ct).ConfigureAwait(false);

        try
        {
            var atuais = await ListarInternoAsync(conn, tx, ct).ConfigureAwait(false);
            var novosIds = colaboradores
                .Select(c => c.Id)
                .Where(id => id != Guid.Empty)
                .ToHashSet();

            foreach (var atual in atuais)
            {
                if (!novosIds.Contains(atual.Id))
                {
                    await using var del = new NpgsqlCommand(
                        "DELETE FROM public.colaboradores WHERE id = @id",
                        conn, tx);
                    del.Parameters.AddWithValue("id", atual.Id);
                    await del.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                }
            }

            foreach (var c in colaboradores)
            {
                if (c.Id == Guid.Empty || atuais.All(a => a.Id != c.Id))
                    await InserirInternoAsync(conn, tx, c, ct).ConfigureAwait(false);
                else
                    await AtualizarInternoAsync(conn, tx, c, ct).ConfigureAwait(false);
            }

            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }

    private static async Task<List<Colaborador>> ListarInternoAsync(
        NpgsqlConnection conn, NpgsqlTransaction tx, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            $"SELECT {SelectColumns} FROM public.colaboradores ORDER BY nome ASC",
            conn, tx);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var lista = new List<Colaborador>();
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
            lista.Add(LerLinha(reader));
        return lista;
    }

    private static async Task InserirInternoAsync(
        NpgsqlConnection conn, NpgsqlTransaction tx, Colaborador c, CancellationToken ct)
    {
        if (c.Id == Guid.Empty) c.Id = Guid.NewGuid();
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO public.colaboradores
              (id, nome, usuario_discord, cargo, cor, foto_url, de_ferias, ausente, observacoes)
            VALUES
              (@id, @nome, @usuario_discord, @cargo, @cor, @foto_url, @de_ferias, @ausente, @observacoes)
            """,
            conn, tx);
        PreencherParametros(cmd, c);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static async Task AtualizarInternoAsync(
        NpgsqlConnection conn, NpgsqlTransaction tx, Colaborador c, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            """
            UPDATE public.colaboradores SET
              nome = @nome, usuario_discord = @usuario_discord, cargo = @cargo, cor = @cor,
              foto_url = @foto_url, de_ferias = @de_ferias, ausente = @ausente, observacoes = @observacoes
            WHERE id = @id
            """,
            conn, tx);
        PreencherParametros(cmd, c);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static Colaborador LerLinha(NpgsqlDataReader r) => new()
    {
        Id = r.GetGuid(0),
        Nome = r.GetString(1),
        UsuarioDiscord = r.IsDBNull(2) ? "" : r.GetString(2),
        Cargo = r.IsDBNull(3) ? "Auxiliar" : r.GetString(3),
        Cor = r.IsDBNull(4) ? "#FFC300" : r.GetString(4),
        FotoUrl = r.IsDBNull(5) ? null : r.GetString(5),
        DeFerias = !r.IsDBNull(6) && r.GetBoolean(6),
        Ausente = !r.IsDBNull(7) && r.GetBoolean(7),
        Observacoes = r.IsDBNull(8) ? null : r.GetString(8),
    };

    private static void PreencherParametros(NpgsqlCommand cmd, Colaborador c)
    {
        cmd.Parameters.AddWithValue("id", c.Id);
        cmd.Parameters.AddWithValue("nome", c.Nome.Trim());
        cmd.Parameters.AddWithValue("usuario_discord", c.UsuarioDiscord?.Trim() ?? "");
        cmd.Parameters.AddWithValue("cargo", c.Cargo?.Trim() ?? "Auxiliar");
        cmd.Parameters.AddWithValue("cor", c.Cor?.Trim() ?? "#FFC300");
        cmd.Parameters.AddWithValue("foto_url", (object?)c.FotoUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("de_ferias", c.DeFerias);
        cmd.Parameters.AddWithValue("ausente", c.Ausente);
        cmd.Parameters.AddWithValue("observacoes", (object?)c.Observacoes ?? DBNull.Value);
    }

    private static void ValidarDuplicatas(IReadOnlyList<Colaborador> colaboradores)
    {
        var duplicados = colaboradores
            .GroupBy(c => c.Nome.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicados.Count > 0)
            throw new InvalidOperationException(
                $"Nomes duplicados: {string.Join(", ", duplicados)}");
    }
}
