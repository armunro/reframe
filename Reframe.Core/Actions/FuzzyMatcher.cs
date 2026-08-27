using System;
using System.Collections.Generic;
using System.Linq;

namespace Reframe.Core.Actions;

public static class FuzzyMatcher
{
    public static IReadOnlyList<FuzzyMatchResult<ActionItem>> MatchActions(IEnumerable<ActionItem> items, string? query)
    {
        if (items == null) return [];

        if (string.IsNullOrWhiteSpace(query))
        {
            return items.Select(item => new FuzzyMatchResult<ActionItem>(item, 0)).ToList();
        }

        string cleanQuery = query.Trim();
        var results = new List<FuzzyMatchResult<ActionItem>>();

        foreach (var item in items)
        {
            var match = EvaluateActionMatch(item, cleanQuery);
            if (match.IsMatch)
            {
                results.Add(new FuzzyMatchResult<ActionItem>(item, match.Score, match.MatchedRanges));
            }
        }

        return results
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.Item.Title.Length)
            .ThenBy(r => r.Item.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static (bool IsMatch, int Score, IReadOnlyList<(int Start, int Length)> MatchedRanges) EvaluateActionMatch(ActionItem item, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return (true, 0, []);
        }

        int highestScore = 0;
        bool isMatch = false;
        List<(int Start, int Length)> titleRanges = [];

        // 1. Check Title match
        var (titleMatch, titleScore, ranges) = MatchString(item.Title, query);
        if (titleMatch)
        {
            isMatch = true;
            // High weight for title matches
            highestScore = Math.Max(highestScore, titleScore + 100);
            titleRanges = ranges;
        }

        // 2. Check Id match (e.g. CamelCase, ToJsonObjects, SqlIn)
        var (idMatch, idScore, _) = MatchString(item.Id, query);
        if (idMatch)
        {
            isMatch = true;
            highestScore = Math.Max(highestScore, idScore + 80);
        }

        // 3. Check Category match (e.g. "Tabular", "Structured", "Lines")
        var (catMatch, catScore, _) = MatchString(item.Category, query);
        if (catMatch)
        {
            isMatch = true;
            highestScore = Math.Max(highestScore, catScore + 40);
        }

        // 4. Check Keywords match (e.g. "pretty", "indent", "ts", "poco", "base64", "b64")
        if (item.Keywords != null)
        {
            foreach (var kw in item.Keywords)
            {
                if (string.IsNullOrWhiteSpace(kw)) continue;

                if (string.Equals(kw, query, StringComparison.OrdinalIgnoreCase))
                {
                    isMatch = true;
                    highestScore = Math.Max(highestScore, 180);
                }
                else if (kw.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                {
                    isMatch = true;
                    highestScore = Math.Max(highestScore, 140);
                }
                else
                {
                    var (kwMatch, kwScore, _) = MatchString(kw, query);
                    if (kwMatch)
                    {
                        isMatch = true;
                        highestScore = Math.Max(highestScore, kwScore + 60);
                    }
                }
            }
        }

        // 5. Check Description match
        if (!string.IsNullOrWhiteSpace(item.Description))
        {
            var (descMatch, descScore, _) = MatchString(item.Description, query);
            if (descMatch)
            {
                isMatch = true;
                highestScore = Math.Max(highestScore, descScore + 20);
            }
        }

        // Multi-word query matching (all query words present in title/category/keywords/description)
        var queryWords = query.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        if (queryWords.Length > 1)
        {
            bool allWordsMatched = true;
            int multiWordBonus = 0;

            string fullTarget = $"{item.Title} {item.Category} {(item.Keywords != null ? string.Join(" ", item.Keywords) : string.Empty)} {item.Description}";
            foreach (var word in queryWords)
            {
                if (fullTarget.Contains(word, StringComparison.OrdinalIgnoreCase))
                {
                    multiWordBonus += 30;
                }
                else
                {
                    allWordsMatched = false;
                    break;
                }
            }

            if (allWordsMatched)
            {
                isMatch = true;
                highestScore = Math.Max(highestScore, multiWordBonus + 80);
            }
        }

        return (isMatch, highestScore, titleRanges);
    }

    public static (bool IsMatch, int Score, List<(int Start, int Length)> MatchedRanges) MatchString(string target, string query)
    {
        if (string.IsNullOrEmpty(target) || string.IsNullOrEmpty(query))
        {
            return (false, 0, []);
        }

        // Exact match
        if (string.Equals(target, query, StringComparison.OrdinalIgnoreCase))
        {
            return (true, 200, [(0, target.Length)]);
        }

        // Prefix match
        if (target.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            int score = 150 + (target.Length == query.Length ? 50 : 0);
            return (true, score, [(0, query.Length)]);
        }

        // Substring match
        int subIdx = target.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (subIdx >= 0)
        {
            int score = 100;
            // Bonus if substring is at a word boundary
            if (subIdx == 0 || !char.IsLetterOrDigit(target[subIdx - 1]))
            {
                score += 30;
            }
            return (true, score, [(subIdx, query.Length)]);
        }

        // Acronym / Word Initial match (e.g. "tc" for "Title Case" or "To Csv", "jto" for "Json To Objects")
        var words = target.Split(new[] { ' ', '_', '-', '.', '/', '➔', '→', ':' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 1)
        {
            string acronym = new string(words.Where(w => w.Length > 0).Select(w => char.ToLowerInvariant(w[0])).ToArray());
            string lowerQuery = query.ToLowerInvariant();
            if (acronym.StartsWith(lowerQuery, StringComparison.OrdinalIgnoreCase))
            {
                return (true, 120, [(0, 1)]);
            }
        }

        // Sequential fuzzy subsequence matching
        int targetIdx = 0;
        int queryIdx = 0;
        int scoreSubseq = 50;
        int consecutiveCount = 0;
        int prevMatchedTargetIdx = -2;
        var ranges = new List<(int Start, int Length)>();

        while (targetIdx < target.Length && queryIdx < query.Length)
        {
            char tc = char.ToLowerInvariant(target[targetIdx]);
            char qc = char.ToLowerInvariant(query[queryIdx]);

            if (tc == qc)
            {
                // Word start bonus
                bool isWordStart = targetIdx == 0 || !char.IsLetterOrDigit(target[targetIdx - 1]);
                if (isWordStart)
                {
                    scoreSubseq += 15;
                }

                // Consecutive match bonus
                if (targetIdx == prevMatchedTargetIdx + 1)
                {
                    consecutiveCount++;
                    scoreSubseq += consecutiveCount * 5;
                }
                else
                {
                    // Gap penalty
                    if (prevMatchedTargetIdx >= 0)
                    {
                        int gap = targetIdx - prevMatchedTargetIdx - 1;
                        scoreSubseq -= Math.Min(gap * 2, 20);
                    }
                    consecutiveCount = 0;
                }

                ranges.Add((targetIdx, 1));
                prevMatchedTargetIdx = targetIdx;
                queryIdx++;
            }

            targetIdx++;
        }

        if (queryIdx == query.Length)
        {
            // Merged adjacent ranges
            var merged = MergeAdjacentRanges(ranges);
            return (true, Math.Max(scoreSubseq, 10), merged);
        }

        return (false, 0, []);
    }

    private static List<(int Start, int Length)> MergeAdjacentRanges(List<(int Start, int Length)> ranges)
    {
        if (ranges.Count <= 1) return ranges;

        var result = new List<(int Start, int Length)>();
        var current = ranges[0];

        for (int i = 1; i < ranges.Count; i++)
        {
            var next = ranges[i];
            if (current.Start + current.Length == next.Start)
            {
                current = (current.Start, current.Length + next.Length);
            }
            else
            {
                result.Add(current);
                current = next;
            }
        }
        result.Add(current);
        return result;
    }
}
