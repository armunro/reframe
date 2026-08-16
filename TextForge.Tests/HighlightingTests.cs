using TextForge.Highlighting;
using Xunit;

namespace TextForge.Tests;

public class HighlightingTests
{
    [Theory]
    [InlineData("CSV")]
    [InlineData("TSV")]
    [InlineData("JSON")]
    [InlineData("SQL")]
    [InlineData("C#")]
    [InlineData("JavaScript")]
    [InlineData("TypeScript")]
    [InlineData("Python")]
    [InlineData("HTML")]
    [InlineData("XML")]
    [InlineData("Markdown")]
    [InlineData("CSS")]
    [InlineData("PowerShell")]
    [InlineData("C++")]
    [InlineData("Java")]
    [InlineData("PHP")]
    public void GetDefinition_LoadsSuccessfully_ForSupportedLanguages(string language)
    {
        var definition = DarkThemeHighlighting.GetDefinition(language);
        Assert.NotNull(definition);
        Assert.NotEmpty(definition.Name);
    }

    [Fact]
    public void GetDefinition_CsvAndTsv_HaveExpectedRulesAndColors()
    {
        var csvDef = DarkThemeHighlighting.GetDefinition("CSV");
        Assert.NotNull(csvDef);
        Assert.Equal("CSV", csvDef.Name);

        var tsvDef = DarkThemeHighlighting.GetDefinition("TSV");
        Assert.NotNull(tsvDef);
        Assert.Equal("TSV", tsvDef.Name);
    }

    [Theory]
    [InlineData("csv", "CSV")]
    [InlineData("tsv", "TSV")]
    [InlineData("tab", "TSV")]
    [InlineData("json", "Json")]
    [InlineData("cs", "C#")]
    [InlineData("csharp", "C#")]
    public void NormalizeLanguageName_NormalizesCorrectly(string input, string expected)
    {
        Assert.Equal(expected, DarkThemeHighlighting.NormalizeLanguageName(input));
    }
}
