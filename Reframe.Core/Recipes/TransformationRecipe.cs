using System;
using System.Collections.Generic;
using System.Linq;

namespace Reframe.Core.Recipes;

public class TransformationRecipe
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Untitled Recipe";
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Custom";
    public string? Hotkey { get; set; }
    public bool AutoSendToInput { get; set; }
    public bool WatchClipboard { get; set; }
    public bool IsBuiltIn { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public List<string> Tags { get; set; } = new();
    public List<RecipeStep> Steps { get; set; } = new();

    public int StepCount => Steps.Count;

    public string StepSummary => Steps.Count switch
    {
        0 => "No steps",
        1 => Steps[0].Title,
        _ => string.Join(" ➔ ", Steps.Select(s => s.Title))
    };

    public TransformationRecipe()
    {
    }

    public TransformationRecipe(
        string name,
        string description = "",
        string category = "Custom",
        string? hotkey = null,
        IEnumerable<RecipeStep>? steps = null,
        bool isBuiltIn = false,
        string? id = null)
    {
        Id = id ?? Guid.NewGuid().ToString("N");
        Name = name;
        Description = description;
        Category = category;
        Hotkey = hotkey;
        IsBuiltIn = isBuiltIn;
        CreatedDate = DateTime.UtcNow;
        if (steps != null)
        {
            Steps.AddRange(steps);
        }
    }

    public TransformationRecipe Clone(bool asNewCustom = true)
    {
        return new TransformationRecipe
        {
            Id = asNewCustom ? Guid.NewGuid().ToString("N") : Id,
            Name = asNewCustom ? $"{Name} (Copy)" : Name,
            Description = Description,
            Category = asNewCustom ? "Custom" : Category,
            Hotkey = Hotkey,
            AutoSendToInput = AutoSendToInput,
            WatchClipboard = WatchClipboard,
            IsBuiltIn = asNewCustom ? false : IsBuiltIn,
            CreatedDate = DateTime.UtcNow,
            Tags = new List<string>(Tags),
            Steps = Steps.Select(s => s.Clone()).ToList()
        };
    }
}
