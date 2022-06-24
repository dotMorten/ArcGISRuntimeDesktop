using Esri.ArcGISRuntime;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;

namespace ArcGISRuntimeDesktop.ViewModels;

public class ApplicationViewModel : BaseViewModel
{
    public static ApplicationViewModel Instance { get; } = new ApplicationViewModel();

    private ApplicationViewModel()
    {
        Documents.CollectionChanged += Documents_CollectionChanged;
        _ = LocationDataSource.StartAsync();
    }

    private void Documents_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (_activeDocument is null && e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && e.NewItems is not null && e.NewItems.Count > 0)
            ActiveDocument = (Document)e.NewItems[0]!;
        else if (_activeDocument is null || !Documents.Contains(_activeDocument))
            ActiveDocument = Documents.FirstOrDefault();
    }

    public ObservableCollection<Document> Documents { get; } = new ObservableCollection<Document>();

    internal void OnShutdown()
    {
        var documents = ApplicationData.Current.LocalSettings.CreateContainer("Documents", ApplicationDataCreateDisposition.Always);
        documents.Values.Clear();
        int i = 0;
        foreach (var doc in Documents)
        {
            if (doc.TrySerialize(out string data))
            {
                documents.Values.Add((i++).ToString("0000"), data);
            }
        }
    }

    internal async void LoadDocuments()
    {
        var documents = ApplicationData.Current.LocalSettings.CreateContainer("Documents", ApplicationDataCreateDisposition.Always);
        foreach(var key in documents.Values.Keys.OrderBy(k=>k))
        {
            try
            {
                var doc = await Document.CreateFromSerialized((string)documents.Values[key]);
                if (doc != null)
                    AddDocument(doc);
            }
            catch { }
        }
        if (Documents.Count == 0)
        {
            // Create default set of documents
            var map = new Map(new Basemap(Basemaps!.First().Item!));
            var dataset = new Esri.ArcGISRuntime.Ogc.KmlDataset(new Uri("https://www.arcgis.com/sharing/rest/content/items/600748d4464442288f6db8a4ba27dc95/data"));
            KmlLayer fileLayer = new KmlLayer(dataset);
            map.OperationalLayers.Add(fileLayer);
            AddDocument(new MapDocument("Airplanes", map));
            AddDocument(new SceneDocument(string.Empty));
            await AddDocument("92263bc0cc69466386bcb9846e70d080");
            await AddDocument("1dcf522a5e23476bbee87ff89849f4c0");
            await AddDocument("62d8c4ad9e104425bff60c0cdd8efaf1");
        }
    }

    internal void AddDocument(Document document)
    {
        if (string.IsNullOrEmpty(document.Name))
        {
            var prefix = document is MapDocument ? "Map " : "Scene ";
            int counter = 1;
            while (true)
            {
                if (!Documents.Any(d => d.Name == (prefix + counter.ToString())))
                    break;
                counter++;
            }
            document.Name = prefix + counter.ToString();
        }
        Documents.Add(document);
    }

    internal async Task AddDocument(string itemId)
    {
        var item = await PortalItem.CreateAsync(await ArcGISPortal.CreateAsync(), itemId);
        if (item.Type == PortalItemType.WebMap)
            AddDocument(new MapDocument(item.Title, new Map(item)));
        else if (item.Type == PortalItemType.WebScene)
            AddDocument(new SceneDocument(item.Title, new Scene(item)));
    }

    private Document? _activeDocument;

    public Document? ActiveDocument
    {
        get { return _activeDocument; }
        set { _activeDocument = value; OnPropertyChanged(); }
    }

    public void NewMapDocument()
    {
        var doc = new MapDocument("");
        AddDocument(doc);
        ActiveDocument = doc;
    }
    public void NewSceneDocument()
    {
        var doc = new SceneDocument("");
        AddDocument(doc);
        ActiveDocument = doc;
    }

    public void RemoveDocument(Document? document)
    {
        if (document is not null && Documents.Contains(document))
            Documents.Remove(document);
    }

    private LocationDataSource _LocationDataSource = LocationDataSource.CreateDefault();

    public LocationDataSource LocationDataSource
    {
        get => _LocationDataSource;
        set { _LocationDataSource = value; OnPropertyChanged(); }
    }

    public AppSettings AppSettings { get; } = new AppSettings();

    private PortalUser? _PortalUser;

    public PortalUser? PortalUser
    {
        get { return _PortalUser; }
        private set
        {
            _PortalUser = value; OnPropertyChanged();
            AppSettings.PortalUser = value?.FullName;
            
           
        }
    }

    public async Task SetUserAsync(PortalUser? value)
    {
        PortalUserThumbnail = null;
        OnPropertyChanged(nameof(PortalUserThumbnail));
        PortalUser = value;
        if (value != null)
        {
            try
            {
                await RefreshUserThumbnail(value);
                await RefreshBasemaps(value.Portal);
            }
            catch { }
        }
    }

    private async Task RefreshBasemaps(ArcGISPortal value)
    {
        try { 
        Basemaps = await value.GetDeveloperBasemapsAsync();
        OnPropertyChanged(nameof(Basemaps)); OnPropertyChanged(nameof(PortalUserThumbnail));
        }
        catch
        {
        }
    }

    private async Task RefreshUserThumbnail(PortalUser value)
    {
        try
        {
            using var stream = await value.GetThumbnailDataAsync();
            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(stream.AsRandomAccessStream());
            PortalUserThumbnail = bitmap;
            OnPropertyChanged(nameof(PortalUserThumbnail));
        }
        catch
        {
        }
    }

    public IEnumerable<Basemap>? Basemaps { get; private set; }

    public ImageSource? PortalUserThumbnail { get; private set; }

    public async Task SignOut()
    {
        PortalUser = null;
        await Esri.ArcGISRuntime.Security.AuthenticationManager.Current.RemoveAndRevokeAllCredentialsAsync();
    }
}
