using System;
using System.Linq;
using System.Threading;
using Reframe.Core.Actions;
using Reframe.Core.Scripting;
using Reframe.ViewModels;
using Xunit;

namespace Reframe.Tests;

public class CSharpScriptingTests
{
    private readonly CSharpScriptEngine _engine = new();

    [Fact]
    public void Evaluate_BasicExpression_ReturnsExpectedOutput()
    {
        var result = _engine.Evaluate("input.ToUpper()", "hello world");
        Assert.True(result.IsSuccess);
        Assert.Equal("HELLO WORLD", result.Output);
        Assert.Equal("string", result.ReturnTypeName);
    }

    [Fact]
    public void Evaluate_LinqExpressionOnInput_ReturnsFormattedLines()
    {
        string input = "apple, banana, cherry, apple, date";
        string script = "input.Split(',').Select(x => x.Trim()).Where(x => x.StartsWith(\"a\"))";

        var result = _engine.Evaluate(script, input);
        Assert.True(result.IsSuccess);
        Assert.Equal("apple\napple", result.Output.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Evaluate_LinesGlobal_TransformsLines()
    {
        string input = "Line 1\nLine 2\nLine 3";
        string script = "lines.Select((line, i) => $\"Item #{i + 1}: {line}\")";

        var result = _engine.Evaluate(script, input);
        Assert.True(result.IsSuccess);
        Assert.Equal("Item #1: Line 1\nItem #2: Line 2\nItem #3: Line 3", result.Output.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Evaluate_NonEmptyLinesGlobal_FiltersOutWhitespaceLines()
    {
        string input = "Line 1\n\n   \nLine 2\n\t\nLine 3\n";
        string script = "nonEmptyLines.Select(l => l.ToUpper())";

        var result = _engine.Evaluate(script, input);
        Assert.True(result.IsSuccess);
        Assert.Equal("LINE 1\nLINE 2\nLINE 3", result.Output.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Evaluate_PrintAndDumpGlobals_OutputsCapturedLines()
    {
        string script = @"
            print(""Header"");
            dump(new { ID = 1, Name = ""Test"" });
            print(""Footer"");
        ";

        var result = _engine.Evaluate(script, string.Empty);
        Assert.True(result.IsSuccess);
        Assert.Contains("Header", result.Output);
        Assert.Contains("\"ID\": 1", result.Output);
        Assert.Contains("\"Name\": \"Test\"", result.Output);
        Assert.Contains("Footer", result.Output);
    }

    [Fact]
    public void Evaluate_AnonymousObjectList_SerializesToJson()
    {
        string input = "101, Alice\n102, Bob";
        string script = "lines.Select(l => l.Split(',')).Select(cols => new { ID = cols[0].Trim(), Name = cols[1].Trim() })";

        var result = _engine.Evaluate(script, input);
        Assert.True(result.IsSuccess);
        Assert.Contains("\"ID\": \"101\"", result.Output);
        Assert.Contains("\"Name\": \"Alice\"", result.Output);
        Assert.Contains("\"ID\": \"102\"", result.Output);
        Assert.Contains("\"Name\": \"Bob\"", result.Output);
    }

    [Fact]
    public void Evaluate_NumericSum_ReturnsFormattedNumber()
    {
        string input = "Price: 10.50, Tax: 2.25, Discount: -1.75";
        string script = "Regex.Matches(input, @\"-?\\d+(\\.\\d+)?\").Select(m => double.Parse(m.Value)).Sum()";

        var result = _engine.Evaluate(script, input);
        Assert.True(result.IsSuccess);
        Assert.Equal(11.0, double.Parse(result.Output), 2);
    }

    [Fact]
    public void Evaluate_SyntaxError_ReturnsDiagnosticsAndFailure()
    {
        string script = "input.Where(unknown => )";

        var result = _engine.Evaluate(script, "test");
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void Evaluate_RuntimeError_ReturnsCleanErrorMessage()
    {
        string script = "int a = 0; int b = 10 / a; return b;";

        var result = _engine.Evaluate(script, string.Empty);
        Assert.False(result.IsSuccess);
        Assert.Contains("divide by zero", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ScriptLibraryCatalog_AllPresets_ExecuteSuccessfullyOnSampleInput()
    {
        foreach (var preset in ScriptLibraryCatalog.Presets)
        {
            string sample = preset.SampleInput ?? "Sample line 1\nSample line 2\nSample line 3";
            var result = _engine.Evaluate(preset.Script, sample);

            Assert.True(result.IsSuccess, $"Preset '{preset.Title}' failed to evaluate: {result.ErrorMessage}");
            Assert.False(string.IsNullOrEmpty(result.Output), $"Preset '{preset.Title}' produced empty output on sample text");
        }
    }

    [Fact]
    public void MainViewModel_ScriptingIntegration_WorksEndToEnd()
    {
        var vm = new MainViewModel
        {
            InputText = "one\ntwo\nthree\nfour"
        };

        Assert.NotNull(vm.ScriptPresets);
        Assert.NotEmpty(vm.ScriptPresets);
        Assert.NotNull(vm.ScriptResult);
        Assert.True(vm.ScriptResult.IsSuccess);

        // Change script to uppercase lines
        vm.CSharpScript = "lines.Select(x => x.ToUpper())";
        Assert.Equal("ONE\nTWO\nTHREE\nFOUR", vm.ScriptOutput.Replace("\r\n", "\n"));

        // Test sending script output to input
        vm.SendScriptOutputToInputCommand.Execute(null);
        Assert.Equal("ONE\nTWO\nTHREE\nFOUR", vm.InputText.Replace("\r\n", "\n"));

        // Test sending script output to output editor
        vm.SendScriptOutputToOutputCommand.Execute(null);
        Assert.Equal(vm.ScriptOutput, vm.OutputText);
    }

    [Fact]
    public void MainViewModel_ApplyPreset_UpdatesScriptAndExecutes()
    {
        var vm = new MainViewModel
        {
            InputText = "cherry\napple\nbanana\napple"
        };

        var sortPreset = ScriptLibraryCatalog.Presets.First(p => p.Id == "DeduplicateAndSort");
        vm.ApplyScriptPreset(sortPreset, loadSample: false);

        Assert.Equal(sortPreset.Script, vm.CSharpScript);
        Assert.Equal("apple\nbanana\ncherry", vm.ScriptOutput.Replace("\r\n", "\n"));
    }

    [Fact]
    public void ActionRegistry_ContainsScriptingActions_AndCanBeSearched()
    {
        var actions = ActionRegistry.AllActions;

        Assert.Contains(actions, a => a.Id == "OpenScriptingTab");
        Assert.Contains(actions, a => a.Id == "ExecuteCSharpScript");
        Assert.Contains(actions, a => a.Id == "SendScriptOutputToInput");
        Assert.Contains(actions, a => a.Id.StartsWith("ScriptPreset:"));

        var searchResults = ActionRegistry.Search("linq");
        Assert.NotEmpty(searchResults);
        Assert.Contains(searchResults, a => a.Category == "C# Scripting");
    }

    [Fact]
    public void MainViewModel_ExecuteActionItem_OpensScriptingTabAndAppliesPreset()
    {
        var vm = new MainViewModel();

        var openTabAction = ActionRegistry.AllActions.First(a => a.Id == "OpenScriptingTab");
        vm.ExecuteActionItem(openTabAction);
        Assert.Equal(4, vm.SelectedCenterTabIndex);

        var presetAction = ActionRegistry.AllActions.First(a => a.Id == "ScriptPreset:FilterAndTrimLines");
        vm.ExecuteActionItem(presetAction);
        Assert.Equal(4, vm.SelectedCenterTabIndex);
        Assert.Contains("lines.Select", vm.CSharpScript);
    }
}
