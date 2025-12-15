using MyMCPClient;

public class Program
{
    public static async Task Main(string[] args)
    {
        var mcpClient = new MCPClient();

        var ollamaClient = new OllamaClient("qwen2.5");

        try
        {
            Console.WriteLine("Iniciando servidor MCP...");
            await mcpClient.StartAsync(@"../Server/bin/Release/net10.0/Server.exe");

            var tools = await mcpClient.ListToolsAsync();
            Console.WriteLine($"Ferramentas MCP disponíveis: {string.Join(", ", tools.Select(t => t.Name))}");

            foreach (var tool in tools)
            {
                if (string.IsNullOrEmpty(tool.Description))
                {
                    if (tool.Name == "get_time")
                    {
                        tool.Description = "Obtém a data e hora atual do sistema. Use APENAS quando o usuário explicitamente perguntar que horas são, qual a data atual, ou solicitar informações de tempo.";
                    }
                }
                else
                {
                    tool.Description = $"{tool.Description} Use esta ferramenta APENAS quando necessário.";
                }
            }

            Console.WriteLine("\n" + new string('=', 50));
            Console.WriteLine("Cliente Ollama + MCP iniciado!");
            Console.WriteLine(new string('=', 50) + "\n");

            while (true)
            {
                Console.Write("Você: ");
                var userInput = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(userInput) ||
                    userInput.ToLower() is "sair" or "exit" or "quit")
                {
                    break;
                }

                var response = await ollamaClient.ChatAsync(userInput, tools, mcpClient);
                Console.WriteLine($"\nAssistente: {response}\n");
            }
        }
        finally
        {
            mcpClient.Stop();
        }
    }
}