using Reframe.Core.Analysis;
using Reframe.ViewModels;
using Xunit;

namespace Reframe.Tests;

public class StructuredDataViewModelTests
{
    [Fact]
    public void InputText_JsonInput_SetsStructuredNodesAndSelectsStructuredTab()
    {
        var vm = new MainViewModel();
        vm.InputText = "{\n  \"name\": \"Reframe\",\n  \"version\": 1.0\n}";

        Assert.True(vm.HasStructuredData);
        Assert.Equal("JSON", vm.StructuredFormatDescription);
        Assert.NotEmpty(vm.StructuredNodes);
        Assert.Equal(1, vm.SelectedCenterTabIndex); // Structured Tree View
    }

    [Fact]
    public void InputText_XmlInput_SetsStructuredNodesAndSelectsStructuredTab()
    {
        var vm = new MainViewModel();
        vm.InputText = "<root><item id=\"1\">Sample</item></root>";

        Assert.True(vm.HasStructuredData);
        Assert.Equal("XML", vm.StructuredFormatDescription);
        Assert.NotEmpty(vm.StructuredNodes);
        Assert.Equal(1, vm.SelectedCenterTabIndex); // Structured Tree View
    }

    [Fact]
    public void InputText_YamlInput_SetsStructuredNodes()
    {
        var vm = new MainViewModel();
        vm.InputText = "app:\n  name: Reframe\n  port: 8080";

        Assert.True(vm.HasStructuredData);
        Assert.Equal("YAML", vm.StructuredFormatDescription);
        Assert.NotEmpty(vm.StructuredNodes);
    }

    [Fact]
    public void LoadSample_Xml_LoadsXmlSampleAndPopulatesTree()
    {
        var vm = new MainViewModel();
        vm.LoadSampleCommand.Execute("xml");

        Assert.Contains("<catalog>", vm.InputText);
        Assert.Contains("<book id=\"bk101\">", vm.InputText);
        Assert.True(vm.HasStructuredData);
        Assert.Equal("XML", vm.StructuredFormatDescription);
        Assert.Equal(1, vm.SelectedCenterTabIndex);
    }

    [Fact]
    public void Actions_XmlToJsonAndJsonToXml_ExecutesThroughViewModel()
    {
        var vm = new MainViewModel();
        vm.InputText = "<service name=\"auth\"><port>5000</port></service>";

        vm.ActionCommand.Execute("XmlToJson");
        Assert.Contains("\"service\":", vm.OutputText);
        Assert.Contains("\"@name\": \"auth\"", vm.OutputText);
        Assert.Contains("\"port\": 5000", vm.OutputText);

        // Convert back from JSON to XML
        vm.InputText = vm.OutputText;
        vm.ActionCommand.Execute("JsonToXml");
        Assert.Contains("<service", vm.OutputText);
        Assert.Contains("port", vm.OutputText);
    }

    [Fact]
    public void Actions_XmlToYamlAndYamlToXml_ExecutesThroughViewModel()
    {
        var vm = new MainViewModel();
        vm.InputText = "<config><theme>Dark</theme><fontSize>14</fontSize></config>";

        vm.ActionCommand.Execute("XmlToYaml");
        Assert.Contains("config:", vm.OutputText);
        Assert.Contains("theme: Dark", vm.OutputText);
        Assert.Contains("fontSize: 14", vm.OutputText);

        vm.InputText = vm.OutputText;
        vm.ActionCommand.Execute("YamlToXml");
        Assert.Contains("<config>", vm.OutputText);
        Assert.Contains("<theme>Dark</theme>", vm.OutputText);
    }

    [Fact]
    public void Actions_FlattenAndUnflatten_ExecutesThroughViewModel()
    {
        var vm = new MainViewModel();
        vm.InputText = "{\"user\":{\"name\":\"Alice\",\"age\":30}}";

        vm.ActionCommand.Execute("FlattenStructured");
        Assert.Contains("user.name = \"Alice\"", vm.OutputText);
        Assert.Contains("user.age = 30", vm.OutputText);

        vm.InputText = vm.OutputText;
        vm.ActionCommand.Execute("UnflattenStructured");
        Assert.Contains("\"name\": \"Alice\"", vm.OutputText);
        Assert.Contains("\"age\": 30", vm.OutputText);
    }

    [Fact]
    public void Actions_SortStructuredKeys_SortsPropertiesAlphabetically()
    {
        var vm = new MainViewModel();
        vm.InputText = "{\"z\":1,\"a\":2,\"k\":3}";

        vm.ActionCommand.Execute("SortStructuredKeys");
        Assert.Contains("\"a\": 2", vm.OutputText);
        Assert.Contains("\"k\": 3", vm.OutputText);
        Assert.Contains("\"z\": 1", vm.OutputText);
    }

    [Fact]
    public void Actions_ExtractStructuredPathsAndKeys()
    {
        var vm = new MainViewModel();
        vm.InputText = "{\"profile\":{\"nickname\":\"forge\",\"id\":42}}";

        vm.ActionCommand.Execute("ExtractStructuredPaths");
        Assert.Contains("$.profile.nickname", vm.OutputText);
        Assert.Contains("$.profile.id", vm.OutputText);

        vm.ActionCommand.Execute("ExtractStructuredKeys");
        Assert.Contains("nickname", vm.OutputText);
        Assert.Contains("id", vm.OutputText);
    }

    [Fact]
    public void Actions_MinifyJsonAndMinifyXml()
    {
        var vm = new MainViewModel();
        vm.InputText = "{\n  \"a\": 1,\n  \"b\": 2\n}";

        vm.ActionCommand.Execute("MinifyJson");
        Assert.Equal("{\"a\":1,\"b\":2}", vm.OutputText);

        vm.InputText = "<root>\n  <item>1</item>\n</root>";
        vm.ActionCommand.Execute("MinifyXml");
        Assert.Contains("<root><item>1</item></root>", vm.OutputText);
    }

    [Fact]
    public void TreeCommands_ExpandCollapseAndFilter()
    {
        var vm = new MainViewModel();
        vm.InputText = "{\"a\":{\"b\":{\"target\":999}}}";

        Assert.NotEmpty(vm.StructuredNodes);

        vm.CollapseAllStructuredNodesCommand.Execute(null);
        Assert.False(vm.StructuredNodes[0].IsExpanded);

        vm.ExpandAllStructuredNodesCommand.Execute(null);
        Assert.True(vm.StructuredNodes[0].IsExpanded);

        vm.StructuredFilterQuery = "target";
        Assert.True(vm.StructuredNodes[0].IsVisible);

        vm.StructuredFilterQuery = "nonexistent";
        Assert.False(vm.StructuredNodes[0].IsVisible);
    }

    [Fact]
    public void CopyNodeCommands_WithPathAndValue()
    {
        var vm = new MainViewModel();
        vm.InputText = "{\"name\":\"Reframe\"}";

        var nameNode = vm.StructuredNodes[0].Children[0];
        vm.SelectedStructuredNode = nameNode;

        vm.CopyStructuredPathCommand.Execute(null);
        Assert.Contains("Copied path", vm.StatusMessage);

        vm.CopyStructuredValueCommand.Execute(null);
        Assert.Contains("Copied node value", vm.StatusMessage);

        vm.ExtractSelectedStructuredNodeCommand.Execute(null);
        Assert.Equal("Reframe", vm.OutputText);
    }
}
