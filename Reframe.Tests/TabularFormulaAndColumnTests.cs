using System;
using System.Collections.Generic;
using Reframe.Core.Tabular;
using Reframe.Core.Tabular.Converters;
using Reframe.Core.Tabular.Formulas;
using Reframe.Core.Tabular.Models;
using Reframe.Core.Tabular.Parsers;
using Xunit;

namespace Reframe.Tests;

public class TabularFormulaAndColumnTests
{
    private TabularData CreateSampleTable()
    {
        return new TabularData
        {
            Columns = new List<string> { "First Name", "Last Name", "Email", "SKU", "Price", "Qty" },
            Rows = new List<List<string>>
            {
                new() { "Alice", "Smith", "alice@example.com", "PROD-1029-US", "19.95", "3" },
                new() { "Bob", "Jones", "bob@internal.org", "PROD-4058-EU", "5.50", "10" },
                new() { "Charlie", "Brown", "charlie@example.com", "DEV-9912-APAC", "120.00", "1" }
            },
            HasHeaders = true,
            Delimiter = ','
        };
    }

    [Fact]
    public void TabularFormulaEngine_ConcatAndAmpersand_CombinesColumnsCorrectly()
    {
        var table = CreateSampleTable();

        // Using & operator
        var fullName1 = TabularFormulaEngine.Evaluate("[First Name] & \" \" & [Last Name]", table.Columns, table.Rows[0]);
        Assert.Equal("Alice Smith", fullName1);

        // Using CONCAT function
        var fullName2 = TabularFormulaEngine.Evaluate("=CONCAT([First Name], \", \", [Last Name])", table.Columns, table.Rows[1]);
        Assert.Equal("Bob, Jones", fullName2);

        // Using Column Letters A & B
        var fullName3 = TabularFormulaEngine.Evaluate("CONCAT(A, \" - \", B)", table.Columns, table.Rows[2]);
        Assert.Equal("Charlie - Brown", fullName3);
    }

    [Fact]
    public void TabularFormulaEngine_JoinAndTextJoin_JoinsTextWithDelimiter()
    {
        var table = CreateSampleTable();

        var joined = TabularFormulaEngine.Evaluate("JOIN(\" | \", [First Name], [Last Name], [Email])", table.Columns, table.Rows[0]);
        Assert.Equal("Alice | Smith | alice@example.com", joined);

        var textJoined = TabularFormulaEngine.Evaluate("TEXTJOIN(\"-\", TRUE, [First Name], \"\", [Last Name])", table.Columns, table.Rows[0]);
        Assert.Equal("Alice-Smith", textJoined);
    }

    [Fact]
    public void TabularFormulaEngine_SplitAndSplitPart_SplitsCellDataIntoNewParts()
    {
        var table = CreateSampleTable();

        // SPLIT by hyphen, 1st part
        var part1 = TabularFormulaEngine.Evaluate("SPLIT([SKU], \"-\", 1)", table.Columns, table.Rows[0]);
        Assert.Equal("PROD", part1);

        // SPLIT by hyphen, 2nd part
        var part2 = TabularFormulaEngine.Evaluate("SPLIT([SKU], \"-\", 2)", table.Columns, table.Rows[0]);
        Assert.Equal("1029", part2);

        // SPLIT by hyphen, last part (negative index -1)
        var partLast = TabularFormulaEngine.Evaluate("SPLIT([SKU], \"-\", -1)", table.Columns, table.Rows[0]);
        Assert.Equal("US", partLast);

        // SPLIT_PART alias
        var emailDomain = TabularFormulaEngine.Evaluate("SPLIT_PART([Email], \"@\", 2)", table.Columns, table.Rows[0]);
        Assert.Equal("example.com", emailDomain);

        // Out of bounds returns empty string without error
        var outOfBounds = TabularFormulaEngine.Evaluate("SPLIT([SKU], \"-\", 10)", table.Columns, table.Rows[0]);
        Assert.Equal(string.Empty, outOfBounds);
    }

    [Fact]
    public void TabularFormulaEngine_SubstringMidLeftRight_SlicesTextAccurately()
    {
        var table = CreateSampleTable();

        var sub = TabularFormulaEngine.Evaluate("SUBSTRING([SKU], 1, 4)", table.Columns, table.Rows[0]);
        Assert.Equal("PROD", sub);

        var mid = TabularFormulaEngine.Evaluate("MID([SKU], 6, 4)", table.Columns, table.Rows[0]);
        Assert.Equal("1029", mid);

        var left = TabularFormulaEngine.Evaluate("LEFT([First Name], 3)", table.Columns, table.Rows[0]);
        Assert.Equal("Ali", left);

        var right = TabularFormulaEngine.Evaluate("RIGHT([Email], 3)", table.Columns, table.Rows[0]);
        Assert.Equal("com", right);
    }

    [Fact]
    public void TabularFormulaEngine_FindSearchIndexOf_LocatesCharacters()
    {
        var table = CreateSampleTable();

        // FIND (case-sensitive)
        var findPos = TabularFormulaEngine.Evaluate("FIND(\"-\", [SKU])", table.Columns, table.Rows[0]);
        Assert.Equal("5", findPos);

        // SEARCH (case-insensitive)
        var searchPos = TabularFormulaEngine.Evaluate("SEARCH(\"ALICE\", [Email])", table.Columns, table.Rows[0]);
        Assert.Equal("1", searchPos);

        // INDEXOF
        var indexOf = TabularFormulaEngine.Evaluate("INDEXOF([Email], \"@\")", table.Columns, table.Rows[0]);
        Assert.Equal("6", indexOf);

        // Not found returns 0
        var notFound = TabularFormulaEngine.Evaluate("FIND(\"XYZ\", [SKU])", table.Columns, table.Rows[0]);
        Assert.Equal("0", notFound);
    }

    [Fact]
    public void TabularFormulaEngine_ReplaceAndSubstitute_ReplacesText()
    {
        var table = CreateSampleTable();

        var replaced = TabularFormulaEngine.Evaluate("REPLACE([SKU], \"-\", \"_\")", table.Columns, table.Rows[0]);
        Assert.Equal("PROD_1029_US", replaced);

        var substituted = TabularFormulaEngine.Evaluate("SUBSTITUTE([SKU], \"-\", \":\", 1)", table.Columns, table.Rows[0]);
        Assert.Equal("PROD:1029-US", substituted);
    }

    [Fact]
    public void TabularFormulaEngine_RegexFunctions_MatchExtractAndReplace()
    {
        var table = CreateSampleTable();

        // REGEXMATCH
        var isMatch = TabularFormulaEngine.Evaluate(@"REGEXMATCH([Email], ""@example\.com$"")", table.Columns, table.Rows[0]);
        Assert.Equal("TRUE", isMatch);

        var isNotMatch = TabularFormulaEngine.Evaluate(@"REGEXMATCH([Email], ""@example\.com$"")", table.Columns, table.Rows[1]);
        Assert.Equal("FALSE", isNotMatch);

        // REGEXEXTRACT
        var extractedNum = TabularFormulaEngine.Evaluate(@"REGEXEXTRACT([SKU], ""\d+"")", table.Columns, table.Rows[0]);
        Assert.Equal("1029", extractedNum);

        var extractedGroup = TabularFormulaEngine.Evaluate(@"REGEXEXTRACT([Email], ""@(.+)$"", 1)", table.Columns, table.Rows[0]);
        Assert.Equal("example.com", extractedGroup);

        // REGEXREPLACE
        var replaced = TabularFormulaEngine.Evaluate(@"REGEXREPLACE([SKU], ""[A-Z]+"", ""ITEM"")", table.Columns, table.Rows[0]);
        Assert.Equal("ITEM-1029-ITEM", replaced);
    }

    [Fact]
    public void TabularFormulaEngine_CasingFormattingAndUtilities_WorkAsExpected()
    {
        var table = CreateSampleTable();

        Assert.Equal("ALICE", TabularFormulaEngine.Evaluate("UPPER([First Name])", table.Columns, table.Rows[0]));
        Assert.Equal("alice smith", TabularFormulaEngine.Evaluate("LOWER([First Name] & \" \" & [Last Name])", table.Columns, table.Rows[0]));
        Assert.Equal("5", TabularFormulaEngine.Evaluate("LEN([First Name])", table.Columns, table.Rows[0]));
        Assert.Equal("00003", TabularFormulaEngine.Evaluate("PADLEFT([Qty], 5, \"0\")", table.Columns, table.Rows[0]));
        Assert.Equal("ecilA", TabularFormulaEngine.Evaluate("REVERSE([First Name])", table.Columns, table.Rows[0]));
        Assert.Equal("1", TabularFormulaEngine.Evaluate("ROW()", table.Columns, table.Rows[0], rowIndex: 0));
        Assert.Equal("2", TabularFormulaEngine.Evaluate("ROW()", table.Columns, table.Rows[1], rowIndex: 1));
    }

    [Fact]
    public void TabularFormulaEngine_LogicFunctions_EvaluateConditionalBranches()
    {
        var table = CreateSampleTable();

        // IF
        var ifResult1 = TabularFormulaEngine.Evaluate("IF([Qty] > 5, \"Bulk\", \"Single\")", table.Columns, table.Rows[0]);
        Assert.Equal("Single", ifResult1);

        var ifResult2 = TabularFormulaEngine.Evaluate("IF([Qty] > 5, \"Bulk\", \"Single\")", table.Columns, table.Rows[1]);
        Assert.Equal("Bulk", ifResult2);

        // IFS
        var ifsResult = TabularFormulaEngine.Evaluate("IFS([Price] > 100, \"High\", [Price] > 10, \"Medium\", TRUE, \"Low\")", table.Columns, table.Rows[0]);
        Assert.Equal("Medium", ifsResult);

        // SWITCH
        var switchResult = TabularFormulaEngine.Evaluate("SWITCH([First Name], \"Alice\", \"Admin\", \"Bob\", \"User\", \"Guest\")", table.Columns, table.Rows[0]);
        Assert.Equal("Admin", switchResult);

        // COALESCE
        var coalesceResult = TabularFormulaEngine.Evaluate("COALESCE(\"\", [First Name], \"Fallback\")", table.Columns, table.Rows[0]);
        Assert.Equal("Alice", coalesceResult);
    }

    [Fact]
    public void TabularFormulaEngine_Arithmetic_CalculatesNumbers()
    {
        var table = CreateSampleTable();

        var total = TabularFormulaEngine.Evaluate("[Price] * [Qty]", table.Columns, table.Rows[0]);
        Assert.Equal("59.85", total);

        var rounded = TabularFormulaEngine.Evaluate("ROUND([Price] * 1.0825, 2)", table.Columns, table.Rows[0]);
        Assert.Equal("21.6", rounded);

        var sum = TabularFormulaEngine.Evaluate("SUM([Qty], 10, 5)", table.Columns, table.Rows[0]);
        Assert.Equal("18", sum);
    }

    [Fact]
    public void TabularData_AddCalculatedColumn_AddsNewColumnAcrossAllRows()
    {
        var table = CreateSampleTable();

        var newTable = table.AddCalculatedColumn("Full Name", "[First Name] & \" \" & [Last Name]");

        Assert.Equal(7, newTable.Columns.Count);
        Assert.Equal("Full Name", newTable.Columns[6]);
        Assert.Equal("Alice Smith", newTable.Rows[0][6]);
        Assert.Equal("Bob Jones", newTable.Rows[1][6]);
        Assert.Equal("Charlie Brown", newTable.Rows[2][6]);
    }

    [Fact]
    public void TabularData_AddCalculatedColumn_WithInsertIndex_InsertsAtTargetPosition()
    {
        var table = CreateSampleTable();

        var newTable = table.AddCalculatedColumn("Full Name", "[First Name] & \" \" & [Last Name]", insertIndex: 2);

        Assert.Equal(7, newTable.Columns.Count);
        Assert.Equal("Full Name", newTable.Columns[2]);
        Assert.Equal("Alice Smith", newTable.Rows[0][2]);
        Assert.Equal("Bob Jones", newTable.Rows[1][2]);
        Assert.Equal("Charlie Brown", newTable.Rows[2][2]);
        Assert.Equal("Email", newTable.Columns[3]);
    }

    [Fact]
    public void TabularData_RemoveColumn_ByIndexAndName_RemovesAccurately()
    {
        var table = CreateSampleTable();

        // Remove by Index 0 ("First Name")
        var tableNoFirst = table.RemoveColumn(0);
        Assert.Equal(5, tableNoFirst.Columns.Count);
        Assert.Equal("Last Name", tableNoFirst.Columns[0]);
        Assert.Equal("Smith", tableNoFirst.Rows[0][0]);

        // Remove by Name ("Email")
        var tableNoEmail = table.RemoveColumn("Email");
        Assert.Equal(5, tableNoEmail.Columns.Count);
        Assert.DoesNotContain("Email", tableNoEmail.Columns);
        Assert.Equal("SKU", tableNoEmail.Columns[2]);
        Assert.Equal("PROD-1029-US", tableNoEmail.Rows[0][2]);

        // Remove multiple by names
        var tableClean = table.RemoveColumns(new[] { "First Name", "Last Name", "SKU" });
        Assert.Equal(3, tableClean.Columns.Count);
        Assert.Equal(new[] { "Email", "Price", "Qty" }, tableClean.Columns);
    }

    [Fact]
    public void TabularFormulaEngine_AvailableFunctionsCatalog_ReturnsHelpList()
    {
        var functions = TabularFormulaEngine.GetAvailableFunctions();
        Assert.NotEmpty(functions);
        Assert.Contains(functions, f => f.Name == "CONCAT");
        Assert.Contains(functions, f => f.Name == "SPLIT");
        Assert.Contains(functions, f => f.Name == "REGEXEXTRACT");
        Assert.Contains(functions, f => f.Name == "SUBSTRING");
    }
}
