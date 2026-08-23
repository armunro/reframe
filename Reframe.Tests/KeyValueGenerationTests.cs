using System.Text.Json;
using Reframe.Core.Tabular;
using Reframe.Core.Tabular.Converters;
using Reframe.Core.Tabular.Parsers;
using Reframe.ViewModels;
using Xunit;

namespace Reframe.Tests;

public class KeyValueGenerationTests
{
    private const string SampleServerData = @"Environment,Server Name,Service / Role,Pipeline
ALL,PCU052A116,Management Server,N/A
PCU-PROD,PCU052W220,""Web — Image Resizer, PDF Compiler, Resources"",5 – Live/Ops
PCU-PROD,PCU052W221,""Web — Data Entry, Admin"",5 – Live/Ops
PCU-STAGE,PCU052W210,Reporting — SSRS,4 – QA
PCU-TEST,PCU052W161,""Web — Matrix API, Subscriber Portal, Data Entry, Mobile API Gateway"",2 – QA";

    [Fact]
    public void TabularConverter_ToKeyValueJson_WithIncludeRestOfColumns_GeneratesObjectValues()
    {
        var table = TabularParser.DetectAndParse(SampleServerData);
        Assert.NotNull(table);

        // Key column = Server Name (index 1)
        string json = TabularConverter.ToKeyValueJson(table, keyColIndex: 1, includeRestOfColumns: true, indented: true);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("PCU052A116", out var server1));
        Assert.Equal("ALL", server1.GetProperty("Environment").GetString());
        Assert.Equal("Management Server", server1.GetProperty("Service / Role").GetString());
        Assert.Equal("N/A", server1.GetProperty("Pipeline").GetString());
        Assert.False(server1.TryGetProperty("Server Name", out _));

        Assert.True(root.TryGetProperty("PCU052W220", out var server2));
        Assert.Equal("PCU-PROD", server2.GetProperty("Environment").GetString());
        Assert.Equal("Web — Image Resizer, PDF Compiler, Resources", server2.GetProperty("Service / Role").GetString());
        Assert.Equal("5 – Live/Ops", server2.GetProperty("Pipeline").GetString());
    }

    [Fact]
    public void TabularConverter_ToKeyValueYaml_WithIncludeRestOfColumns_GeneratesYamlMapWithChildren()
    {
        var table = TabularParser.DetectAndParse(SampleServerData);
        Assert.NotNull(table);

        // Key column = Server Name (index 1)
        string yaml = TabularConverter.ToKeyValueYaml(table, keyColIndex: 1, includeRestOfColumns: true);

        Assert.Contains("PCU052A116:", yaml);
        Assert.Contains("Environment: ALL", yaml);
        Assert.Contains("Service / Role: Management Server", yaml);
        Assert.Contains("Pipeline: N/A", yaml);
        Assert.Contains("PCU052W220:", yaml);
        Assert.Contains("Environment: PCU-PROD", yaml);
    }

    [Fact]
    public void TabularConverter_ToKeyValueQueryString_WithIncludeRestOfColumns_GeneratesBracketedQuery()
    {
        var table = TabularParser.DetectAndParse(SampleServerData);
        Assert.NotNull(table);

        string query = TabularConverter.ToKeyValueQueryString(table, keyColIndex: 1, includeRestOfColumns: true);

        Assert.Contains("PCU052A116[Environment]=ALL", query);
        Assert.Contains("PCU052A116[Pipeline]=N%2FA", query);
    }

    [Fact]
    public void TabularData_ToKeyValueObjectPairs_ReturnsKeyAndDictionaryPairs()
    {
        var table = TabularParser.DetectAndParse(SampleServerData);
        Assert.NotNull(table);

        var pairs = table.ToKeyValueObjectPairs(keyColumnIndex: 1);

        Assert.Equal(5, pairs.Count);
        Assert.Equal("PCU052A116", pairs[0].Key);
        Assert.Equal("ALL", pairs[0].Value["Environment"]);
        Assert.Equal("Management Server", pairs[0].Value["Service / Role"]);
        Assert.Equal("N/A", pairs[0].Value["Pipeline"]);
        Assert.False(pairs[0].Value.ContainsKey("Server Name"));
    }

    [Fact]
    public void MainViewModel_TableToKeyValueJson_WithKeyValueIncludeRestOfColumns_GeneratesJson()
    {
        var vm = new MainViewModel();
        vm.InputText = SampleServerData;
        vm.SelectedKeyColumn = "Server Name";
        vm.KeyValueIncludeRestOfColumns = true;

        vm.ActionCommand.Execute("TableToKeyValueJson");

        using var doc = JsonDocument.Parse(vm.OutputText);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("PCU052A116", out var s1));
        Assert.Equal("ALL", s1.GetProperty("Environment").GetString());
        Assert.Equal("Management Server", s1.GetProperty("Service / Role").GetString());
    }

    [Fact]
    public void MainViewModel_TableToKeyValueYaml_WithKeyValueIncludeRestOfColumns_GeneratesYaml()
    {
        var vm = new MainViewModel();
        vm.InputText = SampleServerData;
        vm.SelectedKeyColumn = "Server Name";
        vm.KeyValueIncludeRestOfColumns = true;

        vm.ActionCommand.Execute("TableToKeyValueYaml");

        Assert.Contains("PCU052A116:", vm.OutputText);
        Assert.Contains("Environment: ALL", vm.OutputText);
        Assert.Contains("Service / Role: Management Server", vm.OutputText);
    }

    [Fact]
    public void MainViewModel_TableToKeyValueJson_WithoutIncludeRest_GeneratesSingleValueMap()
    {
        var vm = new MainViewModel();
        vm.InputText = SampleServerData;
        vm.SelectedKeyColumn = "Server Name";
        vm.SelectedValueColumn = "Environment";
        vm.KeyValueIncludeRestOfColumns = false;

        vm.ActionCommand.Execute("TableToKeyValueJson");

        using var doc = JsonDocument.Parse(vm.OutputText);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("PCU052A116", out var env));
        Assert.Equal("ALL", env.GetString());
    }

    [Fact]
    public void TabularConverter_ToKeyValueJson_WithSurrogateHeadersAndIncludeRest_UsesOverriddenHeaders()
    {
        string noHeaderCsv = @"Prod,ServerA,Database,1
Stage,ServerB,Web,2";

        var table = TabularParser.DetectAndParse(noHeaderCsv, assumeHeader: false);
        Assert.NotNull(table);
        table.OverrideHeaders(new[] { "Env", "Host", "Role", "Tier" });

        string json = TabularConverter.ToKeyValueJson(table, keyColIndex: 1, includeRestOfColumns: true);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("ServerA", out var hostA));
        Assert.Equal("Prod", hostA.GetProperty("Env").GetString());
        Assert.Equal("Database", hostA.GetProperty("Role").GetString());
        Assert.Equal(1, hostA.GetProperty("Tier").GetInt64());
    }

    [Fact]
    public void TabularConverter_ToKeyValueJson_WithDuplicateKeys_RetainsFirstEntry()
    {
        string csvWithDups = @"ID,Name,Role,Active
101,Alice,Admin,true
101,AliceDuplicate,User,false
102,Bob,Developer,true";

        var table = TabularParser.DetectAndParse(csvWithDups);
        Assert.NotNull(table);

        string json = TabularConverter.ToKeyValueJson(table, keyColIndex: 0, includeRestOfColumns: true);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("101", out var user101));
        Assert.Equal("Alice", user101.GetProperty("Name").GetString());
        Assert.Equal("Admin", user101.GetProperty("Role").GetString());
        Assert.True(user101.GetProperty("Active").GetBoolean());
    }

    [Fact]
    public void TabularConverter_ToKeyValueYaml_WithScalarInference_InfersNumbersAndBools()
    {
        string csv = @"Host,Port,SSL,Weight
web-1,443,true,1.5
web-2,80,false,2.0";

        var table = TabularParser.DetectAndParse(csv);
        Assert.NotNull(table);

        string yaml = TabularConverter.ToKeyValueYaml(table, keyColIndex: 0, includeRestOfColumns: true);

        Assert.Contains("web-1:", yaml);
        Assert.Contains("Port: 443", yaml);
        Assert.Contains("SSL: true", yaml);
        Assert.Contains("Weight: 1.5", yaml);
    }
}
