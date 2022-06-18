using System.Linq;
using WinUIEx;

namespace ArcGISRuntimeDesktop.Windows;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SplashScreen : WinUIEx.SplashScreen 
{
    public SplashScreen(Type window) : base(window)
    {
        IsAlwaysOnTop = false;
        this.InitializeComponent();
    }

    protected override async Task OnLoading()
    {
        WindowExtensions.SetForegroundWindow(this);
        AppInitializer initializer = new AppInitializer();
        initializer.ProgressChanged += (s, progress) => this.progress.Value = progress;
        initializer.StatusTextChanged += (s, status) => this.status.Text = status;
        try
        {
            await initializer.Initialize(this);
        }
        catch(System.Exception ex)
        {
            var dialog = WinUIEx.WindowExtensions.CreateMessageDialog(this, "Error initializing", ex.Message);
            await dialog.ShowAsync();
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
    }
}
