using GaapMcp.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace GaapMcp.Infrastructure
{
    public interface IMcpClient : IDisposable
    {
        Task StartAsync(string mcpExecutablePath);
        Task<IReadOnlyList<IMcpTool>> ListToolsAsync();
        Task<ToolCallResult> CallToolAsync(string toolName, IDictionary<string, object> arguments);
        void Stop();
    }
}
