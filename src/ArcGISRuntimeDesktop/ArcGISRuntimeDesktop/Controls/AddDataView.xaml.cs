using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
using Windows.System;

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
            var parameters = new PortalQueryParameters(AgoSearchTextBox.Text);
            var items = await user.Portal.FindItemsAsync(parameters);
            AgoSearchResults.ItemsSource = new PortalQuerySource(user.Portal, items);
            
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
}

