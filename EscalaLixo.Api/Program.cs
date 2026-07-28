using EscalaLixo.Api.Endpoints;
using EscalaLixo.Api.Infrastructure;
using EscalaLixo.Api.Servicos;
using EscalaLixo.Servicos;

var builder = WebApplication.CreateBuilder(args);

var appDataDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "EscalaLixo");
Directory.CreateDirectory(appDataDir);
builder.Configuration.AddJsonFile(
    Path.Combine(appDataDir, "appsettings.json"),
    optional: true,
    reloadOnChange: true);

var pastaDados = Path.GetFullPath(
    builder.Configuration["PastaDados"] ?? Path.Combine(AppContext.BaseDirectory, "..", "data"));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!PostgresConnection.ConexaoValida(connectionString))
{
    Console.Error.WriteLine(
        "[EscalaLixo] PostgreSQL obrigatório. Configure ConnectionStrings:DefaultConnection em " +
        Path.Combine(appDataDir, "appsettings.json") +
        " ou em appsettings.json junto à API.");
    Console.Error.WriteLine($"Motivo: {PostgresConnection.MotivoInvalido(connectionString)}");
    return 1;
}

var pastaMigrations = PostgresConnection.ResolverPastaMigrations();

var colaboradoresRepo = new RepositorioColaboradoresPostgres(connectionString!);
var escalaHistorico = new RepositorioEscalaHistoricoPostgres(connectionString!);
var repoInstance = new ServicoRepositorioArquivos(pastaDados, colaboradoresRepo, escalaHistorico);

builder.Services.AddSingleton<IRepositorioColaboradores>(colaboradoresRepo);
builder.Services.AddSingleton(colaboradoresRepo);
builder.Services.AddSingleton(escalaHistorico);
builder.Services.AddSingleton(repoInstance);
var configRepo = new RepositorioConfiguracaoAppPostgres(connectionString!);
var gifRepo = new RepositorioBibliotecaGifs(connectionString!);

builder.Services.AddSingleton(configRepo);
builder.Services.AddSingleton(gifRepo);
builder.Services.AddSingleton(new ServicoConfiguracaoApp(configRepo, pastaDados, builder.Configuration));
builder.Services.AddSingleton(new ServicoBibliotecaGifs(gifRepo, configRepo));
builder.Services.AddSingleton<ServicoValidacaoEscala>();
builder.Services.AddSingleton<ServicoDuplas>();
builder.Services.AddSingleton<ServicoRotacaoVisual>();
builder.Services.AddHostedService<ServicoAgendamentoDiscord>();

try
{
    var migracao = new ServicoMigracaoSupabase(connectionString!, pastaMigrations);
    var aplicadas = await migracao.AplicarPendentesAsync();
    if (aplicadas.Count > 0)
        Console.WriteLine($"[Supabase] Migrations aplicadas: {string.Join(", ", aplicadas)}");
    else
        Console.WriteLine("[Supabase] PostgreSQL conectado — nenhuma migration pendente.");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[Supabase] ERRO — migrations falharam: {ex.Message}");
    return 1;
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();
app.UseCors();

app.MapHealthEndpoints(connectionString!);
app.MapDatabaseEndpoints(connectionString!, pastaMigrations);
app.MapEstadoEndpoints();
app.MapEscalaEndpoints();
app.MapRotacaoEndpoints();
app.MapColaboradoresEndpoints();
app.MapConfigEndpoints();
app.MapGifEndpoints();
app.MapDiscordEndpoints();

await app.RunAsync();
return 0;
