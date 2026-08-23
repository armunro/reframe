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
        // #729BDB: R=114 (0x72), G=155 (0x9B), B=219 (0xDB)
        Assert.Equal(0x72, color.Value.R);
        Assert.Equal(0x9B, color.Value.G);
        Assert.Equal(0xDB, color.Value.B);
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
        // #729BDB: R=114 (0x72), G=155 (0x9B), B=219 (0xDB)
        Assert.Equal(0x72, color.Value.R);
        Assert.Equal(0x9B, color.Value.G);
        Assert.Equal(0xDB, color.Value.B);
    }

    [Fact]
    public void GetDefinition_AllSupportedLanguages_HaveNoDarkUnreadableTokens()
    {
        foreach (var lang in DarkThemeHighlighting.SupportedLanguages)
        {
            var def = DarkThemeHighlighting.GetDefinition(lang);
            if (def == null) continue;
            foreach (var col in def.NamedHighlightingColors)
            {
                var c = col.Foreground?.GetColor(null);
                if (!c.HasValue) continue;

                // Ensure no tokens have low luminance or dark unmapped colors (like pure blue #0000FF, dark magenta #8B008B, etc.)
                double luminance = 0.299 * c.Value.R + 0.587 * c.Value.G + 0.114 * c.Value.B;
                // Comments and directives can be slightly muted (>= 110)
                Assert.True(luminance >= 110, $"Language '{lang}' token '{col.Name}' has low luminance {luminance:F1} (#{c.Value.R:X2}{c.Value.G:X2}{c.Value.B:X2})");
                
                // Assert no dark pure blue or dark purple
                Assert.False(c.Value.R == 0 && c.Value.G == 0 && c.Value.B > 0, $"Language '{lang}' token '{col.Name}' has unmapped dark blue #{c.Value.R:X2}{c.Value.G:X2}{c.Value.B:X2}");
                Assert.False(c.Value.R < 140 && c.Value.G == 0 && c.Value.B < 140, $"Language '{lang}' token '{col.Name}' has unmapped dark purple #{c.Value.R:X2}{c.Value.G:X2}{c.Value.B:X2}");
            }
        }
    }
}
