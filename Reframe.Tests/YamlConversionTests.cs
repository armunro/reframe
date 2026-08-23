using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Reframe.Core.Analysis;
using Reframe.Core.Analysis.Analyzers;
using Reframe.Core.Analysis.Models;
using Reframe.Core.Tabular;
using Reframe.Core.Tabular.Converters;
using Reframe.Core.Tabular.Models;
using Reframe.Core.Tabular.Parsers;
using Reframe.Core.Transformers;
using Reframe.Core.Transformers.Developer;
using Reframe.Core.Transformers.Formatting;
using Reframe.Highlighting;
using Reframe.ViewModels;
using Xunit;

namespace Reframe.Tests;

public class YamlConversionTests
{
    [Fact]
    public void TabularConverter_ToYaml_ConvertsTableToYamlListOfObjects()
    {
        var table = new TabularData
        {
            Columns = new List<string> { "Id", "Name", "Score", "Active" },
            Rows = new List<List<string>>
            {
                new() { "1", "Alice", "98.5", "true" },
                new() { "2", "Bob", "85", "false" }
            },
            HasHeaders = true
        };

        string yaml = TabularConverter.ToYaml(table);

        Assert.Contains("Id: 1", yaml);
        Assert.Contains("Name: Alice", yaml);
        Assert.Contains("Score: 98.5", yaml);
        Assert.Contains("Active: true", yaml);
        Assert.Contains("Name: Bob", yaml);
    }

    [Fact]
    public void TabularConverter_ToYamlArrays_ConvertsTableToYamlListOfLists()
    {
        var table = new TabularData
        {
            Columns = new List<string> { "Col1", "Col2" },
            Rows = new List<List<string>>
            {
                new() { "A", "B" },
                new() { "C", "D" }
            },
            HasHeaders = true
        };

        string yaml = TabularConverter.ToYamlArrays(table);

        Assert.Contains("Col1", yaml);
        Assert.Contains("Col2", yaml);
        Assert.Contains("A", yaml);
        Assert.Contains("B", yaml);
    }

    [Fact]
    public void TabularConverter_ToKeyValueYaml_GeneratesYamlDictionary()
    {
        var table = new TabularData
        {
            Columns = new List<string> { "Property", "Value" },
            Rows = new List<List<string>>
            {
                new() { "timeout", "30" },
                new() { "retries", "3" },
                new() { "enabled", "true" }
            },
            HasHeaders = true
        };

        string yaml = TabularConverter.ToKeyValueYaml(table, 0, 1);

        Assert.Contains("timeout: 30", yaml);
        Assert.Contains("retries: 3", yaml);
        Assert.Contains("enabled: true", yaml);
    }

    [Fact]
    public void TabularParser_TryParseYaml_ParsesYamlListOfObjects()
    {
        string yaml = @"
- id: 101
  name: Widget
  price: 19.99
- id: 102
  name: Gadget
  price: 29.99
";

        var table = TabularParser.TryParseYaml(yaml);

        Assert.NotNull(table);
        Assert.True(table.HasHeaders);
        Assert.Equal(3, table.Columns.Count);
        Assert.Contains("id", table.Columns);
        Assert.Contains("name", table.Columns);
        Assert.Contains("price", table.Columns);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("101", table.Rows[0][0]);
        Assert.Equal("Widget", table.Rows[0][1]);
        Assert.Equal("19.99", table.Rows[0][2]);
    }

    [Fact]
    public void TabularParser_DetectAndParse_AutoDetectsYamlTable()
    {
        string yaml = @"
- firstName: Jane
  lastName: Doe
  age: 28
- firstName: John
  lastName: Smith
  age: 34
";

        var table = TabularParser.DetectAndParse(yaml);

        Assert.NotNull(table);
        Assert.Equal(3, table.Columns.Count);
        Assert.Equal(2, table.Rows.Count);
    }

    [Fact]
    public void DeveloperTransformers_ToYamlArray_FormatsItemsAsYamlList()
    {
        string text = "Alpha\nBeta\nGamma";
        string yaml = DeveloperTransformers.ToYamlArray(text);

        Assert.Contains("- Alpha", yaml);
        Assert.Contains("- Beta", yaml);
        Assert.Contains("- Gamma", yaml);
    }

    [Fact]
    public void DeveloperTransformers_ToYamlArray_FormatsNumbers()
    {
        string text = "10\n20\n30";
        string yaml = DeveloperTransformers.ToYamlArray(text);

        Assert.Contains("- 10", yaml);
        Assert.Contains("- 20", yaml);
        Assert.Contains("- 30", yaml);
    }

    [Fact]
    public void DeveloperTransformers_KeyValuePairsToYaml_ConvertsLinesToYaml()
    {
        string text = "host: localhost\nport: 8080\nssl: true";
        string yaml = DeveloperTransformers.KeyValuePairsToYaml(text);

        Assert.Contains("host: localhost", yaml);
        Assert.Contains("port: 8080", yaml);
        Assert.Contains("ssl: true", yaml);
    }

    [Fact]
    public void DeveloperTransformers_JsonToYaml_ConvertsJsonStringToYaml()
    {
        string json = "{\"name\": \"Reframe\", \"version\": 1, \"features\": [\"Lines\", \"Tables\"]}";
        string yaml = DeveloperTransformers.JsonToYaml(json);

        Assert.Contains("name: Reframe", yaml);
        Assert.Contains("version: 1", yaml);
        Assert.Contains("features:", yaml);
        Assert.Contains("- Lines", yaml);
        Assert.Contains("- Tables", yaml);
    }

    [Fact]
    public void DeveloperTransformers_YamlToJson_ConvertsYamlStringToJson()
    {
        string yaml = @"
name: Reframe
version: 1
tags:
  - utility
  - text
";
        string json = DeveloperTransformers.YamlToJson(yaml, indented: false);

        Assert.Contains("\"name\":\"Reframe\"", json);
        Assert.Contains("\"version\":1", json);
        Assert.Contains("\"tags\":[\"utility\",\"text\"]", json);
    }

    [Fact]
    public void TextBeautifier_BeautifyYaml_FormatsAndStandardizesYaml()
    {
        string input = "title: Sample\nitems:\n- a\n- b";
        string beautified = TextBeautifier.BeautifyYaml(input);

        Assert.Contains("title: Sample", beautified);
        Assert.Contains("items:", beautified);
    }

    [Fact]
    public void TextBeautifier_CanBeautify_ReturnsTrueForValidYaml()
    {
        string yaml = "- name: Alice\n  age: 30";
        Assert.True(TextBeautifier.CanBeautify(yaml));
    }

    [Fact]
    public void TextAnalyzer_DetectsYamlArrayOfObjects()
    {
        string yaml = @"
- server: web-01
  status: active
  cpu: 45
- server: web-02
  status: standby
  cpu: 10
";

        var result = TextAnalyzer.Analyze(yaml);

        Assert.Equal(DetectedFormat.Yaml, result.Format);
        Assert.True(result.IsTabular);
        Assert.Equal(3, result.ColumnCount);
        Assert.Equal(2, result.RowCount);
    }

    [Fact]
    public void DarkThemeHighlighting_SupportsYamlLanguage()
    {
        var def1 = DarkThemeHighlighting.GetDefinition("YAML");
        var def2 = DarkThemeHighlighting.GetDefinition("yaml");
        var def3 = DarkThemeHighlighting.GetDefinition("yml");

        Assert.NotNull(def1);
        Assert.NotNull(def2);
        Assert.NotNull(def3);
        Assert.Contains("YAML", DarkThemeHighlighting.SupportedLanguages);
    }

    [Fact]
    public void MainViewModel_Actions_ConvertTabularToYaml()
    {
        var vm = new MainViewModel();
        vm.InputText = "Name,Role,Level\nAlice,Developer,5\nBob,Designer,3";

        vm.ActionCommand.Execute("ToYaml");

        Assert.Contains("Name: Alice", vm.OutputText);
        Assert.Contains("Role: Developer", vm.OutputText);
        Assert.Contains("Level: 5", vm.OutputText);
        Assert.Contains("Name: Bob", vm.OutputText);
        Assert.Equal("YAML", vm.EffectiveOutputSyntax);
    }

    [Fact]
    public void MainViewModel_Actions_JsonToYamlAndYamlToJsonRoundtrip()
    {
        var vm = new MainViewModel();
        vm.InputText = "{\"app\":\"Reframe\",\"stars\":100}";

        vm.ActionCommand.Execute("JsonToYaml");
        Assert.Contains("app: Reframe", vm.OutputText);
        Assert.Contains("stars: 100", vm.OutputText);

        // Feed YAML back to Json
        vm.InputText = vm.OutputText;
        vm.ActionCommand.Execute("YamlToJson");

        Assert.Contains("\"app\": \"Reframe\"", vm.OutputText);
        Assert.Contains("\"stars\": 100", vm.OutputText);
    }

    [Fact]
    public void MainViewModel_LoadSample_LoadsYamlSampleData()
    {
        var vm = new MainViewModel();
        vm.LoadSampleCommand.Execute("yaml");

        Assert.Contains("Development", vm.InputText);
        Assert.Contains("Engineering", vm.InputText);
        Assert.True(vm.Analysis.IsTabular);
        Assert.Equal(DetectedFormat.Yaml, vm.Analysis.Format);
    }
}
