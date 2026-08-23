using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Reframe.Core.History;

public class InputHistoryManager : INotifyPropertyChanged
{
    private int _currentIndex = -1;
    private int _maxItems = 100;

    public ObservableCollection<InputHistoryItem> Items { get; } = new();

    public int MaxItems
    {
        get => _maxItems;
        set
        {
            if (_maxItems != value)
            {
                _maxItems = value;
                OnPropertyChanged();
                TrimToMax();
            }
        }
    }

    public int CurrentIndex
    {
        get => _currentIndex;
        private set
        {
            if (_currentIndex != value)
            {
                _currentIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentItem));
                OnPropertyChanged(nameof(CanGoBack));
                OnPropertyChanged(nameof(CanGoForward));
            }
        }
    }

    public InputHistoryItem? CurrentItem =>
        _currentIndex >= 0 && _currentIndex < Items.Count ? Items[_currentIndex] : null;

    public bool CanGoBack => Items.Count > 0 && _currentIndex < Items.Count - 1 && _currentIndex >= 0;
    public bool CanGoForward => Items.Count > 0 && _currentIndex > 0;
    public int Count => Items.Count;

    public InputHistoryItem? AddEntry(string text, string source = "Pasted")
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        // Deduplicate consecutive identical inputs
        if (Items.Count > 0 && string.Equals(Items[0].FullText, text, StringComparison.Ordinal))
        {
            SetCurrent(Items[0]);
            return Items[0];
        }

        // Unmark previous current items
        foreach (var it in Items)
        {
            it.IsCurrent = false;
        }

        var entry = InputHistoryItem.Create(text, source);
        entry.IsCurrent = true;
        Items.Insert(0, entry);
        CurrentIndex = 0;

        TrimToMax();

        OnPropertyChanged(nameof(Count));
        return entry;
    }

    public InputHistoryItem? Restore(InputHistoryItem item)
    {
        if (item == null) return null;

        int index = Items.IndexOf(item);
        if (index < 0)
        {
            // If not in collection, add it
            return AddEntry(item.FullText, item.Source);
        }

        foreach (var it in Items)
        {
            it.IsCurrent = ReferenceEquals(it, item);
        }

        CurrentIndex = index;
        return item;
    }

    public InputHistoryItem? GoBack()
    {
        if (!CanGoBack) return null;
        return Restore(Items[_currentIndex + 1]);
    }

    public InputHistoryItem? GoForward()
    {
        if (!CanGoForward) return null;
        return Restore(Items[_currentIndex - 1]);
    }

    public bool Delete(InputHistoryItem item)
    {
        if (item == null) return false;

        int index = Items.IndexOf(item);
        if (index < 0) return false;

        bool wasCurrent = item.IsCurrent;
        Items.RemoveAt(index);

        if (Items.Count == 0)
        {
            CurrentIndex = -1;
        }
        else if (wasCurrent)
        {
            int newIndex = Math.Min(index, Items.Count - 1);
            Restore(Items[newIndex]);
        }
        else if (index < _currentIndex)
        {
            CurrentIndex--;
        }

        OnPropertyChanged(nameof(Count));
        return true;
    }

    public void Clear()
    {
        Items.Clear();
        CurrentIndex = -1;
        OnPropertyChanged(nameof(Count));
    }

    public void SetCurrent(InputHistoryItem? item)
    {
        if (item == null)
        {
            foreach (var it in Items) it.IsCurrent = false;
            CurrentIndex = -1;
            return;
        }

        int index = Items.IndexOf(item);
        if (index >= 0)
        {
            foreach (var it in Items) it.IsCurrent = ReferenceEquals(it, item);
            CurrentIndex = index;
        }
    }

    private void TrimToMax()
    {
        while (Items.Count > _maxItems)
        {
            Items.RemoveAt(Items.Count - 1);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
