using System.Collections.Generic;

namespace Reframe.Core.Actions;

public class FuzzyMatchResult<T>
{
    public T Item { get; }
    public int Score { get; }
    public IReadOnlyList<(int Start, int Length)> MatchedRanges { get; }

    public FuzzyMatchResult(T item, int score, IReadOnlyList<(int Start, int Length)>? matchedRanges = null)
    {
        Item = item;
        Score = score;
        MatchedRanges = matchedRanges ?? [];
    }
}
