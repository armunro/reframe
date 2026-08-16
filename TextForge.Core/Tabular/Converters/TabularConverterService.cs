using System.Text;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace TextForge.Core.Tabular;

public class TabularConverterService : ITabularConverter
{
    public static TabularConverterService Instance { get; } = new();

    public string ToCsv(TabularData table, char delimiter = ',')
    {
        var sb = new StringBuilder();

        if (table.HasHeaders && table.Columns.Count > 0)
        {
            sb.AppendLine(string.Join(delimiter, table.Columns.Select(c => EscapeCsvCell(c, delimiter))));
        }

        foreach (var row in table.Rows)
        {
            sb.AppendLine(string.Join(delimiter, row.Select(c => EscapeCsvCell(c, delimiter))));
        }

        return sb.ToString().TrimEnd('\r', '\n');
    }

    public string ToTsv(TabularData table) => ToCsv(table, '\t');

    public string ToMarkdownTable(TabularData table)
    {
        if (table.Columns.Count == 0 && table.Rows.Count == 0) return string.Empty;

        int colCount = Math.Max(table.Columns.Count, table.Rows.Count > 0 ? table.Rows.Max(r => r.Count) : 0);
        if (colCount == 0) return string.Empty;

        // Calculate maximum width per column
        var widths = new int[colCount];
        for (int i = 0; i < colCount; i++)
        {
            string colHeader = i < table.Columns.Count ? table.Columns[i] : $"Col{i + 1}";
            widths[i] = Math.Max(3, colHeader.Length);
        }

        foreach (var row in table.Rows)
        {
            for (int i = 0; i < colCount; i++)
            {
                string cell = i < row.Count ? row[i] : string.Empty;
                widths[i] = Math.Max(widths[i], cell.Length);
            }
        }

        var sb = new StringBuilder();

        // Header
        sb.Append("| ");
        for (int i = 0; i < colCount; i++)
        {
            string header = i < table.Columns.Count ? table.Columns[i] : $"Col{i + 1}";
            sb.Append(header.PadRight(widths[i]));
            sb.Append(" | ");
        }
        sb.AppendLine();

        // Separator
        sb.Append("| ");
        for (int i = 0; i < colCount; i++)
        {
            sb.Append(new string('-', widths[i]));
            sb.Append(" | ");
        }
        sb.AppendLine();

        // Rows
        foreach (var row in table.Rows)
        {
            sb.Append("| ");
            for (int i = 0; i < colCount; i++)
            {
                string cell = i < row.Count ? row[i] : string.Empty;
                sb.Append(cell.PadRight(widths[i]));
                sb.Append(" | ");
            }
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd('\r', '\n');
    }

    public string ToJsonArrayOfObjects(TabularData table, bool indented = true)
    {
        var list = new List<Dictionary<string, object?>>();

        var headers = table.Columns.Select((c, idx) => string.IsNullOrWhiteSpace(c) ? $"Column_{idx + 1}" : c).ToList();

        foreach (var row in table.Rows)
        {
            var dict = new Dictionary<string, object?>();
            for (int i = 0; i < headers.Count; i++)
            {
                string rawVal = i < row.Count ? row[i] : string.Empty;
                // Infer type if integer or double or bool
                if (long.TryParse(rawVal, out long lVal))
                {
                    dict[headers[i]] = lVal;
                }
                else if (double.TryParse(rawVal, System.Globalization.CultureInfo.InvariantCulture, out double dVal))
                {
                    dict[headers[i]] = dVal;
                }
                else if (bool.TryParse(rawVal, out bool bVal))
                {
                    dict[headers[i]] = bVal;
                }
                else
                {
                    dict[headers[i]] = rawVal;
                }
            }
            list.Add(dict);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = indented,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        return JsonSerializer.Serialize(list, options);
    }

    public string ToJsonArrayOfArrays(TabularData table, bool indented = true)
    {
        var list = new List<List<string>>();
        if (table.HasHeaders && table.Columns.Count > 0)
        {
            list.Add(table.Columns);
        }
        list.AddRange(table.Rows);

        var options = new JsonSerializerOptions
        {
            WriteIndented = indented,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        return JsonSerializer.Serialize(list, options);
    }

    public string ToSqlInsertStatements(TabularData table, string tableName = "MyTable")
    {
        if (table.Rows.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        string cols = string.Join(", ", table.Columns.Select(c => $"[{c}]"));

        foreach (var row in table.Rows)
        {
            var vals = row.Select(FormatSqlValue);
            sb.AppendLine($"INSERT INTO [{tableName}] ({cols}) VALUES ({string.Join(", ", vals)});");
        }

        return sb.ToString().TrimEnd('\r', '\n');
    }

    public string ToHtmlTable(TabularData table)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<table class=\"table\">");

        if (table.HasHeaders && table.Columns.Count > 0)
        {
            sb.AppendLine("  <thead>");
            sb.AppendLine("    <tr>");
            foreach (var col in table.Columns)
            {
                sb.AppendLine($"      <th>{System.Net.WebUtility.HtmlEncode(col)}</th>");
            }
            sb.AppendLine("    </tr>");
            sb.AppendLine("  </thead>");
        }

        sb.AppendLine("  <tbody>");
        foreach (var row in table.Rows)
        {
            sb.AppendLine("    <tr>");
            foreach (var cell in row)
            {
                sb.AppendLine($"      <td>{System.Net.WebUtility.HtmlEncode(cell)}</td>");
            }
            sb.AppendLine("    </tr>");
        }
        sb.AppendLine("  </tbody>");
        sb.Append("</table>");

        return sb.ToString();
    }

    public string ToYaml(TabularData table)
    {
        var list = new List<Dictionary<string, object?>>();
        var headers = table.Columns.Select((c, idx) => string.IsNullOrWhiteSpace(c) ? $"Column_{idx + 1}" : c).ToList();

        foreach (var row in table.Rows)
        {
            var dict = new Dictionary<string, object?>();
            for (int i = 0; i < headers.Count; i++)
            {
                string rawVal = i < row.Count ? row[i] : string.Empty;
                if (long.TryParse(rawVal, out long lVal))
                {
                    dict[headers[i]] = lVal;
                }
                else if (double.TryParse(rawVal, System.Globalization.CultureInfo.InvariantCulture, out double dVal))
                {
                    dict[headers[i]] = dVal;
                }
                else if (bool.TryParse(rawVal, out bool bVal))
                {
                    dict[headers[i]] = bVal;
                }
                else
                {
                    dict[headers[i]] = rawVal;
                }
            }
            list.Add(dict);
        }

        var serializer = new SerializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .Build();

        return serializer.Serialize(list).TrimEnd('\r', '\n');
    }

    public string ToYamlArrays(TabularData table)
    {
        var list = new List<List<string>>();
        if (table.HasHeaders && table.Columns.Count > 0)
        {
            list.Add(table.Columns);
        }
        list.AddRange(table.Rows);

        var serializer = new SerializerBuilder().Build();
        return serializer.Serialize(list).TrimEnd('\r', '\n');
    }

    public string ToKeyValueJson(TabularData table, int keyColIndex, int valueColIndex, bool includeRestOfColumns = false, bool indented = true)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = indented,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        if (includeRestOfColumns || valueColIndex < 0)
        {
            var headers = table.Columns.Select((c, idx) => string.IsNullOrWhiteSpace(c) ? $"Column_{idx + 1}" : c).ToList();
            var dict = new Dictionary<string, Dictionary<string, object?>>();

            foreach (var row in table.Rows)
            {
                string key = (keyColIndex >= 0 && keyColIndex < row.Count) ? row[keyColIndex] : string.Empty;
                if (string.IsNullOrEmpty(key) || dict.ContainsKey(key)) continue;

                var valDict = new Dictionary<string, object?>();
                for (int i = 0; i < headers.Count; i++)
                {
                    if (i == keyColIndex) continue;
                    string rawVal = i < row.Count ? row[i] : string.Empty;
                    if (long.TryParse(rawVal, out long lVal))
                        valDict[headers[i]] = lVal;
                    else if (double.TryParse(rawVal, System.Globalization.CultureInfo.InvariantCulture, out double dVal))
                        valDict[headers[i]] = dVal;
                    else if (bool.TryParse(rawVal, out bool bVal))
                        valDict[headers[i]] = bVal;
                    else
                        valDict[headers[i]] = rawVal;
                }
                dict[key] = valDict;
            }

            return JsonSerializer.Serialize(dict, options);
        }
        else
        {
            var dict = new Dictionary<string, string>();
            foreach (var row in table.Rows)
            {
                string key = (keyColIndex >= 0 && keyColIndex < row.Count) ? row[keyColIndex] : string.Empty;
                string val = (valueColIndex >= 0 && valueColIndex < row.Count) ? row[valueColIndex] : string.Empty;
                if (!string.IsNullOrEmpty(key) && !dict.ContainsKey(key))
                {
                    dict[key] = val;
                }
            }

            return JsonSerializer.Serialize(dict, options);
        }
    }

    public string ToKeyValueJson(TabularData table, int keyColIndex, bool includeRestOfColumns, bool indented = true)
        => ToKeyValueJson(table, keyColIndex, -1, includeRestOfColumns, indented);

    public string ToKeyValueYaml(TabularData table, int keyColIndex, int valueColIndex, bool includeRestOfColumns = false)
    {
        var dict = new Dictionary<string, object?>();

        if (includeRestOfColumns || valueColIndex < 0)
        {
            var headers = table.Columns.Select((c, idx) => string.IsNullOrWhiteSpace(c) ? $"Column_{idx + 1}" : c).ToList();
            foreach (var row in table.Rows)
            {
                string key = (keyColIndex >= 0 && keyColIndex < row.Count) ? row[keyColIndex] : string.Empty;
                if (string.IsNullOrEmpty(key) || dict.ContainsKey(key)) continue;

                var valDict = new Dictionary<string, object?>();
                for (int i = 0; i < headers.Count; i++)
                {
                    if (i == keyColIndex) continue;
                    string rawVal = i < row.Count ? row[i] : string.Empty;
                    if (long.TryParse(rawVal, out long lVal))
                        valDict[headers[i]] = lVal;
                    else if (double.TryParse(rawVal, System.Globalization.CultureInfo.InvariantCulture, out double dVal))
                        valDict[headers[i]] = dVal;
                    else if (bool.TryParse(rawVal, out bool bVal))
                        valDict[headers[i]] = bVal;
                    else
                        valDict[headers[i]] = rawVal;
                }
                dict[key] = valDict;
            }
        }
        else
        {
            foreach (var row in table.Rows)
            {
                string key = (keyColIndex >= 0 && keyColIndex < row.Count) ? row[keyColIndex] : string.Empty;
                string val = (valueColIndex >= 0 && valueColIndex < row.Count) ? row[valueColIndex] : string.Empty;
                if (!string.IsNullOrEmpty(key) && !dict.ContainsKey(key))
                {
                    if (long.TryParse(val, out long lVal))
                        dict[key] = lVal;
                    else if (double.TryParse(val, System.Globalization.CultureInfo.InvariantCulture, out double dVal))
                        dict[key] = dVal;
                    else if (bool.TryParse(val, out bool bVal))
                        dict[key] = bVal;
                    else
                        dict[key] = val;
                }
            }
        }

        var serializer = new SerializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .Build();

        return serializer.Serialize(dict).TrimEnd('\r', '\n');
    }

    public string ToKeyValueYaml(TabularData table, int keyColIndex, bool includeRestOfColumns)
        => ToKeyValueYaml(table, keyColIndex, -1, includeRestOfColumns);

    public string ToKeyValueQueryString(TabularData table, int keyColIndex, int valueColIndex, bool includeRestOfColumns = false)
    {
        var pairs = new List<string>();

        if (includeRestOfColumns || valueColIndex < 0)
        {
            var headers = table.Columns.Select((c, idx) => string.IsNullOrWhiteSpace(c) ? $"Column_{idx + 1}" : c).ToList();
            foreach (var row in table.Rows)
            {
                string key = (keyColIndex >= 0 && keyColIndex < row.Count) ? row[keyColIndex] : string.Empty;
                if (string.IsNullOrEmpty(key)) continue;

                for (int i = 0; i < headers.Count; i++)
                {
                    if (i == keyColIndex) continue;
                    string colName = headers[i];
                    string val = i < row.Count ? row[i] : string.Empty;
                    pairs.Add($"{Uri.EscapeDataString(key)}[{Uri.EscapeDataString(colName)}]={Uri.EscapeDataString(val)}");
                }
            }
        }
        else
        {
            foreach (var row in table.Rows)
            {
                string key = (keyColIndex >= 0 && keyColIndex < row.Count) ? row[keyColIndex] : string.Empty;
                string val = (valueColIndex >= 0 && valueColIndex < row.Count) ? row[valueColIndex] : string.Empty;
                if (!string.IsNullOrEmpty(key))
                {
                    pairs.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(val)}");
                }
            }
        }

        return string.Join("&", pairs);
    }

    public string ToKeyValueQueryString(TabularData table, int keyColIndex, bool includeRestOfColumns)
        => ToKeyValueQueryString(table, keyColIndex, -1, includeRestOfColumns);

    public string ToSqlInClause(TabularData table, int colIndex, bool quoteStrings = true)
    {
        var items = table.ExtractColumn(colIndex).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        if (items.Count == 0) return "IN ()";

        var formatted = items.Select(item =>
        {
            if (quoteStrings)
            {
                return $"'{item.Replace("'", "''")}'";
            }
            return item;
        });

        return $"IN ({string.Join(", ", formatted)})";
    }

    private static string EscapeCsvCell(string cell, char delimiter)
    {
        bool mustQuote = cell.Contains(delimiter) || cell.Contains('"') || cell.Contains('\n') || cell.Contains('\r');
        if (mustQuote)
        {
            return $"\"{cell.Replace("\"", "\"\"")}\"";
        }
        return cell;
    }

    private static string FormatSqlValue(string val)
    {
        if (string.IsNullOrEmpty(val)) return "NULL";
        if (long.TryParse(val, out _) || double.TryParse(val, System.Globalization.CultureInfo.InvariantCulture, out _))
        {
            return val;
        }
        return $"'{val.Replace("'", "''")}'";
    }
}
