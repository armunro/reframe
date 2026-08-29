using Reframe.Core.Tabular.Parsers;
using Reframe.ViewModels;
using Xunit;

namespace Reframe.Tests;

public class SampleDataTests
{
    [Fact]
    public void LoadSampleCommand_Emails_LoadsEmailList()
    {
        var vm = new MainViewModel();
        vm.LoadSampleCommand.Execute("emails");

        Assert.NotEmpty(vm.InputText);
        Assert.Contains("john.doe@example.com", vm.InputText);
        Assert.Contains("tony.stark@stark-industries.com", vm.InputText);

        var lines = vm.InputText.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.True(lines.Length >= 20);
        foreach (var line in lines)
        {
            Assert.Contains("@", line);
        }
    }

    [Fact]
    public void LoadSampleCommand_LargeTable_HasOver100RowsAndParsesAsTabularData()
    {
        var vm = new MainViewModel();
        vm.LoadSampleCommand.Execute("large_table");

        Assert.NotEmpty(vm.InputText);
        var table = TabularParser.DetectAndParse(vm.InputText, true);

        Assert.NotNull(table);
        Assert.True(table.Rows.Count > 100, $"Expected > 100 rows, but got {table.Rows.Count}");
        Assert.Equal(125, table.Rows.Count);
        Assert.Contains("ID", table.Columns);
        Assert.Contains("FirstName", table.Columns);
        Assert.Contains("LastName", table.Columns);
        Assert.Contains("Email", table.Columns);
        Assert.Contains("Salary", table.Columns);

        // Verify Tabular ViewModel state
        Assert.True(vm.HasTabularData);
        Assert.NotNull(vm.PreviewDataTable);
        Assert.True(vm.PreviewDataTable.Rows.Count > 100);
    }

    [Fact]
    public void LoadSampleCommand_Logs_LoadsServerLogs()
    {
        var vm = new MainViewModel();
        vm.LoadSampleCommand.Execute("logs");

        Assert.NotEmpty(vm.InputText);
        Assert.Contains("GET /api/v1/users", vm.InputText);
        Assert.Contains("HTTP/1.1", vm.InputText);
    }

    [Theory]
    [InlineData("html")]
    [InlineData("numbers")]
    [InlineData("csv")]
    [InlineData("tsv")]
    [InlineData("markdown")]
    [InlineData("json")]
    [InlineData("yaml")]
    [InlineData("xml")]
    [InlineData("delimited")]
    [InlineData("query")]
    public void LoadSampleCommand_AllStandardSamples_LoadSuccessfully(string sampleType)
    {
        var vm = new MainViewModel();
        vm.LoadSampleCommand.Execute(sampleType);

        Assert.False(string.IsNullOrWhiteSpace(vm.InputText));
        Assert.Equal($"Loaded sample: {sampleType}", vm.StatusMessage);
    }
}
