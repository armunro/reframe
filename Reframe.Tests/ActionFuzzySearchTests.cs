using System;
using System.Linq;
using Reframe.Core.Actions;
using Reframe.ViewModels;
using Xunit;

namespace Reframe.Tests;

public class ActionFuzzySearchTests
{
    [Fact]
    public void ActionRegistry_ContainsAllMajorCategoriesAndActions()
    {
        var actions = ActionRegistry.AllActions;

        Assert.NotEmpty(actions);
        Assert.True(actions.Count >= 50, $"Expected at least 50 actions, got {actions.Count}");

        var categories = actions.Select(a => a.Category).Distinct().ToList();
        Assert.Contains("Lines", categories);
        Assert.Contains("Tabular", categories);
        Assert.Contains("Structured", categories);
        Assert.Contains("Code & Developer", categories);
        Assert.Contains("Case Conversion", categories);
        Assert.Contains("Encodings & Formatting", categories);
        Assert.Contains("Navigation & Workflow", categories);

        // Verify each action item is well-formed
        foreach (var action in actions)
        {
            Assert.False(string.IsNullOrWhiteSpace(action.Id), $"Action with empty Id found: {action.Title}");
            Assert.False(string.IsNullOrWhiteSpace(action.Title), $"Action with empty Title found: {action.Id}");
            Assert.False(string.IsNullOrWhiteSpace(action.Category), $"Action with empty Category found: {action.Id}");
            Assert.NotNull(action.Keywords);
        }

        // Verify uniqueness of IDs
        var ids = actions.Select(a => a.Id).ToList();
        var duplicates = ids.GroupBy(x => x, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Assert.Empty(duplicates);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FuzzyMatcher_EmptyOrNullQuery_ReturnsAllActions(string? emptyQuery)
    {
        var actions = ActionRegistry.AllActions;
        var results = FuzzyMatcher.MatchActions(actions, emptyQuery);

        Assert.Equal(actions.Count, results.Count);
    }

    [Fact]
    public void FuzzyMatcher_ExactMatch_ScoresTopRanked()
    {
        var actions = ActionRegistry.AllActions;
        var results = FuzzyMatcher.MatchActions(actions, "SQL IN (...) Clause");

        Assert.NotEmpty(results);
        Assert.Equal("SqlIn", results[0].Item.Id);
        Assert.True(results[0].Score >= 200);
    }

    [Theory]
    [InlineData("sql", "SqlIn")]
    [InlineData("camel", "CamelCase")]
    [InlineData("pascal", "PascalCase")]
    [InlineData("yaml", "YamlToXml")]
    [InlineData("csv", "ToCsv")]
    [InlineData("tsv", "ToTsv")]
    [InlineData("json", "JsonToXml")]
    [InlineData("to json", "ToJsonObjects")]
    [InlineData("to yaml", "ToYaml")]
    [InlineData("markdown", "ToMarkdownTable")]
    [InlineData("html", "ToHtmlTable")]
    [InlineData("quote", "QuoteLines")]
    [InlineData("join", "JoinLines")]
    [InlineData("trim", "TrimLines")]
    [InlineData("sort", "SortLines")]
    [InlineData("dedup", "Deduplicate")]
    [InlineData("base64", "Base64Encode")]
    [InlineData("jwt", "JwtDecode")]
    public void FuzzyMatcher_PrefixAndCommonKeywords_FindExpectedActionAtTop(string query, string expectedTopId)
    {
        var actions = ActionRegistry.AllActions;
        var results = FuzzyMatcher.MatchActions(actions, query);

        Assert.NotEmpty(results);
        var matchedIds = results.Take(5).Select(r => r.Item.Id).ToList();
        Assert.Contains(expectedTopId, matchedIds);
    }

    [Theory]
    [InlineData("b64", "Base64Decode")]
    [InlineData("ts", "ToTypeScriptInterfaces")]
    [InlineData("poco", "ToCSharpClasses")]
    [InlineData("prettify", "FormatJson")]
    [InlineData("minify", "MinifyJson")]
    [InlineData("transpose", "TransposeTable")]
    [InlineData("where in", "SqlIn")]
    public void FuzzyMatcher_AliasAndKeywords_FindMatchingAction(string alias, string expectedActionId)
    {
        var actions = ActionRegistry.AllActions;
        var results = FuzzyMatcher.MatchActions(actions, alias);

        Assert.NotEmpty(results);
        var matchedIds = results.Select(r => r.Item.Id).ToList();
        Assert.Contains(expectedActionId, matchedIds);
    }

    [Fact]
    public void FuzzyMatcher_CaseInsensitiveMatching_WorksReliably()
    {
        var actions = ActionRegistry.AllActions;

        var lowerResults = FuzzyMatcher.MatchActions(actions, "sql in");
        var upperResults = FuzzyMatcher.MatchActions(actions, "SQL IN");
        var mixedResults = FuzzyMatcher.MatchActions(actions, "SqL iN");

        Assert.Equal(lowerResults.Count, upperResults.Count);
        Assert.Equal(lowerResults.Count, mixedResults.Count);
        Assert.Equal(lowerResults[0].Item.Id, upperResults[0].Item.Id);
        Assert.Equal(lowerResults[0].Item.Id, mixedResults[0].Item.Id);
    }

    [Fact]
    public void FuzzyMatcher_NonMatchingQuery_ReturnsEmpty()
    {
        var actions = ActionRegistry.AllActions;
        var results = FuzzyMatcher.MatchActions(actions, "xyznonexistentquery99999");

        Assert.Empty(results);
    }

    [Fact]
    public void MainViewModel_CommandPalette_OpenAndCloseCommands_WorkCorrectly()
    {
        var vm = new MainViewModel();

        Assert.False(vm.IsCommandPaletteOpen);

        vm.OpenCommandPaletteCommand.Execute(null);
        Assert.True(vm.IsCommandPaletteOpen);
        Assert.NotEmpty(vm.FilteredActions);
        Assert.NotNull(vm.SelectedAction);

        vm.CloseCommandPaletteCommand.Execute(null);
        Assert.False(vm.IsCommandPaletteOpen);

        vm.ToggleCommandPaletteCommand.Execute(null);
        Assert.True(vm.IsCommandPaletteOpen);

        vm.ToggleCommandPaletteCommand.Execute(null);
        Assert.False(vm.IsCommandPaletteOpen);
    }

    [Fact]
    public void MainViewModel_ActionSearchQuery_UpdatesFilteredActionsAndSelectedAction()
    {
        var vm = new MainViewModel
        {
            IsCommandPaletteOpen = true,
            ActionSearchQuery = "camel"
        };

        Assert.NotEmpty(vm.FilteredActions);
        Assert.NotNull(vm.SelectedAction);
        Assert.Contains("Camel", vm.SelectedAction.Title, StringComparison.OrdinalIgnoreCase);
        Assert.True(vm.HasActionResults);
        Assert.Contains("actions", vm.ActionResultsCountText);

        vm.ActionSearchQuery = "xyznonexistent999";
        Assert.Empty(vm.FilteredActions);
        Assert.Null(vm.SelectedAction);
        Assert.False(vm.HasActionResults);
    }

    [Fact]
    public void MainViewModel_SelectNextAndPreviousActionCommands_CycleSelection()
    {
        var vm = new MainViewModel
        {
            IsCommandPaletteOpen = true,
            ActionSearchQuery = "sort"
        };

        Assert.True(vm.FilteredActions.Count > 1);

        var first = vm.SelectedAction;
        Assert.NotNull(first);

        vm.SelectNextActionCommand.Execute(null);
        var second = vm.SelectedAction;
        Assert.NotNull(second);
        Assert.NotEqual(first, second);

        vm.SelectPreviousActionCommand.Execute(null);
        Assert.Equal(first, vm.SelectedAction);
    }

    [Fact]
    public void MainViewModel_ExecuteActionItem_RunsTransformationAndClosesPalette()
    {
        var vm = new MainViewModel
        {
            InputText = "apple\nbanana\ncherry",
            IsCommandPaletteOpen = true
        };

        var sqlInAction = ActionRegistry.AllActions.First(a => a.Id == "SqlIn");
        vm.ExecuteActionItemCommand.Execute(sqlInAction);

        Assert.False(vm.IsCommandPaletteOpen);
        Assert.Equal("IN ('apple', 'banana', 'cherry')", vm.OutputText);
        Assert.Contains("SqlIn", vm.StatusMessage);
    }

    [Fact]
    public void MainViewModel_ExecuteActionItem_SwitchesSidebarTab_WhenTabSpecified()
    {
        var vm = new MainViewModel
        {
            InputText = "id,name\n1,alice",
            SelectedSidebarTabIndex = 0
        };

        var toYamlAction = ActionRegistry.AllActions.First(a => a.Id == "ToYaml");
        vm.ExecuteActionItemCommand.Execute(toYamlAction);

        Assert.Equal(1, vm.SelectedSidebarTabIndex); // Tabular tab
        Assert.Contains("name: alice", vm.OutputText);
    }

    [Fact]
    public void MainViewModel_ExecuteActionItem_RunsCaseConversions()
    {
        var vm = new MainViewModel
        {
            InputText = "user_account_profile_id"
        };

        var camelAction = ActionRegistry.AllActions.First(a => a.Id == "CamelCase");
        vm.ExecuteActionItemCommand.Execute(camelAction);

        Assert.Equal("userAccountProfileId", vm.OutputText);

        var constantAction = ActionRegistry.AllActions.First(a => a.Id == "ConstantCase");
        vm.ExecuteActionItemCommand.Execute(constantAction);

        Assert.Equal("USER_ACCOUNT_PROFILE_ID", vm.OutputText);
    }

    [Fact]
    public void MainViewModel_ExecuteActionItem_RunsWorkflowCommands()
    {
        var vm = new MainViewModel
        {
            InputText = "Hello World",
            OutputText = "HELLO WORLD"
        };

        var sendToInputAction = ActionRegistry.AllActions.First(a => a.Id == "SendOutputToInput");
        vm.ExecuteActionItemCommand.Execute(sendToInputAction);

        Assert.Equal("HELLO WORLD", vm.InputText);
        Assert.Equal("Copied output to input", vm.StatusMessage);
    }

    [Fact]
    public void MainViewModel_InputAndOutputWordWrap_AreDistinctAndIndependent()
    {
        var vm = new MainViewModel();

        // Initially both false
        Assert.False(vm.IsInputWordWrap);
        Assert.False(vm.IsOutputWordWrap);

        // Track property changed events
        var changedProps = new System.Collections.Generic.List<string>();
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName != null)
                changedProps.Add(e.PropertyName);
        };

        // Enable input wrap only
        vm.IsInputWordWrap = true;
        Assert.True(vm.IsInputWordWrap);
        Assert.False(vm.IsOutputWordWrap);
        Assert.Contains(nameof(MainViewModel.IsInputWordWrap), changedProps);

        changedProps.Clear();

        // Enable output wrap only
        vm.IsOutputWordWrap = true;
        Assert.True(vm.IsInputWordWrap);
        Assert.True(vm.IsOutputWordWrap);
        Assert.Contains(nameof(MainViewModel.IsOutputWordWrap), changedProps);

        // Disable input wrap
        vm.IsInputWordWrap = false;
        Assert.False(vm.IsInputWordWrap);
        Assert.True(vm.IsOutputWordWrap);
    }

    [Fact]
    public void MainViewModel_ExecuteActionItem_TogglesInputAndOutputWordWrapIndependently()
    {
        var vm = new MainViewModel();

        var toggleInputWrapAction = ActionRegistry.AllActions.First(a => a.Id == "ToggleInputWordWrap");
        var toggleOutputWrapAction = ActionRegistry.AllActions.First(a => a.Id == "ToggleOutputWordWrap");

        // Toggle input wrap
        vm.ExecuteActionItemCommand.Execute(toggleInputWrapAction);
        Assert.True(vm.IsInputWordWrap);
        Assert.False(vm.IsOutputWordWrap);
        Assert.Contains("Input word wrap enabled", vm.StatusMessage);

        // Toggle output wrap
        vm.ExecuteActionItemCommand.Execute(toggleOutputWrapAction);
        Assert.True(vm.IsInputWordWrap);
        Assert.True(vm.IsOutputWordWrap);
        Assert.Contains("Output word wrap enabled", vm.StatusMessage);

        // Toggle input wrap off
        vm.ExecuteActionItemCommand.Execute(toggleInputWrapAction);
        Assert.False(vm.IsInputWordWrap);
        Assert.True(vm.IsOutputWordWrap);
        Assert.Contains("Input word wrap disabled", vm.StatusMessage);
    }

    [Fact]
    public void ActionItem_ParameterRequirementProperties_ReturnCorrectBadgesAndToolTips()
    {
        var paramAction = new ActionItem(
            id: "JoinLines",
            title: "Join Lines into Single Row",
            category: "Lines",
            requiresParameters: true,
            targetSectionKey: "Lines_JoinLines");

        var directAction = new ActionItem(
            id: "TrimLines",
            title: "Trim & Clean Lines",
            category: "Lines",
            requiresParameters: false);

        Assert.True(paramAction.RequiresParameters);
        Assert.Equal("⚙ Parameters", paramAction.ParameterRequirementBadgeText);
        Assert.Contains("parameters configured", paramAction.ParameterRequirementToolTip);

        Assert.False(directAction.RequiresParameters);
        Assert.Equal("⚡ Direct", directAction.ParameterRequirementBadgeText);
        Assert.Contains("without additional parameters", directAction.ParameterRequirementToolTip);
    }

    [Theory]
    [InlineData("JoinLines", true, 0, "Lines_JoinLines")]
    [InlineData("QuoteLines", true, 0, "Lines_QuoteLines")]
    [InlineData("SplitLine", true, 0, "Lines_SplitDelimitedLine")]
    [InlineData("PrefixSuffix", true, 0, "Lines_PrefixSuffix")]
    [InlineData("ReplaceInLines", true, 0, "Lines_FindReplace")]
    [InlineData("ToSqlInserts", true, 1, "Tabular_SqlInsertStatements")]
    [InlineData("ExtractColumn", true, 1, "Tabular_ColumnSelectionExtract")]
    [InlineData("TableToKeyValueJson", true, 1, "Tabular_KeyValueGenerator")]
    [InlineData("QueryStructuredPath", true, 2, "Structured_QueryExtraction")]
    [InlineData("PickStructuredKeys", true, 2, "Structured_KeyFiltering")]
    [InlineData("OmitStructuredKeys", true, 2, "Structured_KeyFiltering")]
    [InlineData("TrimLines", false, 0, "Lines_FilterTrimNumber")]
    [InlineData("ToCsv", false, 1, "Tabular_FullTableConversions")]
    [InlineData("FormatJson", false, 2, "Structured_FormatMinify")]
    [InlineData("SqlIn", false, 3, "Code_SqlQueries")]
    [InlineData("CamelCase", false, 4, "CaseEnc_CaseConversions")]
    public void ActionRegistry_ParameterizedVsDirectActions_ConfiguredAccurately(
        string actionId,
        bool expectedRequiresParams,
        int expectedSidebarTab,
        string expectedTargetSectionKey)
    {
        var action = ActionRegistry.AllActions.FirstOrDefault(a => a.Id == actionId);
        Assert.NotNull(action);
        Assert.Equal(expectedRequiresParams, action.RequiresParameters);
        Assert.Equal(expectedSidebarTab, action.TargetSidebarTab);
        Assert.Equal(expectedTargetSectionKey, action.TargetSectionKey);
    }

    [Fact]
    public void MainViewModel_ExecuteActionItem_ParameterizedAction_NavigatesToTabAndRequestsHighlight()
    {
        var vm = new MainViewModel
        {
            IsRealTimeTransform = false,
            InputText = "apple\nbanana\ncherry",
            OutputText = "placeholder output",
            SelectedSidebarTabIndex = 3,
            IsCommandPaletteOpen = true
        };

        string? requestedHighlightKey = null;
        vm.HighlightSectionRequested += (s, key) =>
        {
            requestedHighlightKey = key;
        };

        var joinLinesAction = ActionRegistry.AllActions.First(a => a.Id == "JoinLines");
        Assert.True(joinLinesAction.RequiresParameters);

        vm.ExecuteActionItemCommand.Execute(joinLinesAction);

        // Should switch to tab 0 (Lines), request highlight on Lines_JoinLines, and close palette
        Assert.Equal(0, vm.SelectedSidebarTabIndex);
        Assert.Equal("Lines_JoinLines", requestedHighlightKey);
        Assert.False(vm.IsCommandPaletteOpen);
        // Should not have executed a transform or altered the output
        Assert.Equal("placeholder output", vm.OutputText);
    }

    [Fact]
    public void MainViewModel_ExecuteActionItem_DirectAction_ExecutesImmediatelyAndClosesPalette()
    {
        var vm = new MainViewModel
        {
            InputText = "apple\nbanana\ncherry",
            SelectedSidebarTabIndex = 0,
            IsCommandPaletteOpen = true
        };

        string? requestedHighlightKey = null;
        vm.HighlightSectionRequested += (s, key) =>
        {
            requestedHighlightKey = key;
        };

        var sqlInAction = ActionRegistry.AllActions.First(a => a.Id == "SqlIn");
        Assert.False(sqlInAction.RequiresParameters);

        vm.ExecuteActionItemCommand.Execute(sqlInAction);

        // Should execute transformation immediately, close palette, and not request highlight
        Assert.False(vm.IsCommandPaletteOpen);
        Assert.Null(requestedHighlightKey);
        Assert.Equal("IN ('apple', 'banana', 'cherry')", vm.OutputText);
    }
}
