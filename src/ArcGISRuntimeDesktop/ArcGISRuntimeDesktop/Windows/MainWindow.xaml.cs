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
        try
        {
            TaskBarIcon = WinUIEx.Icon.FromFile(Path.Combine(new FileInfo(System.Environment.ProcessPath!).Directory!.FullName,"Assets/ArcGIS.ico"));
        }
        catch
        {
        }
        WinUIEx.WindowExtensions.SetForegroundWindow(this);
        this.Closed += MainWindow_Closed;
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        ApplicationViewModel.Instance.OnShutdown();
    }
}
