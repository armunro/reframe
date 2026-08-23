namespace Reframe.Core.Tabular;

public class DelimitedTabularParser : ITabularParser
{
    private readonly char? _delimiter;

    public DelimitedTabularParser(char? delimiter = null)
    {
        _delimiter = delimiter;
    }

    public static DelimitedTabularParser AutoDetect { get; } = new();

    public bool CanParse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        if (_delimiter.HasValue) return true;
        return TabularParser.DetectDelimiter(text) != null;
    }

    public TabularData? Parse(string? text, bool? assumeHeader = null, IEnumerable<string>? surrogateHeaders = null)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        char delimiter = _delimiter ?? TabularParser.DetectDelimiter(text) ?? ',';
        return TabularParser.Parse(text, delimiter, assumeHeader, surrogateHeaders);
    }
}
