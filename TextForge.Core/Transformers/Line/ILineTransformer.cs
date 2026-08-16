namespace TextForge.Core.Transformers;

public interface ILineTransformer
{
    string QuoteLines(string? text, QuoteStyle style = QuoteStyle.SingleQuotes, string customPrefix = "", string customSuffix = "", bool skipEmpty = true, bool escapeInnerQuotes = true);
    string JoinLines(string? text, string delimiter = ", ", QuoteStyle itemQuote = QuoteStyle.None, string itemPrefix = "", string itemSuffix = "", string overallPrefix = "", string overallSuffix = "", bool skipEmpty = true);
    string SplitLine(string? text, string? delimiter = null, bool isRegex = false, bool trimItems = true, bool removeEmpty = true);
    string TrimLines(string? text, bool trimStart = true, bool trimEnd = true, bool removeEmptyLines = true, bool collapseWhitespace = false);
    string SortLines(string? text, SortOrder order = SortOrder.AlphabeticalAsc);
    string DeduplicateLines(string? text, DeduplicateMode mode = DeduplicateMode.Distinct, bool caseSensitive = false);
    string FilterLines(string? text, string filterQuery, bool isRegex = false, bool keepMatching = true, bool caseSensitive = false);
    string ReplaceInLines(string? text, string find, string replaceWith = "", bool isRegex = false, bool caseSensitive = false, bool skipEmpty = false);
    string AddPrefixSuffix(string? text, string prefix = "", string suffix = "", bool skipEmpty = true, bool prefixSkipFirst = false, bool prefixSkipLast = false, bool suffixSkipFirst = false, bool suffixSkipLast = false);
    string NumberLines(string? text, string format = "{n}. ", int startNumber = 1, bool skipEmpty = true);
    string ExtractRegex(string? text, string regexPattern, int captureGroup = 0);
}
