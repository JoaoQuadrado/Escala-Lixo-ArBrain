using Microsoft.Extensions.Configuration;

namespace EscalaLixo;

public static class ConfiguracaoLeitura
{
    public static string Texto(IConfiguration configuracao, string chaveAtual, string? chaveLegada = null)
    {
        var v = configuracao[chaveAtual];
        if (!string.IsNullOrEmpty(v))
            return v;
        if (chaveLegada is not null)
        {
            v = configuracao[chaveLegada];
            if (!string.IsNullOrEmpty(v))
                return v;
        }

        return "";
    }

    public static int Hora0a23(IConfiguration configuracao, int padrao, params string?[] chaves)
    {
        foreach (var chave in chaves)
        {
            if (string.IsNullOrEmpty(chave))
                continue;
            if (int.TryParse(configuracao[chave], out var h) && h is >= 0 and <= 23)
                return h;
        }

        return padrao;
    }
}
