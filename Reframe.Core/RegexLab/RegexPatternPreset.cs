using System.Text.RegularExpressions;

namespace Reframe.Core.RegexLab;

public class RegexPatternPreset
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Pattern { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string SampleText { get; init; } = string.Empty;
    public RegexOptions DefaultOptions { get; init; } = RegexOptions.None;
    public string Icon { get; init; } = "🧪";

    public override string ToString() => $"{Name} ({Category})";
}
