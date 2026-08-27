namespace Reframe.Core.Scripting;

/// <summary>
/// Preset C# / LINQ expression or script for quick transformation workflows.
/// </summary>
public record ScriptPreset
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string Category { get; init; }
    public required string Script { get; init; }
    public string? SampleInput { get; init; }
}
