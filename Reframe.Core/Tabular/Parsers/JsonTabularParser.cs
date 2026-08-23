using Reframe.Core.Tabular.Models;

namespace Reframe.Core.Tabular.Parsers;

public class JsonTabularParser : ITabularParser
{
    public static JsonTabularParser Instance { get; } = new();

    public bool CanParse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        string trimmed = text.Trim();
        return (trimmed.StartsWith('[') && trimmed.EndsWith(']')) || (trimmed.StartsWith('{') && trimmed.EndsWith('}'));
    }

    public TabularData? Parse(string? text, bool? assumeHeader = null, IEnumerable<string>? surrogateHeaders = null)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var table = TabularParser.TryParseJsonArray(text, assumeHeader);
        if (table != null && surrogateHeaders != null)
        {
            var list = surrogateHeaders.ToList();
            if (list.Count > 0) table.OverrideHeaders(list);
        }
        return table;
    }
}
