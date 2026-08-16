namespace TextForge.Core.Analysis;

public interface ITextAnalyzer
{
    TextAnalysisResult Analyze(string? text, bool? hasHeaders = null);
}
