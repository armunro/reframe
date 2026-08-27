using System;

namespace Reframe.Core.RegexLab;

public class RegexGroupItem
{
    public int GroupIndex { get; init; }
    public string GroupName { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public int Index { get; init; }
    public int Length { get; init; }
    public string Value { get; init; } = string.Empty;
    public bool Success { get; init; }
    public bool IsNamed { get; init; }

    public override string ToString() => $"Group {GroupName} [{Index}..{Index + Length}]: {Value}";
}
