using Reframe.Core.Tabular.Models;

namespace Reframe.Core.Tabular.Parsers;

public class HtmlTabularParser : ITabularParser
{
    public static HtmlTabularParser Instance { get; } = new();

    public bool CanParse(string? text) => HtmlTableParser.IsHtmlTable(text);

    public TabularData? Parse(string? text, bool? assumeHeader = null, IEnumerable<string>? surrogateHeaders = null)
    {
        if (!CanParse(text)) return null;
        var table = HtmlTableParser.Parse(text, assumeHeader);
        if (surrogateHeaders != null)
        {
            var list = surrogateHeaders.ToList();
            if (list.Count > 0) table.OverrideHeaders(list);
        }
        return table;
    }
}
