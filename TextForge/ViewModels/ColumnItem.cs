using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TextForge.ViewModels;

public class ColumnItem : INotifyPropertyChanged
{
    private bool _isSelected = true;
    private string _name = string.Empty;
    private int _index;
    private string _sampleValue = string.Empty;

    public int Index
    {
        get => _index;
        set
        {
            if (_index != value)
            {
                _index = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayIndex));
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    public int DisplayIndex => _index + 1;

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    public string SampleValue
    {
        get => _sampleValue;
        set
        {
            if (_sampleValue != value)
            {
                _sampleValue = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayText));
                OnPropertyChanged(nameof(HasSampleValue));
            }
        }
    }

    public bool HasSampleValue => !string.IsNullOrWhiteSpace(_sampleValue);

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

    public string DisplayText => string.IsNullOrEmpty(SampleValue) 
        ? $"[{Index + 1}] {Name}" 
        : $"[{Index + 1}] {Name} (e.g. {SampleValue})";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
