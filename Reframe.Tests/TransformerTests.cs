using Reframe.Core.Analysis;
using Reframe.Core.Analysis.Analyzers;
using Reframe.Core.Analysis.Models;
using Reframe.Core.Tabular;
using Reframe.Core.Tabular.Converters;
using Reframe.Core.Tabular.Models;
using Reframe.Core.Tabular.Parsers;
using Reframe.Core.Transformers;
using Reframe.Core.Transformers.Case;
using Reframe.Core.Transformers.Developer;
using Reframe.Core.Transformers.Encoding;
using Reframe.Core.Transformers.Line;
using Xunit;

namespace Reframe.Tests;

public class TransformerTests
{
    [Fact]
    public void QuoteLines_SingleQuotes_QuotesAndEscapes()
    {
        string input = "Apple\nBanana\nO'Reilly\n\nDate";
        string result = LineTransformers.QuoteLines(input, QuoteStyle.SingleQuotes, skipEmpty: true);
        string expected = "'Apple'\n'Banana'\n'O''Reilly'\n'Date'".Replace("\n", Environment.NewLine);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void QuoteLines_DoubleQuotes_QuotesAndEscapes()
    {
        string input = "Hello\n\"World\"\nTest";
        string result = LineTransformers.QuoteLines(input, QuoteStyle.DoubleQuotes, skipEmpty: true);
        string expected = "\"Hello\"\n\"\\\"World\\\"\"\n\"Test\"".Replace("\n", Environment.NewLine);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void JoinLines_JoinsWithDelimiterAndWrap()
    {
        string input = "100\n200\n300";
        string result = LineTransformers.JoinLines(input, delimiter: ", ", itemQuote: QuoteStyle.SingleQuotes, overallPrefix: "(", overallSuffix: ")");
        Assert.Equal("('100', '200', '300')", result);
    }

    [Fact]
    public void SplitLine_SplitsCommaDelimited()
    {
        string input = "apple, banana, cherry, date";
        string result = LineTransformers.SplitLine(input, delimiter: ",", trimItems: true);
        string expected = "apple\nbanana\ncherry\ndate".Replace("\n", Environment.NewLine);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SplitLine_AutoDetectsDelimiter()
    {
        string input = "val1\tval2\tval3";
        string result = LineTransformers.SplitLine(input);
        string expected = "val1\nval2\nval3".Replace("\n", Environment.NewLine);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SortLines_NaturalNumericSort()
    {
        string input = "item10\nitem2\nitem1\nitem20";
        string result = LineTransformers.SortLines(input, SortOrder.NaturalNumericAsc);
        string expected = "item1\nitem2\nitem10\nitem20".Replace("\n", Environment.NewLine);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void DeduplicateLines_Distinct()
    {
        string input = "apple\nbanana\nApple\nbanana\ncherry";
        string result = LineTransformers.DeduplicateLines(input, DeduplicateMode.Distinct, caseSensitive: false);
        string expected = "apple\nbanana\ncherry".Replace("\n", Environment.NewLine);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void DeduplicateLines_DuplicatesOnly()
    {
        string input = "apple\nbanana\napple\ncherry";
        string result = LineTransformers.DeduplicateLines(input, DeduplicateMode.DuplicatesOnly, caseSensitive: false);
        Assert.Equal("apple", result);
    }

    [Fact]
    public void TrimLines_CollapsesWhitespaceAndRemovesEmpty()
    {
        string input = "  hello   world  \n\n   foo   bar   ";
        string result = LineTransformers.TrimLines(input, collapseWhitespace: true, removeEmptyLines: true);
        string expected = "hello world\nfoo bar".Replace("\n", Environment.NewLine);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FilterLines_RegexFilter()
    {
        string input = "123-abc\n456-def\nxyz-789";
        string result = LineTransformers.FilterLines(input, @"^\d+", isRegex: true, keepMatching: true);
        string expected = "123-abc\n456-def".Replace("\n", Environment.NewLine);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void NumberLines_FormatsWithNumbers()
    {
        string input = "First\nSecond\nThird";
        string result = LineTransformers.NumberLines(input, format: "{0n}: ");
        string expected = "01: First\n02: Second\n03: Third".Replace("\n", Environment.NewLine);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Tabular_CsvParsingWithQuotes()
    {
        string csv = "Id,Name,Notes\n1,\"Doe, John\",\"Hello\nWorld\"\n2,Jane,Single";
        var table = TabularParser.Parse(csv, ',');
        Assert.Equal(3, table.Columns.Count);
        Assert.Equal("Id", table.Columns[0]);
        Assert.Equal("Name", table.Columns[1]);
        Assert.Equal("Notes", table.Columns[2]);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("Doe, John", table.Rows[0][1]);
    }

    [Fact]
    public void Tabular_MarkdownTableParsingAndConversion()
    {
        string md = "| Name | Age | City |\n| --- | --- | --- |\n| Alice | 30 | New York |\n| Bob | 25 | London |";
        Assert.True(MarkdownTableParser.IsMarkdownTable(md));
        var table = MarkdownTableParser.Parse(md);
        Assert.Equal(3, table.Columns.Count);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("Alice", table.Rows[0][0]);

        string json = TabularConverter.ToJsonArrayOfObjects(table, indented: false);
        Assert.Contains("\"Name\":\"Alice\"", json);
        Assert.Contains("\"Age\":30", json);

        string csv = TabularConverter.ToCsv(table);
        Assert.Contains("Name,Age,City", csv);
        Assert.Contains("Alice,30,New York", csv);
    }

    [Fact]
    public void Tabular_Transpose()
    {
        string csv = "Col1,Col2\nA,B\nC,D";
        var table = TabularParser.Parse(csv, ',');
        var transposed = table.Transpose();

        Assert.Equal("Col1", transposed.Columns[0]);
        Assert.Equal("A", transposed.Columns[1]);
        Assert.Equal("C", transposed.Columns[2]);

        Assert.Single(transposed.Rows);
        Assert.Equal("Col2", transposed.Rows[0][0]);
        Assert.Equal("B", transposed.Rows[0][1]);
        Assert.Equal("D", transposed.Rows[0][2]);
    }

    [Fact]
    public void Tabular_SqlInsertStatements()
    {
        var table = new TabularData
        {
            Columns = new List<string> { "Id", "Name", "Score" },
            Rows = new List<List<string>>
            {
                new() { "1", "Alice", "95.5" },
                new() { "2", "Bob's Team", "80" }
            }
        };

        string sql = TabularConverter.ToSqlInsertStatements(table, "Users");
        Assert.Contains("INSERT INTO [Users] ([Id], [Name], [Score]) VALUES (1, 'Alice', 95.5);", sql);
        Assert.Contains("INSERT INTO [Users] ([Id], [Name], [Score]) VALUES (2, 'Bob''s Team', 80);", sql);
    }

    [Fact]
    public void Developer_SqlInClause_NumbersAndStrings()
    {
        string numbers = "101\n102\n103";
        string sqlNumbers = DeveloperTransformers.ToSqlInClause(numbers);
        Assert.Equal("IN (101, 102, 103)", sqlNumbers);

        string strings = "apple\nbanana\no'reilly";
        string sqlStrings = DeveloperTransformers.ToSqlInClause(strings);
        Assert.Equal("IN ('apple', 'banana', 'o''reilly')", sqlStrings);
    }

    [Fact]
    public void Developer_CSharpAndTypeScriptArrays()
    {
        string numbers = "1\n2\n3";
        string cs = DeveloperTransformers.ToCSharpArray(numbers);
        Assert.Contains("var items = new int[]", cs);

        string ts = DeveloperTransformers.ToTypeScriptArray("foo\nbar");
        Assert.Contains("\"foo\"", ts);
        Assert.Contains("\"bar\"", ts);
    }

    [Fact]
    public void Developer_QueryStringToKeyValueAndJson()
    {
        string query = "?name=John%20Doe&age=30&active=true";
        string kv = DeveloperTransformers.QueryStringToKeyValuePairs(query);
        Assert.Contains("name: John Doe", kv);
        Assert.Contains("age: 30", kv);

        string json = DeveloperTransformers.KeyValuePairsToJson(kv);
        Assert.Contains("\"name\": \"John Doe\"", json);
        Assert.Contains("\"age\": 30", json);
    }

    [Fact]
    public void CaseTransformers_ConvertsVariousCasings()
    {
        string input = "user_account_balance";
        Assert.Equal("userAccountBalance", CaseTransformers.ChangeCase(input, TextCasing.CamelCase));
        Assert.Equal("UserAccountBalance", CaseTransformers.ChangeCase(input, TextCasing.PascalCase));
        Assert.Equal("user-account-balance", CaseTransformers.ChangeCase(input, TextCasing.KebabCase));
        Assert.Equal("USER_ACCOUNT_BALANCE", CaseTransformers.ChangeCase(input, TextCasing.ConstantCase));
        Assert.Equal("User Account Balance", CaseTransformers.ChangeCase(input, TextCasing.TitleCase));
    }

    [Fact]
    public void EncodingTransformers_Base64AndUrlAndCSharp()
    {
        string raw = "Hello World! Special & chars.";
        string encoded = EncodingTransformers.Base64Encode(raw);
        string decoded = EncodingTransformers.Base64Decode(encoded);
        Assert.Equal(raw, decoded);

        string csharpEscaped = EncodingTransformers.EscapeCSharpString("Hello \"World\"\r\nTab\there");
        Assert.Equal("\"Hello \\\"World\\\"\\r\\nTab\\there\"", csharpEscaped);
        Assert.Equal("Hello \"World\"\r\nTab\there", EncodingTransformers.UnescapeCSharpString(csharpEscaped));
    }

    [Fact]
    public void TextAnalyzer_DetectsFormatsAndMetrics()
    {
        string numbers = "10\n20\n30\n40";
        var res1 = TextAnalyzer.Analyze(numbers);
        Assert.Equal(DetectedFormat.MultiLineNumbers, res1.Format);
        Assert.Equal(4, res1.NonEmptyLineCount);

        string csv = "Name,Age\nAlice,30\nBob,25";
        var res2 = TextAnalyzer.Analyze(csv);
        Assert.True(res2.IsTabular);
        Assert.Equal(DetectedFormat.CsvTable, res2.Format);
        Assert.Equal(2, res2.ColumnCount);
        Assert.Equal(2, res2.RowCount);
    }

    [Fact]
    public void Developer_SqlInMultiLine()
    {
        string ids = "101\n102\n103";
        string sql = DeveloperTransformers.ToSqlInClause(ids, multiLine: true);
        Assert.Contains("IN (", sql);
        Assert.Contains("    101,", sql);
        Assert.Contains("    102,", sql);
        Assert.Contains("    103", sql);
        Assert.EndsWith(")", sql);
    }

    [Fact]
    public void Tabular_SelectColumns()
    {
        string csv = "ColA,ColB,ColC\n1,2,3\n4,5,6";
        var table = TabularParser.Parse(csv, ',');
        var subTable = table.SelectColumns(new[] { 0, 2 });
        Assert.Equal(2, subTable.Columns.Count);
        Assert.Equal("ColA", subTable.Columns[0]);
        Assert.Equal("ColC", subTable.Columns[1]);
        Assert.Equal("1", subTable.Rows[0][0]);
        Assert.Equal("3", subTable.Rows[0][1]);
    }

    [Fact]
    public void Tabular_ExtractColumn()
    {
        string csv = "Name,Age,City\nAlice,30,NYC\nBob,25,LA";
        var table = TabularParser.Parse(csv, ',');
        var ages = table.ExtractColumn(1);
        Assert.Equal(2, ages.Count);
        Assert.Equal("30", ages[0]);
        Assert.Equal("25", ages[1]);
    }

    [Fact]
    public void Encoding_JwtDecode()
    {
        // Sample token with {"alg":"HS256","typ":"JWT"} and {"sub":"1234567890","name":"John Doe"}
        string jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIn0.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        string decoded = EncodingTransformers.JwtDecode(jwt);
        Assert.Contains("\"alg\": \"HS256\"", decoded);
        Assert.Contains("\"name\": \"John Doe\"", decoded);
    }

    [Fact]
    public void LineTransformers_PrefixSuffixAndCustomQuotes()
    {
        string input = "Alpha\nBeta";
        string quoted = LineTransformers.QuoteLines(input, QuoteStyle.Custom, customPrefix: "<b>", customSuffix: "</b>");
        string expected = "<b>Alpha</b>\n<b>Beta</b>".Replace("\n", Environment.NewLine);
        Assert.Equal(expected, quoted);

        string prefixed = LineTransformers.AddPrefixSuffix(input, prefix: "Item: ", suffix: ";");
        string expectedPrefixed = "Item: Alpha;\nItem: Beta;".Replace("\n", Environment.NewLine);
        Assert.Equal(expectedPrefixed, prefixed);
    }

    [Fact]
    public void LineTransformers_PrefixSuffix_SkipFirstAndLastRowOptions()
    {
        string input = "Alpha\nBeta\nGamma";

        // Skip first row on prefix
        string res1 = LineTransformers.AddPrefixSuffix(input, prefix: "AND ", suffix: "", prefixSkipFirst: true);
        string expected1 = "Alpha\nAND Beta\nAND Gamma".Replace("\n", Environment.NewLine);
        Assert.Equal(expected1, res1);

        // Skip last row on suffix
        string res2 = LineTransformers.AddPrefixSuffix(input, prefix: "", suffix: ",", suffixSkipLast: true);
        string expected2 = "Alpha,\nBeta,\nGamma".Replace("\n", Environment.NewLine);
        Assert.Equal(expected2, res2);

        // Combined: skip first on prefix, skip last on suffix
        string res3 = LineTransformers.AddPrefixSuffix(input, prefix: "AND ", suffix: ",", prefixSkipFirst: true, suffixSkipLast: true);
        string expected3 = "Alpha,\nAND Beta,\nAND Gamma".Replace("\n", Environment.NewLine);
        Assert.Equal(expected3, res3);

        // Skip last on prefix, skip first on suffix
        string res4 = LineTransformers.AddPrefixSuffix(input, prefix: "[P] ", suffix: " [S]", prefixSkipLast: true, suffixSkipFirst: true);
        string expected4 = "[P] Alpha\n[P] Beta [S]\nGamma [S]".Replace("\n", Environment.NewLine);
        Assert.Equal(expected4, res4);

        // Skip first and last on prefix
        string res5 = LineTransformers.AddPrefixSuffix(input, prefix: "-> ", suffix: "", prefixSkipFirst: true, prefixSkipLast: true);
        string expected5 = "Alpha\n-> Beta\nGamma".Replace("\n", Environment.NewLine);
        Assert.Equal(expected5, res5);
    }

    [Fact]
    public void LineTransformers_PrefixSuffix_WithEmptyLinesAndSkipOptions()
    {
        string input = "\nFirst\n\nSecond\nThird\n";

        // skipEmpty = true: First is at index 1, Third is at index 4
        string res = LineTransformers.AddPrefixSuffix(input, prefix: "START ", suffix: " END", skipEmpty: true, prefixSkipFirst: true, suffixSkipLast: true);
        string expected = "\nFirst END\n\nSTART Second END\nSTART Third\n".Replace("\n", Environment.NewLine);
        Assert.Equal(expected, res);
    }

    [Fact]
    public void LineTransformers_ReplaceInLines_BasicAndCaseSensitivity()
    {
        string input = "Hello World\nhello everyone\nHELLO ALL";
        
        // Case-insensitive replace
        string result1 = LineTransformers.ReplaceInLines(input, "hello", "Hi", caseSensitive: false);
        string expected1 = "Hi World\nHi everyone\nHi ALL".Replace("\n", Environment.NewLine);
        Assert.Equal(expected1, result1);

        // Case-sensitive replace
        string result2 = LineTransformers.ReplaceInLines(input, "hello", "Hi", caseSensitive: true);
        string expected2 = "Hello World\nHi everyone\nHELLO ALL".Replace("\n", Environment.NewLine);
        Assert.Equal(expected2, result2);
    }

    [Fact]
    public void LineTransformers_ReplaceInLines_RegexAndCaptureGroups()
    {
        string input = "order-1234\norder-5678\nuser-9999";
        string result = LineTransformers.ReplaceInLines(input, @"order-(\d+)", "invoice_$1", isRegex: true);
        string expected = "invoice_1234\ninvoice_5678\nuser-9999".Replace("\n", Environment.NewLine);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void LineTransformers_ReplaceInLines_SkipEmptyAndMultipleOccurrences()
    {
        string input = "foo bar foo\n\nfoo baz foo";
        string result = LineTransformers.ReplaceInLines(input, "foo", "qux", skipEmpty: true);
        string expected = "qux bar qux\n\nqux baz qux".Replace("\n", Environment.NewLine);
        Assert.Equal(expected, result);
    }
}
