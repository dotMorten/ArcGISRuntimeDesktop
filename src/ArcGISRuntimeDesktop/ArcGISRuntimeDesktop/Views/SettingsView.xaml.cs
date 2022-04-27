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

namespace ArcGISRuntimeDesktop.Views
{
    public sealed partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            this.InitializeComponent();
        }

        public ApplicationViewModel VM => ApplicationViewModel.Instance;
        public int ThemeSelectedIndex => (int)VM.AppSettings.Theme;
        private void Theme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var idx = ((ComboBox)sender).SelectedIndex;
            switch (idx)
            {
                case 0: VM.AppSettings.Theme = ElementTheme.Default; break;
                case 1: VM.AppSettings.Theme = ElementTheme.Light; break;
                case 2: VM.AppSettings.Theme = ElementTheme.Dark; break;
            }
        }
    }
}
