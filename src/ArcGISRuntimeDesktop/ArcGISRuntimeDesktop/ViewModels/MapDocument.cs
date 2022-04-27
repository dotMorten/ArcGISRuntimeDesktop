using Esri.ArcGISRuntime.Ogc;
using Esri.ArcGISRuntime.UI;

namespace ArcGISRuntimeDesktop.ViewModels;
public class MapDocument : Document
{
    private static Map CreateDefaultMap()
    {
        var map = new Map(Basemap.CreateStreetsVector());
        return map;
    }
    public MapDocument() : this(string.Empty) { }

    public MapDocument(string name) : this(name, CreateDefaultMap())
    {
    }

    public MapDocument(string name, Map map) : base(name)
    {
        Map = map;
    }

    public Map Map { get; }

    public override GeoModel GeoDocument => Map;

    public override bool Is3D => false;

    private LocationDisplay? _locationDisplay;

    public LocationDisplay? ActiveLocationDisplay
    {
        get { return _locationDisplay; }
        set { _locationDisplay = value; OnPropertyChanged(); }
    }

}
