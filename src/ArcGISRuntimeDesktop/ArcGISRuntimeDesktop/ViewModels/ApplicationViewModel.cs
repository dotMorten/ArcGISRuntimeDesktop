using Esri.ArcGISRuntime;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

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
        var doc = new MapDocument();
        AddDocument(doc);
        ActiveDocument = doc;
    }
    public void NewSceneDocument()
    {
        var doc = new SceneDocument();
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
        set
        {
            _PortalUser = value; OnPropertyChanged();
            AppSettings.PortalUser = value?.FullName;
            PortalUserThumbnail = null;
            OnPropertyChanged(nameof(PortalUserThumbnail));
            if (value != null)
            {
                RefreshUserThumbnail(value);
                RefreshBasemaps(value.Portal);
            }
        }
    }

    private async void RefreshBasemaps(ArcGISPortal value)
    {
        try { 
        Basemaps = await value.GetDeveloperBasemapsAsync();
        OnPropertyChanged(nameof(Basemaps)); OnPropertyChanged(nameof(PortalUserThumbnail));
        }
        catch
        {
        }
    }

    private async void RefreshUserThumbnail(PortalUser value)
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
