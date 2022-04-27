using Microsoft.UI.Windowing;
using Windows.UI;

namespace ArcGISRuntimeDesktop.Windows
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SignInWindow : WinUIEx.WindowEx
    {
        public SignInWindow()
        {
            this.InitializeComponent();
            this.Width = 640;
            this.Height = 480;
            this.IsResizable = false;
            this.Closed += (s, e) => tcs.TrySetCanceled();
            this.AppWindow.Title = "ArcGIS Desktop";
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                this.AppWindow.TitleBar.InactiveBackgroundColor =
                    this.AppWindow.TitleBar.BackgroundColor =
                    this.AppWindow.TitleBar.ButtonBackgroundColor =
                    this.AppWindow.TitleBar.ButtonInactiveBackgroundColor =
                    Color.FromArgb(255, 19, 13, 78);
                this.AppWindow.TitleBar.ForegroundColor =
                this.AppWindow.TitleBar.ButtonForegroundColor =
                    Microsoft.UI.Colors.White;
            }
            this.Closed += SignInWindow_Closed;
            WinUIEx.WindowExtensions.CenterOnScreen(this);
        }

        private void SignInWindow_Closed(object sender, WindowEventArgs args)
        {
            tcs.TrySetCanceled();
        }

        private async Task SignIn()
        {
            var serviceUri = ApplicationViewModel.Instance.AppSettings.PortalUrl;
         
            try
            {
                status.Text = "Waiting for sign in... Check your browser.";
                ArcGISPortal arcgisPortal = await ArcGISPortal.CreateAsync(serviceUri, true);
                tcs.TrySetResult(arcgisPortal.User!);
                status.Text = string.Empty;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                status.Text = "Failed to sign in: " + ex.Message;
            }
        }

        private void SignIn_Click(object sender, RoutedEventArgs e)
        {
            _ = SignIn();
        }

        private readonly TaskCompletionSource<PortalUser> tcs = new TaskCompletionSource<PortalUser>();
        public Task<PortalUser> SignInAsync() => tcs.Task;
    }
}
