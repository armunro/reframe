using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TextForge.Core.State;

/// <summary>
/// Manages and persists the open/collapsed state of collapsible sections in TextForge.
/// Default state for all sections is collapsed (false).
/// </summary>
public class SectionStateManager
{
    private static readonly Lazy<SectionStateManager> _defaultInstance = new(() =>
    {
        var manager = new SectionStateManager();
        manager.Load();
        return manager;
    });

    public static SectionStateManager Instance => _defaultInstance.Value;

    private readonly object _lock = new();
    private readonly Dictionary<string, bool> _states = new(StringComparer.OrdinalIgnoreCase);
    private string _storageFilePath;

    public SectionStateManager(string? storageFilePath = null)
    {
        _storageFilePath = storageFilePath ?? GetDefaultStoragePath();
    }

    public string StorageFilePath
    {
        get => _storageFilePath;
        set => _storageFilePath = value;
    }

    public static string GetDefaultStoragePath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TextForge",
            "section_states.json");
    }

    /// <summary>
    /// Gets the expanded state for the specified section key.
    /// Defaults to false (collapsed) if no saved state exists.
    /// </summary>
    public bool GetState(string key, bool defaultValue = false)
    {
        if (string.IsNullOrWhiteSpace(key)) return defaultValue;

        lock (_lock)
        {
            return _states.TryGetValue(key, out var isExpanded) ? isExpanded : defaultValue;
        }
    }

    /// <summary>
    /// Sets the expanded state for the specified section key and persists it.
    /// </summary>
    public void SetState(string key, bool isExpanded, bool autoSave = true)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        bool changed = false;
        lock (_lock)
        {
            if (!_states.TryGetValue(key, out var current) || current != isExpanded)
            {
                _states[key] = isExpanded;
                changed = true;
            }
        }

        if (changed && autoSave)
        {
            Save();
        }
    }

    /// <summary>
    /// Checks whether a state is explicitly recorded for the specified section key.
    /// </summary>
    public bool HasState(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;

        lock (_lock)
        {
            return _states.ContainsKey(key);
        }
    }

    /// <summary>
    /// Returns a copy of all tracked states.
    /// </summary>
    public Dictionary<string, bool> GetAllStates()
    {
        lock (_lock)
        {
            return new Dictionary<string, bool>(_states, StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Clears all tracked states in memory.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _states.Clear();
        }
    }

    /// <summary>
    /// Loads saved section states from disk.
    /// </summary>
    public void Load(string? filePath = null)
    {
        string path = filePath ?? _storageFilePath;
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            var loaded = JsonSerializer.Deserialize<Dictionary<string, bool>>(json);
            if (loaded != null)
            {
                lock (_lock)
                {
                    _states.Clear();
                    foreach (var kvp in loaded)
                    {
                        _states[kvp.Key] = kvp.Value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load section states: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves section states to disk.
    /// </summary>
    public void Save(string? filePath = null)
    {
        string path = filePath ?? _storageFilePath;
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            Dictionary<string, bool> snapshot;
            lock (_lock)
            {
                snapshot = new Dictionary<string, bool>(_states, StringComparer.OrdinalIgnoreCase);
            }

            string json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save section states: {ex.Message}");
        }
    }
}
