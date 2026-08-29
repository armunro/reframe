using System;
using System.Linq;
using Reframe.Core.Analysis.Analyzers;
using Reframe.Core.Analysis.Models;
using Reframe.Core.Tabular.Converters;
using Reframe.Core.Tabular.Parsers;
using Reframe.ViewModels;
using Xunit;

namespace Reframe.Tests;

public class LineListTabularParserTests
{
    [Fact]
    public void Parse_ListOfNumbers_ParsesAsSingleColumnTableWithNoHeaderByDefault()
    {
        string input = "1001\n1002\n1003\n1004\n1005";
        var table = LineListTabularParser.Instance.Parse(input);

        Assert.NotNull(table);
        Assert.Single(table.Columns);
        Assert.Equal("Column 1", table.Columns[0]);
        Assert.Equal(5, table.Rows.Count);
        Assert.Equal("1001", table.Rows[0][0]);
        Assert.Equal("1005", table.Rows[4][0]);
    }

    [Fact]
    public void Parse_ListOfText_ParsesAsSingleColumnTable()
    {
        string input = "AAA\nBBB\nCCC\nDDD\nEEE";
        var table = LineListTabularParser.Instance.Parse(input);

        Assert.NotNull(table);
        Assert.Single(table.Columns);
        Assert.Equal(5, table.Rows.Count);
        Assert.Equal("AAA", table.Rows[0][0]);
        Assert.Equal("EEE", table.Rows[4][0]);
    }

    [Fact]
    public void Parse_WithExplicitHeaderFlagTrue_UsesFirstLineAsHeader()
    {
        string input = "EmployeeID\n1001\n1002\n1003";
        var table = LineListTabularParser.Instance.Parse(input, assumeHeader: true);

        Assert.NotNull(table);
        Assert.Single(table.Columns);
        Assert.Equal("EmployeeID", table.Columns[0]);
        Assert.Equal(3, table.Rows.Count);
        Assert.Equal("1001", table.Rows[0][0]);
    }

    [Fact]
    public void Parse_WithSurrogateHeaders_OverridesColumnName()
    {
        string input = "1001\n1002\n1003";
        var table = LineListTabularParser.Instance.Parse(input, assumeHeader: false, surrogateHeaders: new[] { "Code" });

        Assert.NotNull(table);
        Assert.Single(table.Columns);
        Assert.Equal("Code", table.Columns[0]);
        Assert.Equal(3, table.Rows.Count);
    }

    [Fact]
    public void DefaultTextAnalyzer_MultiLineNumbers_DetectsAsMultiLineNumbers()
    {
        string input = "1001\n1002\n1003\n1004\n1005";
        var analyzer = new DefaultTextAnalyzer();
        var result = analyzer.Analyze(input);

        Assert.Equal(DetectedFormat.MultiLineNumbers, result.Format);
        Assert.False(result.IsTabular);
        Assert.Equal(5, result.NonEmptyLineCount);
    }

    [Fact]
    public void MainViewModel_LinesToTable_SetsTableAndConverts()
    {
        var vm = new MainViewModel
        {
            InputText = "1001\n1002\n1003\n1004\n1005"
        };

        vm.ActionCommand.Execute("LinesToTable");

        Assert.True(vm.HasTabularData);
        Assert.NotNull(vm.PreviewDataTable);
        Assert.Single(vm.PreviewDataTable.Columns);
        Assert.Equal(5, vm.PreviewDataTable.Rows.Count);

        vm.ActionCommand.Execute("ToMarkdownTable");
        Assert.Contains("Column 1", vm.OutputText);
        Assert.Contains("1001", vm.OutputText);
        Assert.Contains("1005", vm.OutputText);
    }

    [Fact]
    public void MainViewModel_SplitLinesToTable_SplitsIntoMultipleColumns()
    {
        var vm = new MainViewModel
        {
            InputText = "1001-A-North\n1002-B-South\n1003-C-East",
            SplitDelimiter = "-"
        };

        vm.ActionCommand.Execute("SplitLinesToTable");

        Assert.True(vm.HasTabularData);
        Assert.NotNull(vm.PreviewDataTable);
        Assert.Equal(3, vm.PreviewDataTable.Columns.Count);
        Assert.Equal(3, vm.PreviewDataTable.Rows.Count);
    }

    [Fact]
    public void MainViewModel_AddCalculatedColumn_WorksOnListOfNumbers()
    {
        var vm = new MainViewModel
        {
            InputText = "1001\n1002\n1003\n1004\n1005",
            TableNewColumnFormula = "CONCAT('ID-', [Column 1])"
        };

        vm.ActionCommand.Execute("LinesToTable");
        vm.ActionCommand.Execute("AddCalculatedColumn");
        Assert.Contains("ID-1001", vm.OutputText);
        Assert.Contains("ID-1005", vm.OutputText);
    }
}
