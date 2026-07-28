namespace EscalaLixo.Servicos;

public static class ServicoDiscordHelpers
{
    public static bool IdDiscordValido(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;
        var t = id.Trim();
        if (t.Length < 15 || t.Length > 22)
            return false;
        foreach (var c in t)
        {
            if (!char.IsDigit(c))
                return false;
        }

        return true;
    }
}
