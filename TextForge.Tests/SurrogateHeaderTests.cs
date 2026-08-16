using System.Data;
using TextForge.Core.Tabular;
using TextForge.ViewModels;
using Xunit;

namespace TextForge.Tests;

public class SurrogateHeaderTests
{
    [Fact]
    public void TabularData_OverrideHeaders_OnTableWithoutHeaders_SetsHeadersAndKeepsAllRows()
    {
        var table = new TabularData
        {
            Delimiter = ',',
            HasHeaders = false,
            Columns = new List<string> { "Column 1", "Column 2", "Column 3" },
            Rows = new List<List<string>>
            {
                new() { "101", "Alice", "Developer" },
                new() { "102", "Bob", "Designer" }
            }
        };

        table.OverrideHeaders(new[] { "ID", "Name", "Role" });

        Assert.True(table.HasHeaders);
        Assert.Equal(new[] { "ID", "Name", "Role" }, table.Columns);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("101", table.Rows[0][0]);
        Assert.Equal("Alice", table.Rows[0][1]);
    }

    [Fact]
    public void TabularData_OverrideHeaders_OnTableWithExistingHeaders_ReplacesHeaderNames()
    {
        var table = new TabularData
        {
            Delimiter = ',',
            HasHeaders = true,
            Columns = new List<string> { "c1", "c2", "c3" },
            Rows = new List<List<string>>
            {
                new() { "101", "Alice", "Developer" },
                new() { "102", "Bob", "Designer" }
            }
        };

        table.OverrideHeaders(new[] { "User_ID", "Full_Name", "Position" });

        Assert.True(table.HasHeaders);
        Assert.Equal(new[] { "User_ID", "Full_Name", "Position" }, table.Columns);
        Assert.Equal(2, table.Rows.Count);
    }

    [Fact]
    public void TabularData_OverrideHeaders_WithFewerHeaders_PadsRemainingColumns()
    {
        var table = new TabularData
        {
            Delimiter = ',',
            HasHeaders = true,
            Columns = new List<string> { "A", "B", "C" },
            Rows = new List<List<string>>
            {
                new() { "1", "2", "3" }
            }
        };

        table.OverrideHeaders(new[] { "First" });

        Assert.Equal(3, table.Columns.Count);
        Assert.Equal("First", table.Columns[0]);
        Assert.Equal("B", table.Columns[1]);
        Assert.Equal("C", table.Columns[2]);
    }

    [Fact]
    public void TabularData_OverrideHeaders_WithMoreHeaders_ExpandsColumnsAndPadsRows()
    {
        var table = new TabularData
        {
            Delimiter = ',',
            HasHeaders = true,
            Columns = new List<string> { "A", "B" },
            Rows = new List<List<string>>
            {
                new() { "1", "2" }
            }
        };

        table.OverrideHeaders(new[] { "ColA", "ColB", "ColC", "ColD" });

        Assert.Equal(4, table.Columns.Count);
        Assert.Equal(new[] { "ColA", "ColB", "ColC", "ColD" }, table.Columns);
        Assert.Equal(4, table.Rows[0].Count);
        Assert.Equal("1", table.Rows[0][0]);
        Assert.Equal("2", table.Rows[0][1]);
        Assert.Equal("", table.Rows[0][2]);
        Assert.Equal("", table.Rows[0][3]);
    }

    [Fact]
    public void TabularData_WithSurrogateHeaders_ReturnsNewInstanceWithoutMutatingOriginal()
    {
        var original = new TabularData
        {
            Delimiter = ',',
            HasHeaders = true,
            Columns = new List<string> { "Original1", "Original2" },
            Rows = new List<List<string>>
            {
                new() { "V1", "V2" }
            }
        };

        var modified = original.WithSurrogateHeaders(new[] { "New1", "New2" });

        Assert.Equal(new[] { "Original1", "Original2" }, original.Columns);
        Assert.Equal(new[] { "New1", "New2" }, modified.Columns);
    }

    [Fact]
    public void TabularData_SetSurrogateHeaders_GeneratesDefaultColumnNames()
    {
        var table = new TabularData
        {
            Delimiter = ',',
            HasHeaders = false,
            Columns = new List<string>(),
            Rows = new List<List<string>>
            {
                new() { "A", "B", "C" }
            }
        };

        table.SetSurrogateHeaders("Field_");

        Assert.True(table.HasHeaders);
        Assert.Equal(new[] { "Field_1", "Field_2", "Field_3" }, table.Columns);
    }

    [Fact]
    public void TabularParser_ParseHeaderList_HandlesCommaTabPipeSemicolonAndMultiline()
    {
        var fromComma = TabularParser.ParseHeaderList("id, name, email");
        Assert.Equal(new[] { "id", "name", "email" }, fromComma);

        var fromTab = TabularParser.ParseHeaderList("id\tname\temail");
        Assert.Equal(new[] { "id", "name", "email" }, fromTab);

        var fromPipe = TabularParser.ParseHeaderList("id | name | email");
        Assert.Equal(new[] { "id", "name", "email" }, fromPipe);

        var fromSemicolon = TabularParser.ParseHeaderList("id; name; email");
        Assert.Equal(new[] { "id", "name", "email" }, fromSemicolon);

        var fromMultiline = TabularParser.ParseHeaderList("id\r\nname\r\nemail");
        Assert.Equal(new[] { "id", "name", "email" }, fromMultiline);
    }

    [Fact]
    public void TabularParser_ParseHeaderList_HandlesQuotesAndSpaces()
    {
        var fromQuotes = TabularParser.ParseHeaderList("\"User ID\", \"Full Name\", \"Email Address\"");
        Assert.Equal(new[] { "User ID", "Full Name", "Email Address" }, fromQuotes);

        var fromSpaces = TabularParser.ParseHeaderList("alpha beta gamma");
        Assert.Equal(new[] { "alpha", "beta", "gamma" }, fromSpaces);
    }

    [Fact]
    public void TabularParser_DetectAndParse_WithSurrogateHeaders_AppliesHeadersDirectly()
    {
        string csv = "101,John,Doe\n102,Jane,Smith";
        var table = TabularParser.DetectAndParse(csv, assumeHeader: false, surrogateHeaders: new[] { "ID", "First", "Last" });

        Assert.NotNull(table);
        Assert.True(table.HasHeaders);
        Assert.Equal(new[] { "ID", "First", "Last" }, table.Columns);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("101", table.Rows[0][0]);
    }

    [Fact]
    public void TabularConverter_ToJsonArrayOfObjects_WithSurrogateHeaders_UsesSurrogateKeys()
    {
        var table = new TabularData
        {
            Delimiter = ',',
            HasHeaders = false,
            Columns = new List<string> { "Col1", "Col2" },
            Rows = new List<List<string>>
            {
                new() { "1", "Alice" },
                new() { "2", "Bob" }
            }
        };

        table.OverrideHeaders(new[] { "userId", "userName" });
        string json = TabularConverter.ToJsonArrayOfObjects(table);

        Assert.Contains("\"userId\": 1", json);
        Assert.Contains("\"userName\": \"Alice\"", json);
        Assert.Contains("\"userName\": \"Bob\"", json);
    }

    [Fact]
    public void TabularConverter_ToYaml_WithSurrogateHeaders_UsesSurrogateKeys()
    {
        var table = new TabularData
        {
            Delimiter = ',',
            HasHeaders = true,
            Columns = new List<string> { "c1", "c2" },
            Rows = new List<List<string>>
            {
                new() { "100", "Widget" }
            }
        };

        table.OverrideHeaders(new[] { "sku", "productName" });
        string yaml = TabularConverter.ToYaml(table);

        Assert.Contains("sku: 100", yaml);
        Assert.Contains("productName: Widget", yaml);
    }

    [Fact]
    public void TabularConverter_ToSqlInsertStatements_WithSurrogateHeaders_UsesSurrogateColumnNames()
    {
        var table = new TabularData
        {
            Delimiter = ',',
            HasHeaders = false,
            Columns = new List<string> { "Column 1", "Column 2" },
            Rows = new List<List<string>>
            {
                new() { "10", "Active" }
            }
        };

        table.OverrideHeaders(new[] { "account_id", "status" });
        string sql = TabularConverter.ToSqlInsertStatements(table, "Accounts");

        Assert.Contains("INSERT INTO [Accounts] ([account_id], [status]) VALUES (10, 'Active');", sql);
    }

    [Fact]
    public void MainViewModel_SurrogateHeaders_UpdatesDetectedColumnsAndPreview()
    {
        var vm = new MainViewModel();
        vm.InputText = "101,Alice,Developer\n102,Bob,Designer";
        vm.HasHeaders = false;

        Assert.Equal(new[] { "Column 1", "Column 2", "Column 3" }, vm.DetectedColumns);

        vm.SurrogateHeaders = "ID, Name, Title";

        Assert.Equal(new[] { "ID", "Name", "Title" }, vm.DetectedColumns);
        Assert.NotNull(vm.PreviewDataTable);
        Assert.Equal(3, vm.PreviewDataTable.Columns.Count);
        Assert.Equal("ID", vm.PreviewDataTable.Columns[0].ColumnName);
        Assert.Equal("Name", vm.PreviewDataTable.Columns[1].ColumnName);
        Assert.Equal("Title", vm.PreviewDataTable.Columns[2].ColumnName);
        Assert.Equal(2, vm.PreviewDataTable.Rows.Count);
    }

    [Fact]
    public void MainViewModel_SurrogateHeaders_ConversionsUseSurrogateHeaders()
    {
        var vm = new MainViewModel();
        vm.InputText = "101,Alice\n102,Bob";
        vm.HasHeaders = false;
        vm.SurrogateHeaders = "uid, username";

        vm.ActionCommand.Execute("ToJsonObjects");

        Assert.Contains("\"uid\": 101", vm.OutputText);
        Assert.Contains("\"username\": \"Alice\"", vm.OutputText);
        Assert.Contains("\"username\": \"Bob\"", vm.OutputText);

        vm.ActionCommand.Execute("ToYaml");

        Assert.Contains("uid: 101", vm.OutputText);
        Assert.Contains("username: Alice", vm.OutputText);
    }

    [Fact]
    public void MainViewModel_PrependSurrogateHeader_Action_FormatsTextWithHeaderRow()
    {
        var vm = new MainViewModel();
        vm.InputText = "101,Alice\n102,Bob";
        vm.HasHeaders = false;
        vm.SurrogateHeaders = "ID, Name";

        vm.ActionCommand.Execute("PrependSurrogateHeader");

        Assert.StartsWith("ID,Name", vm.OutputText);
        Assert.Contains("101,Alice", vm.OutputText);
        Assert.Contains("102,Bob", vm.OutputText);
    }

    [Fact]
    public void MainViewModel_GenerateSurrogateHeadersAction_PopulatesSurrogateHeaders()
    {
        var vm = new MainViewModel();
        vm.InputText = "10,20,30,40\n50,60,70,80";
        vm.HasHeaders = false;

        vm.ActionCommand.Execute("GenerateSurrogateHeaders");

        Assert.Equal("Col1, Col2, Col3, Col4", vm.SurrogateHeaders);
        Assert.Equal(new[] { "Col1", "Col2", "Col3", "Col4" }, vm.DetectedColumns);
    }

    [Fact]
    public void MainViewModel_ClearSurrogateHeadersAction_RestoresOriginalHeaders()
    {
        var vm = new MainViewModel();
        vm.InputText = "id,name\n101,Alice";
        vm.SurrogateHeaders = "custom_id, custom_name";

        Assert.Equal(new[] { "custom_id", "custom_name" }, vm.DetectedColumns);

        vm.ActionCommand.Execute("ClearSurrogateHeaders");

        Assert.Equal(string.Empty, vm.SurrogateHeaders);
        Assert.Equal(new[] { "id", "name" }, vm.DetectedColumns);
    }
}
