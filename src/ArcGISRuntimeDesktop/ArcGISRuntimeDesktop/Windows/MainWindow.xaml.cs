using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.UI;

namespace ArcGISRuntimeDesktop.Windows;

public sealed partial class MainWindow : WinUIEx.WindowEx
{
    public MainWindow()
    {
        this.Width = ApplicationViewModel.Instance.AppSettings.WindowWidth;
        this.Height = ApplicationViewModel.Instance.AppSettings.WindowHeight;
        this.PersistenceId = "MainWindow";
        this.InitializeComponent();
        this.SizeChanged += MainWindow_SizeChanged;
        this.PositionChanged += MainWindow_PositionChanged;
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            AppWindow.TitleBar.BackgroundColor = Color.FromArgb(0xFF, 0x00, 0x7A, 0xC2);
            AppWindow.TitleBar.ForegroundColor = Colors.White;
            AppWindow.TitleBar.ButtonBackgroundColor = AppWindow.TitleBar.BackgroundColor;
        }
    }

    private void MainWindow_PositionChanged(object? sender, EventArgs e) => StoreWindowLocation();

    private void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs args) => StoreWindowLocation();

    private void StoreWindowLocation()
    {
        ApplicationViewModel.Instance.AppSettings.WindowWidth = Width;
        ApplicationViewModel.Instance.AppSettings.WindowHeight = Height;
    }
}
