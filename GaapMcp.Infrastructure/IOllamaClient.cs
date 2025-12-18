using GaapMcp.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace GaapMcp.Infrastructure
{
    public interface IOllamaClient
    {
        Task<string> ChatAsync(
            string message,
            IReadOnlyList<IMcpTool>? tools = null,
            IMcpClient? mcpClient = null);
    }
}
