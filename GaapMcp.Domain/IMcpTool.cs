using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace GaapMcp.Domain
{
    public interface IMcpTool
    {
        string Name { get; }
        string Description { get; }
        JsonElement InputSchema { get; }
    }
}
