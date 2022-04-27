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
        var map = new Map(Basemap.CreateLightGrayCanvas());
        KmlDataset dataset = new KmlDataset(new Uri("https://www.arcgis.com/sharing/rest/content/items/600748d4464442288f6db8a4ba27dc95/data"));
        KmlLayer fileLayer = new KmlLayer(dataset);
        map.OperationalLayers.Add(fileLayer);
        ViewModel.AddDocument(new MapDocument("Airplanes", map));
        ViewModel.AddDocument(new SceneDocument());
        _ = ViewModel.AddDocument("92263bc0cc69466386bcb9846e70d080");
        ViewModel.AppSettings.PropertyChanged += AppSettings_PropertyChanged;
        if (!ViewModel.AppSettings.IsSidePanePinned)
        {
            splitViewPane.DisplayMode = SplitViewDisplayMode.CompactOverlay;
            splitViewPane.IsPaneOpen = false;
        }
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
