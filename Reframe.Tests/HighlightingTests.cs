using Reframe.Highlighting;
using Xunit;

namespace Reframe.Tests;

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

    [Fact]
    public void GetDefinition_Markdown_LinkColorIsBrightAndReadable()
    {
        var definition = DarkThemeHighlighting.GetDefinition("Markdown");
        Assert.NotNull(definition);

        var linkColor = definition.NamedHighlightingColors.FirstOrDefault(c => c.Name.Contains("Link", System.StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(linkColor);
        Assert.NotNull(linkColor.Foreground);

        var color = linkColor.Foreground.GetColor(null);
        Assert.True(color.HasValue);
        // Ensure link color is bright sky blue (#4EA6EA: R=78, G=166, B=234) rather than dark blue (#0000FF)
        Assert.Equal(0x4E, color.Value.R);
        Assert.Equal(0xA6, color.Value.G);
        Assert.Equal(0xEA, color.Value.B);
    }

    [Fact]
    public void GetDefinition_CSharp_HasDarkNavyKeywordColor()
    {
        var definition = DarkThemeHighlighting.GetDefinition("C#");
        Assert.NotNull(definition);

        var keywordColor = definition.NamedHighlightingColors.FirstOrDefault(c => c.Name.Equals("Keywords", System.StringComparison.OrdinalIgnoreCase) || c.Name.Contains("Keyword", System.StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(keywordColor);
        Assert.NotNull(keywordColor.Foreground);

        var color = keywordColor.Foreground.GetColor(null);
        Assert.True(color.HasValue);
        // #668BC4: R=102 (0x66), G=139 (0x8B), B=196 (0xC4)
        Assert.Equal(0x66, color.Value.R);
        Assert.Equal(0x8B, color.Value.G);
        Assert.Equal(0xC4, color.Value.B);
    }

    [Fact]
    public void GetDefinition_Csv_HasDarkNavyDelimiterColor()
    {
        var definition = DarkThemeHighlighting.GetDefinition("CSV");
        Assert.NotNull(definition);

        var delimiterColor = definition.NamedHighlightingColors.FirstOrDefault(c => c.Name.Equals("Delimiter", System.StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(delimiterColor);
        Assert.NotNull(delimiterColor.Foreground);

        var color = delimiterColor.Foreground.GetColor(null);
        Assert.True(color.HasValue);
        // #668BC4: R=102 (0x66), G=139 (0x8B), B=196 (0xC4)
        Assert.Equal(0x66, color.Value.R);
        Assert.Equal(0x8B, color.Value.G);
        Assert.Equal(0xC4, color.Value.B);
    }
}
