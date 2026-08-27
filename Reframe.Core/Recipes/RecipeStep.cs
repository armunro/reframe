using System;
using System.Collections.Generic;

namespace Reframe.Core.Recipes;

public class RecipeStep
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ActionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "⚡";
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, string> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public RecipeStep()
    {
    }

    public RecipeStep(
        string actionId,
        string title,
        string category = "General",
        string description = "",
        string icon = "⚡",
        Dictionary<string, string>? parameters = null,
        bool isEnabled = true)
    {
        Id = Guid.NewGuid().ToString("N");
        ActionId = actionId;
        Title = title;
        Category = category;
        Description = description;
        Icon = icon;
        IsEnabled = isEnabled;
        Parameters = parameters != null
            ? new Dictionary<string, string>(parameters, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public RecipeStep Clone()
    {
        return new RecipeStep
        {
            Id = Guid.NewGuid().ToString("N"),
            ActionId = ActionId,
            Title = Title,
            Category = Category,
            Description = Description,
            Icon = Icon,
            IsEnabled = IsEnabled,
            Parameters = new Dictionary<string, string>(Parameters, StringComparer.OrdinalIgnoreCase)
        };
    }
}
