using System;
using System.Collections.Generic;
using System.Data;

namespace Reframe.Core.RegexLab;

public class RegexLabResult
{
    public bool IsValid { get; init; } = true;
    public string? ErrorMessage { get; init; }
    public string Pattern { get; init; } = string.Empty;
    public int TotalMatches => Matches.Count;
    public int TotalGroups { get; init; }
    public double ExecutionTimeMs { get; init; }
    public IReadOnlyList<RegexMatchItem> Matches { get; init; } = Array.Empty<RegexMatchItem>();
    public IReadOnlyList<string> GroupHeaders { get; init; } = Array.Empty<string>();
    public DataTable? GroupTable { get; init; }

    public static RegexLabResult Empty => new()
    {
        IsValid = true,
        Matches = Array.Empty<RegexMatchItem>(),
        GroupHeaders = Array.Empty<string>()
    };

    public static RegexLabResult Error(string pattern, string error) => new()
    {
        IsValid = false,
        Pattern = pattern,
        ErrorMessage = error,
        Matches = Array.Empty<RegexMatchItem>(),
        GroupHeaders = Array.Empty<string>()
    };
}
