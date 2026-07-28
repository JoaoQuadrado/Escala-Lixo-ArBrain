using EscalaLixo.Modelos;

namespace EscalaLixo.Servicos;

/// <summary>
/// Fachada de leitura/gravação com validação — colaboradores, escala e histórico.
/// </summary>
public sealed class ServicoRepositorioArquivos : IDisposable
{
    private readonly IRepositorioColaboradores _colaboradores;
    private readonly IRepositorioEscalaHistorico _escalaHistorico;
    private readonly ServicoValidacaoEscala _validacao = new();

    public ServicoRepositorioArquivos(
        string pastaBase,
        IRepositorioColaboradores colaboradores,
        IRepositorioEscalaHistorico escalaHistorico)
    {
        PastaBase = pastaBase;
        _colaboradores = colaboradores;
        _escalaHistorico = escalaHistorico;
        Directory.CreateDirectory(pastaBase);
    }

    public string PastaBase { get; }
    public IRepositorioColaboradores Colaboradores => _colaboradores;
    public bool UsaPostgres => _escalaHistorico.UsaPostgres;

    public Task<List<Colaborador>> LerColaboradoresAsync(CancellationToken ct = default) =>
        _colaboradores.ListarAsync(ct);

    public Task SalvarColaboradoresAsync(List<Colaborador> lista, CancellationToken ct = default) =>
        _colaboradores.SalvarTodosAsync(lista, ct);

    public Task<EscalaSemana?> LerEscalaAsync(CancellationToken ct = default) =>
        _escalaHistorico.LerEscalaAsync(ct);

    public Task<string> ObterHashEscalaAsync(CancellationToken ct = default) =>
        _escalaHistorico.ObterHashEscalaAsync(ct);

    public async Task<ResultadoValidacaoEscala> SalvarEscalaAsync(
        EscalaSemana escala,
        IReadOnlyList<Colaborador> colaboradores,
        bool sanitizar = true,
        CancellationToken ct = default)
    {
        if (sanitizar)
            escala = _validacao.Sanitizar(escala);

        var resultado = _validacao.Validar(escala, colaboradores);
        if (!resultado.Valido)
            return resultado;

        await _escalaHistorico.SalvarEscalaAsync(escala, ct).ConfigureAwait(false);
        return resultado;
    }

    public async Task<(EscalaSemana Escala, ResultadoValidacaoEscala Resultado)> MoverColaboradorAsync(
        EscalaSemana escala,
        IReadOnlyList<Colaborador> colaboradores,
        string nome,
        string diaOrigemPt,
        string diaDestinoPt,
        int? indiceDestino = null,
        CancellationToken ct = default)
    {
        var movimento = _validacao.ValidarMovimento(escala, colaboradores, nome, diaOrigemPt, diaDestinoPt);
        if (!movimento.Valido)
            return (escala, movimento);

        var nomeTrim = nome.Trim();
        var colaborador = colaboradores.FirstOrDefault(c =>
            string.Equals(c.Nome.Trim(), nomeTrim, StringComparison.OrdinalIgnoreCase));

        if (_escalaHistorico.SuportaMovimentoAtomico && colaborador is not null && colaborador.Id != Guid.Empty)
        {
            try
            {
                var slotDestino = SlotDestinoParaBanco(diaDestinoPt);
                var ordem = indiceDestino ?? 0;

                await _escalaHistorico.MoverColaboradorAtomicoAsync(
                    colaborador.Id, slotDestino, ordem, ct).ConfigureAwait(false);

                var escalaAtualizada = await _escalaHistorico.LerEscalaAsync(ct).ConfigureAwait(false) ?? escala;
                var resultadoDb = _validacao.Validar(escalaAtualizada, colaboradores);
                return (escalaAtualizada, resultadoDb);
            }
            catch (InvalidOperationException ex)
            {
                return (escala, ResultadoValidacaoEscala.Falha(ex.Message));
            }
        }

        var origemFila = ServicoValidacaoEscala.EhFilaEsperaPublico(diaOrigemPt);
        var origemBloqueado = ServicoValidacaoEscala.EhBloqueadoPublico(diaOrigemPt);
        var destinoFila = ServicoValidacaoEscala.EhFilaEsperaPublico(diaDestinoPt);
        var destinoBloqueado = ServicoValidacaoEscala.EhBloqueadoPublico(diaDestinoPt);

        if (origemBloqueado)
        {
            escala.Bloqueados = escala.Bloqueados
                .Where(n => !string.Equals(n.Trim(), nomeTrim, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        else if (origemFila)
        {
            escala.FilaEspera = escala.FilaEspera
                .Where(n => !string.Equals(n.Trim(), nomeTrim, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        else
        {
            var origem = escala.Dias.FirstOrDefault(d =>
                string.Equals(d.DiaDaSemana, diaOrigemPt, StringComparison.OrdinalIgnoreCase));
            if (origem is null)
                return (escala, ResultadoValidacaoEscala.Falha("Dia de origem inválido."));
            origem.Nomes = origem.Nomes
                .Where(n => !string.Equals(n.Trim(), nomeTrim, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (destinoBloqueado)
        {
            if (!escala.Bloqueados.Any(n =>
                    string.Equals(n.Trim(), nomeTrim, StringComparison.OrdinalIgnoreCase)))
            {
                var idx = indiceDestino.HasValue
                    ? Math.Clamp(indiceDestino.Value, 0, escala.Bloqueados.Count)
                    : escala.Bloqueados.Count;
                escala.Bloqueados.Insert(idx, nomeTrim);
            }

            escala.FilaEspera = escala.FilaEspera
                .Where(n => !string.Equals(n.Trim(), nomeTrim, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        else if (destinoFila)
        {
            escala.Bloqueados = escala.Bloqueados
                .Where(n => !string.Equals(n.Trim(), nomeTrim, StringComparison.OrdinalIgnoreCase))
                .ToList();
            escala.FilaEspera = escala.FilaEspera
                .Where(n => !string.Equals(n.Trim(), nomeTrim, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!escala.FilaEspera.Any(n =>
                    string.Equals(n.Trim(), nomeTrim, StringComparison.OrdinalIgnoreCase)))
            {
                var idx = indiceDestino.HasValue
                    ? Math.Clamp(indiceDestino.Value, 0, escala.FilaEspera.Count)
                    : escala.FilaEspera.Count;
                escala.FilaEspera.Insert(idx, nomeTrim);
            }
        }
        else
        {
            var destino = escala.Dias.FirstOrDefault(d =>
                string.Equals(d.DiaDaSemana, diaDestinoPt, StringComparison.OrdinalIgnoreCase));
            if (destino is null)
                return (escala, ResultadoValidacaoEscala.Falha("Dia de destino inválido."));

            if (!destino.Nomes.Any(n =>
                    string.Equals(n.Trim(), nomeTrim, StringComparison.OrdinalIgnoreCase)))
            {
                var idx = indiceDestino.HasValue
                    ? Math.Clamp(indiceDestino.Value, 0, destino.Nomes.Count)
                    : destino.Nomes.Count;
                destino.Nomes.Insert(idx, nomeTrim);
            }

            escala.Bloqueados = escala.Bloqueados
                .Where(n => !string.Equals(n.Trim(), nomeTrim, StringComparison.OrdinalIgnoreCase))
                .ToList();
            escala.FilaEspera = escala.FilaEspera
                .Where(n => !string.Equals(n.Trim(), nomeTrim, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        escala = _validacao.Sanitizar(escala);
        var final = _validacao.Validar(escala, colaboradores);
        if (!final.Valido)
            return (escala, final);

        await _escalaHistorico.SalvarEscalaAsync(escala, ct).ConfigureAwait(false);
        return (escala, final);
    }

    public async Task<(EscalaSemana Escala, ResultadoValidacaoEscala Resultado)> TrocarColaboradoresAsync(
        EscalaSemana escala,
        IReadOnlyList<Colaborador> colaboradores,
        string nomeA,
        string diaAPt,
        string nomeB,
        string diaBPt,
        CancellationToken ct = default)
    {
        var troca = _validacao.ValidarTroca(escala, colaboradores, nomeA, diaAPt, nomeB, diaBPt);
        if (!troca.Valido)
            return (escala, troca);

        var nomeATrim = nomeA.Trim();
        var nomeBTrim = nomeB.Trim();
        var colA = colaboradores.FirstOrDefault(c =>
            string.Equals(c.Nome.Trim(), nomeATrim, StringComparison.OrdinalIgnoreCase));
        var colB = colaboradores.FirstOrDefault(c =>
            string.Equals(c.Nome.Trim(), nomeBTrim, StringComparison.OrdinalIgnoreCase));

        if (_escalaHistorico.SuportaMovimentoAtomico &&
            colA is not null && colA.Id != Guid.Empty &&
            colB is not null && colB.Id != Guid.Empty)
        {
            try
            {
                await _escalaHistorico.TrocarColaboradoresAtomicoAsync(colA.Id, colB.Id, ct).ConfigureAwait(false);
                var escalaAtualizada = await _escalaHistorico.LerEscalaAsync(ct).ConfigureAwait(false) ?? escala;
                var resultadoDb = _validacao.Validar(escalaAtualizada, colaboradores);
                return (escalaAtualizada, resultadoDb);
            }
            catch (InvalidOperationException ex)
            {
                return (escala, ResultadoValidacaoEscala.Falha(ex.Message));
            }
        }

        AplicarTrocaInMemory(escala, diaAPt, nomeATrim, diaBPt, nomeBTrim);

        escala = _validacao.Sanitizar(escala);
        var final = _validacao.Validar(escala, colaboradores);
        if (!final.Valido)
            return (escala, final);

        await _escalaHistorico.SalvarEscalaAsync(escala, ct).ConfigureAwait(false);
        return (escala, final);
    }

    private static void AplicarTrocaInMemory(
        EscalaSemana escala,
        string diaAPt,
        string nomeA,
        string diaBPt,
        string nomeB)
    {
        var locA = LocalizarColaborador(escala, diaAPt, nomeA)
            ?? throw new InvalidOperationException($"Colaborador \"{nomeA}\" não encontrado.");
        locA.Lista.RemoveAt(locA.Indice);

        var locB = LocalizarColaborador(escala, diaBPt, nomeB)
            ?? throw new InvalidOperationException($"Colaborador \"{nomeB}\" não encontrado.");
        locB.Lista.RemoveAt(locB.Indice);

        locA.Lista.Insert(locA.Indice, nomeB);
        locB.Lista.Insert(locB.Indice, nomeA);
    }

    private sealed record LocalColaborador(List<string> Lista, int Indice);

    private static LocalColaborador? LocalizarColaborador(EscalaSemana escala, string diaPt, string nome)
    {
        if (ServicoValidacaoEscala.EhFilaEsperaPublico(diaPt))
        {
            var idx = escala.FilaEspera.FindIndex(n =>
                string.Equals(n.Trim(), nome, StringComparison.OrdinalIgnoreCase));
            return idx >= 0 ? new LocalColaborador(escala.FilaEspera, idx) : null;
        }

        if (ServicoValidacaoEscala.EhBloqueadoPublico(diaPt))
        {
            var idx = escala.Bloqueados.FindIndex(n =>
                string.Equals(n.Trim(), nome, StringComparison.OrdinalIgnoreCase));
            return idx >= 0 ? new LocalColaborador(escala.Bloqueados, idx) : null;
        }

        var dia = escala.Dias.FirstOrDefault(d =>
            string.Equals(d.DiaDaSemana, diaPt, StringComparison.OrdinalIgnoreCase));
        if (dia is null)
            return null;

        var i = dia.Nomes.FindIndex(n =>
            string.Equals(n.Trim(), nome, StringComparison.OrdinalIgnoreCase));
        return i >= 0 ? new LocalColaborador(dia.Nomes, i) : null;
    }

    public Task<HistoricoPares> LerHistoricoAsync(CancellationToken ct = default) =>
        _escalaHistorico.LerHistoricoAsync(ct);

    public Task SalvarHistoricoAsync(HistoricoPares historico, CancellationToken ct = default) =>
        _escalaHistorico.SalvarHistoricoAsync(historico, ct);

    public Task ArquivarEscalaAsync(EscalaSemana escala, string motivo, CancellationToken ct = default) =>
        _escalaHistorico.ArquivarEscalaAsync(escala, motivo, ct);

    public Task<List<EscalaHistoricoResumo>> ListarHistoricoEscalasAsync(int limite = 50, CancellationToken ct = default) =>
        _escalaHistorico.ListarHistoricoEscalasAsync(limite, ct);

    public Task<EscalaHistoricoCompleta?> LerHistoricoEscalaAsync(Guid id, CancellationToken ct = default) =>
        _escalaHistorico.LerHistoricoEscalaAsync(id, ct);

    public void Dispose() { }

    private static string SlotDestinoParaBanco(string diaDestinoPt)
    {
        if (ServicoValidacaoEscala.EhFilaEsperaPublico(diaDestinoPt))
            return "fila_espera";
        if (ServicoValidacaoEscala.EhBloqueadoPublico(diaDestinoPt))
            return ServicoValidacaoEscala.ChaveBloqueados;
        return diaDestinoPt.Trim();
    }
}
