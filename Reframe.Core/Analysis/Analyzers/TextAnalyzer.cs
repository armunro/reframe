using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Reframe.Core.Tabular;
using Reframe.Core.Transformers;

namespace Reframe.Core.Analysis;

public static class TextAnalyzer
{
    public static ITextAnalyzer Instance { get; set; } = DefaultTextAnalyzer.Instance;

    public static TextAnalysisResult Analyze(string? text, bool? hasHeaders = null)
    {
        return Instance.Analyze(text, hasHeaders);
    }
}
