using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TextForge.Core.Transformers;

public enum QuoteStyle
{
    None,
    SingleQuotes,      // 'item'
    DoubleQuotes,      // "item"
    Backticks,         // `item`
    SquareBrackets,    // [item]
    Parentheses,       // (item)
    CurlyBraces,       // {item}
    Custom             // prefix + item + suffix
}

public enum SortOrder
{
    AlphabeticalAsc,
    AlphabeticalDesc,
    CaseInsensitiveAsc,
    CaseInsensitiveDesc,
    NaturalNumericAsc,
    NaturalNumericDesc,
    LengthAsc,
    LengthDesc,
    Reverse
}

public enum DeduplicateMode
{
    Distinct,
    DuplicatesOnly,
    CountOccurrences
}

public static class LineTransformers
{
    public static ILineTransformer Instance { get; set; } = LineTransformerService.Instance;

    public static string QuoteLines(
        string? text,
        QuoteStyle style = QuoteStyle.SingleQuotes,
        string customPrefix = "",
        string customSuffix = "",
        bool skipEmpty = true,
        bool escapeInnerQuotes = true)
        => Instance.QuoteLines(text, style, customPrefix, customSuffix, skipEmpty, escapeInnerQuotes);

    public static string JoinLines(
        string? text,
        string delimiter = ", ",
        QuoteStyle itemQuote = QuoteStyle.None,
        string itemPrefix = "",
        string itemSuffix = "",
        string overallPrefix = "",
        string overallSuffix = "",
        bool skipEmpty = true)
        => Instance.JoinLines(text, delimiter, itemQuote, itemPrefix, itemSuffix, overallPrefix, overallSuffix, skipEmpty);

    public static string SplitLine(
        string? text,
        string? delimiter = null,
        bool isRegex = false,
        bool trimItems = true,
        bool removeEmpty = true)
        => Instance.SplitLine(text, delimiter, isRegex, trimItems, removeEmpty);

    public static string TrimLines(
        string? text,
        bool trimStart = true,
        bool trimEnd = true,
        bool removeEmptyLines = true,
        bool collapseWhitespace = false)
        => Instance.TrimLines(text, trimStart, trimEnd, removeEmptyLines, collapseWhitespace);

    public static string SortLines(string? text, SortOrder order = SortOrder.AlphabeticalAsc)
        => Instance.SortLines(text, order);

    public static string DeduplicateLines(
        string? text,
        DeduplicateMode mode = DeduplicateMode.Distinct,
        bool caseSensitive = false)
        => Instance.DeduplicateLines(text, mode, caseSensitive);

    public static string FilterLines(
        string? text,
        string filterQuery,
        bool isRegex = false,
        bool keepMatching = true,
        bool caseSensitive = false)
        => Instance.FilterLines(text, filterQuery, isRegex, keepMatching, caseSensitive);

    public static string ReplaceInLines(
        string? text,
        string find,
        string replaceWith = "",
        bool isRegex = false,
        bool caseSensitive = false,
        bool skipEmpty = false)
        => Instance.ReplaceInLines(text, find, replaceWith, isRegex, caseSensitive, skipEmpty);

    public static string AddPrefixSuffix(
        string? text,
        string prefix = "",
        string suffix = "",
        bool skipEmpty = true,
        bool prefixSkipFirst = false,
        bool prefixSkipLast = false,
        bool suffixSkipFirst = false,
        bool suffixSkipLast = false)
        => Instance.AddPrefixSuffix(text, prefix, suffix, skipEmpty, prefixSkipFirst, prefixSkipLast, suffixSkipFirst, suffixSkipLast);

    public static string NumberLines(string? text, string format = "{n}. ", int startNumber = 1, bool skipEmpty = true)
        => Instance.NumberLines(text, format, startNumber, skipEmpty);

    public static string ExtractRegex(string? text, string regexPattern, int captureGroup = 0)
        => Instance.ExtractRegex(text, regexPattern, captureGroup);
}

public class NaturalStringComparer : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        int ix = 0, iy = 0;
        while (ix < x.Length && iy < y.Length)
        {
            if (char.IsDigit(x[ix]) && char.IsDigit(y[iy]))
            {
                int startX = ix;
                while (ix < x.Length && char.IsDigit(x[ix])) ix++;
                string numStrX = x.Substring(startX, ix - startX);

                int startY = iy;
                while (iy < y.Length && char.IsDigit(y[iy])) iy++;
                string numStrY = y.Substring(startY, iy - startY);

                if (long.TryParse(numStrX, out long numX) && long.TryParse(numStrY, out long numY))
                {
                    int numComp = numX.CompareTo(numY);
                    if (numComp != 0) return numComp;
                }
                else
                {
                    int strComp = string.Compare(numStrX, numStrY, StringComparison.Ordinal);
                    if (strComp != 0) return strComp;
                }
            }
            else
            {
                int chComp = char.ToUpperInvariant(x[ix]).CompareTo(char.ToUpperInvariant(y[iy]));
                if (chComp != 0) return chComp;
                ix++;
                iy++;
            }
        }

        return x.Length.CompareTo(y.Length);
    }
}
