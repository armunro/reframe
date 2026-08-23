using Reframe.Core.Analysis;
using Reframe.Core.History;
using Reframe.ViewModels;
using Xunit;

namespace Reframe.Tests;

public class InputHistoryTests
{
    [Fact]
    public void AddEntry_ExtractsMetadataAndSetsCurrent()
    {
        var manager = new InputHistoryManager();
        string csv = "Id,Name,Role\n1,Alice,Engineer\n2,Bob,Designer";

        var item = manager.AddEntry(csv, "Pasted (CSV)");

        Assert.NotNull(item);
        Assert.Single(manager.Items);
        Assert.Equal(0, manager.CurrentIndex);
        Assert.Equal(item, manager.CurrentItem);
        Assert.True(item.IsCurrent);
        Assert.Equal("Pasted (CSV)", item.Source);
        Assert.Equal(DetectedFormat.CsvTable, item.Format);
        Assert.True(item.IsTabular);
        Assert.Equal(2, item.RowCount);
        Assert.Equal(3, item.ColumnCount);
        Assert.Equal(3, item.LineCount);
        Assert.NotEmpty(item.Preview);
        Assert.NotEmpty(item.SizeDisplay);
        Assert.NotEmpty(item.TimeDisplay);
    }

    [Fact]
    public void AddEntry_DeduplicatesConsecutiveIdenticalText()
    {
        var manager = new InputHistoryManager();
        string text = "1001\n1002\n1003";

        var item1 = manager.AddEntry(text, "Paste 1");
        var item2 = manager.AddEntry(text, "Paste 2");

        Assert.Single(manager.Items);
        Assert.Same(item1, item2);
        Assert.Equal(1, manager.Count);
    }

    [Fact]
    public void AddEntry_EnforcesMaxItems()
    {
        var manager = new InputHistoryManager { MaxItems = 3 };

        manager.AddEntry("entry 1");
        manager.AddEntry("entry 2");
        manager.AddEntry("entry 3");
        manager.AddEntry("entry 4");

        Assert.Equal(3, manager.Count);
        Assert.Equal("entry 4", manager.Items[0].FullText);
        Assert.Equal("entry 3", manager.Items[1].FullText);
        Assert.Equal("entry 2", manager.Items[2].FullText);
    }

    [Fact]
    public void TimelineNavigation_GoBackAndGoForward()
    {
        var manager = new InputHistoryManager();
        var item1 = manager.AddEntry("State 1", "Pasted 1");
        var item2 = manager.AddEntry("State 2", "Pasted 2");
        var item3 = manager.AddEntry("State 3", "Pasted 3");

        // Currently at index 0 (State 3)
        Assert.True(manager.CanGoBack);
        Assert.False(manager.CanGoForward);
        Assert.Equal("State 3", manager.CurrentItem?.FullText);

        // Go Back to State 2 (index 1)
        var restored2 = manager.GoBack();
        Assert.NotNull(restored2);
        Assert.Equal("State 2", restored2.FullText);
        Assert.Equal(1, manager.CurrentIndex);
        Assert.True(manager.CanGoBack);
        Assert.True(manager.CanGoForward);

        // Go Back to State 1 (index 2)
        var restored1 = manager.GoBack();
        Assert.NotNull(restored1);
        Assert.Equal("State 1", restored1.FullText);
        Assert.Equal(2, manager.CurrentIndex);
        Assert.False(manager.CanGoBack);
        Assert.True(manager.CanGoForward);

        // Go Forward to State 2
        var forward2 = manager.GoForward();
        Assert.NotNull(forward2);
        Assert.Equal("State 2", forward2.FullText);
        Assert.Equal(1, manager.CurrentIndex);

        // Go Forward to State 3
        var forward3 = manager.GoForward();
        Assert.NotNull(forward3);
        Assert.Equal("State 3", forward3.FullText);
        Assert.Equal(0, manager.CurrentIndex);
        Assert.False(manager.CanGoForward);
    }

    [Fact]
    public void Restore_UpdatesCurrentItemAndFlags()
    {
        var manager = new InputHistoryManager();
        var item1 = manager.AddEntry("Text A");
        var item2 = manager.AddEntry("Text B");
        var item3 = manager.AddEntry("Text C");

        manager.Restore(item1!);

        Assert.True(item1!.IsCurrent);
        Assert.False(item2!.IsCurrent);
        Assert.False(item3!.IsCurrent);
        Assert.Equal(2, manager.CurrentIndex);
        Assert.Equal(item1, manager.CurrentItem);
    }

    [Fact]
    public void Delete_RemovesItemAndAdjustsIndex()
    {
        var manager = new InputHistoryManager();
        var item1 = manager.AddEntry("Item 1");
        var item2 = manager.AddEntry("Item 2");
        var item3 = manager.AddEntry("Item 3");

        bool deleted = manager.Delete(item2!);

        Assert.True(deleted);
        Assert.Equal(2, manager.Count);
        Assert.DoesNotContain(item2!, manager.Items);
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var manager = new InputHistoryManager();
        manager.AddEntry("Item 1");
        manager.AddEntry("Item 2");

        manager.Clear();

        Assert.Empty(manager.Items);
        Assert.Equal(-1, manager.CurrentIndex);
        Assert.Null(manager.CurrentItem);
    }

    [Fact]
    public void HistoryTabHeader_Format_MatchesExpectedString()
    {
        var manager = new InputHistoryManager();
        Assert.Equal("History (0)", $"History ({manager.Count})");

        manager.AddEntry("Entry 1");
        Assert.Equal("History (1)", $"History ({manager.Count})");

        manager.AddEntry("Entry 2");
        Assert.Equal("History (2)", $"History ({manager.Count})");
    }

    [Fact]
    public void MainViewModel_HistoryBackAndForwardCommands_NavigateTimeline()
    {
        var vm = new MainViewModel();
        vm.ClearHistoryCommand.Execute(null);

        vm.InputText = "First Snapshot";
        vm.RecordHistory(vm.InputText, "Initial");

        vm.InputText = "Second Snapshot";
        vm.RecordHistory(vm.InputText, "Step 1");

        vm.InputText = "Third Snapshot";
        vm.RecordHistory(vm.InputText, "Step 2");

        Assert.Equal("Third Snapshot", vm.InputText);
        Assert.True(vm.HistoryBackCommand.CanExecute(null));
        Assert.False(vm.HistoryForwardCommand.CanExecute(null));

        // Navigate back to Second Snapshot
        vm.HistoryBackCommand.Execute(null);
        Assert.Equal("Second Snapshot", vm.InputText);
        Assert.True(vm.HistoryBackCommand.CanExecute(null));
        Assert.True(vm.HistoryForwardCommand.CanExecute(null));

        // Navigate back to First Snapshot
        vm.HistoryBackCommand.Execute(null);
        Assert.Equal("First Snapshot", vm.InputText);
        Assert.False(vm.HistoryBackCommand.CanExecute(null));
        Assert.True(vm.HistoryForwardCommand.CanExecute(null));

        // Navigate forward to Second Snapshot
        vm.HistoryForwardCommand.Execute(null);
        Assert.Equal("Second Snapshot", vm.InputText);
        Assert.True(vm.HistoryBackCommand.CanExecute(null));
        Assert.True(vm.HistoryForwardCommand.CanExecute(null));

        // Navigate forward to Third Snapshot
        vm.HistoryForwardCommand.Execute(null);
        Assert.Equal("Third Snapshot", vm.InputText);
        Assert.True(vm.HistoryBackCommand.CanExecute(null));
        Assert.False(vm.HistoryForwardCommand.CanExecute(null));
    }
}
