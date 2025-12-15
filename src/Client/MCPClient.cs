using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace MyMCPClient
{
    public class MCPClient
    {
        private Process mcpProcess;
        private StreamWriter stdin;
        private StreamReader stdout;

        public async Task StartAsync(string mcpExecutablePath)
        {
            mcpProcess = new Process
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

            mcpProcess.Start();
            stdin = mcpProcess.StandardInput;
            stdout = mcpProcess.StandardOutput;

            await Task.Delay(1000);
        }

        public async Task<List<MCPTool>> ListToolsAsync()
        {
            var request = new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "tools/list",
                @params = new { }
            };

            var json = JsonSerializer.Serialize(request);
            await stdin.WriteLineAsync(json);
            await stdin.FlushAsync();

            var responseLine = await stdout.ReadLineAsync();
            var response = JsonSerializer.Deserialize<JsonElement>(responseLine);

            var tools = new List<MCPTool>();
            if (response.TryGetProperty("result", out var result) &&
                result.TryGetProperty("tools", out var toolsArray))
            {
                foreach (var tool in toolsArray.EnumerateArray())
                {
                    tools.Add(new MCPTool
                    {
                        Name = tool.GetProperty("name").GetString(),
                        Description = tool.GetProperty("description").GetString(),
                        InputSchema = tool.GetProperty("inputSchema")
                    });
                }
            }

            return tools;
        }

        public async Task<JsonElement> CallToolAsync(string toolName, Dictionary<string, object> arguments)
        {
            var request = new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "tools/call",
                @params = new
                {
                    name = toolName,
                    arguments = arguments
                }
            };

            var json = JsonSerializer.Serialize(request);
            await stdin.WriteLineAsync(json);
            await stdin.FlushAsync();

            var responseLine = await stdout.ReadLineAsync();
            var response = JsonSerializer.Deserialize<JsonElement>(responseLine);

            return response.GetProperty("result");
        }

        public void Stop()
        {
            stdin?.Close();
            mcpProcess?.Kill();
            mcpProcess?.WaitForExit();
        }
    }
}
