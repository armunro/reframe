using TextForge.ViewModels;
using Xunit;

namespace TextForge.Tests;

public class AutoSendOutputToInputTests
{
    [Fact]
    public void AutoSendOutputToInput_DefaultIsFalse()
    {
        var vm = new MainViewModel();
        Assert.False(vm.AutoSendOutputToInput);
    }

    [Fact]
    public void PropertyChanged_FiresWhenAutoSendOutputToInputChanges()
    {
        var vm = new MainViewModel();
        string? changedProperty = null;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.AutoSendOutputToInput))
            {
                changedProperty = e.PropertyName;
            }
        };

        vm.AutoSendOutputToInput = true;
        Assert.Equal(nameof(MainViewModel.AutoSendOutputToInput), changedProperty);
        Assert.True(vm.AutoSendOutputToInput);
    }

    [Fact]
    public void Disabled_DoesNotSendOutputToInput()
    {
        var vm = new MainViewModel
        {
            AutoSendOutputToInput = false,
            InputText = "hello world"
        };

        vm.ActionCommand.Execute("UpperCase");

        Assert.Equal("HELLO WORLD", vm.OutputText);
        Assert.Equal("hello world", vm.InputText);
    }

    [Fact]
    public void Enabled_AutomaticallySendsOutputToInputOnAction()
    {
        var vm = new MainViewModel();
        vm.InputText = "hello world";
        vm.AutoSendOutputToInput = true;

        vm.ActionCommand.Execute("UpperCase");

        Assert.Equal("HELLO WORLD", vm.OutputText);
        Assert.Equal("HELLO WORLD", vm.InputText);
    }

    [Fact]
    public void Enabled_SupportsSequentialChainedTransformations()
    {
        var vm = new MainViewModel
        {
            AutoSendOutputToInput = true,
            InputText = "apple\nbanana\ncherry"
        };

        // Step 1: Uppercase
        vm.ActionCommand.Execute("UpperCase");
        Assert.Equal($"APPLE{Environment.NewLine}BANANA{Environment.NewLine}CHERRY", vm.InputText);

        // Step 2: Quote lines with single quotes
        vm.ActionCommand.Execute("QuoteSingle");
        Assert.Equal($"'APPLE'{Environment.NewLine}'BANANA'{Environment.NewLine}'CHERRY'", vm.InputText);

        // Step 3: Join lines with comma
        vm.JoinDelimiter = ", ";
        vm.ActionCommand.Execute("JoinComma");
        Assert.Equal("'APPLE', 'BANANA', 'CHERRY'", vm.InputText);
    }

    [Fact]
    public void Enabled_ExecuteTransformCommand_SendsOutputToInput()
    {
        var vm = new MainViewModel
        {
            AutoSendOutputToInput = true,
            InputText = "foo_bar_baz"
        };

        vm.ActionCommand.Execute("PascalCase");
        Assert.Equal("FooBarBaz", vm.InputText);
        Assert.Equal("FooBarBaz", vm.OutputText);
    }

    [Fact]
    public void Enabled_RecordsHistoryWhenOutputIsSentToInput()
    {
        var vm = new MainViewModel
        {
            AutoSendOutputToInput = true,
            InputText = "sample"
        };

        int initialHistoryCount = vm.HistoryCount;
        vm.ActionCommand.Execute("UpperCase");

        Assert.True(vm.HistoryCount > initialHistoryCount);
        Assert.Equal("SAMPLE", vm.InputText);
        Assert.NotNull(vm.SelectedHistoryItem);
        Assert.Contains("UpperCase", vm.SelectedHistoryItem.Source);
    }
}
