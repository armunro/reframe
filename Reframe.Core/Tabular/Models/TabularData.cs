using System.Text.RegularExpressions;
using Reframe.Core.Tabular.Formulas;
using Reframe.Core.Transformers.Line;

namespace Reframe.Core.Tabular.Models;

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

    /// <summary>
    /// Removes a single column by index and returns a new TabularData.
    /// </summary>
    public TabularData RemoveColumn(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= Columns.Count) return Clone();
        return DropColumns(new[] { columnIndex });
    }

    /// <summary>
    /// Removes a single column by name (case-insensitive) and returns a new TabularData.
    /// </summary>
    public TabularData RemoveColumn(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName)) return Clone();
        int idx = Columns.FindIndex(c => string.Equals(c, columnName, StringComparison.OrdinalIgnoreCase));
        if (idx < 0) return Clone();
        return RemoveColumn(idx);
    }

    /// <summary>
    /// Removes multiple columns by names (case-insensitive) and returns a new TabularData.
    /// </summary>
    public TabularData RemoveColumns(IEnumerable<string> columnNames)
    {
        var nameSet = new HashSet<string>(columnNames, StringComparer.OrdinalIgnoreCase);
        var dropIndices = new List<int>();
        for (int i = 0; i < Columns.Count; i++)
        {
            if (nameSet.Contains(Columns[i]))
            {
                dropIndices.Add(i);
            }
        }
        return DropColumns(dropIndices);
    }

    /// <summary>
    /// Adds a new column with a default or constant value at the optional insertIndex.
    /// </summary>
    public TabularData AddColumn(string columnName, string defaultValue = "", int? insertIndex = null)
    {
        var result = Clone();
        string cleanColName = string.IsNullOrWhiteSpace(columnName)
            ? $"Column_{result.Columns.Count + 1}"
            : columnName.Trim();

        int targetIndex = insertIndex.HasValue
            ? Math.Clamp(insertIndex.Value, 0, result.Columns.Count)
            : result.Columns.Count;

        result.Columns.Insert(targetIndex, cleanColName);

        for (int r = 0; r < result.Rows.Count; r++)
        {
            if (targetIndex < result.Rows[r].Count)
            {
                result.Rows[r].Insert(targetIndex, defaultValue);
            }
            else
            {
                while (result.Rows[r].Count < targetIndex)
                {
                    result.Rows[r].Add(string.Empty);
                }
                result.Rows[r].Add(defaultValue);
            }
        }

        return result;
    }

    /// <summary>
    /// Adds a new column computed by a custom row function at the optional insertIndex.
    /// </summary>
    public TabularData AddColumn(string columnName, Func<IReadOnlyList<string>, int, string> rowValueProvider, int? insertIndex = null)
    {
        var result = Clone();
        string cleanColName = string.IsNullOrWhiteSpace(columnName)
            ? $"Column_{result.Columns.Count + 1}"
            : columnName.Trim();

        int targetIndex = insertIndex.HasValue
            ? Math.Clamp(insertIndex.Value, 0, result.Columns.Count)
            : result.Columns.Count;

        result.Columns.Insert(targetIndex, cleanColName);

        for (int r = 0; r < result.Rows.Count; r++)
        {
            string val = rowValueProvider(result.Rows[r], r) ?? string.Empty;
            if (targetIndex < result.Rows[r].Count)
            {
                result.Rows[r].Insert(targetIndex, val);
            }
            else
            {
                while (result.Rows[r].Count < targetIndex)
                {
                    result.Rows[r].Add(string.Empty);
                }
                result.Rows[r].Add(val);
            }
        }

        return result;
    }

    /// <summary>
    /// Evaluates an Excel-like formula for each row and adds the calculated column.
    /// </summary>
    public TabularData AddCalculatedColumn(string columnName, string formula, int? insertIndex = null)
    {
        return TabularFormulaEngine.AddCalculatedColumn(this, columnName, formula, insertIndex);
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

    /// <summary>
    /// Converts rows into key-value pairs where key is from the key column and value is a dictionary
    /// containing all other column names and their values.
    /// </summary>
    public List<KeyValuePair<string, Dictionary<string, object?>>> ToKeyValueObjectPairs(int keyColumnIndex)
    {
        var list = new List<KeyValuePair<string, Dictionary<string, object?>>>();
        var headers = Columns.Select((c, idx) => string.IsNullOrWhiteSpace(c) ? $"Column_{idx + 1}" : c).ToList();

        foreach (var row in Rows)
        {
            string key = (keyColumnIndex >= 0 && keyColumnIndex < row.Count) ? row[keyColumnIndex] : string.Empty;
            var valDict = new Dictionary<string, object?>();
            for (int i = 0; i < headers.Count; i++)
            {
                if (i == keyColumnIndex) continue;
                string rawVal = i < row.Count ? row[i] : string.Empty;
                if (long.TryParse(rawVal, out long lVal))
                {
                    valDict[headers[i]] = lVal;
                }
                else if (double.TryParse(rawVal, System.Globalization.CultureInfo.InvariantCulture, out double dVal))
                {
                    valDict[headers[i]] = dVal;
                }
                else if (bool.TryParse(rawVal, out bool bVal))
                {
                    valDict[headers[i]] = bVal;
                }
                else
                {
                    valDict[headers[i]] = rawVal;
                }
            }
            list.Add(new KeyValuePair<string, Dictionary<string, object?>>(key, valDict));
        }
        return list;
    }

    /// <summary>
    /// Overrides current column headers with the provided surrogate headers in-place,
    /// setting HasHeaders to true and standardizing row lengths.
    /// </summary>
    public void OverrideHeaders(IEnumerable<string> headers)
    {
        var headerList = headers.Where(h => !string.IsNullOrWhiteSpace(h)).Select(h => h.Trim()).ToList();
        if (headerList.Count == 0) return;

        int targetColCount = Math.Max(Columns.Count, headerList.Count);
        if (targetColCount == 0 && Rows.Count > 0)
        {
            targetColCount = Rows.Max(r => r.Count);
        }
        targetColCount = Math.Max(targetColCount, headerList.Count);

        var newColumns = new List<string>();
        for (int i = 0; i < targetColCount; i++)
        {
            if (i < headerList.Count && !string.IsNullOrWhiteSpace(headerList[i]))
            {
                newColumns.Add(headerList[i]);
            }
            else if (i < Columns.Count && !string.IsNullOrWhiteSpace(Columns[i]))
            {
                newColumns.Add(Columns[i]);
            }
            else
            {
                newColumns.Add($"Column {i + 1}");
            }
        }

        Columns = newColumns;
        HasHeaders = true;

        foreach (var row in Rows)
        {
            while (row.Count < Columns.Count)
            {
                row.Add(string.Empty);
            }
        }
    }

    /// <summary>
    /// Returns a clone of the table with surrogate headers applied.
    /// </summary>
    public TabularData WithSurrogateHeaders(IEnumerable<string> headers)
    {
        var result = Clone();
        result.OverrideHeaders(headers);
        return result;
    }

    /// <summary>
    /// Generates and assigns surrogate headers (e.g. Column_1, Column_2 or Col1, Col2).
    /// </summary>
    public void SetSurrogateHeaders(string prefix = "Column_", int? count = null)
    {
        int colCount = count ?? (Columns.Count > 0 ? Columns.Count : (Rows.Count > 0 ? Rows.Max(r => r.Count) : 0));
        if (colCount <= 0) colCount = 1;

        var genHeaders = Enumerable.Range(1, colCount).Select(i => $"{prefix}{i}").ToList();
        OverrideHeaders(genHeaders);
    }
}
