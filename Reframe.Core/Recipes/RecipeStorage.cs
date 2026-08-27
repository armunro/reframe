using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Reframe.Core.Recipes;

public static class RecipeStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    public static string GetDefaultFilePath()
    {
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Reframe");
        return Path.Combine(folder, "recipes.json");
    }

    public static string ExportToJson(TransformationRecipe recipe)
    {
        if (recipe == null) throw new ArgumentNullException(nameof(recipe));
        return JsonSerializer.Serialize(recipe, JsonOptions);
    }

    public static string ExportAllToJson(IEnumerable<TransformationRecipe> recipes)
    {
        if (recipes == null) throw new ArgumentNullException(nameof(recipes));
        var package = new RecipePackage
        {
            Version = "1.0",
            ExportedAt = DateTime.UtcNow,
            Recipes = recipes.ToList()
        };
        return JsonSerializer.Serialize(package, JsonOptions);
    }

    public static List<TransformationRecipe> ImportFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<TransformationRecipe>();

        string trimmed = json.Trim();

        try
        {
            // Case 1: Recipe package object with "recipes" array
            if (trimmed.StartsWith("{", StringComparison.Ordinal))
            {
                using var doc = JsonDocument.Parse(trimmed);
                if (doc.RootElement.TryGetProperty("recipes", out var recipesElement) && recipesElement.ValueKind == JsonValueKind.Array)
                {
                    var pkg = JsonSerializer.Deserialize<RecipePackage>(trimmed, JsonOptions);
                    if (pkg?.Recipes != null && pkg.Recipes.Count > 0)
                    {
                        return SanitizeImportedRecipes(pkg.Recipes);
                    }
                }

                // Case 2: Single recipe object
                var singleRecipe = JsonSerializer.Deserialize<TransformationRecipe>(trimmed, JsonOptions);
                if (singleRecipe != null && (!string.IsNullOrEmpty(singleRecipe.Name) || singleRecipe.Steps.Count > 0))
                {
                    return SanitizeImportedRecipes(new[] { singleRecipe });
                }
            }

            // Case 3: JSON array of recipes
            if (trimmed.StartsWith("[", StringComparison.Ordinal))
            {
                var list = JsonSerializer.Deserialize<List<TransformationRecipe>>(trimmed, JsonOptions);
                if (list != null && list.Count > 0)
                {
                    return SanitizeImportedRecipes(list);
                }
            }
        }
        catch
        {
            // Fallback: return empty on malformed input
        }

        return new List<TransformationRecipe>();
    }

    public static List<TransformationRecipe> LoadUserPresets(string? filePath = null)
    {
        string path = filePath ?? GetDefaultFilePath();
        if (!File.Exists(path))
        {
            return RecipeEngine.GetDefaultPresets();
        }

        try
        {
            string content = File.ReadAllText(path);
            var loaded = ImportFromJson(content);
            if (loaded.Count > 0)
            {
                // Ensure default presets exist if not already in loaded list
                var defaults = RecipeEngine.GetDefaultPresets();
                foreach (var def in defaults)
                {
                    if (!loaded.Any(r => r.Id == def.Id || r.Name == def.Name))
                    {
                        loaded.Add(def);
                    }
                }
                return loaded;
            }
        }
        catch
        {
            // Fallback to default presets on disk/read error
        }

        return RecipeEngine.GetDefaultPresets();
    }

    public static bool SaveUserPresets(IEnumerable<TransformationRecipe> recipes, string? filePath = null)
    {
        string path = filePath ?? GetDefaultFilePath();
        try
        {
            string dir = Path.GetDirectoryName(path)!;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = ExportAllToJson(recipes);
            File.WriteAllText(path, json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static List<TransformationRecipe> SanitizeImportedRecipes(IEnumerable<TransformationRecipe> recipes)
    {
        var sanitized = new List<TransformationRecipe>();
        foreach (var r in recipes)
        {
            if (string.IsNullOrWhiteSpace(r.Name))
            {
                r.Name = "Imported Recipe";
            }

            if (string.IsNullOrEmpty(r.Id))
            {
                r.Id = Guid.NewGuid().ToString("N");
            }

            r.Steps ??= new List<RecipeStep>();
            foreach (var step in r.Steps)
            {
                if (string.IsNullOrEmpty(step.Id))
                {
                    step.Id = Guid.NewGuid().ToString("N");
                }
                step.Parameters ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            sanitized.Add(r);
        }
        return sanitized;
    }
}

public class RecipePackage
{
    public string Version { get; set; } = "1.0";
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    public List<TransformationRecipe> Recipes { get; set; } = new();
}
