using System.ComponentModel;
using System.Runtime.CompilerServices;
using TextForge.Core.Analysis;

namespace TextForge.Core.History;

public class InputHistoryItem : INotifyPropertyChanged
{
    private bool _isCurrent;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Source { get; set; } = "Pasted";
    public string FullText { get; set; } = string.Empty;
    public string Preview { get; set; } = string.Empty;
    public DetectedFormat Format { get; set; } = DetectedFormat.Empty;
    public string FormatName { get; set; } = "Plain Text";
    public int LineCount { get; set; }
    public int CharCount { get; set; }
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public bool IsTabular { get; set; }
    public string SizeDisplay { get; set; } = string.Empty;

    public bool IsCurrent
    {
        get => _isCurrent;
        set
        {
            if (_isCurrent != value)
            {
                _isCurrent = value;
                OnPropertyChanged();
            }
        }
    }

    public string TimeDisplay => Timestamp.ToString("HH:mm:ss");
    public string DateDisplay => Timestamp.ToString("yyyy-MM-dd");
    public string RelativeTime
    {
        get
        {
            var span = DateTime.Now - Timestamp;
            if (span.TotalSeconds < 10) return "Just now";
            if (span.TotalSeconds < 60) return $"{(int)span.TotalSeconds}s ago";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            return DateDisplay;
        }
    }

    public static InputHistoryItem Create(string text, string source = "Pasted")
    {
        text ??= string.Empty;
        var analysis = TextAnalyzer.Analyze(text);

        // Generate clean preview snippet (first 3-4 lines, max 200 chars)
        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var previewLines = lines.Take(4).Select(l => l.Length > 80 ? l.Substring(0, 77) + "..." : l);
        string preview = string.Join(Environment.NewLine, previewLines);
        if (lines.Length > 4)
        {
            preview += $"{Environment.NewLine}... (+{lines.Length - 4} more lines)";
        }

        string sizeDisplay = analysis.IsTabular
            ? $"{analysis.RowCount} rows × {analysis.ColumnCount} cols • {analysis.CharacterCount} chars"
            : $"{analysis.LineCount} lines • {analysis.CharacterCount} chars";

        return new InputHistoryItem
        {
            Timestamp = DateTime.Now,
            Source = source,
            FullText = text,
            Preview = preview,
            Format = analysis.Format,
            FormatName = analysis.FormatDescription,
            LineCount = analysis.LineCount,
            CharCount = analysis.CharacterCount,
            RowCount = analysis.RowCount,
            ColumnCount = analysis.ColumnCount,
            IsTabular = analysis.IsTabular,
            SizeDisplay = sizeDisplay,
            IsCurrent = true
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
