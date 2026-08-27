using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Reframe.Core.Actions;
using Reframe.Core.Recipes;
using Reframe.ViewModels;
using Xunit;

namespace Reframe.Tests;

public class RecipeAndPipelineTests
{
    [Fact]
    public void RecipeCatalog_ContainsAllRequiredStepCategoriesAndActions()
    {
        var items = RecipeCatalog.GetAllCatalogItems();
        Assert.NotEmpty(items);
        Assert.True(items.Count >= 30, $"Expected at least 30 catalog steps, found {items.Count}");

        var categories = items.Select(i => i.Category).Distinct().ToList();
        Assert.Contains("Extraction", categories);
        Assert.Contains("Lines", categories);
        Assert.Contains("Code", categories);
        Assert.Contains("Case", categories);
        Assert.Contains("Encoding", categories);
        Assert.Contains("Structured", categories);
        Assert.Contains("Tabular", categories);

        // Verify uniqueness of action IDs
        var ids = items.Select(i => i.ActionId).ToList();
        var duplicates = ids.GroupBy(x => x, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Assert.Empty(duplicates);
    }

    [Fact]
    public void RecipeEngine_ChainsUrlExtractionDeduplicationSortingAndJsonArray()
    {
        // e.g., Watch Clipboard ➔ Extract URLs ➔ Deduplicate ➔ Sort Alphabetically ➔ Wrap in JSON Array
        string rawText = @"
Here is a list of links:
Visit https://example.com/zeta and also https://example.com/alpha!
Check http://test.org/beta again here: https://example.com/alpha
And duplicate http://test.org/beta as well.
";
        var recipe = new TransformationRecipe(
            name: "Extract URLs to JSON Array",
            steps: new[]
            {
                new RecipeStep("ExtractUrls", "Extract URLs", "Extraction"),
                new RecipeStep("Deduplicate", "Deduplicate Lines", "Lines"),
                new RecipeStep("SortAlphabetical", "Sort Alphabetically", "Lines"),
                new RecipeStep("ToJsonArray", "Wrap in JSON Array", "Code")
            });

        var result = RecipeEngine.Instance.Execute(recipe, rawText);

        Assert.True(result.Success);
        Assert.Equal(4, result.StepResults.Count);
        Assert.All(result.StepResults, s => Assert.True(s.Success));

        // Parse JSON array to verify content and ordering
        using var doc = JsonDocument.Parse(result.Output);
        var array = doc.RootElement.EnumerateArray().Select(e => e.GetString()).ToList();

        Assert.Equal(3, array.Count);
        Assert.Equal("http://test.org/beta", array[0]);
        Assert.Equal("https://example.com/alpha", array[1]);
        Assert.Equal("https://example.com/zeta", array[2]);
    }

    [Fact]
    public void RecipeEngine_ChainsTextToCleanSqlInClause()
    {
        string rawInput = " 100, 20 , 100 , 5, 20, 1000 ";
        var recipe = new TransformationRecipe(
            name: "Text to Clean SQL IN Clause",
            steps: new[]
            {
                new RecipeStep("SplitLines", "Split into Lines", "Lines", parameters: new() { ["Delimiter"] = "," }),
                new RecipeStep("TrimLines", "Trim Lines", "Lines"),
                new RecipeStep("Deduplicate", "Deduplicate Lines", "Lines"),
                new RecipeStep("SortNatural", "Sort Naturally", "Lines"),
                new RecipeStep("SqlIn", "Generate SQL IN (...)", "Code")
            });

        var result = RecipeEngine.Instance.Execute(recipe, rawInput);

        Assert.True(result.Success);
        Assert.Equal("IN (5, 20, 100, 1000)", result.Output.Trim());
    }

    [Fact]
    public void RecipeEngine_ChainsCsvToFormattedJson()
    {
        string csv = "id,name,role\n1,Alice,Developer\n2,Bob,Architect";
        var recipe = new TransformationRecipe(
            name: "CSV to Beautified JSON",
            steps: new[]
            {
                new RecipeStep("TableToJsonObjects", "Table ➔ JSON Objects", "Tabular"),
                new RecipeStep("FormatJson", "Beautify JSON", "Structured")
            });

        var result = RecipeEngine.Instance.Execute(recipe, csv);

        Assert.True(result.Success);
        Assert.Contains("\"id\": 1", result.Output);
        Assert.Contains("\"name\": \"Alice\"", result.Output);
        Assert.Contains("\"role\": \"Architect\"", result.Output);
    }

    [Fact]
    public void RecipeEngine_DisabledStepIsSkipped()
    {
        string input = "apple\nbanana\ncherry";
        var recipe = new TransformationRecipe(
            name: "Test Skipping",
            steps: new[]
            {
                new RecipeStep("UpperCase", "Convert to UPPERCASE", "Case", isEnabled: false),
                new RecipeStep("QuoteSingle", "Wrap in Single Quotes", "Lines", isEnabled: true)
            });

        var result = RecipeEngine.Instance.Execute(recipe, input);

        Assert.True(result.Success);
        Assert.Equal("'apple'\n'banana'\n'cherry'", result.Output.Replace("\r\n", "\n"));
    }

    [Fact]
    public void RecipeEngine_DefaultPresetsAreCompleteAndValid()
    {
        var presets = RecipeEngine.GetDefaultPresets();
        Assert.NotEmpty(presets);
        Assert.True(presets.Count >= 8);

        foreach (var preset in presets)
        {
            Assert.False(string.IsNullOrWhiteSpace(preset.Name));
            Assert.NotEmpty(preset.Steps);
            Assert.True(preset.IsBuiltIn);

            // Execute on sample input to verify each preset runs without throwing
            var res = RecipeEngine.Instance.Execute(preset, "https://example.com/test\n101\n102");
            Assert.True(res.Success, $"Preset '{preset.Name}' failed execution: {res.ErrorMessage}");
        }
    }

    [Fact]
    public void RecipeStorage_ExportAndImportSingleRecipe_MatchesOriginal()
    {
        var original = new TransformationRecipe(
            name: "Custom Data Pipeline",
            description: "A test recipe for verifying import/export",
            category: "Testing",
            hotkey: "Ctrl+Alt+9",
            steps: new[]
            {
                new RecipeStep("TrimLines", "Trim Lines", "Lines"),
                new RecipeStep("Deduplicate", "Deduplicate Lines", "Lines"),
                new RecipeStep("ToTypeScriptArray", "Wrap in TypeScript Array", "Code")
            });

        string json = RecipeStorage.ExportToJson(original);
        Assert.False(string.IsNullOrWhiteSpace(json));

        var importedList = RecipeStorage.ImportFromJson(json);
        Assert.Single(importedList);

        var imported = importedList[0];
        Assert.Equal(original.Name, imported.Name);
        Assert.Equal(original.Description, imported.Description);
        Assert.Equal(original.Category, imported.Category);
        Assert.Equal(original.Hotkey, imported.Hotkey);
        Assert.Equal(3, imported.Steps.Count);
        Assert.Equal("TrimLines", imported.Steps[0].ActionId);
        Assert.Equal("Deduplicate", imported.Steps[1].ActionId);
        Assert.Equal("ToTypeScriptArray", imported.Steps[2].ActionId);
    }

    [Fact]
    public void RecipeStorage_ExportAndImportPackage_PreservesAllRecipes()
    {
        var list = new List<TransformationRecipe>
        {
            new TransformationRecipe("Pipeline 1", "Desc 1", "Cat 1", steps: new[] { new RecipeStep("ExtractEmails", "Extract Emails") }),
            new TransformationRecipe("Pipeline 2", "Desc 2", "Cat 2", steps: new[] { new RecipeStep("CamelCase", "camelCase") })
        };

        string json = RecipeStorage.ExportAllToJson(list);
        Assert.False(string.IsNullOrWhiteSpace(json));
        Assert.Contains("\"recipes\":", json);

        var imported = RecipeStorage.ImportFromJson(json);
        Assert.Equal(2, imported.Count);
        Assert.Equal("Pipeline 1", imported[0].Name);
        Assert.Equal("Pipeline 2", imported[1].Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid json text")]
    [InlineData("{ broken }")]
    public void RecipeStorage_ImportInvalidJson_ReturnsEmptyListWithoutException(string invalidJson)
    {
        var imported = RecipeStorage.ImportFromJson(invalidJson);
        Assert.Empty(imported);
    }

    [Fact]
    public void MainViewModel_PipelineBuildingAndExecution_WorksCorrectly()
    {
        var vm = new MainViewModel();
        vm.InputText = "https://b.com\nhttps://a.com\nhttps://b.com";

        // Clear pipeline
        vm.ClearPipeline();
        Assert.False(vm.HasPipelineSteps);
        Assert.Equal(0, vm.PipelineStepCount);

        // Add steps
        vm.AddStepToPipeline("ExtractUrls");
        vm.AddStepToPipeline("Deduplicate");
        vm.AddStepToPipeline("SortAlphabetical");
        vm.AddStepToPipeline("ToJsonArray");

        Assert.True(vm.HasPipelineSteps);
        Assert.Equal(4, vm.PipelineStepCount);

        // Execute pipeline
        vm.ExecuteCurrentPipeline();

        Assert.Contains("https://a.com", vm.OutputText);
        Assert.Contains("https://b.com", vm.OutputText);
        Assert.Contains("[", vm.OutputText);
        Assert.Contains("]", vm.OutputText);
    }

    [Fact]
    public void MainViewModel_PipelineReorderingAndStepRemoval()
    {
        var vm = new MainViewModel();
        vm.ClearPipeline();

        vm.AddStepToPipeline("TrimLines");
        vm.AddStepToPipeline("Deduplicate");
        vm.AddStepToPipeline("SortAlphabetical");

        Assert.Equal(3, vm.CurrentPipelineSteps.Count);
        Assert.Equal("TrimLines", vm.CurrentPipelineSteps[0].ActionId);
        Assert.Equal("Deduplicate", vm.CurrentPipelineSteps[1].ActionId);
        Assert.Equal("SortAlphabetical", vm.CurrentPipelineSteps[2].ActionId);

        // Move Deduplicate up
        vm.MovePipelineStepUp(vm.CurrentPipelineSteps[1]);
        Assert.Equal("Deduplicate", vm.CurrentPipelineSteps[0].ActionId);
        Assert.Equal("TrimLines", vm.CurrentPipelineSteps[1].ActionId);

        // Move Deduplicate down
        vm.MovePipelineStepDown(vm.CurrentPipelineSteps[0]);
        Assert.Equal("TrimLines", vm.CurrentPipelineSteps[0].ActionId);
        Assert.Equal("Deduplicate", vm.CurrentPipelineSteps[1].ActionId);

        // Remove step
        vm.RemovePipelineStep(vm.CurrentPipelineSteps[1]);
        Assert.Equal(2, vm.CurrentPipelineSteps.Count);
        Assert.Equal("TrimLines", vm.CurrentPipelineSteps[0].ActionId);
        Assert.Equal("SortAlphabetical", vm.CurrentPipelineSteps[1].ActionId);
    }

    [Fact]
    public void MainViewModel_SavePresetAndExecuteViaDynamicActionRegistry()
    {
        var vm = new MainViewModel();
        vm.InputText = "apple\nbanana\napple";
        vm.ClearPipeline();
        vm.AddStepToPipeline("TrimLines");
        vm.AddStepToPipeline("Deduplicate");
        vm.AddStepToPipeline("ToCSharpArray");

        vm.SavePipelineAsRecipe("My Custom Fruit Array", "Deduplicates and wraps in C# array", "Ctrl+Alt+F");

        // Verify it was added to SavedRecipes
        var saved = vm.SavedRecipes.FirstOrDefault(r => r.Name == "My Custom Fruit Array");
        Assert.NotNull(saved);

        // Verify ActionRegistry now contains this dynamic action
        var actionId = $"Recipe:{saved.Id}";
        var foundActions = ActionRegistry.Search("Fruit Array");
        Assert.Contains(foundActions, a => a.Id == actionId);

        // Execute via Action Item dispatch
        var actionItem = foundActions.First(a => a.Id == actionId);
        vm.ExecuteActionItem(actionItem);

        Assert.Contains("var items = new string[]", vm.OutputText);
        Assert.Contains("\"apple\"", vm.OutputText);
        Assert.Contains("\"banana\"", vm.OutputText);
    }

    [Fact]
    public void MainViewModel_DuplicateAndDeleteRecipe()
    {
        var vm = new MainViewModel();
        var initialCount = vm.SavedRecipes.Count;

        var samplePreset = vm.SavedRecipes.First();
        vm.DuplicateRecipe(samplePreset);

        Assert.Equal(initialCount + 1, vm.SavedRecipes.Count);
        var copy = vm.SavedRecipes.Last();
        Assert.Contains("(Copy)", copy.Name);
        Assert.False(copy.IsBuiltIn);

        // Delete the copied recipe
        vm.DeleteRecipe(copy);
        Assert.Equal(initialCount, vm.SavedRecipes.Count);
    }

    [Fact]
    public void MainViewModel_ImportExportRecipeJson()
    {
        var vm = new MainViewModel();
        string recipeJson = """
        {
            "name": "Team Web Scraper Pipeline",
            "description": "Shared pipeline across team",
            "category": "Web",
            "hotkey": "Ctrl+Alt+W",
            "steps": [
                {
                    "actionId": "ExtractUrls",
                    "title": "Extract URLs",
                    "category": "Extraction",
                    "isEnabled": true
                },
                {
                    "actionId": "SortAlphabetical",
                    "title": "Sort Alphabetically",
                    "category": "Lines",
                    "isEnabled": true
                }
            ]
        }
        """;

        int importedCount = vm.ImportRecipes(recipeJson);
        Assert.Equal(1, importedCount);

        var imported = vm.SavedRecipes.FirstOrDefault(r => r.Name == "Team Web Scraper Pipeline");
        Assert.NotNull(imported);
        Assert.Equal(2, imported.Steps.Count);

        // Export single recipe
        string exported = vm.ExportRecipe(imported);
        Assert.Contains("Team Web Scraper Pipeline", exported);

        // Cleanup
        vm.DeleteRecipe(imported);
    }
}
