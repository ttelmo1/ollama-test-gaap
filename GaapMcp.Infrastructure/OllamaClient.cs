using GaapMcp.Domain;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace GaapMcp.Infrastructure
{
    public class OllamaClient : IOllamaClient
    {
        private readonly HttpClient _httpClient;
        private readonly OllamaOptions _options;

        public OllamaClient(HttpClient httpClient, IOptions<OllamaOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _httpClient.Timeout = TimeSpan.FromMinutes(_options.TimeoutMinutes);
        }

        public async Task<string> ChatAsync(
            string message,
            IReadOnlyList<IMcpTool>? tools = null,
            IMcpClient? mcpClient = null)
        {
            var messages = InitializeMessages(message);

            while (true)
            {
                var payload = BuildPayload(messages, tools);
                var responseJson = await SendChatRequestAsync(payload);
                var assistantMessage = responseJson.GetProperty("message");

                messages.Add(JsonSerializer.Deserialize<Dictionary<string, object>>(assistantMessage.GetRawText())!);

                if (await TryHandleToolCallsAsync(assistantMessage, messages, mcpClient))
                {
                    continue;
                }

                return GetFinalAssistantContent(assistantMessage);
            }
        }

        private List<object> InitializeMessages(string userMessage)
        {
            return new List<object>
        {
            new { role = "system", content = _options.SystemPrompt },
            new { role = "user", content = userMessage }
        };
        }

        private object BuildPayload(List<object> messages, IReadOnlyList<IMcpTool>? tools)
        {
            return new
            {
                model = _options.Model,
                messages = messages.ToArray(),
                stream = false,
                tools = tools?.Select(t => new
                {
                    type = "function",
                    function = new
                    {
                        name = t.Name,
                        description = t.Description,
                        parameters = t.InputSchema
                    }
                }).ToList()
            };
        }

        private async Task<JsonElement> SendChatRequestAsync(object payload)
        {
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_options.BaseUrl}/api/chat", content);

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<JsonElement>(responseJson);
        }

        private async Task<bool> TryHandleToolCallsAsync(
            JsonElement assistantMessage,
            List<object> messages,
            IMcpClient? mcpClient)
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
                await ProcessToolCallAsync(toolCall, messages, mcpClient);
            }

            return true;
        }

        private async Task ProcessToolCallAsync(
            JsonElement toolCall,
            List<object> messages,
            IMcpClient mcpClient)
        {
            var function = toolCall.GetProperty("function");
            var toolName = function.GetProperty("name").GetString()!;
            var arguments = function.TryGetProperty("arguments", out var args)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(args.GetRawText())!
                : new Dictionary<string, object>();

            Console.WriteLine($"Chamando ferramenta: {toolName}");
            Console.WriteLine($"Argumentos: {JsonSerializer.Serialize(arguments)}");

            var toolResult = await mcpClient.CallToolAsync(toolName, arguments);
            var toolContent = toolResult.ExtractText();

            Console.WriteLine($"Resultado: {toolContent}\n");

            messages.Add(new
            {
                role = "tool",
                content = toolContent
            });
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
