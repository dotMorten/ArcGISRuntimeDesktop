using Esri.ArcGISRuntime.Ogc;
using Esri.ArcGISRuntime.UI;

namespace ArcGISRuntimeDesktop.ViewModels;
public class MapDocument : Document
{
    private static Map CreateDefaultMap()
    {
        var map = new Map(BasemapStyle.ArcGISStreets);
        if (ApplicationViewModel.Instance.Basemaps?.FirstOrDefault(f => f.Item?.Name?.EndsWith("_Streets") == true) is Basemap bm)
        {
            map = new Map(bm);
        }
        return map;
    }
    public MapDocument(Map model) : this(model.Item?.Name ?? string.Empty,  model) { }

    public MapDocument(string name) : this(name, CreateDefaultMap())
    {
    }

    public MapDocument(string name, Map map) : base(name, map)
    {
    }

    public Map Map => (Map)GeoDocument;

    public override bool Is3D => false;

    private LocationDisplay? _locationDisplay;

    public LocationDisplay? ActiveLocationDisplay
    {
        get { return _locationDisplay; }
        set { _locationDisplay = value; OnPropertyChanged(); }
    }

}
