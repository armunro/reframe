using System;

namespace Reframe.Services;

public class ClipboardChangedEventArgs : EventArgs
{
    public string? Text { get; }
    public string? Html { get; }

    public ClipboardChangedEventArgs(string? text, string? html = null)
    {
        Text = text;
        Html = html;
    }
}

public interface IClipboardWatcher : IDisposable
{
    event EventHandler<ClipboardChangedEventArgs>? ClipboardChanged;
    bool IsRunning { get; }
    void Start();
    void Stop();
}
