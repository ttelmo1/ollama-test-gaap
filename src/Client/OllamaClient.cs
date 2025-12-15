using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyMCPClient
{
    public class OllamaClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _model;

        private const string SystemPrompt = @"
Você é um assistente útil. Regras importantes sobre ferramentas:

1. Use ferramentas APENAS quando o usuário explicitamente solicitar a informação que a ferramenta fornece
2. Para saudações (olá, oi, tudo bem) ou conversa casual, NUNCA use ferramentas - responda diretamente
3. A ferramenta 'get_time' deve ser usada SOMENTE quando o usuário perguntar: ""que horas são?"", ""qual a data?"", ""me diga a hora"", etc.
4. Se não tiver certeza, prefira NÃO usar a ferramenta e responder normalmente

Lembre-se: ferramentas têm custo computacional. Use com sabedoria.
";

        public OllamaClient(string model = "llama3.2", string baseUrl = "http://localhost:11434")
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5)
            };

            _baseUrl = baseUrl;
            _model = model;
        }

        public async Task<string> ChatAsync(
            string message,
            List<MCPTool>? tools = null,
            MCPClient? mcpClient = null)
        {
            var messages = InitializeMessages(message);

            while (true)
            {
                var payload = BuildPayload(messages, tools);
                var responseJson = await SendChatRequestAsync(payload);
                var assistantMessage = responseJson.GetProperty("message");

                messages.Add(JsonSerializer.Deserialize<object>(assistantMessage.GetRawText())!);

                if (TryHandleToolCalls(assistantMessage, messages, mcpClient))
                {
                    continue;
                }

                return GetFinalAssistantContent(assistantMessage);
            }
        }


        private static List<object> InitializeMessages(string userMessage)
        {
            return new List<object>
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = userMessage }
            };
        }

        private object BuildPayload(List<object> messages, List<MCPTool>? tools)
        {
            return new
            {
                model = _model,
                messages = messages.ToArray(),
                stream = false,
                tools = tools?.ConvertAll(t => new
                {
                    type = "function",
                    function = new
                    {
                        name = t.Name,
                        description = t.Description,
                        parameters = t.InputSchema
                    }
                })
            };
        }

        private async Task<JsonElement> SendChatRequestAsync(object payload)
        {
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/chat", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<JsonElement>(responseJson);
        }

        private bool TryHandleToolCalls(
            JsonElement assistantMessage,
            List<object> messages,
            MCPClient? mcpClient)
        {
            if (!assistantMessage.TryGetProperty("tool_calls", out var toolCalls) ||
                toolCalls.ValueKind != JsonValueKind.Array ||
                toolCalls.GetArrayLength() == 0)
            {
                return false;
            }

            if (mcpClient == null)
            {
                throw new InvalidOperationException(
                    "O modelo solicitou ferramentas, mas MCPClient não foi fornecido.");
            }

            Console.WriteLine("\n🔧 Ollama solicitou uso de ferramentas MCP...");

            foreach (var toolCall in toolCalls.EnumerateArray())
            {
                ProcessToolCall(toolCall, messages, mcpClient).GetAwaiter().GetResult();
            }

            return true;
        }

        private async Task ProcessToolCall(
            JsonElement toolCall,
            List<object> messages,
            MCPClient mcpClient)
        {
            var function = toolCall.GetProperty("function");
            var toolName = function.GetProperty("name").GetString()!;

            var arguments = function.TryGetProperty("arguments", out var args)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(args.GetRawText())!
                : new Dictionary<string, object>();

            Console.WriteLine($"Chamando ferramenta: {toolName}");
            Console.WriteLine($"Argumentos: {JsonSerializer.Serialize(arguments)}");

            var toolResult = await mcpClient.CallToolAsync(toolName, arguments);
            var toolContent = ExtractToolTextContent(toolResult);

            Console.WriteLine($"Resultado: {toolContent}\n");

            messages.Add(new
            {
                role = "tool",
                content = toolContent
            });
        }

        private static string ExtractToolTextContent(JsonElement toolResult)
        {
            if (toolResult.TryGetProperty("content", out var contentArray) &&
                contentArray.ValueKind == JsonValueKind.Array)
            {
                var texts = new List<string>();

                foreach (var item in contentArray.EnumerateArray())
                {
                    if (item.TryGetProperty("type", out var type) &&
                        type.GetString() == "text" &&
                        item.TryGetProperty("text", out var text))
                    {
                        texts.Add(text.GetString()!);
                    }
                }

                if (texts.Count > 0)
                {
                    return string.Join(" ", texts);
                }
            }

            return toolResult.GetRawText();
        }

        private static string GetFinalAssistantContent(JsonElement assistantMessage)
        {
            var content = assistantMessage.GetProperty("content").GetString();
            return string.IsNullOrWhiteSpace(content)
                ? "(Sem resposta final do modelo)"
                : content;
        }
    }
}
