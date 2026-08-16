using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace TextForge.Core.Tabular;

public static class HtmlTableParser
{
    private static readonly Regex TableTagRegex = new(@"<table\b[^>]*>(.*?)</table>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex RowTagRegex = new(@"<tr\b[^>]*>(.*?)</tr>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex CellTagRegex = new(@"<(th|td)\b([^>]*)>(.*?)</\1>|<(th|td)\b([^>]*)/>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex ColSpanRegex = new(@"\bcolspan\s*=\s*[""']?(\d+)[""']?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RowSpanRegex = new(@"\browspan\s*=\s*[""']?(\d+)[""']?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ScriptStyleCommentRegex = new(@"<!--.*?-->|<script\b[^>]*>.*?</script>|<style\b[^>]*>.*?</style>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex HtmlTagRegex = new(@"<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex BrTagRegex = new(@"<br\s*/?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool IsHtmlTable(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        // Fast check
        if (text.Contains("<table", StringComparison.OrdinalIgnoreCase) && 
            (text.Contains("</table>", StringComparison.OrdinalIgnoreCase) || text.Contains("<tr", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // Clean comments/scripts/styles first
        string cleaned = ScriptStyleCommentRegex.Replace(text, " ");

        // Check for presence of table, tr, and td/th tags
        bool hasTable = TableTagRegex.IsMatch(cleaned) || (cleaned.Contains("<tr", StringComparison.OrdinalIgnoreCase) && (cleaned.Contains("<td", StringComparison.OrdinalIgnoreCase) || cleaned.Contains("<th", StringComparison.OrdinalIgnoreCase)));
        return hasTable;
    }

    public static string ExtractTableHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;

        // 1. If HTML clipboard contains StartFragment and EndFragment comments, extract inner fragment
        string candidate = html;
        int startFrag = candidate.IndexOf("<!--StartFragment-->", StringComparison.OrdinalIgnoreCase);
        int endFrag = candidate.IndexOf("<!--EndFragment-->", StringComparison.OrdinalIgnoreCase);
        if (startFrag >= 0 && endFrag > startFrag)
        {
            startFrag += "<!--StartFragment-->".Length;
            candidate = candidate.Substring(startFrag, endFrag - startFrag).Trim();
        }

        // 2. Look for table tag in candidate
        var match = TableTagRegex.Match(candidate);
        if (match.Success)
        {
            return match.Value.Trim();
        }

        // 3. Look for table tag in original html if not found in fragment
        var fullMatch = TableTagRegex.Match(html);
        if (fullMatch.Success)
        {
            return fullMatch.Value.Trim();
        }

        // 4. If no table tag, but rows exist, wrap rows in <table>
        if (RowTagRegex.IsMatch(candidate))
        {
            return $"<table>\n{candidate}\n</table>";
        }

        return candidate.Trim();
    }

    public static TabularData Parse(string? html, bool? assumeHeader = null)
    {
        var result = new TabularData { Delimiter = null, HasHeaders = assumeHeader ?? true };
        if (string.IsNullOrWhiteSpace(html)) return result;

        // Clean out HTML comments (like <!--StartFragment-->), styles, and scripts
        string cleanedHtml = ScriptStyleCommentRegex.Replace(html, " ");

        var tableMatches = TableTagRegex.Matches(cleanedHtml);
        string tableHtmlToParse;

        if (tableMatches.Count > 0)
        {
            // If multiple tables, choose the one with the most rows or use the first
            Match bestTable = tableMatches[0];
            int maxRowCount = 0;
            foreach (Match tm in tableMatches)
            {
                int rowCount = RowTagRegex.Matches(tm.Value).Count;
                if (rowCount > maxRowCount)
                {
                    maxRowCount = rowCount;
                    bestTable = tm;
                }
            }
            tableHtmlToParse = bestTable.Value;
        }
        else
        {
            // If no <table>...</table> wrapper, check if it's a raw <tr>...</tr> sequence
            tableHtmlToParse = cleanedHtml;
        }

        var rowMatches = RowTagRegex.Matches(tableHtmlToParse);
        if (rowMatches.Count == 0) return result;

        // We will build a 2D matrix accounting for colspans and rowspans
        var matrix = new List<List<string>>();
        // Track spans: dictionary of (targetRow, targetCol) -> text
        var spannedCells = new Dictionary<(int Row, int Col), string>();

        bool firstRowHasTh = false;

        for (int r = 0; r < rowMatches.Count; r++)
        {
            var rowContent = rowMatches[r].Groups[1].Value;
            var cellMatches = CellTagRegex.Matches(rowContent);
            
            var currentRow = new List<string>();
            int currentCol = 0;

            if (r == 0)
            {
                // Check if any cell in row 0 is <th>
                foreach (Match cm in cellMatches)
                {
                    string tagName = cm.Groups[1].Success ? cm.Groups[1].Value : cm.Groups[4].Value;
                    if (tagName.Equals("th", StringComparison.OrdinalIgnoreCase))
                    {
                        firstRowHasTh = true;
                        break;
                    }
                }
            }

            foreach (Match cellMatch in cellMatches)
            {
                // Fill any positions occupied by previous rowspans
                while (spannedCells.ContainsKey((r, currentCol)))
                {
                    currentRow.Add(spannedCells[(r, currentCol)]);
                    currentCol++;
                }

                string attributes = cellMatch.Groups[2].Success ? cellMatch.Groups[2].Value : cellMatch.Groups[5].Value;
                string innerHtml = cellMatch.Groups[3].Success ? cellMatch.Groups[3].Value : string.Empty;

                string cellValue = CleanCellText(innerHtml);

                int colspan = 1;
                var csMatch = ColSpanRegex.Match(attributes);
                if (csMatch.Success && int.TryParse(csMatch.Groups[1].Value, out int cs) && cs > 1)
                {
                    colspan = cs;
                }

                int rowspan = 1;
                var rsMatch = RowSpanRegex.Match(attributes);
                if (rsMatch.Success && int.TryParse(rsMatch.Groups[1].Value, out int rs) && rs > 1)
                {
                    rowspan = rs;
                }

                // Add cell value for current column
                currentRow.Add(cellValue);

                // Handle colspan
                for (int c = 1; c < colspan; c++)
                {
                    currentRow.Add(cellValue); // or empty string, but repeating value or keeping width aligns columns
                }

                // Handle rowspan for future rows
                if (rowspan > 1)
                {
                    for (int spanR = 1; spanR < rowspan; spanR++)
                    {
                        for (int spanC = 0; spanC < colspan; spanC++)
                        {
                            spannedCells[(r + spanR, currentCol + spanC)] = cellValue;
                        }
                    }
                }

                currentCol += colspan;
            }

            // Fill trailing spanned cells if any
            while (spannedCells.ContainsKey((r, currentCol)))
            {
                currentRow.Add(spannedCells[(r, currentCol)]);
                currentCol++;
            }

            if (currentRow.Count > 0)
            {
                matrix.Add(currentRow);
            }
        }

        if (matrix.Count == 0) return result;

        // Standardize column lengths
        int maxCols = matrix.Max(row => row.Count);
        foreach (var row in matrix)
        {
            while (row.Count < maxCols)
            {
                row.Add(string.Empty);
            }
        }

        // Determine headers
        bool hasHeaders;
        if (assumeHeader.HasValue)
        {
            hasHeaders = assumeHeader.Value;
        }
        else if (firstRowHasTh)
        {
            hasHeaders = true;
        }
        else
        {
            hasHeaders = TabularParser.DetectHasHeaders(matrix);
        }

        result.HasHeaders = hasHeaders;

        if (hasHeaders)
        {
            result.Columns = matrix[0];
            // Ensure header names are not blank
            for (int i = 0; i < result.Columns.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(result.Columns[i]))
                {
                    result.Columns[i] = $"Column {i + 1}";
                }
            }
            result.Rows = matrix.Skip(1).ToList();
        }
        else
        {
            result.Columns = Enumerable.Range(1, maxCols).Select(i => $"Column {i}").ToList();
            result.Rows = matrix;
        }

        return result;
    }

    private static string CleanCellText(string rawHtml)
    {
        if (string.IsNullOrEmpty(rawHtml)) return string.Empty;

        // Replace <br> with spaces (or newlines if desired, but spaces keep tabular rows clean)
        string text = BrTagRegex.Replace(rawHtml, " ");

        // Strip HTML tags
        text = HtmlTagRegex.Replace(text, " ");

        // HTML Decode entities (&nbsp;, &amp;, &lt;, &gt;, &#39;, &quot;, &#xNN;, etc.)
        text = WebUtility.HtmlDecode(text);

        // Replace non-breaking space \u00A0 with standard space
        text = text.Replace('\u00A0', ' ');

        // Collapse multiple spaces/newlines inside cell
        var sb = new StringBuilder(text.Length);
        bool prevSpace = false;
        foreach (char c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                if (!prevSpace)
                {
                    sb.Append(' ');
                    prevSpace = true;
                }
            }
            else
            {
                sb.Append(c);
                prevSpace = false;
            }
        }

        return sb.ToString().Trim();
    }
}
