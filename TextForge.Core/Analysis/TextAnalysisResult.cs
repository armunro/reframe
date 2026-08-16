namespace TextForge.Core.Analysis;

public enum DetectedFormat
{
    Empty,
    SingleLine,
    DelimitedSingleLine,
    MultiLineList,
    MultiLineNumbers,
    CsvTable,
    TsvTable,
    MarkdownTable,
    HtmlTable,
    Json,
    SqlInClause,
    KeyValuePairs,
    Base64,
    UrlEncoded
}

public class TextAnalysisResult
{
    public int CharacterCount { get; init; }
    public int CharacterCountNoSpaces { get; init; }
    public int LineCount { get; init; }
    public int NonEmptyLineCount { get; init; }
    public int WordCount { get; init; }
    public int DistinctLineCount { get; init; }
    public DetectedFormat Format { get; init; }
    public string FormatDescription { get; init; } = string.Empty;
    public char? DetectedDelimiter { get; init; }
    public bool IsTabular { get; init; }
    public bool HasHeaders { get; init; } = true;
    public int ColumnCount { get; init; }
    public int RowCount { get; init; }
    public IReadOnlyList<string> SampleHeaders { get; init; } = Array.Empty<string>();
    public bool HasDuplicates { get; init; }
    public int DuplicateCount { get; init; }
}
