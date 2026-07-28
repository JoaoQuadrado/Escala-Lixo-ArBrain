using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EscalaLixo.Modelos;
using Npgsql;
using NpgsqlTypes;

namespace EscalaLixo.Servicos;

public sealed class RepositorioEscalaHistoricoPostgres : IRepositorioEscalaHistorico
{
    private static readonly string[] DiasUteisPt =
    [
        "segunda-feira",
        "terça-feira",
        "quarta-feira",
        "quinta-feira",
        "sexta-feira",
    ];

    private readonly string _connectionString;

    public RepositorioEscalaHistoricoPostgres(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string é obrigatória.");
        _connectionString = connectionString;
    }

    public bool UsaPostgres => true;

    public bool SuportaMovimentoAtomico => UsaPostgres;

    private bool _posicoesDisponiveis;

    public async Task<EscalaSemana?> LerEscalaAsync(CancellationToken ct = default)
    {
        await using var conn = await AbrirAsync(ct).ConfigureAwait(false);
        _posicoesDisponiveis = await TabelaPosicoesExisteAsync(conn, ct).ConfigureAwait(false);

        if (_posicoesDisponiveis)
        {
            var posicoes = await LerPosicoesAsync(conn, null, ct).ConfigureAwait(false);
            if (posicoes.Count > 0)
                return await MontarEscalaDePosicoesAsync(conn, posicoes, ct).ConfigureAwait(false);
        }

        await using var cmd = new NpgsqlCommand(
            "SELECT inicio_semana, dias, fila_espera, bloqueados FROM public.escala_ativa WHERE id = 1",
            conn);

        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var escala = LerEscalaDoReader(reader);
            if (_posicoesDisponiveis)
                await SalvarEscalaAsync(escala, ct).ConfigureAwait(false);
            return escala;
        }

        return null;
    }

    public async Task MoverColaboradorAtomicoAsync(
        Guid colaboradorId,
        string slotDestino,
        int ordem,
        CancellationToken ct = default)
    {
        await using var conn = await AbrirAsync(ct).ConfigureAwait(false);
        if (!await TabelaPosicoesExisteAsync(conn, ct).ConfigureAwait(false))
            throw new NotSupportedException("Tabela escala_posicoes não disponível.");

        await using var tx = await conn.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            await using (var cmd = new NpgsqlCommand(
                "SELECT public.mover_colaborador_escala(@cid, @slot, @ordem)",
                conn, tx))
            {
                cmd.Parameters.AddWithValue("cid", colaboradorId);
                cmd.Parameters.AddWithValue("slot", slotDestino);
                cmd.Parameters.AddWithValue("ordem", ordem);
                await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            await AtualizarCacheJsonAsync(conn, tx, ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);
            _posicoesDisponiveis = true;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw new InvalidOperationException(
                "Colaborador duplicado na escala — cada pessoa só pode estar num lugar por semana.", ex);
        }
        catch (PostgresException ex) when (
            ex.Message.Contains("DUPLA_CHEIA", StringComparison.Ordinal) ||
            ex.Message.Contains("SLOT_INVALIDO", StringComparison.Ordinal) ||
            ex.Message.Contains("COLABORADOR_INEXISTENTE", StringComparison.Ordinal))
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw new InvalidOperationException(ex.MessageText, ex);
        }
        catch
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }

    public async Task TrocarColaboradoresAtomicoAsync(
        Guid colaboradorIdA,
        Guid colaboradorIdB,
        CancellationToken ct = default)
    {
        await using var conn = await AbrirAsync(ct).ConfigureAwait(false);
        if (!await TabelaPosicoesExisteAsync(conn, ct).ConfigureAwait(false))
            throw new NotSupportedException("Tabela escala_posicoes não disponível.");

        await using var tx = await conn.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            var posA = await LerPosicaoColaboradorAsync(conn, tx, colaboradorIdA, ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Colaborador A não encontrado na escala.");
            var posB = await LerPosicaoColaboradorAsync(conn, tx, colaboradorIdB, ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Colaborador B não encontrado na escala.");

            await ExecutarMoverNoTxAsync(conn, tx, colaboradorIdA, "fila_espera", 999_999, ct).ConfigureAwait(false);
            await ExecutarMoverNoTxAsync(conn, tx, colaboradorIdB, posA.Slot, posA.Ordem, ct).ConfigureAwait(false);
            await ExecutarMoverNoTxAsync(conn, tx, colaboradorIdA, posB.Slot, posB.Ordem, ct).ConfigureAwait(false);

            await AtualizarCacheJsonAsync(conn, tx, ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);
            _posicoesDisponiveis = true;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw new InvalidOperationException(
                "Colaborador duplicado na escala — cada pessoa só pode estar num lugar por semana.", ex);
        }
        catch (PostgresException ex) when (
            ex.Message.Contains("DUPLA_CHEIA", StringComparison.Ordinal) ||
            ex.Message.Contains("SLOT_INVALIDO", StringComparison.Ordinal) ||
            ex.Message.Contains("COLABORADOR_INEXISTENTE", StringComparison.Ordinal))
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw new InvalidOperationException(ex.MessageText, ex);
        }
        catch
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }

    private sealed record PosicaoColaborador(string Slot, int Ordem);

    private static async Task<PosicaoColaborador?> LerPosicaoColaboradorAsync(
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        Guid colaboradorId,
        CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            """
            SELECT slot::text, ordem
            FROM public.escala_posicoes
            WHERE escala_id = 1 AND colaborador_id = @cid
            """,
            conn, tx);
        cmd.Parameters.AddWithValue("cid", colaboradorId);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
            return null;
        return new PosicaoColaborador(reader.GetString(0), reader.GetInt32(1));
    }

    private static async Task ExecutarMoverNoTxAsync(
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        Guid colaboradorId,
        string slotDestino,
        int ordem,
        CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT public.mover_colaborador_escala(@cid, @slot, @ordem)",
            conn, tx);
        cmd.Parameters.AddWithValue("cid", colaboradorId);
        cmd.Parameters.AddWithValue("slot", slotDestino);
        cmd.Parameters.AddWithValue("ordem", ordem);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private async Task AtualizarCacheJsonAsync(
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        CancellationToken ct)
    {
        var posicoes = await LerPosicoesAsync(conn, tx, ct).ConfigureAwait(false);
        var escala = await MontarEscalaDePosicoesAsync(conn, posicoes, ct).ConfigureAwait(false);

        var json = JsonSerializer.Serialize(escala, JsonOpcoes.Gravar);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        var diasJson = JsonSerializer.Serialize(escala.Dias, JsonOpcoes.Gravar);
        var filaJson = JsonSerializer.Serialize(escala.FilaEspera, JsonOpcoes.Gravar);
        var bloqueadosJson = JsonSerializer.Serialize(escala.Bloqueados, JsonOpcoes.Gravar);

        if (!DateOnly.TryParse(escala.InicioDaSemana, out var inicio))
            inicio = DateOnly.FromDateTime(DateTime.Today);

        await using var cmd = new NpgsqlCommand(
            """
            UPDATE public.escala_ativa SET
              inicio_semana = @inicio,
              dias = @dias::jsonb,
              fila_espera = @fila::jsonb,
              bloqueados = @bloqueados::jsonb,
              conteudo_hash = @hash,
              updated_at = now()
            WHERE id = 1
            """,
            conn, tx);

        cmd.Parameters.AddWithValue("inicio", inicio);
        cmd.Parameters.Add("dias", NpgsqlDbType.Jsonb).Value = diasJson;
        cmd.Parameters.Add("fila", NpgsqlDbType.Jsonb).Value = filaJson;
        cmd.Parameters.Add("bloqueados", NpgsqlDbType.Jsonb).Value = bloqueadosJson;
        cmd.Parameters.AddWithValue("hash", hash);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task<string> ObterHashEscalaAsync(CancellationToken ct = default)
    {
        await using var conn = await AbrirAsync(ct).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(
            "SELECT conteudo_hash FROM public.escala_ativa WHERE id = 1",
            conn);
        var result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
        if (result is string hash && !string.IsNullOrEmpty(hash))
            return hash;

        return string.Empty;
    }

    public async Task SalvarEscalaAsync(EscalaSemana escala, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(escala, JsonOpcoes.Gravar);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        var diasJson = JsonSerializer.Serialize(escala.Dias, JsonOpcoes.Gravar);
        var filaJson = JsonSerializer.Serialize(escala.FilaEspera, JsonOpcoes.Gravar);
        var bloqueadosJson = JsonSerializer.Serialize(escala.Bloqueados, JsonOpcoes.Gravar);

        if (!DateOnly.TryParse(escala.InicioDaSemana, out var inicio))
            inicio = DateOnly.FromDateTime(DateTime.Today);

        await using var conn = await AbrirAsync(ct).ConfigureAwait(false);
        await using var tx = await conn.BeginTransactionAsync(ct).ConfigureAwait(false);

        try
        {
            if (await TabelaPosicoesExisteAsync(conn, ct).ConfigureAwait(false))
                await SincronizarPosicoesAsync(conn, tx, escala, ct).ConfigureAwait(false);

            await using (var cmd = new NpgsqlCommand(
                """
                INSERT INTO public.escala_ativa (id, inicio_semana, dias, fila_espera, bloqueados, conteudo_hash)
                VALUES (1, @inicio, @dias::jsonb, @fila::jsonb, @bloqueados::jsonb, @hash)
                ON CONFLICT (id) DO UPDATE SET
                  inicio_semana = EXCLUDED.inicio_semana,
                  dias = EXCLUDED.dias,
                  fila_espera = EXCLUDED.fila_espera,
                  bloqueados = EXCLUDED.bloqueados,
                  conteudo_hash = EXCLUDED.conteudo_hash,
                  updated_at = now()
                """,
                conn, tx))
            {
                cmd.Parameters.AddWithValue("inicio", inicio);
                cmd.Parameters.Add("dias", NpgsqlDbType.Jsonb).Value = diasJson;
                cmd.Parameters.Add("fila", NpgsqlDbType.Jsonb).Value = filaJson;
                cmd.Parameters.Add("bloqueados", NpgsqlDbType.Jsonb).Value = bloqueadosJson;
                cmd.Parameters.AddWithValue("hash", hash);
                await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            await tx.CommitAsync(ct).ConfigureAwait(false);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw new InvalidOperationException(
                "Colaborador duplicado na escala — cada pessoa só pode estar num lugar por semana.", ex);
        }
        catch (PostgresException ex) when (ex.Message.Contains("DUPLA_CHEIA", StringComparison.Ordinal))
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw new InvalidOperationException(ex.MessageText, ex);
        }
        catch
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<HistoricoPares> LerHistoricoAsync(CancellationToken ct = default)
    {
        await using var conn = await AbrirAsync(ct).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(
            "SELECT contagens_pares, sequencias_consecutivas FROM public.historico_pares WHERE id = 1",
            conn);

        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (await reader.ReadAsync(ct).ConfigureAwait(false))
            return LerHistoricoDoReader(reader);

        return new HistoricoPares();
    }

    public async Task SalvarHistoricoAsync(HistoricoPares historico, CancellationToken ct = default)
    {
        var contagensJson = JsonSerializer.Serialize(historico.ContagensDosPares, JsonOpcoes.Gravar);
        var sequenciasJson = JsonSerializer.Serialize(historico.SequenciasConsecutivas, JsonOpcoes.Gravar);

        await using var conn = await AbrirAsync(ct).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO public.historico_pares (id, contagens_pares, sequencias_consecutivas)
            VALUES (1, @contagens::jsonb, @sequencias::jsonb)
            ON CONFLICT (id) DO UPDATE SET
              contagens_pares = EXCLUDED.contagens_pares,
              sequencias_consecutivas = EXCLUDED.sequencias_consecutivas,
              updated_at = now()
            """,
            conn);

        cmd.Parameters.Add("contagens", NpgsqlDbType.Jsonb).Value = contagensJson;
        cmd.Parameters.Add("sequencias", NpgsqlDbType.Jsonb).Value = sequenciasJson;
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private async Task SincronizarPosicoesAsync(
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        EscalaSemana escala,
        CancellationToken ct)
    {
        var nomeParaId = await CarregarMapaNomesAsync(conn, tx, ct).ConfigureAwait(false);

        await using (var del = new NpgsqlCommand(
            "DELETE FROM public.escala_posicoes WHERE escala_id = 1", conn, tx))
            await del.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

        var vistos = new HashSet<Guid>();

        foreach (var dia in escala.Dias)
        {
            var slot = dia.DiaDaSemana.Trim();
            if (!DiasUteisPt.Contains(slot, StringComparer.OrdinalIgnoreCase))
                continue;

            for (var i = 0; i < dia.Nomes.Count; i++)
            {
                var nome = dia.Nomes[i]?.Trim() ?? "";
                if (string.IsNullOrEmpty(nome))
                    continue;
                if (!nomeParaId.TryGetValue(nome, out var colaboradorId))
                    continue;
                if (!vistos.Add(colaboradorId))
                    throw new InvalidOperationException($"Colaborador duplicado: \"{nome}\".");

                await InserirPosicaoAsync(conn, tx, colaboradorId, slot, i, ct).ConfigureAwait(false);
            }
        }

        for (var i = 0; i < escala.Bloqueados.Count; i++)
        {
            var nome = escala.Bloqueados[i]?.Trim() ?? "";
            if (string.IsNullOrEmpty(nome))
                continue;
            if (!nomeParaId.TryGetValue(nome, out var colaboradorId))
                continue;
            if (!vistos.Add(colaboradorId))
                throw new InvalidOperationException($"Colaborador duplicado: \"{nome}\".");

            await InserirPosicaoAsync(conn, tx, colaboradorId, "bloqueados", i, ct).ConfigureAwait(false);
        }

        for (var i = 0; i < escala.FilaEspera.Count; i++)
        {
            var nome = escala.FilaEspera[i]?.Trim() ?? "";
            if (string.IsNullOrEmpty(nome))
                continue;
            if (!nomeParaId.TryGetValue(nome, out var colaboradorId))
                continue;
            if (!vistos.Add(colaboradorId))
                throw new InvalidOperationException($"Colaborador duplicado: \"{nome}\".");

            await InserirPosicaoAsync(conn, tx, colaboradorId, "fila_espera", i, ct).ConfigureAwait(false);
        }
    }

    private static async Task InserirPosicaoAsync(
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        Guid colaboradorId,
        string slot,
        int ordem,
        CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO public.escala_posicoes (escala_id, colaborador_id, slot, ordem)
            VALUES (1, @cid, @slot::public.escala_slot, @ordem)
            """,
            conn, tx);
        cmd.Parameters.AddWithValue("cid", colaboradorId);
        cmd.Parameters.AddWithValue("slot", slot);
        cmd.Parameters.AddWithValue("ordem", ordem);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static async Task<Dictionary<string, Guid>> CarregarMapaNomesAsync(
        NpgsqlConnection conn,
        NpgsqlTransaction? tx,
        CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand("SELECT id, nome FROM public.colaboradores", conn, tx);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var mapa = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
            mapa[reader.GetString(1).Trim()] = reader.GetGuid(0);
        return mapa;
    }

    private static async Task<List<(string Slot, string Nome, int Ordem)>> LerPosicoesAsync(
        NpgsqlConnection conn,
        NpgsqlTransaction? tx,
        CancellationToken ct)
    {
        const string sql = """
            SELECT p.slot::text, c.nome, p.ordem
            FROM public.escala_posicoes p
            JOIN public.colaboradores c ON c.id = p.colaborador_id
            WHERE p.escala_id = 1
            ORDER BY p.slot, p.ordem
            """;

        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var lista = new List<(string, string, int)>();
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
            lista.Add((reader.GetString(0), reader.GetString(1), reader.GetInt32(2)));
        return lista;
    }

    private async Task<EscalaSemana> MontarEscalaDePosicoesAsync(
        NpgsqlConnection conn,
        List<(string Slot, string Nome, int Ordem)> posicoes,
        CancellationToken ct)
    {
        var inicio = DateTime.Today.ToString("yyyy-MM-dd");
        await using (var cmd = new NpgsqlCommand(
            "SELECT inicio_semana FROM public.escala_ativa WHERE id = 1", conn))
        {
            var result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
            if (result is DateTime dt)
                inicio = dt.ToString("yyyy-MM-dd");
        }

        var dias = DiasUteisPt
            .Select(d => new AtribuicaoDia { DiaDaSemana = d, Nomes = [] })
            .ToList();
        var bloqueados = new List<string>();
        var filaEspera = new List<string>();

        foreach (var grupo in posicoes.GroupBy(p => p.Slot, StringComparer.OrdinalIgnoreCase))
        {
            if (string.Equals(grupo.Key, "bloqueados", StringComparison.OrdinalIgnoreCase))
            {
                bloqueados.AddRange(grupo.OrderBy(p => p.Ordem).Select(p => p.Nome));
                continue;
            }

            if (string.Equals(grupo.Key, "fila_espera", StringComparison.OrdinalIgnoreCase))
            {
                filaEspera.AddRange(grupo.OrderBy(p => p.Ordem).Select(p => p.Nome));
                continue;
            }

            var dia = dias.FirstOrDefault(d =>
                string.Equals(d.DiaDaSemana, grupo.Key, StringComparison.OrdinalIgnoreCase));
            if (dia is null)
                continue;

            dia.Nomes = grupo.OrderBy(p => p.Ordem).Select(p => p.Nome).ToList();
        }

        return new EscalaSemana
        {
            InicioDaSemana = inicio,
            Dias = dias,
            FilaEspera = filaEspera,
            Bloqueados = bloqueados,
        };
    }

    private static async Task<bool> TabelaPosicoesExisteAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            """
            SELECT 1 FROM information_schema.tables
            WHERE table_schema = 'public' AND table_name = 'escala_posicoes'
            LIMIT 1
            """,
            conn);
        return await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false) is not null;
    }

    private async Task<NpgsqlConnection> AbrirAsync(CancellationToken ct)
    {
        var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        return conn;
    }

    private static EscalaSemana LerEscalaDoReader(NpgsqlDataReader reader)
    {
        var inicio = reader.GetDateTime(0).ToString("yyyy-MM-dd");
        var diasJson = reader.GetString(1);
        var filaJson = reader.GetString(2);
        var bloqueadosJson = reader.FieldCount > 3 && !reader.IsDBNull(3)
            ? reader.GetString(3)
            : "[]";

        return new EscalaSemana
        {
            InicioDaSemana = inicio,
            Dias = JsonSerializer.Deserialize<List<AtribuicaoDia>>(diasJson, JsonOpcoes.Ler) ?? [],
            FilaEspera = JsonSerializer.Deserialize<List<string>>(filaJson, JsonOpcoes.Ler) ?? [],
            Bloqueados = JsonSerializer.Deserialize<List<string>>(bloqueadosJson, JsonOpcoes.Ler) ?? [],
        };
    }

    private static HistoricoPares LerHistoricoDoReader(NpgsqlDataReader reader)
    {
        var contagensJson = reader.GetString(0);
        var sequenciasJson = reader.GetString(1);

        return new HistoricoPares
        {
            ContagensDosPares = JsonSerializer.Deserialize<Dictionary<string, int>>(contagensJson, JsonOpcoes.Ler)
                ?? new Dictionary<string, int>(StringComparer.Ordinal),
            SequenciasConsecutivas = JsonSerializer.Deserialize<Dictionary<string, int>>(sequenciasJson, JsonOpcoes.Ler)
                ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
        };
    }

    public async Task ArquivarEscalaAsync(EscalaSemana escala, string motivo, CancellationToken ct = default)
    {
        var diasJson = JsonSerializer.Serialize(escala.Dias, JsonOpcoes.Gravar);
        var filaJson = JsonSerializer.Serialize(escala.FilaEspera, JsonOpcoes.Gravar);
        var bloqueadosJson = JsonSerializer.Serialize(escala.Bloqueados, JsonOpcoes.Gravar);

        if (!DateOnly.TryParse(escala.InicioDaSemana, out var inicio))
            inicio = DateOnly.FromDateTime(DateTime.Today);

        await using var conn = await AbrirAsync(ct).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO public.escala_historico (inicio_semana, dias, fila_espera, bloqueados, motivo)
            VALUES (@inicio, @dias::jsonb, @fila::jsonb, @bloqueados::jsonb, @motivo)
            """,
            conn);

        cmd.Parameters.AddWithValue("inicio", inicio);
        cmd.Parameters.Add("dias", NpgsqlDbType.Jsonb).Value = diasJson;
        cmd.Parameters.Add("fila", NpgsqlDbType.Jsonb).Value = filaJson;
        cmd.Parameters.Add("bloqueados", NpgsqlDbType.Jsonb).Value = bloqueadosJson;
        cmd.Parameters.AddWithValue("motivo", motivo.Trim());
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task<List<EscalaHistoricoResumo>> ListarHistoricoEscalasAsync(int limite = 50, CancellationToken ct = default)
    {
        limite = Math.Clamp(limite, 1, 200);

        await using var conn = await AbrirAsync(ct).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(
            """
            SELECT id, inicio_semana, motivo, arquivado_em, dias
            FROM public.escala_historico
            ORDER BY arquivado_em DESC
            LIMIT @limite
            """,
            conn);
        cmd.Parameters.AddWithValue("limite", limite);

        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        var lista = new List<EscalaHistoricoResumo>();
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var diasJson = reader.GetString(4);
            var dias = JsonSerializer.Deserialize<List<AtribuicaoDia>>(diasJson, JsonOpcoes.Ler) ?? [];
            var total = dias.Sum(d => d.Nomes.Count(n => !string.IsNullOrWhiteSpace(n)));

            lista.Add(new EscalaHistoricoResumo
            {
                Id = reader.GetGuid(0),
                InicioSemana = reader.GetDateTime(1).ToString("yyyy-MM-dd"),
                Motivo = reader.GetString(2),
                ArquivadoEm = reader.GetDateTime(3),
                TotalEscalados = total,
            });
        }

        return lista;
    }

    public async Task<EscalaHistoricoCompleta?> LerHistoricoEscalaAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = await AbrirAsync(ct).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(
            """
            SELECT id, inicio_semana, dias, fila_espera, bloqueados, motivo, arquivado_em
            FROM public.escala_historico
            WHERE id = @id
            """,
            conn);
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false))
            return null;

        var escala = new EscalaSemana
        {
            InicioDaSemana = reader.GetDateTime(1).ToString("yyyy-MM-dd"),
            Dias = JsonSerializer.Deserialize<List<AtribuicaoDia>>(reader.GetString(2), JsonOpcoes.Ler) ?? [],
            FilaEspera = JsonSerializer.Deserialize<List<string>>(reader.GetString(3), JsonOpcoes.Ler) ?? [],
            Bloqueados = JsonSerializer.Deserialize<List<string>>(reader.GetString(4), JsonOpcoes.Ler) ?? [],
        };

        return new EscalaHistoricoCompleta
        {
            Id = reader.GetGuid(0),
            Escala = escala,
            Motivo = reader.GetString(5),
            ArquivadoEm = reader.GetDateTime(6),
        };
    }
}
