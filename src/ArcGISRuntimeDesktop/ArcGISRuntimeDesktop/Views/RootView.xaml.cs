using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Ogc;
using Esri.ArcGISRuntime.UI.Controls;
using Microsoft.UI.Xaml.Input;

namespace ArcGISRuntimeDesktop.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class RootView : Page
{
    public RootView()
    {
        this.InitializeComponent();
        ViewModel.LoadDocuments();
        this.Loaded += RootView_Loaded;
        ViewModel.AppSettings.PropertyChanged += AppSettings_PropertyChanged;
        if (!ViewModel.AppSettings.IsSidePanePinned)
        {
            splitViewPane.DisplayMode = SplitViewDisplayMode.CompactOverlay;
            splitViewPane.IsPaneOpen = false;
        }
    }

    private void RootView_Loaded(object sender, RoutedEventArgs e)
    {
        ((FrameworkElement)XamlRoot.Content).RequestedTheme = ViewModel.AppSettings.Theme;
    }

    private void AppSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppSettings.IsSidePanePinned))
        {
            //TODO: change to converter
            if (ViewModel.AppSettings.IsSidePanePinned)
                splitViewPane.DisplayMode = SplitViewDisplayMode.Inline;
            else
                splitViewPane.DisplayMode = SplitViewDisplayMode.CompactOverlay;
        }
        else if (e.PropertyName == nameof(AppSettings.Theme))
        {
            ((FrameworkElement)XamlRoot.Content).RequestedTheme = ViewModel.AppSettings.Theme;
        }
    }

    public ApplicationViewModel ViewModel => ApplicationViewModel.Instance;

    private void Basemap_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var pi = ((GridView)sender).SelectedItem as Basemap;
        if (pi != null && pi.Item != null && ViewModel.ActiveDocument != null)
            ViewModel.ActiveDocument.GeoDocument.Basemap = new Basemap(pi.Item);
    }

    private void SidePane_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        splitViewPane.IsPaneOpen = true;
    }
}
