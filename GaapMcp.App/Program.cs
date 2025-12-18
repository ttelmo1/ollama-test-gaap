using GaapMcp.App;
using GaapMcp.Domain;
using GaapMcp.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        // Configurações
        services.Configure<OllamaOptions>(
            context.Configuration.GetSection(OllamaOptions.SectionName));

        // HttpClient para Ollama
        services.AddHttpClient<IOllamaClient, OllamaClient>();

        // MCP Client
        services.AddSingleton<IMcpClient, ProcessMcpClient>();

        // CLI
        services.AddTransient<ChatCli>();
    });

var host = builder.Build();

// Executar o CLI
var cli = host.Services.GetRequiredService<ChatCli>();
await cli.RunAsync();
