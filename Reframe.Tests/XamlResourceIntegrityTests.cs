using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Reframe.Tests;

public class XamlResourceIntegrityTests
{
    [Fact]
    public void AppXaml_ContainsSecondaryToolButtonStyle()
    {
        string? solutionDir = FindSolutionDirectory();
        Assert.NotNull(solutionDir);

        string appXamlPath = Path.Combine(solutionDir, "Reframe", "App.xaml");
        Assert.True(File.Exists(appXamlPath), $"App.xaml not found at {appXamlPath}");

        string content = File.ReadAllText(appXamlPath);
        Assert.Contains("x:Key=\"SecondaryToolButtonStyle\"", content);
        Assert.Contains("x:Key=\"ToolButtonStyle\"", content);
        Assert.Contains("x:Key=\"PrimaryToolButtonStyle\"", content);
    }

    [Fact]
    public void MainWindowXaml_ContainsSecondaryToolButtonStyle()
    {
        string? solutionDir = FindSolutionDirectory();
        Assert.NotNull(solutionDir);

        string mainXamlPath = Path.Combine(solutionDir, "Reframe", "MainWindow.xaml");
        Assert.True(File.Exists(mainXamlPath), $"MainWindow.xaml not found at {mainXamlPath}");

        string content = File.ReadAllText(mainXamlPath);
        Assert.Contains("x:Key=\"SecondaryToolButtonStyle\"", content);
    }

    [Fact]
    public void MainWindowXaml_AllStaticResources_ExistInAppOrWindowResources()
    {
        string? solutionDir = FindSolutionDirectory();
        Assert.NotNull(solutionDir);

        string appXamlPath = Path.Combine(solutionDir, "Reframe", "App.xaml");
        string mainXamlPath = Path.Combine(solutionDir, "Reframe", "MainWindow.xaml");

        Assert.True(File.Exists(appXamlPath));
        Assert.True(File.Exists(mainXamlPath));

        string appXaml = File.ReadAllText(appXamlPath);
        string mainXaml = File.ReadAllText(mainXamlPath);

        // Find all StaticResource references in MainWindow.xaml
        var matches = Regex.Matches(mainXaml, @"\{StaticResource\s+([A-Za-z0-9_]+)\}");
        foreach (Match match in matches)
        {
            string resourceKey = match.Groups[1].Value;

            // Should exist as x:Key="resourceKey" in either App.xaml or MainWindow.xaml
            bool existsInApp = appXaml.Contains($"x:Key=\"{resourceKey}\"");
            bool existsInMain = mainXaml.Contains($"x:Key=\"{resourceKey}\"");

            Assert.True(existsInApp || existsInMain,
                $"StaticResource '{resourceKey}' used in MainWindow.xaml but not found with x:Key in App.xaml or MainWindow.xaml");
        }
    }

    private static string? FindSolutionDirectory()
    {
        string? current = AppContext.BaseDirectory;
        while (current != null)
        {
            if (File.Exists(Path.Combine(current, "reframe.sln")))
            {
                return current;
            }
            current = Directory.GetParent(current)?.FullName;
        }
        return null;
    }
}
