using TextForge.Core.Analysis;
using TextForge.Core.Structured;
using TextForge.Core.Tabular;
using TextForge.Core.Transformers;
using Xunit;

namespace TextForge.Tests;

public class SolidInterfacesTests
{
    [Fact]
    public void TextAnalyzer_ImplementsInterface_AndAllowsPluggingCustomAnalyzer()
    {
        var defaultAnalyzer = DefaultTextAnalyzer.Instance;
        Assert.NotNull(defaultAnalyzer);
        Assert.IsAssignableFrom<ITextAnalyzer>(defaultAnalyzer);

        var result = defaultAnalyzer.Analyze("hello world");
        Assert.Equal(2, result.WordCount);

        var originalInstance = TextAnalyzer.Instance;
        try
        {
            var customAnalyzer = new CustomMockAnalyzer();
            TextAnalyzer.Instance = customAnalyzer;

            var customResult = TextAnalyzer.Analyze("anything");
            Assert.Equal(999, customResult.WordCount);
            Assert.Equal("Custom Mock", customResult.FormatDescription);
        }
        finally
        {
            TextAnalyzer.Instance = originalInstance;
        }
    }

    private class CustomMockAnalyzer : ITextAnalyzer
    {
        public TextAnalysisResult Analyze(string? text, bool? hasHeaders = null)
        {
            return new TextAnalysisResult
            {
                WordCount = 999,
                FormatDescription = "Custom Mock"
            };
        }
    }

    [Fact]
    public void TabularParsers_ImplementITabularParser_AndSupportCustomPlugins()
    {
        Assert.IsAssignableFrom<ITabularParser>(HtmlTabularParser.Instance);
        Assert.IsAssignableFrom<ITabularParser>(MarkdownTabularParser.Instance);
        Assert.IsAssignableFrom<ITabularParser>(JsonTabularParser.Instance);
        Assert.IsAssignableFrom<ITabularParser>(YamlTabularParser.Instance);
        Assert.IsAssignableFrom<ITabularParser>(DelimitedTabularParser.AutoDetect);
        Assert.IsAssignableFrom<ITabularParser>(TabularParserService.Instance);

        var parserService = new TabularParserService();
        var customParser = new CustomAtSignTabularParser();
        parserService.RegisterParser(customParser, index: 0);

        string atSignData = "name@age\nAlice@30\nBob@25";
        var parsed = parserService.Parse(atSignData);

        Assert.NotNull(parsed);
        Assert.Equal(2, parsed.Columns.Count);
        Assert.Equal("name", parsed.Columns[0]);
        Assert.Equal("age", parsed.Columns[1]);
        Assert.Equal(2, parsed.Rows.Count);
    }

    private class CustomAtSignTabularParser : ITabularParser
    {
        public bool CanParse(string? text) => text != null && text.Contains('@') && text.Contains('\n');

        public TabularData? Parse(string? text, bool? assumeHeader = null, IEnumerable<string>? surrogateHeaders = null)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) return null;

            var result = new TabularData { Delimiter = '@', HasHeaders = assumeHeader ?? true };
            var rows = lines.Select(l => l.Split('@').Select(c => c.Trim()).ToList()).ToList();
            if (result.HasHeaders && rows.Count > 0)
            {
                result.Columns = rows[0];
                result.Rows = rows.Skip(1).ToList();
            }
            else
            {
                result.Rows = rows;
            }
            return result;
        }
    }

    [Fact]
    public void TabularConverter_ImplementsITabularConverter_AndSupportsCustomConverter()
    {
        Assert.IsAssignableFrom<ITabularConverter>(TabularConverterService.Instance);

        var table = new TabularData
        {
            Columns = new List<string> { "Col1", "Col2" },
            Rows = new List<List<string>>
            {
                new() { "A", "B" }
            }
        };

        var csv = TabularConverterService.Instance.ToCsv(table);
        Assert.Contains("Col1,Col2", csv);

        var originalInstance = TabularConverter.Instance;
        try
        {
            TabularConverter.Instance = new CustomTabularConverter();
            Assert.Equal("CUSTOM_CSV", TabularConverter.ToCsv(table));
        }
        finally
        {
            TabularConverter.Instance = originalInstance;
        }
    }

    private class CustomTabularConverter : ITabularConverter
    {
        public string ToCsv(TabularData table, char delimiter = ',') => "CUSTOM_CSV";
        public string ToTsv(TabularData table) => "CUSTOM_TSV";
        public string ToMarkdownTable(TabularData table) => "CUSTOM_MD";
        public string ToJsonArrayOfObjects(TabularData table, bool indented = true) => "[]";
        public string ToJsonArrayOfArrays(TabularData table, bool indented = true) => "[]";
        public string ToSqlInsertStatements(TabularData table, string tableName = "MyTable") => "";
        public string ToHtmlTable(TabularData table) => "<table></table>";
        public string ToKeyValueJson(TabularData table, int keyColIndex, int valueColIndex, bool includeRestOfColumns = false, bool indented = true) => "{}";
        public string ToKeyValueJson(TabularData table, int keyColIndex, bool includeRestOfColumns, bool indented = true) => "{}";
        public string ToYaml(TabularData table) => "";
        public string ToYamlArrays(TabularData table) => "";
        public string ToKeyValueYaml(TabularData table, int keyColIndex, int valueColIndex, bool includeRestOfColumns = false) => "";
        public string ToKeyValueYaml(TabularData table, int keyColIndex, bool includeRestOfColumns) => "";
        public string ToKeyValueQueryString(TabularData table, int keyColIndex, int valueColIndex, bool includeRestOfColumns = false) => "";
        public string ToKeyValueQueryString(TabularData table, int keyColIndex, bool includeRestOfColumns) => "";
        public string ToSqlInClause(TabularData table, int colIndex, bool quoteStrings = true) => "IN ()";
    }

    [Fact]
    public void StructuredDataParser_ImplementsIStructuredDataParser_AndSupportsPlugin()
    {
        Assert.IsAssignableFrom<IStructuredDataParser>(StructuredDataParserService.Instance);

        var parsed = StructuredDataParserService.Instance.Parse("{\"key\": \"val\"}");
        Assert.True(parsed.Success);
        Assert.Equal("JSON", parsed.Format);

        var originalInstance = StructuredDataParser.Instance;
        try
        {
            StructuredDataParser.Instance = new CustomStructuredParser();
            var res = StructuredDataParser.Parse("test");
            Assert.Equal("CUSTOM", res.Format);
        }
        finally
        {
            StructuredDataParser.Instance = originalInstance;
        }
    }

    private class CustomStructuredParser : IStructuredDataParser
    {
        public StructuredDataParseResult Parse(string? text) => new() { Success = true, Format = "CUSTOM" };
        public StructuredDataParseResult TryParseJson(string json) => new() { Success = true, Format = "CUSTOM_JSON" };
        public StructuredDataParseResult TryParseXml(string xml) => new() { Success = true, Format = "CUSTOM_XML" };
        public StructuredDataParseResult TryParseYaml(string yaml) => new() { Success = true, Format = "CUSTOM_YAML" };
    }

    [Fact]
    public void Transformers_ImplementSpecificInterfaces_AndSupportPipelineAndRegistry()
    {
        Assert.IsAssignableFrom<ICaseTransformer>(CaseTransformerService.Instance);
        Assert.IsAssignableFrom<ILineTransformer>(LineTransformerService.Instance);
        Assert.IsAssignableFrom<IEncodingTransformer>(EncodingTransformerService.Instance);
        Assert.IsAssignableFrom<IDeveloperTransformer>(DeveloperTransformerService.Instance);
        Assert.IsAssignableFrom<ITextBeautifier>(TextBeautifierService.Instance);
        Assert.IsAssignableFrom<IStructuredTransformer>(StructuredTransformerService.Instance);

        // Test DelegateTextTransformer & Pipeline
        var upperTransformer = new DelegateTextTransformer("Upper", s => s?.ToUpperInvariant() ?? "");
        var trimTransformer = new DelegateTextTransformer("Trim", s => s?.Trim() ?? "");

        var pipeline = new TransformerPipeline("CleanAndUpper")
            .Add(trimTransformer)
            .Add(upperTransformer);

        string result = pipeline.Transform("   hello world   ");
        Assert.Equal("HELLO WORLD", result);

        // Test Registry
        var registry = new TransformerRegistry();
        registry.Register(pipeline);
        registry.Register("Reverse", s => s == null ? "" : new string(s.Reverse().ToArray()));

        var found = registry.GetTransformer("CleanAndUpper");
        Assert.NotNull(found);
        Assert.Equal("HELLO WORLD", found.Transform("  hello world  "));

        var reverseTransformer = registry.GetTransformer("Reverse");
        Assert.NotNull(reverseTransformer);
        Assert.Equal("dlrow", reverseTransformer.Transform("world"));
    }
}
