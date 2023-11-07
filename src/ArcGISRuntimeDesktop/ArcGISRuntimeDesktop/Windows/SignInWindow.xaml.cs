using Microsoft.UI.Windowing;
using Windows.UI;
using WinUIEx;

namespace ArcGISRuntimeDesktop.Windows
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SignInWindow : Window
    {
        public SignInWindow()
        {
            this.InitializeComponent();

            //this.ExtendsContentIntoTitleBar = true;
            this.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            var manager = WindowManager.Get(this);
            manager.Width = 640;
            manager.Height = 400;
            manager.IsMaximizable = false;
            manager.IsMinimizable = false;
            manager.IsResizable = false;
            this.Closed += (s, e) => tcs.TrySetCanceled();
            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                this.AppWindow.TitleBar.InactiveBackgroundColor =
                    this.AppWindow.TitleBar.BackgroundColor =
                    this.AppWindow.TitleBar.ButtonBackgroundColor =
                    this.AppWindow.TitleBar.ButtonInactiveBackgroundColor =
                    Color.FromArgb(0, 19, 13, 78);
                this.AppWindow.TitleBar.ForegroundColor =
                this.AppWindow.TitleBar.ButtonForegroundColor =
                    Microsoft.UI.Colors.White;
                this.AppWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
            }
            this.Closed += SignInWindow_Closed;
            WinUIEx.WindowExtensions.SetIcon(this, new Microsoft.UI.IconId() { Value = 0 });
            this.CenterOnScreen();
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
