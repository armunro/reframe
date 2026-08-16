using System.Text;
using System.Text.RegularExpressions;
using TextForge.Core.Transformers;

namespace TextForge.Core.Tabular;

public class TabularData
{
    public char? Delimiter { get; set; }
    public List<string> Columns { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();

    public bool HasHeaders { get; set; } = true;

    public TabularData Clone()
    {
        return new TabularData
        {
            Delimiter = Delimiter,
            Columns = new List<string>(Columns),
            Rows = Rows.Select(r => new List<string>(r)).ToList(),
            HasHeaders = HasHeaders
        };
    }

    public TabularData Transpose()
    {
        var result = new TabularData();
        if (Columns.Count == 0 && Rows.Count == 0)
            return result;

        // Collect all data including headers as row 0 if HasHeaders
        var allRows = new List<List<string>>();
        if (Columns.Count > 0 && HasHeaders)
        {
            allRows.Add(Columns);
        }
        allRows.AddRange(Rows);

        int maxCols = allRows.Max(r => r.Count);
        int totalRows = allRows.Count;

        for (int c = 0; c < maxCols; c++)
        {
            var newRow = new List<string>();
            for (int r = 0; r < totalRows; r++)
            {
                newRow.Add(c < allRows[r].Count ? allRows[r][c] : string.Empty);
            }
            if (c == 0)
            {
                result.Columns = newRow;
            }
            else
            {
                result.Rows.Add(newRow);
            }
        }

        result.HasHeaders = true;
        result.Delimiter = Delimiter;
        return result;
    }

    public List<string> ExtractColumn(int columnIndex)
    {
        var list = new List<string>();
        foreach (var row in Rows)
        {
            if (columnIndex >= 0 && columnIndex < row.Count)
            {
                list.Add(row[columnIndex]);
            }
            else
            {
                list.Add(string.Empty);
            }
        }
        return list;
    }

    public TabularData SelectColumns(IEnumerable<int> columnIndices)
    {
        var idxList = columnIndices.ToList();
        var result = new TabularData
        {
            Delimiter = Delimiter,
            HasHeaders = HasHeaders,
            Columns = idxList.Select(i => i >= 0 && i < Columns.Count ? Columns[i] : $"Col{i + 1}").ToList()
        };

        foreach (var row in Rows)
        {
            var newRow = idxList.Select(i => i >= 0 && i < row.Count ? row[i] : string.Empty).ToList();
            result.Rows.Add(newRow);
        }

        return result;
    }

    public TabularData DropColumns(IEnumerable<int> columnIndicesToDrop)
    {
        var dropSet = new HashSet<int>(columnIndicesToDrop);
        var keepIndices = Enumerable.Range(0, Columns.Count).Where(i => !dropSet.Contains(i)).ToList();
        return SelectColumns(keepIndices);
    }

    public TabularData ReorderColumns(IEnumerable<int> newOrder)
    {
        return SelectColumns(newOrder);
    }

    public TabularData TransformColumns(IEnumerable<int> columnIndices, Func<string, string> transform)
    {
        var indices = new HashSet<int>(columnIndices);
        var result = Clone();

        for (int r = 0; r < result.Rows.Count; r++)
        {
            for (int c = 0; c < result.Rows[r].Count; c++)
            {
                if (indices.Contains(c))
                {
                    result.Rows[r][c] = transform(result.Rows[r][c]);
                }
            }
        }

        return result;
    }

    public TabularData TransformColumn(int columnIndex, Func<string, string> transform)
    {
        return TransformColumns(new[] { columnIndex }, transform);
    }

    public TabularData SortByColumn(int columnIndex, SortOrder order = SortOrder.NaturalNumericAsc)
    {
        if (columnIndex < 0 || Rows.Count == 0) return Clone();

        var result = Clone();
        var naturalComparer = new NaturalStringComparer();

        result.Rows = order switch
        {
            SortOrder.NaturalNumericAsc => result.Rows.OrderBy(r => columnIndex < r.Count ? r[columnIndex] : "", naturalComparer).ToList(),
            SortOrder.NaturalNumericDesc => result.Rows.OrderByDescending(r => columnIndex < r.Count ? r[columnIndex] : "", naturalComparer).ToList(),
            SortOrder.AlphabeticalAsc => result.Rows.OrderBy(r => columnIndex < r.Count ? r[columnIndex] : "", StringComparer.CurrentCultureIgnoreCase).ToList(),
            SortOrder.AlphabeticalDesc => result.Rows.OrderByDescending(r => columnIndex < r.Count ? r[columnIndex] : "", StringComparer.CurrentCultureIgnoreCase).ToList(),
            SortOrder.LengthAsc => result.Rows.OrderBy(r => (columnIndex < r.Count ? r[columnIndex] : "").Length).ToList(),
            SortOrder.LengthDesc => result.Rows.OrderByDescending(r => (columnIndex < r.Count ? r[columnIndex] : "").Length).ToList(),
            SortOrder.Reverse => result.Rows.AsEnumerable().Reverse().ToList(),
            _ => result.Rows
        };

        return result;
    }

    public TabularData FilterRows(int columnIndex, string query, bool caseSensitive = false, bool isRegex = false, bool keepMatching = true)
    {
        var result = Clone();
        if (string.IsNullOrEmpty(query)) return result;

        Regex? regex = null;
        if (isRegex)
        {
            try
            {
                var options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                regex = new Regex(query, options);
            }
            catch
            {
                // Invalid regex, treat as plain text
            }
        }

        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        result.Rows = result.Rows.Where(row =>
        {
            string cell = (columnIndex >= 0 && columnIndex < row.Count) ? row[columnIndex] : string.Empty;
            bool matches;
            if (regex != null)
            {
                matches = regex.IsMatch(cell);
            }
            else
            {
                matches = cell.Contains(query, comparison);
            }

            return keepMatching ? matches : !matches;
        }).ToList();

        return result;
    }

    public List<string> ExtractColumnsAsLines(IEnumerable<int> columnIndices, string delimiter = "\t")
    {
        var idxList = columnIndices.ToList();
        var lines = new List<string>();

        foreach (var row in Rows)
        {
            var parts = idxList.Select(i => (i >= 0 && i < row.Count) ? row[i] : string.Empty);
            lines.Add(string.Join(delimiter, parts));
        }

        return lines;
    }

    public List<KeyValuePair<string, string>> ToKeyValuePairs(int keyColumnIndex, int valueColumnIndex)
    {
        var list = new List<KeyValuePair<string, string>>();
        foreach (var row in Rows)
        {
            string key = (keyColumnIndex >= 0 && keyColumnIndex < row.Count) ? row[keyColumnIndex] : string.Empty;
            string val = (valueColumnIndex >= 0 && valueColumnIndex < row.Count) ? row[valueColumnIndex] : string.Empty;
            list.Add(new KeyValuePair<string, string>(key, val));
        }
        return list;
    }
}
