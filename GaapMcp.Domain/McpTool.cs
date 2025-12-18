using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace GaapMcp.Domain
{
    public class McpTool : IMcpTool
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public JsonElement InputSchema { get; set; }
    }
}
