using System;
using System.Collections.Generic;
using System.Linq;

namespace Reframe.Core.Actions;

public class ActionItem
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Category { get; init; } = "General";
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<string> Keywords { get; init; } = Array.Empty<string>();
    public string Icon { get; init; } = "⚡";
    public string? Shortcut { get; init; }
    public int? TargetSidebarTab { get; init; }

    public ActionItem()
    {
    }

    public ActionItem(
        string id,
        string title,
        string category,
        string description = "",
        IEnumerable<string>? keywords = null,
        string icon = "⚡",
        string? shortcut = null,
        int? targetSidebarTab = null)
    {
        Id = id;
        Title = title;
        Category = category;
        Description = description;
        Keywords = keywords != null ? keywords.ToArray() : Array.Empty<string>();
        Icon = icon;
        Shortcut = shortcut;
        TargetSidebarTab = targetSidebarTab;
    }

    public override string ToString() => $"{Title} [{Category}]";
}
