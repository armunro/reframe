using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace Reframe;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ApplicationThemeManager.Apply(ApplicationTheme.Dark);
        ApplicationAccentColorManager.Apply(
            Color.FromRgb(0x00, 0x78, 0xD4),
            ApplicationTheme.Dark
        );
    }
}
