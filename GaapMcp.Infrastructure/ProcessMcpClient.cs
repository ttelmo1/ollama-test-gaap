using GaapMcp.Domain;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace GaapMcp.Infrastructure
{
    public class ProcessMcpClient : IMcpClient
    {
        private Process? _mcpProcess;
        private StreamWriter? _stdin;
        private StreamReader? _stdout;
        private int _requestId;

        public async Task StartAsync(string mcpExecutablePath)
        {
            _mcpProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = mcpExecutablePath,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            _mcpProcess.Start();
            _stdin = _mcpProcess.StandardInput;
            _stdout = _mcpProcess.StandardOutput;

            // Redirecionar stderr para logging em background
            _ = Task.Run(async () =>
            {
                var stderr = _mcpProcess.StandardError;
                while (!stderr.EndOfStream)
                {
                    var line = await stderr.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        await Console.Error.WriteLineAsync($"[MCP Server Error] {line}");
                    }
                }
            });

            // Aguarda inicialização do servidor
            await Task.Delay(1000);
        }

        public async Task<IReadOnlyList<IMcpTool>> ListToolsAsync()
        {
            var request = new
            {
                jsonrpc = "2.0",
                id = ++_requestId,
                method = "tools/list",
                @params = new { }
            };

            var responseLine = await SendRequestAsync(request);
            var response = JsonSerializer.Deserialize<JsonElement>(responseLine);

            var tools = new List<IMcpTool>();

            if (response.TryGetProperty("result", out var result) &&
                result.TryGetProperty("tools", out var toolsArray))
            {
                foreach (var tool in toolsArray.EnumerateArray())
                {
                    tools.Add(new McpTool
                    {
                        Name = tool.GetProperty("name").GetString() ?? string.Empty,
                        Description = tool.GetProperty("description").GetString() ?? string.Empty,
                        InputSchema = tool.GetProperty("inputSchema")
                    });
                }
            }

            return tools;
        }

        public async Task<ToolCallResult> CallToolAsync(string toolName, IDictionary<string, object> arguments)
        {
            var request = new
            {
                jsonrpc = "2.0",
                id = ++_requestId,
                method = "tools/call",
                @params = new
                {
                    name = toolName,
                    arguments = arguments
                }
            };

            var responseLine = await SendRequestAsync(request);
            var response = JsonSerializer.Deserialize<JsonElement>(responseLine);

            return new ToolCallResult
            {
                RawResult = response.GetProperty("result")
            };
        }

        private async Task<string> SendRequestAsync(object request)
        {
            if (_stdin == null || _stdout == null)
                throw new InvalidOperationException("Cliente MCP não foi iniciado. Chame StartAsync primeiro.");

            var json = JsonSerializer.Serialize(request);
            await _stdin.WriteLineAsync(json);
            await _stdin.FlushAsync();

            var responseLine = await _stdout.ReadLineAsync();

            if (string.IsNullOrWhiteSpace(responseLine))
                throw new InvalidOperationException("Resposta vazia do servidor MCP.");

            return responseLine;
        }

        public void Stop()
        {
            try
            {
                _stdin?.Close();
                _mcpProcess?.Kill();
                _mcpProcess?.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro ao parar cliente MCP: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Stop();
            _stdin?.Dispose();
            _stdout?.Dispose();
            _mcpProcess?.Dispose();
        }
    }
}