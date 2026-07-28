using EscalaLixo.Modelos;
using Npgsql;

namespace EscalaLixo.Servicos;

public sealed class RepositorioConfiguracaoAppPostgres
{
    private readonly string _connectionString;

    private const string SelectSql = """
        SELECT webhook_discord, token_bot_discord, id_servidor_discord,
               url_gif_previa_semanal, url_gif_diario, modelo_mensagem_diaria,
               intervalo_verificacao_minutos, hora_notificacao_padrao,
               hora_previa_semanal, hora_lembrete_diario,
               gif_previa_semanal, gif_previa_mime, gif_diario, gif_diario_mime,
               gif_previa_id, gif_diario_id
        FROM public.configuracao_app
        WHERE id = 1
        """;

    public RepositorioConfiguracaoAppPostgres(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string é obrigatória.");
        _connectionString = connectionString;
    }

    public async Task<ConfiguracaoApp?> LerAsync(CancellationToken ct = default)
    {
        await using var conn = await AbrirAsync(ct).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(SelectSql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        return await reader.ReadAsync(ct).ConfigureAwait(false) ? LerLinha(reader) : null;
    }

    public async Task SalvarAsync(ConfiguracaoApp config, CancellationToken ct = default)
    {
        await using var conn = await AbrirAsync(ct).ConfigureAwait(false);
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO public.configuracao_app (
              id, webhook_discord, token_bot_discord, id_servidor_discord,
              url_gif_previa_semanal, url_gif_diario, modelo_mensagem_diaria,
              intervalo_verificacao_minutos, hora_notificacao_padrao,
              hora_previa_semanal, hora_lembrete_diario,
              gif_previa_semanal, gif_previa_mime, gif_diario, gif_diario_mime,
              gif_previa_id, gif_diario_id
            ) VALUES (
              1, @webhook, @token, @guild, @url_previa, @url_diario, @modelo,
              @intervalo, @hora_padrao, @hora_previa, @hora_diario,
              @gif_previa, @gif_previa_mime, @gif_diario, @gif_diario_mime,
              @gif_previa_id, @gif_diario_id
            )
            ON CONFLICT (id) DO UPDATE SET
              webhook_discord = EXCLUDED.webhook_discord,
              token_bot_discord = EXCLUDED.token_bot_discord,
              id_servidor_discord = EXCLUDED.id_servidor_discord,
              url_gif_previa_semanal = EXCLUDED.url_gif_previa_semanal,
              url_gif_diario = EXCLUDED.url_gif_diario,
              modelo_mensagem_diaria = EXCLUDED.modelo_mensagem_diaria,
              intervalo_verificacao_minutos = EXCLUDED.intervalo_verificacao_minutos,
              hora_notificacao_padrao = EXCLUDED.hora_notificacao_padrao,
              hora_previa_semanal = EXCLUDED.hora_previa_semanal,
              hora_lembrete_diario = EXCLUDED.hora_lembrete_diario,
              gif_previa_semanal = EXCLUDED.gif_previa_semanal,
              gif_previa_mime = EXCLUDED.gif_previa_mime,
              gif_diario = EXCLUDED.gif_diario,
              gif_diario_mime = EXCLUDED.gif_diario_mime,
              gif_previa_id = EXCLUDED.gif_previa_id,
              gif_diario_id = EXCLUDED.gif_diario_id,
              updated_at = now()
            """,
            conn);

        PreencherParametros(cmd, config);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static void PreencherParametros(NpgsqlCommand cmd, ConfiguracaoApp c)
    {
        cmd.Parameters.AddWithValue("webhook", c.WebhookDiscord?.Trim() ?? "");
        cmd.Parameters.AddWithValue("token", c.TokenBotDiscord?.Trim() ?? "");
        cmd.Parameters.AddWithValue("guild", c.IdServidorDiscord?.Trim() ?? "");
        cmd.Parameters.AddWithValue("url_previa", c.UrlGifPreviaSemanal?.Trim() ?? "");
        cmd.Parameters.AddWithValue("url_diario", c.UrlGifDiario?.Trim() ?? "");
        cmd.Parameters.AddWithValue("modelo", c.ModeloMensagemDiaria ?? "");
        cmd.Parameters.AddWithValue("intervalo", c.IntervaloVerificacaoMinutos);
        cmd.Parameters.AddWithValue("hora_padrao", c.HoraNotificacaoPadrao);
        cmd.Parameters.AddWithValue("hora_previa", c.HoraPreviaSemanal);
        cmd.Parameters.AddWithValue("hora_diario", c.HoraLembreteDiario);
        cmd.Parameters.AddWithValue("gif_previa", (object?)c.GifPreviaSemanal ?? DBNull.Value);
        cmd.Parameters.AddWithValue("gif_previa_mime", c.GifPreviaMime?.Trim() ?? "");
        cmd.Parameters.AddWithValue("gif_diario", (object?)c.GifDiario ?? DBNull.Value);
        cmd.Parameters.AddWithValue("gif_diario_mime", c.GifDiarioMime?.Trim() ?? "");
        cmd.Parameters.AddWithValue("gif_previa_id", (object?)c.GifPreviaId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("gif_diario_id", (object?)c.GifDiarioId ?? DBNull.Value);
    }

    private static ConfiguracaoApp LerLinha(NpgsqlDataReader r) => new()
    {
        WebhookDiscord = r.GetString(0),
        TokenBotDiscord = r.GetString(1),
        IdServidorDiscord = r.GetString(2),
        UrlGifPreviaSemanal = r.GetString(3),
        UrlGifDiario = r.GetString(4),
        ModeloMensagemDiaria = r.GetString(5),
        IntervaloVerificacaoMinutos = r.GetInt32(6),
        HoraNotificacaoPadrao = r.GetInt32(7),
        HoraPreviaSemanal = r.GetInt32(8),
        HoraLembreteDiario = r.GetInt32(9),
        GifPreviaSemanal = r.IsDBNull(10) ? null : r.GetFieldValue<byte[]>(10),
        GifPreviaMime = r.IsDBNull(11) ? "" : r.GetString(11),
        GifDiario = r.IsDBNull(12) ? null : r.GetFieldValue<byte[]>(12),
        GifDiarioMime = r.IsDBNull(13) ? "" : r.GetString(13),
        GifPreviaId = r.IsDBNull(14) ? null : r.GetGuid(14),
        GifDiarioId = r.IsDBNull(15) ? null : r.GetGuid(15),
    };

    private async Task<NpgsqlConnection> AbrirAsync(CancellationToken ct)
    {
        var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        return conn;
    }
}
