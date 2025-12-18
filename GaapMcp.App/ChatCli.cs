using GaapMcp.Domain;
using GaapMcp.Infrastructure;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace GaapMcp.App
{
    public class ChatCli
    {
        private readonly IOllamaClient _ollamaClient;
        private readonly IMcpClient _mcpClient;
        private readonly IConfiguration _configuration;

        public ChatCli(IOllamaClient ollamaClient, IMcpClient mcpClient, IConfiguration configuration)
        {
            _ollamaClient = ollamaClient;
            _mcpClient = mcpClient;
            _configuration = configuration;
        }

        public async Task RunAsync()
        {
            try
            {
                await InitializeMcpServerAsync();
                var tools = await LoadToolsAsync();

                Console.WriteLine("\n" + new string('=', 50));
                Console.WriteLine("Cliente Ollama + MCP iniciado!");
                Console.WriteLine(new string('=', 50) + "\n");

                await ChatLoopAsync(tools);
            }
            finally
            {
                _mcpClient.Stop();
            }
        }

        private async Task InitializeMcpServerAsync()
        {
            var serverPath = _configuration["McpServer:ExecutablePath"]
                ?? throw new InvalidOperationException("Caminho do servidor MCP não configurado.");

            Console.WriteLine("Iniciando servidor MCP...");
            await _mcpClient.StartAsync(serverPath);
        }

        private async Task<IReadOnlyList<IMcpTool>> LoadToolsAsync()
        {
            var tools = await _mcpClient.ListToolsAsync();
            Console.WriteLine($"Ferramentas MCP disponíveis: {string.Join(", ", tools.Select(t => t.Name))}");

            return EnrichToolDescriptions(tools);
        }

        private static IReadOnlyList<IMcpTool> EnrichToolDescriptions(IReadOnlyList<IMcpTool> tools)
        {
            var enrichedTools = new List<IMcpTool>();

            foreach (var tool in tools)
            {
                var mcpTool = new McpTool
                {
                    Name = tool.Name,
                    Description = tool.Description,
                    InputSchema = tool.InputSchema
                };

                // Enriquecer descrições vazias
                if (string.IsNullOrEmpty(mcpTool.Description))
                {
                    mcpTool.Description = tool.Name switch
                    {
                        "gettime" => "Obtém a data e hora atual do sistema. Use APENAS quando o usuário explicitamente perguntar que horas são, qual a data atual, ou solicitar informações de tempo.",
                        _ => $"Ferramenta {tool.Name}. Use apenas quando necessário."
                    };
                }

                enrichedTools.Add(mcpTool);
            }

            return enrichedTools;
        }

        private async Task ChatLoopAsync(IReadOnlyList<IMcpTool> tools)
        {
            while (true)
            {
                Console.Write("Você: ");
                var userInput = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(userInput) ||
                    userInput.ToLower() is "sair" or "exit" or "quit")
                {
                    break;
                }

                try
                {
                    var response = await _ollamaClient.ChatAsync(userInput, tools, _mcpClient);
                    Console.WriteLine($"\nAssistente: {response}\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n❌ Erro: {ex.Message}\n");
                }
            }
        }
    }
}
