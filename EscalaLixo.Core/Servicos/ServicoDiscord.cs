using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using EscalaLixo;
using EscalaLixo.Modelos;

namespace EscalaLixo.Servicos;

public sealed class ServicoDiscord : IDisposable
{
    private const string MarcadorDupla = "{dupla}";

    private readonly HttpClient _http = new();
    private readonly string _urlDoWebhook;
    private readonly ServicoResolucaoUsuarioDiscord? _resolucaoUsuarios;
    private readonly GifAnexo? _gifPreviaSemanal;
    private readonly GifAnexo? _gifDiario;
    private readonly string? _urlGifPreviaSemanal;
    private readonly string? _urlGifDiario;
    private readonly string? _modeloMensagemDiaria;

    public ServicoDiscord(
        string urlDoWebhook,
        ServicoResolucaoUsuarioDiscord? resolucaoUsuarios = null,
        GifAnexo? gifPreviaSemanal = null,
        GifAnexo? gifDiario = null,
        string? urlGifPreviaSemanal = null,
        string? urlGifDiario = null,
        string? modeloMensagemDiaria = null)
    {
        _urlDoWebhook = urlDoWebhook;
        _resolucaoUsuarios = resolucaoUsuarios;
        _gifPreviaSemanal = gifPreviaSemanal;
        _gifDiario = gifDiario ?? gifPreviaSemanal;
        _urlGifPreviaSemanal = string.IsNullOrWhiteSpace(urlGifPreviaSemanal) ? null : urlGifPreviaSemanal.Trim();
        var diario = string.IsNullOrWhiteSpace(urlGifDiario) ? null : urlGifDiario.Trim();
        _urlGifDiario = diario ?? _urlGifPreviaSemanal;
        _modeloMensagemDiaria = string.IsNullOrWhiteSpace(modeloMensagemDiaria) ? null : modeloMensagemDiaria;
    }

    private static readonly JsonSerializerOptions JsonPost = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task NotificarDiaAsync(AtribuicaoDia dia, IReadOnlyList<Colaborador> todosColaboradores, CancellationToken ct = default)
    {
        if (dia.Nomes.Count == 0)
            return;

        var linha = await LinhaMencoesDuplaAsync(dia.Nomes, todosColaboradores, ct).ConfigureAwait(false);

        string corpo;
        if (_modeloMensagemDiaria is null)
        {
            var sb = new StringBuilder();
            sb.Append(BlocoInstrucoesFixas());
            sb.Append('\n');
            sb.Append(linha);
            corpo = sb.ToString();
        }
        else
        {
            corpo = MontarMensagemDiariaComModelo(_modeloMensagemDiaria, linha);
        }

        await PostarConteudoAsync(corpo, ct, _gifDiario, _urlGifDiario).ConfigureAwait(false);
    }

    private static string MontarMensagemDiariaComModelo(string modelo, string linhaMencoes)
    {
        if (modelo.Contains(MarcadorDupla, StringComparison.Ordinal))
            return modelo.Replace(MarcadorDupla, linhaMencoes, StringComparison.Ordinal);

        return modelo.TrimEnd() + "\n\n" + linhaMencoes;
    }

    public async Task NotificarPreviaSemanaAsync(EscalaSemana semana, IReadOnlyList<Colaborador> todosColaboradores, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        sb.Append(BlocoInstrucoesFixas());
        sb.Append("\n\n📅 **Escala da semana** (início ");
        sb.Append(semana.InicioDaSemana);
        sb.Append(")\n");

        for (var i = 0; i < 5; i++)
        {
            var dow = (DayOfWeek)((int)DayOfWeek.Monday + i);
            var diaSemanaPt = DiasSemanaPt.OrdemSemana[i];
            var atrib = semana.Dias.FirstOrDefault(x => DiasSemanaPt.CorrespondeAoDia(x.DiaDaSemana, dow));

            sb.Append("**");
            sb.Append(DiasSemanaPt.Capitalizar(diaSemanaPt));
            sb.Append(' ');
            sb.Append(DiasSemanaPt.Emoji(diaSemanaPt));
            sb.Append(":** ");
            sb.Append(atrib is null || atrib.Nomes.Count == 0
                ? "—"
                : await LinhaMencoesDuplaAsync(atrib.Nomes, todosColaboradores, ct).ConfigureAwait(false));
            sb.Append('\n');
        }

        await PostarConteudoAsync(sb.ToString().TrimEnd(), ct, _gifPreviaSemanal, _urlGifPreviaSemanal).ConfigureAwait(false);
    }

    private async Task<string> MencionarOuNomeAsync(string nome, IReadOnlyList<Colaborador> todos, CancellationToken ct)
    {
        var n = nome.Trim();
        var col = todos.FirstOrDefault(c => string.Equals(c.Nome, n, StringComparison.OrdinalIgnoreCase));
        var entrada = col?.MarcacaoParaResolver() ?? "";
        if (string.IsNullOrWhiteSpace(entrada))
            entrada = n;

        if (_resolucaoUsuarios is not null)
        {
            var id = await _resolucaoUsuarios.ResolverIdAsync(entrada, ct).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(id))
                return $"<@{id}>";
        }

        var marcacacao = col?.MarcacaoParaResolver() ?? "";
        if (ServicoDiscordHelpers.IdDiscordValido(marcacacao))
            return $"<@{marcacacao.Trim()}>";

        return $"**{n}**";
    }

    private async Task<string> LinhaMencoesDuplaAsync(IReadOnlyList<string> nomes, IReadOnlyList<Colaborador> todos, CancellationToken ct)
    {
        var partes = new List<string>(nomes.Count);
        foreach (var nome in nomes)
            partes.Add(await MencionarOuNomeAsync(nome, todos, ct).ConfigureAwait(false));
        return string.Join(" ", partes);
    }

    private static string BlocoInstrucoesFixas()
    {
        return """
📌 Organização Diária – Equipes em Dupla
🗑️ Coleta de Lixo (após 17h30)
•    Banheiros → sacos 50L
•    Cozinha Orgânico → sacos 100L Preto
•    Cozinha Reciclável → sacos 100L Verde
•    Descarte no andar -1 (estacionamento)
☕ Cozinha
•    Pia limpa ✅
•    Escorredor organizado ✅
•    Cafeteiras higienizadas ✅
👥 As duplas do dia são responsáveis por garantir estas tarefas.
🙌 Vamos manter nosso espaço limpo e pronto para o próximo dia!
""".TrimEnd();
    }

    private async Task PostarConteudoAsync(
        string conteudo,
        CancellationToken ct,
        GifAnexo? gifAnexo = null,
        string? gifEmbedUrl = null)
    {
        if (string.IsNullOrWhiteSpace(_urlDoWebhook))
            throw new InvalidOperationException($"A chave «{ChavesConfiguracao.WebhookDiscord}» está vazia ou em falta em appsettings.json.");

        if (gifAnexo is { Dados.Length: > 0 })
        {
            await PostarComAnexoAsync(conteudo, gifAnexo, ct).ConfigureAwait(false);
            return;
        }

        object payload = gifEmbedUrl is null
            ? new { content = conteudo }
            : new
            {
                content = conteudo,
                embeds = new[]
                {
                    new { image = new { url = gifEmbedUrl } }
                }
            };
        using var response = await _http.PostAsJsonAsync(_urlDoWebhook, payload, JsonPost, ct).ConfigureAwait(false);
        await ValidarRespostaAsync(response, ct).ConfigureAwait(false);
    }

    private async Task PostarComAnexoAsync(string conteudo, GifAnexo gif, CancellationToken ct)
    {
        var payload = new
        {
            content = conteudo,
            embeds = new[]
            {
                new { image = new { url = $"attachment://{gif.NomeArquivo}" } }
            }
        };

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(JsonSerializer.Serialize(payload, JsonPost)), "payload_json");

        var arquivo = new ByteArrayContent(gif.Dados);
        arquivo.Headers.ContentType = new MediaTypeHeaderValue(gif.Mime);
        form.Add(arquivo, "files[0]", gif.NomeArquivo);

        using var response = await _http.PostAsync(_urlDoWebhook, form, ct).ConfigureAwait(false);
        await ValidarRespostaAsync(response, ct).ConfigureAwait(false);
    }

    private static async Task ValidarRespostaAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var corpoResposta = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Discord respondeu {(int)response.StatusCode} {response.ReasonPhrase}. Corpo: {corpoResposta}");
        }
    }

    public void Dispose() => _http.Dispose();
}
