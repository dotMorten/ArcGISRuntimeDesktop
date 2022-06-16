using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.UI;

namespace ArcGISRuntimeDesktop.Windows;

public sealed partial class MainWindow : WinUIEx.WindowEx
{
    public MainWindow()
    {
        this.PersistenceId = "MainWindow";
        this.InitializeComponent();
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            AppWindow.TitleBar.BackgroundColor = Color.FromArgb(0xFF, 0x00, 0x7A, 0xC2);
            AppWindow.TitleBar.ForegroundColor = Colors.White;
            AppWindow.TitleBar.ButtonBackgroundColor = AppWindow.TitleBar.BackgroundColor;
        }
    }
}
