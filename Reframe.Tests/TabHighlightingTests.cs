using Reframe.ViewModels;
using Xunit;

namespace Reframe.Tests;

public class TabHighlightingTests
{
    [Fact]
    public void TabularInput_Csv_HighlightsTabularAndPresetsTabs_AndSelectsTabularTab()
    {
        var vm = new MainViewModel();
        vm.InputText = "Name,Age,Role\nAlice,30,Engineer\nBob,25,Designer";

        Assert.True(vm.IsTabularTabHighlighted);
        Assert.True(vm.IsTableTabHighlighted);
        Assert.True(vm.IsPresetsTabHighlighted);
        Assert.False(vm.IsLinesTabHighlighted);
        Assert.True(vm.HasTabularData);
        Assert.False(vm.HasStructuredData);
        Assert.Equal(1, vm.SelectedSidebarTabIndex); // Table / Tabular Tab index (0: Lines, 1: Table, 2: Structured, 3: Code, 4: Case & Enc)
        Assert.Equal(0, vm.SelectedCenterTabIndex);  // Table Grid View index
    }

    [Fact]
    public void TabularInput_Tsv_HighlightsTabularTab()
    {
        var vm = new MainViewModel();
        vm.InputText = "Product\tSKU\tPrice\nLaptop\tLP-100\t999.99\nMouse\tMS-200\t24.99";

        Assert.True(vm.IsTabularTabHighlighted);
        Assert.True(vm.IsTableTabHighlighted);
        Assert.True(vm.HasTabularData);
        Assert.Equal(1, vm.SelectedSidebarTabIndex);
        Assert.Equal(0, vm.SelectedCenterTabIndex);
    }

    [Fact]
    public void TabularInput_HtmlTable_HighlightsTabularTab()
    {
        var vm = new MainViewModel();
        vm.InputText = "<table><tr><th>Id</th><th>Name</th></tr><tr><td>1</td><td>Alice</td></tr></table>";

        Assert.True(vm.IsTabularTabHighlighted);
        Assert.True(vm.IsTableTabHighlighted);
        Assert.True(vm.HasTabularData);
        Assert.Equal(1, vm.SelectedSidebarTabIndex);
    }

    [Fact]
    public void TabularInput_MarkdownTable_HighlightsTabularTab()
    {
        var vm = new MainViewModel();
        vm.InputText = "| Col1 | Col2 |\n| --- | --- |\n| Val1 | Val2 |";

        Assert.True(vm.IsTabularTabHighlighted);
        Assert.True(vm.IsTableTabHighlighted);
        Assert.True(vm.HasTabularData);
        Assert.Equal(1, vm.SelectedSidebarTabIndex);
    }

    [Fact]
    public void MultiLineInput_PlainList_HighlightsLinesAndPresetsTabs_AndSelectsLinesTab()
    {
        var vm = new MainViewModel();
        vm.InputText = "apple\nbanana\ncherry\norange";

        Assert.True(vm.IsLinesTabHighlighted);
        Assert.True(vm.IsPresetsTabHighlighted);
        Assert.False(vm.IsTabularTabHighlighted);
        Assert.False(vm.IsTableTabHighlighted);
        Assert.False(vm.HasTabularData);
        Assert.False(vm.HasStructuredData);
        Assert.Equal(0, vm.SelectedSidebarTabIndex); // Lines Tab index
        Assert.Equal(2, vm.SelectedCenterTabIndex);  // Analysis & Stats index (Table Grid View and Structured Tree View are disabled)
    }

    [Fact]
    public void MultiLineInput_Numbers_HighlightsLinesAndPresetsTabs()
    {
        var vm = new MainViewModel();
        vm.InputText = "1001\n1002\n1003\n1004\n1005";

        Assert.True(vm.IsLinesTabHighlighted);
        Assert.True(vm.IsPresetsTabHighlighted);
        Assert.False(vm.IsTabularTabHighlighted);
        Assert.False(vm.IsTableTabHighlighted);
        Assert.False(vm.HasTabularData);
        Assert.False(vm.HasStructuredData);
        Assert.Equal(0, vm.SelectedSidebarTabIndex);
    }

    [Fact]
    public void DelimitedSingleLine_HighlightsLinesAndPresetsTabs()
    {
        var vm = new MainViewModel();
        vm.InputText = "apple, banana, cherry, orange";

        Assert.True(vm.IsLinesTabHighlighted);
        Assert.True(vm.IsPresetsTabHighlighted);
        Assert.False(vm.IsTabularTabHighlighted);
        Assert.False(vm.IsTableTabHighlighted);
        Assert.False(vm.HasTabularData);
        Assert.Equal(0, vm.SelectedSidebarTabIndex);
    }

    [Fact]
    public void CodeInput_JsonObject_HighlightsCodeAndStructuredTabs_AndSelectsStructuredTab()
    {
        var vm = new MainViewModel();
        vm.InputText = "{\n  \"userId\": 1,\n  \"title\": \"delectus aut autem\"\n}";

        Assert.True(vm.IsStructuredTabHighlighted);
        Assert.True(vm.IsCodeTabHighlighted);
        Assert.True(vm.IsCaseEncTabHighlighted); // JSON beautify / format is available
        Assert.False(vm.HasTabularData);
        Assert.True(vm.HasStructuredData);
        Assert.Equal(2, vm.SelectedSidebarTabIndex); // Structured Tab index
        Assert.Equal(1, vm.SelectedCenterTabIndex);  // Structured Tree View index
    }

    [Fact]
    public void CodeInput_Yaml_HighlightsStructuredTab_AndSelectsStructuredTab()
    {
        var vm = new MainViewModel();
        vm.InputText = "user:\n  id: 101\n  name: Alice\n  active: true";

        Assert.True(vm.IsStructuredTabHighlighted);
        Assert.True(vm.HasStructuredData);
        Assert.Equal(2, vm.SelectedSidebarTabIndex); // Structured Tab index
    }

    [Fact]
    public void CodeInput_Xml_HighlightsStructuredTab_AndSelectsStructuredTab()
    {
        var vm = new MainViewModel();
        vm.InputText = "<user id=\"101\"><name>Alice</name></user>";

        Assert.True(vm.IsStructuredTabHighlighted);
        Assert.True(vm.HasStructuredData);
        Assert.Equal(2, vm.SelectedSidebarTabIndex); // Structured Tab index
    }

    [Fact]
    public void CodeInput_SqlInClause_HighlightsCodeTab()
    {
        var vm = new MainViewModel();
        vm.InputText = "IN (1001, 1002, 1003, 1004)";

        Assert.True(vm.IsCodeTabHighlighted);
        Assert.False(vm.HasTabularData);
        Assert.False(vm.HasStructuredData);
        Assert.Equal(3, vm.SelectedSidebarTabIndex);
    }

    [Fact]
    public void CodeInput_KeyValuePairs_HighlightsCodeTab()
    {
        var vm = new MainViewModel();
        vm.InputText = "host=localhost\nport=5432\ndatabase=mydb\nuser=postgres";

        Assert.True(vm.IsCodeTabHighlighted);
        Assert.False(vm.HasTabularData);
        Assert.False(vm.HasStructuredData);
        Assert.Equal(3, vm.SelectedSidebarTabIndex);
    }

    [Fact]
    public void SingleLineInput_WordOrIdentifier_HighlightsCaseEncTab_AndSelectsCaseEncTab()
    {
        var vm = new MainViewModel();
        vm.InputText = "mySpecialVariableName";

        Assert.True(vm.IsCaseEncTabHighlighted);
        Assert.False(vm.IsTabularTabHighlighted);
        Assert.False(vm.IsTableTabHighlighted);
        Assert.False(vm.HasTabularData);
        Assert.False(vm.HasStructuredData);
        Assert.Equal(4, vm.SelectedSidebarTabIndex); // Case / Enc Tab index
    }

    [Fact]
    public void SingleLineInput_Base64_HighlightsCaseEncTab()
    {
        var vm = new MainViewModel();
        vm.InputText = "SGVsbG8gV29ybGQh";

        Assert.True(vm.IsCaseEncTabHighlighted);
        Assert.False(vm.HasTabularData);
        Assert.False(vm.HasStructuredData);
        Assert.Equal(4, vm.SelectedSidebarTabIndex);
    }

    [Fact]
    public void InputTransitions_FromTabularToMultiLine_UpdatesHighlightsAndTabSelection()
    {
        var vm = new MainViewModel();
        // 1. Enter Tabular text
        vm.InputText = "Col1,Col2\nVal1,Val2\nVal3,Val4";
        Assert.True(vm.HasTabularData);
        Assert.True(vm.IsTableTabHighlighted);
        Assert.Equal(1, vm.SelectedSidebarTabIndex);
        Assert.Equal(0, vm.SelectedCenterTabIndex);

        // 2. Transition to non-tabular multiline text
        vm.InputText = "Just some arbitrary\nmultiline text without\ntable structure";
        Assert.False(vm.HasTabularData);
        Assert.False(vm.IsTabularTabHighlighted);
        Assert.False(vm.IsTableTabHighlighted);
        Assert.True(vm.IsLinesTabHighlighted);
        Assert.Equal(0, vm.SelectedSidebarTabIndex); // Lines tab
        Assert.Equal(2, vm.SelectedCenterTabIndex);  // Analysis & Stats
    }

    [Fact]
    public void EmptyInput_ClearsAllTabHighlights_AndDisablesDataTabs()
    {
        var vm = new MainViewModel();
        vm.InputText = "";

        Assert.False(vm.IsPresetsTabHighlighted);
        Assert.False(vm.IsLinesTabHighlighted);
        Assert.False(vm.IsTabularTabHighlighted);
        Assert.False(vm.IsTableTabHighlighted);
        Assert.False(vm.IsStructuredTabHighlighted);
        Assert.False(vm.IsCodeTabHighlighted);
        Assert.False(vm.IsCaseEncTabHighlighted);
        Assert.False(vm.HasTabularData);
        Assert.False(vm.HasStructuredData);
        Assert.Equal(0, vm.SelectedSidebarTabIndex);
        Assert.Equal(2, vm.SelectedCenterTabIndex);
    }
}
