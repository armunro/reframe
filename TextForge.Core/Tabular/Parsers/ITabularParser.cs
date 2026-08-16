namespace TextForge.Core.Tabular;

public interface ITabularParser
{
    bool CanParse(string? text);
    TabularData? Parse(string? text, bool? assumeHeader = null, IEnumerable<string>? surrogateHeaders = null);
}
