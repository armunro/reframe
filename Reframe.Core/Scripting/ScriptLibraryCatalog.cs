using System;
using System.Collections.Generic;

namespace Reframe.Core.Scripting;

/// <summary>
/// Built-in catalog of C# and LINQ scratchpad presets for common developer transformations.
/// </summary>
public static class ScriptLibraryCatalog
{
    public static IReadOnlyList<ScriptPreset> Presets { get; } = new List<ScriptPreset>
    {
        new()
        {
            Id = "FilterAndTrimLines",
            Title = "Filter & Trim Lines",
            Description = "Trims whitespace from each line and filters out empty lines",
            Category = "Lines & Text",
            Script = "lines.Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x))",
            SampleInput = "  Alpha  \n\n   Beta   \n \n Gamma \n"
        },
        new()
        {
            Id = "DeduplicateAndSort",
            Title = "Deduplicate & Sort Alphabetically",
            Description = "Cleans, deduplicates, and sorts lines in ascending order",
            Category = "Lines & Text",
            Script = "lines.Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x)",
            SampleInput = "Pear\nApple\nBanana\nApple\nOrange\nBanana\n"
        },
        new()
        {
            Id = "LineNumbering",
            Title = "Add Line Numbers",
            Description = "Prepends 3-digit padded line numbers to every line",
            Category = "Lines & Text",
            Script = "lines.Select((line, index) => $\"{(index + 1).ToString().PadLeft(3)} | {line}\")",
            SampleInput = "First entry\nSecond entry\nThird entry\nFourth entry"
        },
        new()
        {
            Id = "GroupByFrequency",
            Title = "Group by Frequency & Count",
            Description = "Groups items by value and outputs item counts sorted by frequency",
            Category = "Aggregation & Stats",
            Script = "lines.Where(x => !string.IsNullOrWhiteSpace(x)).GroupBy(x => x.Trim()).OrderByDescending(g => g.Count()).Select(g => $\"{g.Key}: {g.Count()}\")",
            SampleInput = "GET\nPOST\nGET\nGET\nPUT\nPOST\nDELETE\nGET\n"
        },
        new()
        {
            Id = "ExtractNumbersAndSum",
            Title = "Extract Numbers & Calculate Sum",
            Description = "Finds all integers and decimals in the text and computes their total sum",
            Category = "Aggregation & Stats",
            Script = "Regex.Matches(input, @\"-?\\d+(\\.\\d+)?\").Select(m => double.Parse(m.Value)).Sum()",
            SampleInput = "Item 1: $14.50\nItem 2: $22.00\nDiscount: -5.50\nTax: $3.25"
        },
        new()
        {
            Id = "SqlInClause",
            Title = "Generate SQL IN Clause",
            Description = "Generates a SQL IN ('val1', 'val2', ...) clause from non-empty lines",
            Category = "Code & SQL Generation",
            Script = "\"IN (\" + string.Join(\", \", lines.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => $\"'{l.Trim().Replace(\"'\", \"''\")}'\")) + \")\"",
            SampleInput = "USR-1001\nUSR-1002\nUSR-1003\nUSR-1004"
        },
        new()
        {
            Id = "CSharpStringArray",
            Title = "Wrap in C# String Array",
            Description = "Converts lines into a formatted C# string[] initializer",
            Category = "Code & SQL Generation",
            Script = "\"string[] items = new[]\\n{\\n\" + string.Join(\",\\n\", lines.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => $\"    \\\"{l.Trim().Replace(\"\\\"\", \"\\\\\\\"\")}\\\"\")) + \"\\n};\"",
            SampleInput = "Development\nStaging\nProduction"
        },
        new()
        {
            Id = "CsvToJsonObjects",
            Title = "CSV Lines to JSON Objects",
            Description = "Parses comma-delimited lines into structured dynamic objects and dumps as JSON",
            Category = "Structured & JSON",
            Script = "lines.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Split(',')).Select(cols => new { ID = cols.ElementAtOrDefault(0)?.Trim(), Name = cols.ElementAtOrDefault(1)?.Trim(), Status = cols.ElementAtOrDefault(2)?.Trim() })",
            SampleInput = "101, Alice, Active\n102, Bob, Pending\n103, Charlie, Inactive"
        },
        new()
        {
            Id = "ExtractEmails",
            Title = "Extract Email Addresses",
            Description = "Extracts all valid email addresses using regular expressions",
            Category = "Extraction & Regex",
            Script = "Regex.Matches(input, @\"\\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}\\b\").Select(m => m.Value).Distinct()",
            SampleInput = "Contact us at support@example.com or sales@example.org. CC: dev.team@company.co.uk."
        },
        new()
        {
            Id = "SplitDelimitersAndFlatten",
            Title = "Split Multiple Delimiters & Flatten",
            Description = "Splits input on commas, semicolons, and pipes into a clean distinct list",
            Category = "Lines & Text",
            Script = "input.Split(new[] { ',', ';', '|', '\\n', '\\r' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).Distinct()",
            SampleInput = "tag1, tag2; tag3 | tag4, tag1, tag5; tag2"
        },
        new()
        {
            Id = "Base64EncodeLines",
            Title = "Base64 Encode Lines",
            Description = "Encodes each line into Base64 format",
            Category = "Encoding & Security",
            Script = "lines.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => Convert.ToBase64String(Encoding.UTF8.GetBytes(l)))",
            SampleInput = "Hello World\nReframe C# Scratchpad\nRoslyn Scripting"
        },
        new()
        {
            Id = "RegexWordTransformer",
            Title = "Regex Word Transformation",
            Description = "Transforms words matching regex pattern using an evaluator function",
            Category = "Extraction & Regex",
            Script = "Regex.Replace(input, @\"\\b\\w+\\b\", m => m.Value.Length > 3 ? m.Value.ToUpperInvariant() : m.Value)",
            SampleInput = "the quick brown fox jumps over the lazy dog"
        }
    };
}
