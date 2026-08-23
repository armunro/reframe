using System;
using System.IO;
using System.Linq;
using Reframe.ViewModels;
using Xunit;

namespace Reframe.Tests;

public class FileLoadingTests : IDisposable
{
    private readonly string _tempDirectory;

    public FileLoadingTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "ReframeTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup exceptions
        }
    }

    [Fact]
    public void LoadFromFile_ValidTextFile_LoadsContentAndRecordsHistory()
    {
        string filePath = Path.Combine(_tempDirectory, "sample.txt");
        string expectedContent = "Line 1\nLine 2\nLine 3";
        File.WriteAllText(filePath, expectedContent);

        var vm = new MainViewModel();
        bool result = vm.LoadFromFile(filePath);

        Assert.True(result);
        Assert.Equal(expectedContent, vm.InputText);
        Assert.Contains("sample.txt", vm.StatusMessage);
        Assert.Contains(vm.HistoryItems, h => h.Source == "File: sample.txt");
    }

    [Fact]
    public void LoadFromFile_ValidJsonFile_BeautifiesJsonContent()
    {
        string filePath = Path.Combine(_tempDirectory, "data.json");
        string rawJson = "{\"name\":\"Reframe\",\"version\":1}";
        File.WriteAllText(filePath, rawJson);

        var vm = new MainViewModel();
        bool result = vm.LoadFromFile(filePath);

        Assert.True(result);
        Assert.Contains("\"name\": \"Reframe\"", vm.InputText);
        Assert.Contains("\"version\": 1", vm.InputText);
        Assert.Contains("data.json", vm.StatusMessage);
    }

    [Fact]
    public void LoadFromFile_NonExistentFile_ReturnsFalseAndSetsStatusMessage()
    {
        var vm = new MainViewModel();
        string initialText = vm.InputText;
        string nonExistentPath = Path.Combine(_tempDirectory, "does_not_exist.txt");

        bool result = vm.LoadFromFile(nonExistentPath);

        Assert.False(result);
        Assert.Equal(initialText, vm.InputText);
        Assert.Contains("File not found", vm.StatusMessage);
    }

    [Fact]
    public void LoadFromFile_NullOrWhiteSpacePath_ReturnsFalse()
    {
        var vm = new MainViewModel();
        string initialText = vm.InputText;

        Assert.False(vm.LoadFromFile(string.Empty));
        Assert.False(vm.LoadFromFile("   "));
        Assert.Equal(initialText, vm.InputText);
    }

    [Fact]
    public void LoadFileCommand_WithPathParameter_LoadsFileSuccessfully()
    {
        string filePath = Path.Combine(_tempDirectory, "command_test.csv");
        string content = "Col1,Col2\nVal1,Val2";
        File.WriteAllText(filePath, content);

        var vm = new MainViewModel();
        Assert.True(vm.LoadFileCommand.CanExecute(filePath));
        vm.LoadFileCommand.Execute(filePath);

        Assert.Equal(content, vm.InputText);
        Assert.Contains("command_test.csv", vm.StatusMessage);
    }

    [Fact]
    public void LoadFileCommand_WithOpenFileDialogProvider_LoadsFileSuccessfully()
    {
        string filePath = Path.Combine(_tempDirectory, "dialog_test.txt");
        string content = "Hello from dialog";
        File.WriteAllText(filePath, content);

        var vm = new MainViewModel();
        vm.OpenFileDialogProvider = () => filePath;

        vm.LoadFileCommand.Execute(null);

        Assert.Equal(content, vm.InputText);
        Assert.Contains("dialog_test.txt", vm.StatusMessage);
    }

    [Fact]
    public void LoadFileCommand_WithCanceledDialog_DoesNotChangeInput()
    {
        var vm = new MainViewModel();
        string initialText = vm.InputText;
        vm.OpenFileDialogProvider = () => null;

        vm.LoadFileCommand.Execute(null);

        Assert.Equal(initialText, vm.InputText);
    }
}
