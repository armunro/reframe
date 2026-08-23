using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Reframe.Core.Analysis.Analyzers;

namespace Reframe.Core.Transformers.Line;

public class LineTransformerService : ILineTransformer
{
    public static LineTransformerService Instance { get; } = new();

    public string QuoteLines(
        string? text,
        QuoteStyle style = QuoteStyle.SingleQuotes,
        string customPrefix = "",
        string customSuffix = "",
        bool skipEmpty = true,
        bool escapeInnerQuotes = true)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var (prefix, suffix) = GetQuotePrefixSuffix(style, customPrefix, customSuffix);
        var lines = SplitIntoLines(text);
        var sb = new StringBuilder();

        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];
            if (skipEmpty && string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string processed = line;
            if (escapeInnerQuotes)
            {
                if (style == QuoteStyle.SingleQuotes)
                    processed = processed.Replace("'", "''");
                else if (style == QuoteStyle.DoubleQuotes)
                    processed = processed.Replace("\"", "\\\"");
                else if (style == QuoteStyle.Backticks)
                    processed = processed.Replace("`", "\\`");
            }

            sb.AppendLine($"{prefix}{processed}{suffix}");
        }

        return sb.ToString().TrimEnd('\r', '\n');
    }

    public string JoinLines(
        string? text,
        string delimiter = ", ",
        QuoteStyle itemQuote = QuoteStyle.None,
        string itemPrefix = "",
        string itemSuffix = "",
        string overallPrefix = "",
        string overallSuffix = "",
        bool skipEmpty = true)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var (quotePre, quoteSuf) = GetQuotePrefixSuffix(itemQuote, itemPrefix, itemSuffix);
        var lines = SplitIntoLines(text);
        var items = new List<string>();

        foreach (var line in lines)
        {
            string trimmed = line.Trim();
            if (skipEmpty && string.IsNullOrWhiteSpace(trimmed)) continue;

            items.Add($"{quotePre}{trimmed}{quoteSuf}");
        }

        string joined = string.Join(delimiter, items);
        return $"{overallPrefix}{joined}{overallSuffix}";
    }

    public string SplitLine(
        string? text,
        string? delimiter = null,
        bool isRegex = false,
        bool trimItems = true,
        bool removeEmpty = true)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        string[] items;
        if (string.IsNullOrEmpty(delimiter))
        {
            // Auto-detect delimiter
            var d = TextAnalyzer.Analyze(text).DetectedDelimiter;
            if (d.HasValue)
            {
                items = text.Split(d.Value);
            }
            else
            {
                // Fallback to comma or whitespace
                items = Regex.Split(text, @"[,\t;]+");
            }
        }
        else if (isRegex)
        {
            items = Regex.Split(text, delimiter);
        }
        else
        {
            items = text.Split(new[] { delimiter }, StringSplitOptions.None);
        }

        var result = new List<string>();
        foreach (var item in items)
        {
            string val = trimItems ? item.Trim() : item;
            if (removeEmpty && string.IsNullOrWhiteSpace(val)) continue;
            result.Add(val);
        }

        return string.Join(Environment.NewLine, result);
    }

    public string TrimLines(
        string? text,
        bool trimStart = true,
        bool trimEnd = true,
        bool removeEmptyLines = true,
        bool collapseWhitespace = false)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var lines = SplitIntoLines(text);
        var result = new List<string>();

        foreach (var line in lines)
        {
            string processed = line;
            if (collapseWhitespace)
            {
                processed = Regex.Replace(processed, @"\s+", " ");
            }
            if (trimStart && trimEnd)
            {
                processed = processed.Trim();
            }
            else if (trimStart)
            {
                processed = processed.TrimStart();
            }
            else if (trimEnd)
            {
                processed = processed.TrimEnd();
            }

            if (removeEmptyLines && string.IsNullOrWhiteSpace(processed))
            {
                continue;
            }

            result.Add(processed);
        }

        return string.Join(Environment.NewLine, result);
    }

    public string SortLines(string? text, SortOrder order = SortOrder.AlphabeticalAsc)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var lines = SplitIntoLines(text);

        IEnumerable<string> sorted = order switch
        {
            SortOrder.AlphabeticalAsc => lines.OrderBy(l => l, StringComparer.Ordinal),
            SortOrder.AlphabeticalDesc => lines.OrderByDescending(l => l, StringComparer.Ordinal),
            SortOrder.CaseInsensitiveAsc => lines.OrderBy(l => l, StringComparer.OrdinalIgnoreCase),
            SortOrder.CaseInsensitiveDesc => lines.OrderByDescending(l => l, StringComparer.OrdinalIgnoreCase),
            SortOrder.NaturalNumericAsc => lines.OrderBy(l => l, new NaturalStringComparer()),
            SortOrder.NaturalNumericDesc => lines.OrderByDescending(l => l, new NaturalStringComparer()),
            SortOrder.LengthAsc => lines.OrderBy(l => l.Length).ThenBy(l => l, StringComparer.OrdinalIgnoreCase),
            SortOrder.LengthDesc => lines.OrderByDescending(l => l.Length).ThenBy(l => l, StringComparer.OrdinalIgnoreCase),
            SortOrder.Reverse => lines.AsEnumerable().Reverse(),
            _ => lines
        };

        return string.Join(Environment.NewLine, sorted);
    }

    public static List<string> SplitIntoLines(string text)
    {
        return text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
    }

    public string DeduplicateLines(
        string? text,
        DeduplicateMode mode = DeduplicateMode.Distinct,
        bool caseSensitive = false)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var lines = SplitIntoLines(text);
        var comparer = caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

        if (mode == DeduplicateMode.Distinct)
        {
            return string.Join(Environment.NewLine, lines.Distinct(comparer));
        }

        var groups = lines.GroupBy(l => l, comparer).ToList();

        if (mode == DeduplicateMode.DuplicatesOnly)
        {
            var duplicates = groups.Where(g => g.Count() > 1).Select(g => g.Key);
            return string.Join(Environment.NewLine, duplicates);
        }

        if (mode == DeduplicateMode.CountOccurrences)
        {
            var counts = groups.OrderByDescending(g => g.Count())
                               .Select(g => $"{g.Count(),4}x  {g.Key}");
            return string.Join(Environment.NewLine, counts);
        }

        return text;
    }

    public string FilterLines(
        string? text,
        string filterQuery,
        bool isRegex = false,
        bool keepMatching = true,
        bool caseSensitive = false)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        if (string.IsNullOrEmpty(filterQuery)) return text;

        var lines = SplitIntoLines(text);
        Regex? regex = null;
        if (isRegex)
        {
            var options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            regex = new Regex(filterQuery, options);
        }

        var filtered = lines.Where(line =>
        {
            bool matches;
            if (isRegex && regex != null)
            {
                matches = regex.IsMatch(line);
            }
            else
            {
                var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                matches = line.Contains(filterQuery, comparison);
            }

            return keepMatching ? matches : !matches;
        });

        return string.Join(Environment.NewLine, filtered);
    }

    public string ReplaceInLines(
        string? text,
        string find,
        string replaceWith = "",
        bool isRegex = false,
        bool caseSensitive = false,
        bool skipEmpty = false)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        if (string.IsNullOrEmpty(find)) return text;

        var lines = SplitIntoLines(text);
        var result = new List<string>();

        Regex? regex = null;
        if (isRegex)
        {
            var options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            regex = new Regex(find, options);
        }

        string replacement = replaceWith ?? string.Empty;

        foreach (var line in lines)
        {
            if (skipEmpty && string.IsNullOrWhiteSpace(line))
            {
                result.Add(line);
                continue;
            }

            if (isRegex && regex != null)
            {
                result.Add(regex.Replace(line, replacement));
            }
            else
            {
                var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                result.Add(line.Replace(find, replacement, comparison));
            }
        }

        return string.Join(Environment.NewLine, result);
    }

    public string AddPrefixSuffix(
        string? text,
        string prefix = "",
        string suffix = "",
        bool skipEmpty = true,
        bool prefixSkipFirst = false,
        bool prefixSkipLast = false,
        bool suffixSkipFirst = false,
        bool suffixSkipLast = false)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var lines = SplitIntoLines(text);
        var result = new List<string>();

        int firstEligibleIndex = -1;
        int lastEligibleIndex = -1;

        if (skipEmpty)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                {
                    if (firstEligibleIndex == -1) firstEligibleIndex = i;
                    lastEligibleIndex = i;
                }
            }
        }
        else
        {
            if (lines.Count > 0)
            {
                firstEligibleIndex = 0;
                lastEligibleIndex = lines.Count - 1;
            }
        }

        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];
            if (skipEmpty && string.IsNullOrWhiteSpace(line))
            {
                result.Add(line);
            }
            else
            {
                bool isFirst = (i == firstEligibleIndex);
                bool isLast = (i == lastEligibleIndex);

                string curPrefix = (prefixSkipFirst && isFirst) || (prefixSkipLast && isLast) ? string.Empty : prefix;
                string curSuffix = (suffixSkipFirst && isFirst) || (suffixSkipLast && isLast) ? string.Empty : suffix;

                result.Add($"{curPrefix}{line}{curSuffix}");
            }
        }

        return string.Join(Environment.NewLine, result);
    }

    public string NumberLines(string? text, string format = "{n}. ", int startNumber = 1, bool skipEmpty = true)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var lines = SplitIntoLines(text);
        var result = new List<string>();
        int current = startNumber;

        foreach (var line in lines)
        {
            if (skipEmpty && string.IsNullOrWhiteSpace(line))
            {
                result.Add(line);
            }
            else
            {
                string numPrefix = format
                    .Replace("{n}", current.ToString(CultureInfo.InvariantCulture))
                    .Replace("{0n}", current.ToString("D2", CultureInfo.InvariantCulture))
                    .Replace("{00n}", current.ToString("D3", CultureInfo.InvariantCulture));

                result.Add($"{numPrefix}{line}");
                current++;
            }
        }

        return string.Join(Environment.NewLine, result);
    }

    public string ExtractRegex(string? text, string regexPattern, int captureGroup = 0)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(regexPattern)) return string.Empty;

        var regex = new Regex(regexPattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
        var matches = regex.Matches(text);
        var results = new List<string>();

        foreach (Match match in matches)
        {
            if (captureGroup >= 0 && captureGroup < match.Groups.Count)
            {
                results.Add(match.Groups[captureGroup].Value);
            }
            else
            {
                results.Add(match.Value);
            }
        }

        return string.Join(Environment.NewLine, results);
    }

    private static (string Prefix, string Suffix) GetQuotePrefixSuffix(QuoteStyle style, string customPrefix, string customSuffix)
    {
        return style switch
        {
            QuoteStyle.SingleQuotes => ("'", "'"),
            QuoteStyle.DoubleQuotes => ("\"", "\""),
            QuoteStyle.Backticks => ("`", "`"),
            QuoteStyle.SquareBrackets => ("[", "]"),
            QuoteStyle.Parentheses => ("(", ")"),
            QuoteStyle.CurlyBraces => ("{", "}"),
            QuoteStyle.Custom => (customPrefix, customSuffix),
            _ => (string.Empty, string.Empty)
        };
    }
}
