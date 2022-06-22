using Esri.ArcGISRuntime.Data;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
using Windows.System;
using ArcGISRuntimeDesktop.Helpers;

namespace ArcGISRuntimeDesktop.Controls;
public sealed partial class AddDataView : UserControl
{
    public AddDataView()
    {
        this.InitializeComponent();
    }

    private async void AgoSearchTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        var user = ApplicationViewModel.Instance.PortalUser;
        if (user is null) return;
        if (e.Key == VirtualKey.Enter)
        {
            var parameters = PortalQueryParameters.CreateForItemsOfTypes(new PortalItemType[]
            { PortalItemType.SceneService, PortalItemType.MapService, PortalItemType.FeatureCollection, PortalItemType.FeatureService,
             PortalItemType.BuildingSceneLayer, PortalItemType.ImageService, PortalItemType.KML, PortalItemType.Layer, PortalItemType.MobileBasemapPackage, PortalItemType.MobileMapPackage, PortalItemType.MobileScenePackage,
             PortalItemType.OGCFeatureServer, PortalItemType.ScenePackage, PortalItemType.ShapeFile, PortalItemType.SQLiteGeodatabase, PortalItemType.VectorTilePackage, PortalItemType.VectorTileService, PortalItemType.WebMap, PortalItemType.WebScene,
             PortalItemType.WFS, PortalItemType.WMS, PortalItemType.WMTS }, search:AgoSearchTextBox.Text);
            try
            {
                var items = await user.Portal.FindItemsAsync(parameters);
                AgoSearchResults.ItemsSource = new PortalQuerySource(user.Portal, items);
            }
            catch { }
        }
    }
    private class PortalQuerySource : ObservableCollection<PortalItem>, ISupportIncrementalLoading
    {
        private PortalQueryResultSet<PortalItem> _result;
        private readonly ArcGISPortal _portal;
        public PortalQuerySource(ArcGISPortal portal, PortalQueryResultSet<PortalItem> result)
        {
            _portal = portal;
            _result = result;
            foreach (var item in result.Results)
                base.Items.Add(item);
            
        }
        public bool HasMoreItems => _result.NextQueryParameters is not null;

        public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count) => LoadMoreItemsTaskAsync(count).AsAsyncOperation();

        private async Task<LoadMoreItemsResult> LoadMoreItemsTaskAsync(uint count)
        {
            LoadMoreItemsResult loadMoreItemsResult = new LoadMoreItemsResult() { Count = 0 };
            if (_result.NextQueryParameters is not null)
            {
                int index = this.Items.Count;
                var query = _result.NextQueryParameters;
                query.Limit = (int)count;
                PortalQueryResultSet<PortalItem> result;
                try
                {
                    result = await _portal.FindItemsAsync(query);
                }
                catch (Exception)
                {
                    return loadMoreItemsResult;
                }
                if (result.Results.Any())
                {
                    var list = result.Results?.ToList();
                    if (list != null)
                    {
                        foreach (var item in list)
                            base.Items.Add(item);
                        OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Add, changedItems: list, index));
                        _result = result;
                        loadMoreItemsResult.Count = (uint)list.Count;
                    }
                }
            }
            return loadMoreItemsResult;
        }
    }

    private async void MyPortalItemsView_Loaded(object sender, RoutedEventArgs e)
    {

        var user = ApplicationViewModel.Instance.PortalUser;
        if (user is null) return;
        try
        {
            var items = await user.GetContentAsync();
            MyPortalItemsView.ItemsSource = items.Items;
        }
        catch { }
    }

    private void AddAGODataClick(object sender, RoutedEventArgs e)
    {
        var item = ((Button)sender).CommandParameter as PortalItem;
        if (item is null) return;
        if (item.TryCreateDocument(out var doc))
        {
            ApplicationViewModel.Instance.AddDocument(doc);
            ApplicationViewModel.Instance.ActiveDocument = doc;
        }
        else if (ApplicationViewModel.Instance.ActiveDocument != null && item.TryCreateLayer(out var layer))
        {
            ApplicationViewModel.Instance.ActiveDocument.AddLayer(layer);
        }
    }
}

