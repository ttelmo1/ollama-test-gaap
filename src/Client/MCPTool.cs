using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace MyMCPClient
{
    public class MCPTool
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public JsonElement InputSchema { get; set; }
    }

}
