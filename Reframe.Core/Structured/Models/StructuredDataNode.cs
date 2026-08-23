using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Reframe.Core.Structured;

public class StructuredDataNode : INotifyPropertyChanged
{
    private bool _isExpanded = true;
    private bool _isVisible = true;
    private bool _isSelected;

    public string Name { get; set; } = string.Empty;
    public string? Value { get; set; }
    public StructuredNodeType NodeType { get; set; } = StructuredNodeType.String;
    public string Path { get; set; } = string.Empty;
    public ObservableCollection<StructuredDataNode> Children { get; set; } = new();

    public bool HasChildren => Children.Count > 0;
    public int ChildCount => Children.Count;

    public string DisplayValue
    {
        get
        {
            if (NodeType == StructuredNodeType.Object)
            {
                return $"{{ {Children.Count} {(Children.Count == 1 ? "property" : "properties")} }}";
            }
            if (NodeType == StructuredNodeType.Array)
            {
                return $"[ {Children.Count} {(Children.Count == 1 ? "item" : "items")} ]";
            }
            if (NodeType == StructuredNodeType.Element)
            {
                if (Children.Count > 0)
                {
                    int attrCount = Children.Count(c => c.NodeType == StructuredNodeType.Attribute);
                    int elemCount = Children.Count(c => c.NodeType != StructuredNodeType.Attribute);
                    if (attrCount > 0 && elemCount > 0)
                        return $"<{elemCount} children, {attrCount} attrs>";
                    if (attrCount > 0)
                        return Value != null ? $"\"{Value}\" ({attrCount} attrs)" : $"<{attrCount} attrs>";
                    return $"<{elemCount} children>";
                }
                return Value != null ? $"\"{Value}\"" : string.Empty;
            }
            if (NodeType == StructuredNodeType.String)
            {
                return Value != null ? $"\"{Value}\"" : "\"\"";
            }
            if (NodeType == StructuredNodeType.Null)
            {
                return "null";
            }
            return Value ?? string.Empty;
        }
    }

    public string TypeBadge => NodeType switch
    {
        StructuredNodeType.Object => "{ }",
        StructuredNodeType.Array => "[ ]",
        StructuredNodeType.Element => "< >",
        StructuredNodeType.Attribute => "@",
        StructuredNodeType.String => "str",
        StructuredNodeType.Number => "num",
        StructuredNodeType.Boolean => "bool",
        StructuredNodeType.Null => "null",
        StructuredNodeType.Comment => "/* */",
        _ => "val"
    };

    public string TypeColorHex => NodeType switch
    {
        StructuredNodeType.Object => "#4EC9B0",
        StructuredNodeType.Array => "#4FC1FF",
        StructuredNodeType.Element => "#E5C07B",
        StructuredNodeType.Attribute => "#DCDCAA",
        StructuredNodeType.String => "#CE9178",
        StructuredNodeType.Number => "#B5CEA8",
        StructuredNodeType.Boolean => "#569CD6",
        StructuredNodeType.Null => "#808080",
        StructuredNodeType.Comment => "#6A9955",
        _ => "#D4D4D4"
    };

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible != value)
            {
                _isVisible = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public void ExpandAll()
    {
        IsExpanded = true;
        foreach (var child in Children)
        {
            child.ExpandAll();
        }
    }

    public void CollapseAll()
    {
        IsExpanded = false;
        foreach (var child in Children)
        {
            child.CollapseAll();
        }
    }

    public bool ApplyFilter(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            IsVisible = true;
            foreach (var child in Children)
            {
                child.ApplyFilter(null);
            }
            return true;
        }

        bool nameMatch = Name.Contains(query, StringComparison.OrdinalIgnoreCase);
        bool valueMatch = Value != null && Value.Contains(query, StringComparison.OrdinalIgnoreCase);
        bool pathMatch = Path.Contains(query, StringComparison.OrdinalIgnoreCase);
        bool badgeMatch = TypeBadge.Contains(query, StringComparison.OrdinalIgnoreCase);

        bool selfMatch = nameMatch || valueMatch || pathMatch || badgeMatch;
        bool anyChildMatch = false;

        foreach (var child in Children)
        {
            if (child.ApplyFilter(query))
            {
                anyChildMatch = true;
            }
        }

        IsVisible = selfMatch || anyChildMatch;
        if (anyChildMatch)
        {
            IsExpanded = true;
        }

        return IsVisible;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
