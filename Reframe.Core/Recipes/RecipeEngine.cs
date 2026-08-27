using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Reframe.Core.Recipes;

public class RecipeEngine
{
    public static RecipeEngine Instance { get; } = new();

    public RecipeResult Execute(TransformationRecipe recipe, string? input)
    {
        if (recipe == null) throw new ArgumentNullException(nameof(recipe));
        return ExecuteSteps(recipe.Steps, input);
    }

    public RecipeResult ExecuteSteps(IEnumerable<RecipeStep> steps, string? input)
    {
        var stopwatch = Stopwatch.StartNew();
        var stepResults = new List<RecipeStepExecutionResult>();
        string currentText = input ?? string.Empty;
        int index = 0;

        foreach (var step in steps)
        {
            if (!step.IsEnabled)
            {
                index++;
                continue;
            }

            var stepStopwatch = Stopwatch.StartNew();
            string stepOutput = currentText;
            bool success = true;
            string? errorMessage = null;

            try
            {
                stepOutput = RecipeCatalog.ExecuteStep(step, currentText);
                currentText = stepOutput;
            }
            catch (Exception ex)
            {
                success = false;
                errorMessage = ex.Message;
            }
            finally
            {
                stepStopwatch.Stop();
            }

            string preview = currentText.Length > 80
                ? currentText.Substring(0, 80).Replace("\r\n", " ").Replace("\n", " ") + "..."
                : currentText.Replace("\r\n", " ").Replace("\n", " ");

            stepResults.Add(new RecipeStepExecutionResult
            {
                StepIndex = index + 1,
                StepTitle = string.IsNullOrEmpty(step.Title) ? step.ActionId : step.Title,
                ActionId = step.ActionId,
                Success = success,
                OutputPreview = preview,
                ExecutionTimeMs = stepStopwatch.Elapsed.TotalMilliseconds,
                ErrorMessage = errorMessage
            });

            if (!success)
            {
                stopwatch.Stop();
                return RecipeResult.Failed(
                    errorMessage ?? "Step failed execution",
                    currentText,
                    stepResults,
                    stopwatch.Elapsed.TotalMilliseconds);
            }

            index++;
        }

        stopwatch.Stop();
        return RecipeResult.Ok(currentText, stepResults, stopwatch.Elapsed.TotalMilliseconds);
    }

    public static List<TransformationRecipe> GetDefaultPresets()
    {
        return new List<TransformationRecipe>
        {
            new TransformationRecipe(
                name: "Extract URLs to JSON Array",
                description: "Extracts all HTTP/HTTPS links, deduplicates them, sorts alphabetically, and formats as a JSON array",
                category: "Web & Extraction",
                hotkey: "Ctrl+Alt+1",
                steps: new[]
                {
                    new RecipeStep("ExtractUrls", "Extract URLs", "Extraction", "Extract all HTTP/HTTPS links", "🌐"),
                    new RecipeStep("Deduplicate", "Deduplicate Lines", "Lines", "Remove duplicate lines", "✨"),
                    new RecipeStep("SortAlphabetical", "Sort Alphabetically", "Lines", "Sort lines A-Z", "🔤"),
                    new RecipeStep("ToJsonArray", "Wrap in JSON Array", "Code", "Format as JSON array", "📦")
                },
                isBuiltIn: true,
                id: "preset_extract_urls_to_json"),

            new TransformationRecipe(
                name: "Text to Clean SQL IN Clause",
                description: "Splits delimited items/lines, trims whitespace, removes duplicates, sorts naturally, and wraps in SQL IN (...)",
                category: "Database & SQL",
                hotkey: "Ctrl+Alt+2",
                steps: new[]
                {
                    new RecipeStep("SplitLines", "Split into Lines", "Lines", "Split delimited text into lines", "✂️", new() { ["Delimiter"] = "," }),
                    new RecipeStep("TrimLines", "Trim Lines", "Lines", "Trim whitespace", "🧹"),
                    new RecipeStep("Deduplicate", "Deduplicate Lines", "Lines", "Remove duplicate lines", "✨"),
                    new RecipeStep("SortNatural", "Sort Naturally", "Lines", "Sort with natural numeric ordering", "🔢"),
                    new RecipeStep("SqlIn", "Generate SQL IN (...)", "Code", "Format as SQL IN clause", "🗄️")
                },
                isBuiltIn: true,
                id: "preset_clean_sql_in"),

            new TransformationRecipe(
                name: "Lines to C# Array Literal",
                description: "Trims lines, removes blank lines, removes duplicates, and generates a C# array literal",
                category: "Code Generation",
                hotkey: "Ctrl+Alt+3",
                steps: new[]
                {
                    new RecipeStep("TrimLines", "Trim Lines", "Lines", "Trim whitespace", "🧹"),
                    new RecipeStep("RemoveEmptyLines", "Remove Empty Lines", "Lines", "Remove blank lines", "🗑️"),
                    new RecipeStep("Deduplicate", "Deduplicate Lines", "Lines", "Remove duplicate lines", "✨"),
                    new RecipeStep("ToCSharpArray", "Wrap in C# Array", "Code", "Generate C# string[] array literal", "💻")
                },
                isBuiltIn: true,
                id: "preset_lines_to_csharp_array"),

            new TransformationRecipe(
                name: "Lines to TypeScript Array",
                description: "Trims lines, removes duplicates, and wraps items into a TypeScript const array",
                category: "Code Generation",
                hotkey: "Ctrl+Alt+4",
                steps: new[]
                {
                    new RecipeStep("TrimLines", "Trim Lines", "Lines", "Trim whitespace", "🧹"),
                    new RecipeStep("Deduplicate", "Deduplicate Lines", "Lines", "Remove duplicate lines", "✨"),
                    new RecipeStep("ToTypeScriptArray", "Wrap in TypeScript Array", "Code", "Generate TypeScript const array", "📜")
                },
                isBuiltIn: true,
                id: "preset_lines_to_ts_array"),

            new TransformationRecipe(
                name: "CSV to Beautified JSON",
                description: "Converts CSV/TSV table records into JSON array of objects and applies clean formatting",
                category: "Data Conversion",
                hotkey: "Ctrl+Alt+5",
                steps: new[]
                {
                    new RecipeStep("TableToJsonObjects", "Table ➔ JSON Objects", "Tabular", "Convert table to JSON objects", "📦"),
                    new RecipeStep("FormatJson", "Beautify JSON", "Structured", "Format JSON with indentation", "✨")
                },
                isBuiltIn: true,
                id: "preset_csv_to_json"),

            new TransformationRecipe(
                name: "Clean & Deduplicate Lines",
                description: "Cleans whitespace, removes empty lines, deduplicates, and sorts alphabetically",
                category: "Line Cleaning",
                hotkey: "Ctrl+Alt+6",
                steps: new[]
                {
                    new RecipeStep("TrimLines", "Trim Lines", "Lines", "Trim whitespace and blank lines", "🧹"),
                    new RecipeStep("CollapseWhitespace", "Collapse Whitespace", "Lines", "Collapse multi-spaces", "💨"),
                    new RecipeStep("Deduplicate", "Deduplicate Lines", "Lines", "Remove duplicates", "✨"),
                    new RecipeStep("SortAlphabetical", "Sort Alphabetically", "Lines", "Sort A-Z", "🔤")
                },
                isBuiltIn: true,
                id: "preset_clean_dedup_lines"),

            new TransformationRecipe(
                name: "Extract Emails & Sort",
                description: "Extracts all email addresses, deduplicates them, and sorts alphabetically",
                category: "Web & Extraction",
                hotkey: "Ctrl+Alt+7",
                steps: new[]
                {
                    new RecipeStep("ExtractEmails", "Extract Emails", "Extraction", "Extract email addresses", "📧"),
                    new RecipeStep("Deduplicate", "Deduplicate Lines", "Lines", "Remove duplicate lines", "✨"),
                    new RecipeStep("SortAlphabetical", "Sort Alphabetically", "Lines", "Sort A-Z", "🔤")
                },
                isBuiltIn: true,
                id: "preset_extract_emails_sort"),

            new TransformationRecipe(
                name: "Query String to Prettified JSON",
                description: "Parses URL query parameters into key-value pairs and converts to formatted JSON",
                category: "Web & Extraction",
                steps: new[]
                {
                    new RecipeStep("QueryStringToKv", "Query String ➔ Key-Value", "Structured", "Parse URL query string", "🌐"),
                    new RecipeStep("KvToJson", "Key-Value ➔ JSON Object", "Structured", "Convert to JSON object", "📦"),
                    new RecipeStep("FormatJson", "Beautify JSON", "Structured", "Format JSON with indentation", "✨")
                },
                isBuiltIn: true,
                id: "preset_query_to_json"),

            new TransformationRecipe(
                name: "JSON Key Sort & Prettify",
                description: "Recursively sorts all JSON object keys alphabetically and prettifies formatting",
                category: "Data Conversion",
                steps: new[]
                {
                    new RecipeStep("SortJsonKeys", "Sort JSON Keys", "Structured", "Sort object keys alphabetically", "🔤"),
                    new RecipeStep("FormatJson", "Beautify JSON", "Structured", "Format JSON with indentation", "✨")
                },
                isBuiltIn: true,
                id: "preset_sort_json_keys"),

            new TransformationRecipe(
                name: "Base64 Decode to Prettified JSON",
                description: "Decodes a Base64 encoded payload and formats the resulting JSON",
                category: "Data Conversion",
                steps: new[]
                {
                    new RecipeStep("Base64Decode", "Base64 Decode", "Encoding", "Decode Base64 string", "🔓"),
                    new RecipeStep("FormatJson", "Beautify JSON", "Structured", "Format JSON with indentation", "✨")
                },
                isBuiltIn: true,
                id: "preset_base64_to_json")
        };
    }
}
