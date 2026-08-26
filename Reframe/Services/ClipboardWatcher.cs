using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Reframe.Services;

public class ClipboardWatcher : IClipboardWatcher
{
    private const int WM_CLIPBOARDUPDATE = 0x031D;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    private IntPtr _hwnd = IntPtr.Zero;
    private HwndSource? _hwndSource;
    private bool _isListenerAdded;
    private bool _isRunning;
    private bool _disposed;

    public event EventHandler<ClipboardChangedEventArgs>? ClipboardChanged;

    public bool IsRunning => _isRunning;

    public ClipboardWatcher()
    {
    }

    public ClipboardWatcher(IntPtr hwnd)
    {
        Attach(hwnd);
    }

    public void Attach(IntPtr hwnd)
    {
        if (_hwnd == hwnd) return;
        Detach();

        if (hwnd != IntPtr.Zero)
        {
            _hwnd = hwnd;
            _hwndSource = HwndSource.FromHwnd(hwnd);
            _hwndSource?.AddHook(HwndHook);

            if (_isRunning)
            {
                RegisterListener();
            }
        }
    }

    public void Detach()
    {
        UnregisterListener();
        if (_hwndSource != null)
        {
            _hwndSource.RemoveHook(HwndHook);
            _hwndSource = null;
        }
        _hwnd = IntPtr.Zero;
    }

    public void Start()
    {
        if (_disposed) return;
        _isRunning = true;
        if (_hwnd != IntPtr.Zero)
        {
            RegisterListener();
        }
    }

    public void Stop()
    {
        _isRunning = false;
        UnregisterListener();
    }

    private void RegisterListener()
    {
        if (!_isListenerAdded && _hwnd != IntPtr.Zero)
        {
            _isListenerAdded = AddClipboardFormatListener(_hwnd);
        }
    }

    private void UnregisterListener()
    {
        if (_isListenerAdded && _hwnd != IntPtr.Zero)
        {
            RemoveClipboardFormatListener(_hwnd);
            _isListenerAdded = false;
        }
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_CLIPBOARDUPDATE && _isRunning)
        {
            OnClipboardUpdate();
        }
        return IntPtr.Zero;
    }

    private void OnClipboardUpdate()
    {
        if (!_isRunning || _disposed) return;

        var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        dispatcher.BeginInvoke(new Action(() =>
        {
            if (!_isRunning || _disposed) return;

            try
            {
                var (text, html) = ReadClipboard();
                if (!string.IsNullOrEmpty(text) || !string.IsNullOrEmpty(html))
                {
                    ClipboardChanged?.Invoke(this, new ClipboardChangedEventArgs(text, html));
                }
            }
            catch
            {
                // Ignore transient clipboard access errors
            }
        }), DispatcherPriority.ApplicationIdle);
    }

    public static (string? text, string? html) ReadClipboard()
    {
        for (int retry = 0; retry < 5; retry++)
        {
            try
            {
                string? html = null;
                string? text = null;

                if (Clipboard.ContainsText(TextDataFormat.Html))
                {
                    html = Clipboard.GetText(TextDataFormat.Html);
                }

                if (Clipboard.ContainsText(TextDataFormat.UnicodeText))
                {
                    text = Clipboard.GetText(TextDataFormat.UnicodeText);
                }
                else if (Clipboard.ContainsText(TextDataFormat.Text))
                {
                    text = Clipboard.GetText(TextDataFormat.Text);
                }

                return (text, html);
            }
            catch (COMException)
            {
                Thread.Sleep(20);
            }
            catch
            {
                break;
            }
        }
        return (null, null);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        Detach();
        GC.SuppressFinalize(this);
    }
}
