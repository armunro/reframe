using System;
using System.Collections.Generic;
using System.Linq;

namespace Reframe.Core.RegexLab;

public class RegexMatchItem
{
    public int MatchNumber { get; init; }
    public int Index { get; init; }
    public int Length { get; init; }
    public string Value { get; init; } = string.Empty;
    public IReadOnlyList<RegexGroupItem> Groups { get; init; } = Array.Empty<RegexGroupItem>();

    public RegexGroupItem? GetGroup(string nameOrIndex)
    {
        return Groups.FirstOrDefault(g => 
            string.Equals(g.GroupName, nameOrIndex, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(g.DisplayName, nameOrIndex, StringComparison.OrdinalIgnoreCase) ||
            (int.TryParse(nameOrIndex, out int idx) && g.GroupIndex == idx));
    }

    public override string ToString() => $"Match #{MatchNumber} [{Index}..{Index + Length}]: {Value}";
}
