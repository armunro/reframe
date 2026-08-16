using TextForge.ViewModels;
using Xunit;

namespace TextForge.Tests;

public class TabHighlightingTests
{
    [Fact]
    public void TabularInput_Csv_HighlightsTabularAndPresetsTabs_AndSelectsTabularTab()
    {
        var vm = new MainViewModel();
        vm.InputText = "Name,Age,Role\nAlice,30,Engineer\nBob,25,Designer";

        Assert.True(vm.IsTabularTabHighlighted);
        Assert.True(vm.IsPresetsTabHighlighted);
        Assert.False(vm.IsLinesTabHighlighted);
        Assert.Equal(2, vm.SelectedSidebarTabIndex); // Tabular Tab index
        Assert.Equal(0, vm.SelectedCenterTabIndex);  // Table Grid View index
    }

    [Fact]
    public void TabularInput_Tsv_HighlightsTabularTab()
    {
        var vm = new MainViewModel();
        vm.InputText = "Product\tSKU\tPrice\nLaptop\tLP-100\t999.99\nMouse\tMS-200\t24.99";

        Assert.True(vm.IsTabularTabHighlighted);
        Assert.Equal(2, vm.SelectedSidebarTabIndex);
        Assert.Equal(0, vm.SelectedCenterTabIndex);
    }

    [Fact]
    public void TabularInput_HtmlTable_HighlightsTabularTab()
    {
        var vm = new MainViewModel();
        vm.InputText = "<table><tr><th>Id</th><th>Name</th></tr><tr><td>1</td><td>Alice</td></tr></table>";

        Assert.True(vm.IsTabularTabHighlighted);
        Assert.Equal(2, vm.SelectedSidebarTabIndex);
    }

    [Fact]
    public void TabularInput_MarkdownTable_HighlightsTabularTab()
    {
        var vm = new MainViewModel();
        vm.InputText = "| Col1 | Col2 |\n| --- | --- |\n| Val1 | Val2 |";

        Assert.True(vm.IsTabularTabHighlighted);
        Assert.Equal(2, vm.SelectedSidebarTabIndex);
    }

    [Fact]
    public void MultiLineInput_PlainList_HighlightsLinesAndPresetsTabs_AndSelectsLinesTab()
    {
        var vm = new MainViewModel();
        vm.InputText = "apple\nbanana\ncherry\norange";

        Assert.True(vm.IsLinesTabHighlighted);
        Assert.True(vm.IsPresetsTabHighlighted);
        Assert.False(vm.IsTabularTabHighlighted);
        Assert.Equal(1, vm.SelectedSidebarTabIndex); // Lines Tab index
        Assert.Equal(1, vm.SelectedCenterTabIndex);  // Analysis & Stats index
    }

    [Fact]
    public void MultiLineInput_Numbers_HighlightsLinesAndPresetsTabs()
    {
        var vm = new MainViewModel();
        vm.InputText = "1001\n1002\n1003\n1004\n1005";

        Assert.True(vm.IsLinesTabHighlighted);
        Assert.True(vm.IsPresetsTabHighlighted);
        Assert.False(vm.IsTabularTabHighlighted);
        Assert.Equal(1, vm.SelectedSidebarTabIndex);
    }

    [Fact]
    public void DelimitedSingleLine_HighlightsLinesAndPresetsTabs()
    {
        var vm = new MainViewModel();
        vm.InputText = "apple, banana, cherry, orange";

        Assert.True(vm.IsLinesTabHighlighted);
        Assert.True(vm.IsPresetsTabHighlighted);
        Assert.Equal(1, vm.SelectedSidebarTabIndex);
    }

    [Fact]
    public void CodeInput_JsonObject_HighlightsCodeAndCaseTabs_AndSelectsCodeTab()
    {
        var vm = new MainViewModel();
        vm.InputText = "{\n  \"userId\": 1,\n  \"title\": \"delectus aut autem\"\n}";

        Assert.True(vm.IsCodeTabHighlighted);
        Assert.True(vm.IsCaseEncTabHighlighted); // JSON beautify / format is available
        Assert.Equal(3, vm.SelectedSidebarTabIndex); // Code Tab index
    }

    [Fact]
    public void CodeInput_SqlInClause_HighlightsCodeTab()
    {
        var vm = new MainViewModel();
        vm.InputText = "IN (1001, 1002, 1003, 1004)";

        Assert.True(vm.IsCodeTabHighlighted);
        Assert.Equal(3, vm.SelectedSidebarTabIndex);
    }

    [Fact]
    public void CodeInput_KeyValuePairs_HighlightsCodeTab()
    {
        var vm = new MainViewModel();
        vm.InputText = "host=localhost\nport=5432\ndatabase=mydb\nuser=postgres";

        Assert.True(vm.IsCodeTabHighlighted);
        Assert.Equal(3, vm.SelectedSidebarTabIndex);
    }

    [Fact]
    public void SingleLineInput_WordOrIdentifier_HighlightsCaseEncTab_AndSelectsCaseEncTab()
    {
        var vm = new MainViewModel();
        vm.InputText = "mySpecialVariableName";

        Assert.True(vm.IsCaseEncTabHighlighted);
        Assert.False(vm.IsTabularTabHighlighted);
        Assert.Equal(4, vm.SelectedSidebarTabIndex); // Case / Enc Tab index
    }

    [Fact]
    public void SingleLineInput_Base64_HighlightsCaseEncTab()
    {
        var vm = new MainViewModel();
        vm.InputText = "SGVsbG8gV29ybGQh";

        Assert.True(vm.IsCaseEncTabHighlighted);
        Assert.Equal(4, vm.SelectedSidebarTabIndex);
    }

    [Fact]
    public void EmptyInput_ClearsAllTabHighlights()
    {
        var vm = new MainViewModel();
        vm.InputText = "";

        Assert.False(vm.IsPresetsTabHighlighted);
        Assert.False(vm.IsLinesTabHighlighted);
        Assert.False(vm.IsTabularTabHighlighted);
        Assert.False(vm.IsCodeTabHighlighted);
        Assert.False(vm.IsCaseEncTabHighlighted);
    }
}
