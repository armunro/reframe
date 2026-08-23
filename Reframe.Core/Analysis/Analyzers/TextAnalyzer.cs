using Reframe.Core.Analysis.Models;

namespace Reframe.Core.Analysis.Analyzers;

public static class TextAnalyzer
{
    public static ITextAnalyzer Instance { get; set; } = DefaultTextAnalyzer.Instance;

    public static TextAnalysisResult Analyze(string? text, bool? hasHeaders = null)
    {
        return Instance.Analyze(text, hasHeaders);
    }
}
