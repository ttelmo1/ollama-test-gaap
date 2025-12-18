using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace GaapMcp.Domain
{
    public class ToolCallResult
    {
        public JsonElement RawResult { get; init; }

        public string ExtractText()
        {
            if (RawResult.TryGetProperty("content", out var contentArray) &&
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
                    return string.Join(" ", texts);
            }

            return RawResult.GetRawText();
        }
    }
}
