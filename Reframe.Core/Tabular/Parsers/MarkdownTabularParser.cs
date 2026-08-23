namespace Reframe.Core.Tabular;

public class MarkdownTabularParser : ITabularParser
{
    public static MarkdownTabularParser Instance { get; } = new();

    public bool CanParse(string? text) => MarkdownTableParser.IsMarkdownTable(text);

    public TabularData? Parse(string? text, bool? assumeHeader = null, IEnumerable<string>? surrogateHeaders = null)
    {
        if (!CanParse(text)) return null;
        var table = MarkdownTableParser.Parse(text, assumeHeader);
        if (surrogateHeaders != null)
        {
            var list = surrogateHeaders.ToList();
            if (list.Count > 0) table.OverrideHeaders(list);
        }
        return table;
    }
}
