using ArcGISRuntimeDesktop.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ArcGISRuntimeDesktop.Controls
{
    public sealed partial class Toolbar : UserControl
    {
        public Toolbar()
        {
            this.InitializeComponent();
        }

        public ViewModels.ApplicationViewModel ViewModel
        {
            get { return (ViewModels.ApplicationViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(ViewModels.ApplicationViewModel), typeof(Toolbar), new PropertyMetadata(null));

        object currentNavigationItem;
        private void NavigationViewControl_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if(args.IsSettingsInvoked)
            {
                sender.SelectedItem = currentNavigationItem;
                AppDialog.Instance.ShowAsync("Settings", new SettingsView() );
            }
            else
                currentNavigationItem = args.InvokedItem;
        }

        private void AddLayer_Click(object sender, RoutedEventArgs e)
        {
            AppDialog.Instance.ShowAsync("Add Layer", new AddDataView());
        }

        private async void SignOut_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = ((App)App.Current).MainWindow!;
                await ApplicationViewModel.Instance.SignOut();
                var signinWindow = new Windows.SignInWindow();
                signinWindow.Activate();
                WinUIEx.WindowExtensions.Hide(mainWindow);
                var user = await signinWindow.SignInAsync();
                ApplicationViewModel.Instance.PortalUser = user;
                WinUIEx.WindowExtensions.Show(mainWindow);
                signinWindow.Close();
            }
            catch (OperationCanceledException)
            {
                App.Current.Exit();
            }
        }
    }
}
