using System;
using Reframe.Services;
using Reframe.ViewModels;
using Xunit;

namespace Reframe.Tests;

public class WatchClipboardTests
{
    private class MockClipboardWatcher : IClipboardWatcher
    {
        public event EventHandler<ClipboardChangedEventArgs>? ClipboardChanged;
        public bool IsRunning { get; private set; }
        public int StartCalls { get; private set; }
        public int StopCalls { get; private set; }
        public int DisposeCalls { get; private set; }

        public void Start()
        {
            IsRunning = true;
            StartCalls++;
        }

        public void Stop()
        {
            IsRunning = false;
            StopCalls++;
        }

        public void RaiseClipboardChanged(string? text, string? html = null)
        {
            ClipboardChanged?.Invoke(this, new ClipboardChangedEventArgs(text, html));
        }

        public void Dispose()
        {
            DisposeCalls++;
            Stop();
        }
    }

    [Fact]
    public void WatchClipboard_DefaultIsFalse()
    {
        var vm = new MainViewModel();
        Assert.False(vm.WatchClipboard);
        Assert.False(vm.IsWatchingClipboard);
    }

    [Fact]
    public void PropertyChanged_FiresWhenWatchClipboardChanges()
    {
        var vm = new MainViewModel();
        bool watchClipboardFired = false;
        bool isWatchingClipboardFired = false;

        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.WatchClipboard))
                watchClipboardFired = true;
            if (e.PropertyName == nameof(MainViewModel.IsWatchingClipboard))
                isWatchingClipboardFired = true;
        };

        vm.WatchClipboard = true;

        Assert.True(watchClipboardFired);
        Assert.True(isWatchingClipboardFired);
        Assert.True(vm.WatchClipboard);
        Assert.Contains("Watching clipboard", vm.StatusMessage);

        vm.WatchClipboard = false;
        Assert.False(vm.WatchClipboard);
        Assert.Contains("Stopped watching", vm.StatusMessage);
    }

    [Fact]
    public void Disabled_DoesNotProcessClipboardItems()
    {
        var vm = new MainViewModel
        {
            WatchClipboard = false,
            InputText = "original input"
        };

        vm.ProcessClipboardItem("new clipboard text");

        Assert.Equal("original input", vm.InputText);
    }

    [Fact]
    public void Enabled_UpdatesInputTextAndRecordsHistory()
    {
        var vm = new MainViewModel
        {
            InputText = "initial text",
            WatchClipboard = true
        };

        int initialHistoryCount = vm.HistoryCount;
        vm.ProcessClipboardItem("copied item 1");

        Assert.Equal("copied item 1", vm.InputText);
        Assert.Equal(initialHistoryCount + 1, vm.HistoryCount);
        Assert.NotNull(vm.SelectedHistoryItem);
        Assert.Equal("Clipboard Watch", vm.SelectedHistoryItem.Source);
        Assert.Equal("Added new item from clipboard", vm.StatusMessage);
    }

    [Fact]
    public void Enabled_ProcessesHtmlTableClipboardData()
    {
        var vm = new MainViewModel
        {
            InputText = "initial text",
            WatchClipboard = true
        };

        string html = "<table><tr><th>Name</th><th>Role</th></tr><tr><td>Alice</td><td>Developer</td></tr></table>";
        vm.ProcessClipboardItem("plain text fallback", html);

        Assert.Contains("Name", vm.InputText);
        Assert.Contains("Alice", vm.InputText);
        Assert.NotNull(vm.SelectedHistoryItem);
        Assert.Equal("Clipboard Watch (HTML Table)", vm.SelectedHistoryItem.Source);
    }

    [Fact]
    public void Enabled_IgnoresDuplicateOrUnchangedClipboardContent()
    {
        var vm = new MainViewModel
        {
            InputText = "initial text",
            WatchClipboard = true
        };

        vm.ProcessClipboardItem("item ABC");
        int historyCountAfterFirst = vm.HistoryCount;
        Assert.Equal("item ABC", vm.InputText);

        // Send same content again
        vm.ProcessClipboardItem("item ABC");
        Assert.Equal(historyCountAfterFirst, vm.HistoryCount);

        // Send content identical to current InputText
        vm.ProcessClipboardItem(vm.InputText);
        Assert.Equal(historyCountAfterFirst, vm.HistoryCount);
    }

    [Fact]
    public void Enabled_RealTimeTransformExecutesOnNewClipboardItem()
    {
        var vm = new MainViewModel
        {
            IsRealTimeTransform = true,
            WatchClipboard = true,
            InputText = "init"
        };

        // Select an action
        vm.ActionCommand.Execute("UpperCase");
        Assert.Equal("INIT", vm.OutputText);

        // Receive new clipboard item
        vm.ProcessClipboardItem("hello world");

        Assert.Equal("hello world", vm.InputText);
        Assert.Equal("HELLO WORLD", vm.OutputText);
    }

    [Fact]
    public void ClipboardWatcher_Integration_StartsAndStopsWithToggle()
    {
        var watcher = new MockClipboardWatcher();
        var vm = new MainViewModel
        {
            ClipboardWatcher = watcher
        };

        Assert.False(watcher.IsRunning);

        vm.WatchClipboard = true;
        Assert.True(watcher.IsRunning);
        Assert.Equal(1, watcher.StartCalls);

        // Firing watcher event triggers input update
        watcher.RaiseClipboardChanged("external copied text");
        Assert.Equal("external copied text", vm.InputText);

        vm.WatchClipboard = false;
        Assert.False(watcher.IsRunning);
        Assert.Equal(1, watcher.StopCalls);

        // Firing watcher event when disabled does not change input
        watcher.RaiseClipboardChanged("another copied text");
        Assert.Equal("external copied text", vm.InputText);
    }

    [Fact]
    public void SettingClipboardWatcher_WhenAlreadyEnabled_StartsImmediately()
    {
        var vm = new MainViewModel
        {
            WatchClipboard = true
        };

        var watcher = new MockClipboardWatcher();
        vm.ClipboardWatcher = watcher;

        Assert.True(watcher.IsRunning);
        Assert.Equal(1, watcher.StartCalls);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Enabled_NullOrWhitespaceClipboard_DoesNotChangeInput(string? emptyContent)
    {
        var vm = new MainViewModel
        {
            InputText = "existing input",
            WatchClipboard = true
        };

        int count = vm.HistoryCount;
        vm.ProcessClipboardItem(emptyContent);

        Assert.Equal("existing input", vm.InputText);
        Assert.Equal(count, vm.HistoryCount);
    }

    [Fact]
    public void CopyOutputCommand_SetsLastProcessedClipboardText_ToAvoidReimport()
    {
        var vm = new MainViewModel
        {
            InputText = "hello",
            OutputText = "HELLO",
            WatchClipboard = true
        };

        vm.CopyOutputCommand.Execute(null);

        // When clipboard change event arrives with the copied output text
        vm.ProcessClipboardItem("HELLO");

        // InputText should remain "hello", not overwritten by the output that was just copied
        Assert.Equal("hello", vm.InputText);
    }
}
