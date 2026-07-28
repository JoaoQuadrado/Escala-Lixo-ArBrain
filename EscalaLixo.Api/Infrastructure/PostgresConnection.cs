namespace EscalaLixo.Api.Infrastructure;

internal static class PostgresConnection
{
    public static string ResolverPastaMigrations()
    {
        var candidatos = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "supabase", "migrations"),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "supabase", "migrations")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "supabase", "migrations")),
        };
        return candidatos.FirstOrDefault(Directory.Exists) ?? candidatos[1];
    }

    public static bool ConexaoValida(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return false;

        var invalidos = new[] { "[YOUR-PASSWORD]", "YOUR-PASSWORD", "SUA_SENHA", "senha_aqui", "COLOQUE_SUA_SENHA" };
        return !invalidos.Any(p => connectionString.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    public static string? MotivoInvalido(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return "ConnectionStrings:DefaultConnection não definida.";

        var invalidos = new (string Placeholder, string Mensagem)[]
        {
            ("[YOUR-PASSWORD]", "Senha ainda é o placeholder [YOUR-PASSWORD]."),
            ("YOUR-PASSWORD", "Senha ainda é placeholder."),
            ("COLOQUE_SUA_SENHA", "Substitua COLOQUE_SUA_SENHA_AQUI pela senha do Supabase."),
            ("SUA_SENHA", "Substitua SUA_SENHA pela senha real do banco."),
            ("senha_aqui", "Substitua senha_aqui pela senha real do banco."),
        };

        foreach (var (placeholder, mensagem) in invalidos)
        {
            if (connectionString.Contains(placeholder, StringComparison.OrdinalIgnoreCase))
                return mensagem;
        }

        return null;
    }
}
