using Reframe.Core.Tabular.Models;

namespace Reframe.Core.Tabular.Parsers;

public interface ITabularParser
{
    bool CanParse(string? text);
    TabularData? Parse(string? text, bool? assumeHeader = null, IEnumerable<string>? surrogateHeaders = null);
}
