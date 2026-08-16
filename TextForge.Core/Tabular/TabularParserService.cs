namespace TextForge.Core.Tabular;

public class TabularParserService : ITabularParser
{
    private readonly List<ITabularParser> _parsers;

    public TabularParserService()
    {
        _parsers = new List<ITabularParser>
        {
            HtmlTabularParser.Instance,
            JsonTabularParser.Instance,
            YamlTabularParser.Instance,
            MarkdownTabularParser.Instance,
            DelimitedTabularParser.AutoDetect
        };
    }

    public TabularParserService(IEnumerable<ITabularParser> parsers)
    {
        _parsers = parsers.ToList();
    }

    public static TabularParserService Instance { get; } = new();

    public IReadOnlyList<ITabularParser> Parsers => _parsers.AsReadOnly();

    public void RegisterParser(ITabularParser parser, int index = 0)
    {
        if (index >= 0 && index <= _parsers.Count)
        {
            _parsers.Insert(index, parser);
        }
        else
        {
            _parsers.Add(parser);
        }
    }

    public bool CanParse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        return _parsers.Any(p => p.CanParse(text));
    }

    public TabularData? Parse(string? text, bool? assumeHeader = null, IEnumerable<string>? surrogateHeaders = null)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        foreach (var parser in _parsers)
        {
            if (parser.CanParse(text))
            {
                var table = parser.Parse(text, assumeHeader, surrogateHeaders);
                if (table != null) return table;
            }
        }

        return null;
    }
}
