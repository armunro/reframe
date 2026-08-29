using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Reframe.Core.Http;

/// <summary>
/// Represents a historical record of an executed Web / HTTP request.
/// </summary>
public class WebRequestHistoryItem : INotifyPropertyChanged
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _method = "GET";
    private string _url = string.Empty;
    private string _headers = string.Empty;
    private string? _body;
    private string _destination = "Input";
    private DateTime _timestamp = DateTime.Now;
    private bool _isSuccess;
    private int _statusCode;
    private string _statusDescription = string.Empty;
    private string _responseSummary = string.Empty;
    private long _durationMs;

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Method
    {
        get => _method;
        set
        {
            if (SetProperty(ref _method, value))
            {
                OnPropertyChanged(nameof(DisplayTitle));
                OnPropertyChanged(nameof(MethodBadgeBrushName));
            }
        }
    }

    public string Url
    {
        get => _url;
        set
        {
            if (SetProperty(ref _url, value))
            {
                OnPropertyChanged(nameof(DisplayTitle));
                OnPropertyChanged(nameof(HostDisplay));
            }
        }
    }

    public string Headers
    {
        get => _headers;
        set
        {
            if (SetProperty(ref _headers, value))
            {
                OnPropertyChanged(nameof(HasHeaders));
                OnPropertyChanged(nameof(HeadersCountDisplay));
            }
        }
    }

    public string? Body
    {
        get => _body;
        set
        {
            if (SetProperty(ref _body, value))
            {
                OnPropertyChanged(nameof(HasBody));
            }
        }
    }

    public string Destination
    {
        get => _destination;
        set => SetProperty(ref _destination, value);
    }

    public DateTime Timestamp
    {
        get => _timestamp;
        set
        {
            if (SetProperty(ref _timestamp, value))
            {
                OnPropertyChanged(nameof(DisplayTime));
                OnPropertyChanged(nameof(RelativeTimeDisplay));
            }
        }
    }

    public bool IsSuccess
    {
        get => _isSuccess;
        set => SetProperty(ref _isSuccess, value);
    }

    public int StatusCode
    {
        get => _statusCode;
        set => SetProperty(ref _statusCode, value);
    }

    public string StatusDescription
    {
        get => _statusDescription;
        set => SetProperty(ref _statusDescription, value);
    }

    public string ResponseSummary
    {
        get => _responseSummary;
        set => SetProperty(ref _responseSummary, value);
    }

    public long DurationMs
    {
        get => _durationMs;
        set => SetProperty(ref _durationMs, value);
    }

    public string DisplayTime => Timestamp.ToString("HH:mm:ss");

    public string FullTimestampDisplay => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");

    public string DisplayTitle => $"[{Method.ToUpperInvariant()}] {Url}";

    public bool HasHeaders => !string.IsNullOrWhiteSpace(Headers);

    public bool HasBody => !string.IsNullOrWhiteSpace(Body);

    public string HeadersCountDisplay
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Headers)) return "0 headers";
            var lines = Headers.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            return lines.Length == 1 ? "1 header" : $"{lines.Length} headers";
        }
    }

    public string HostDisplay
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Url)) return string.Empty;
            try
            {
                if (Uri.TryCreate(Url, UriKind.Absolute, out var uri))
                {
                    return uri.Host;
                }
            }
            catch
            {
                // Ignore
            }
            return Url;
        }
    }

    public string MethodBadgeBrushName => Method.ToUpperInvariant() switch
    {
        "GET" => "AccentSuccessBrush",
        "POST" => "AccentWarningBrush",
        "PUT" => "AccentInfoBrush",
        "PATCH" => "AccentBlueBadgeBrush",
        "DELETE" => "AccentErrorBrush",
        _ => "AccentInfoBrush"
    };

    public string RelativeTimeDisplay
    {
        get
        {
            var diff = DateTime.Now - Timestamp;
            if (diff.TotalSeconds < 60) return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            return Timestamp.ToString("MMM dd HH:mm");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(storage, value)) return false;
        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
