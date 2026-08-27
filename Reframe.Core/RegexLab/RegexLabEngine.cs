using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Reframe.Core.RegexLab;

public class RegexLabEngine
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);

    public RegexLabResult Evaluate(string input, string pattern, RegexOptions options = RegexOptions.None, TimeSpan? timeout = null)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return RegexLabResult.Empty;
        }

        var sw = Stopwatch.StartNew();

        try
        {
            var matchTimeout = timeout ?? DefaultTimeout;
            var regex = new Regex(pattern, options, matchTimeout);

            string inputText = input ?? string.Empty;
            var matchCollection = regex.Matches(inputText);

            string[] groupNames = regex.GetGroupNames();
            int[] groupNumbers = regex.GetGroupNumbers();

            var matches = new List<RegexMatchItem>(matchCollection.Count);
            int matchIndex = 1;
            int totalGroups = 0;

            foreach (Match match in matchCollection)
            {
                var groupItems = new List<RegexGroupItem>(groupNames.Length);

                for (int i = 0; i < groupNames.Length; i++)
                {
                    string gName = groupNames[i];
                    int gNumber = groupNumbers[i];
                    var group = match.Groups[gName];

                    bool isNamed = !int.TryParse(gName, out _);
                    string displayName = isNamed 
                        ? $"${gName} (Grp {gNumber})" 
                        : (gNumber == 0 ? "$0 (Full Match)" : $"Group {gNumber}");

                    groupItems.Add(new RegexGroupItem
                    {
                        GroupIndex = gNumber,
                        GroupName = gName,
                        DisplayName = displayName,
                        Index = group.Index,
                        Length = group.Length,
                        Value = group.Value,
                        Success = group.Success,
                        IsNamed = isNamed
                    });

                    if (group.Success && gNumber > 0)
                    {
                        totalGroups++;
                    }
                }

                matches.Add(new RegexMatchItem
                {
                    MatchNumber = matchIndex++,
                    Index = match.Index,
                    Length = match.Length,
                    Value = match.Value,
                    Groups = groupItems
                });
            }

            var groupHeaders = new List<string> { "Match #", "Index", "Length", "Full Match" };
            // Add groups beyond group 0
            for (int i = 0; i < groupNames.Length; i++)
            {
                if (groupNumbers[i] == 0) continue;
                string gName = groupNames[i];
                bool isNamed = !int.TryParse(gName, out _);
                string colName = isNamed ? $"${gName}" : $"Group {groupNumbers[i]}";
                if (!groupHeaders.Contains(colName))
                {
                    groupHeaders.Add(colName);
                }
            }

            DataTable groupTable = BuildGroupTable(matches, groupNames, groupNumbers, groupHeaders);

            sw.Stop();

            return new RegexLabResult
            {
                IsValid = true,
                Pattern = pattern,
                TotalGroups = totalGroups,
                ExecutionTimeMs = sw.Elapsed.TotalMilliseconds,
                Matches = matches,
                GroupHeaders = groupHeaders,
                GroupTable = groupTable
            };
        }
        catch (RegexParseException ex)
        {
            sw.Stop();
            return RegexLabResult.Error(pattern, $"Pattern syntax error: {ex.Message}");
        }
        catch (RegexMatchTimeoutException)
        {
            sw.Stop();
            return RegexLabResult.Error(pattern, "Evaluation timed out (ReDoS protection limit reached).");
        }
        catch (ArgumentException ex)
        {
            sw.Stop();
            return RegexLabResult.Error(pattern, $"Invalid regular expression: {ex.Message}");
        }
        catch (Exception ex)
        {
            sw.Stop();
            return RegexLabResult.Error(pattern, $"Error: {ex.Message}");
        }
    }

    private static DataTable BuildGroupTable(
        List<RegexMatchItem> matches,
        string[] groupNames,
        int[] groupNumbers,
        List<string> groupHeaders)
    {
        var table = new DataTable("RegexMatchGroups");

        table.Columns.Add("Match #", typeof(int));
        table.Columns.Add("Index", typeof(int));
        table.Columns.Add("Length", typeof(int));
        table.Columns.Add("Full Match", typeof(string));

        for (int i = 0; i < groupNames.Length; i++)
        {
            if (groupNumbers[i] == 0) continue;
            string gName = groupNames[i];
            bool isNamed = !int.TryParse(gName, out _);
            string colName = isNamed ? $"${gName}" : $"Group {groupNumbers[i]}";
            if (!table.Columns.Contains(colName))
            {
                table.Columns.Add(colName, typeof(string));
            }
        }

        foreach (var match in matches)
        {
            var row = table.NewRow();
            row["Match #"] = match.MatchNumber;
            row["Index"] = match.Index;
            row["Length"] = match.Length;
            row["Full Match"] = match.Value;

            for (int i = 0; i < groupNames.Length; i++)
            {
                if (groupNumbers[i] == 0) continue;
                string gName = groupNames[i];
                bool isNamed = !int.TryParse(gName, out _);
                string colName = isNamed ? $"${gName}" : $"Group {groupNumbers[i]}";

                var grp = match.Groups.FirstOrDefault(g => g.GroupName == gName);
                row[colName] = grp != null && grp.Success ? grp.Value : string.Empty;
            }

            table.Rows.Add(row);
        }

        return table;
    }

    public string ExtractMatches(RegexLabResult result, string separator = "\n")
    {
        if (result == null || result.Matches.Count == 0) return string.Empty;
        return string.Join(separator, result.Matches.Select(m => m.Value));
    }

    public string ExtractGroupsAsDelimited(RegexLabResult result, string delimiter = "\t", bool includeHeaders = true)
    {
        if (result == null || result.GroupTable == null || result.GroupTable.Rows.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        var table = result.GroupTable;

        if (includeHeaders)
        {
            for (int c = 0; c < table.Columns.Count; c++)
            {
                if (c > 0) sb.Append(delimiter);
                sb.Append(EscapeDelimitedField(table.Columns[c].ColumnName, delimiter));
            }
            sb.AppendLine();
        }

        foreach (DataRow row in table.Rows)
        {
            for (int c = 0; c < table.Columns.Count; c++)
            {
                if (c > 0) sb.Append(delimiter);
                string val = row[c]?.ToString() ?? string.Empty;
                sb.Append(EscapeDelimitedField(val, delimiter));
            }
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd('\r', '\n');
    }

    public string ExtractGroupsAsJson(RegexLabResult result, bool indented = true)
    {
        if (result == null || result.Matches.Count == 0)
        {
            return "[]";
        }

        var list = new List<Dictionary<string, object>>();

        foreach (var match in result.Matches)
        {
            var dict = new Dictionary<string, object>
            {
                ["matchNumber"] = match.MatchNumber,
                ["index"] = match.Index,
                ["length"] = match.Length,
                ["match"] = match.Value
            };

            var groupsDict = new Dictionary<string, string>();
            foreach (var grp in match.Groups)
            {
                if (grp.GroupIndex == 0) continue;
                groupsDict[grp.GroupName] = grp.Success ? grp.Value : string.Empty;
            }

            dict["groups"] = groupsDict;
            list.Add(dict);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = indented
        };

        return JsonSerializer.Serialize(list, options);
    }

    public string Replace(string input, string pattern, string replacement, RegexOptions options = RegexOptions.None, TimeSpan? timeout = null)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(pattern))
        {
            return input ?? string.Empty;
        }

        var matchTimeout = timeout ?? DefaultTimeout;
        return Regex.Replace(input, pattern, replacement ?? string.Empty, options, matchTimeout);
    }

    private static string EscapeDelimitedField(string field, string delimiter)
    {
        if (string.IsNullOrEmpty(field)) return string.Empty;
        if (field.Contains(delimiter) || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }
}
