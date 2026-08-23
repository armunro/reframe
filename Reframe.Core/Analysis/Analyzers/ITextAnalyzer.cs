using Reframe.Core.Analysis.Models;

namespace Reframe.Core.Analysis.Analyzers;

public interface ITextAnalyzer
{
    TextAnalysisResult Analyze(string? text, bool? hasHeaders = null);
}
