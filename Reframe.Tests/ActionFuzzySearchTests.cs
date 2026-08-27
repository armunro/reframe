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
}
