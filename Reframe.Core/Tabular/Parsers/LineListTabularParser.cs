using System;
using System.Collections.Generic;
using System.Linq;
using Reframe.Core.Tabular.Models;

namespace Reframe.Core.Tabular.Parsers;

/// <summary>
/// Parses plain multi-line text or single-column lists of numbers/words into TabularData.
/// </summary>
public class LineListTabularParser : ITabularParser
{
    public static LineListTabularParser Instance { get; } = new();

    public bool CanParse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var lines = GetNonEmptyLines(text);
        return lines.Count > 0;
    }

    public TabularData? Parse(string? text, bool? assumeHeader = null, IEnumerable<string>? surrogateHeaders = null)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var rawLines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        // Exclude trailing blank line if present
        var lines = rawLines.ToList();
        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1]))
        {
            lines.RemoveAt(lines.Count - 1);
        }

        if (lines.Count == 0) return null;

        var allRows = lines.Select(l => (IReadOnlyList<string>)new List<string> { l.TrimEnd('\r') }).ToList();

        bool hasHeaders = assumeHeader ?? TabularParser.DetectHasHeaders(allRows);

        var table = new TabularData
        {
            Delimiter = '\n',
            HasHeaders = hasHeaders
        };

        if (hasHeaders && allRows.Count > 0)
        {
            string headerName = allRows[0][0];
            if (string.IsNullOrWhiteSpace(headerName)) headerName = "Column 1";
            table.Columns = new List<string> { headerName };
            table.Rows = allRows.Skip(1).Select(r => (List<string>)[r[0]]).ToList();
        }
        else
        {
            table.Columns = new List<string> { "Column 1" };
            table.Rows = allRows.Select(r => (List<string>)[r[0]]).ToList();
        }

        if (surrogateHeaders != null)
        {
            var headerList = surrogateHeaders.ToList();
            if (headerList.Count > 0)
            {
                table.OverrideHeaders(headerList);
            }
        }

        return table;
    }

    private static List<string> GetNonEmptyLines(string text)
    {
        return text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                   .Where(l => !string.IsNullOrWhiteSpace(l))
                   .ToList();
    }
}
