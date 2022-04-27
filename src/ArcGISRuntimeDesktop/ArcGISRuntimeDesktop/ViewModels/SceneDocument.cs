namespace ArcGISRuntimeDesktop.ViewModels;

public class SceneDocument : Document
{
    private const string _elevationSourceUrl = "https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer";

    private static Scene CreateDefaultScene()
    {
        var scene = new Scene(Basemap.CreateImageryWithLabelsVector());
        scene.BaseSurface!.ElevationSources.Add(new ArcGISTiledElevationSource(new Uri(_elevationSourceUrl)));
        return scene;
    }
    public SceneDocument() : this("") { }
    
    public SceneDocument(string name) : this(name, CreateDefaultScene()) { }

    public SceneDocument(string name, Scene scene) : base(name)
    {
        Scene = scene;
    }

    public Scene Scene { get; }

    public override GeoModel GeoDocument => Scene;

    public override bool Is3D => true;

    private double _elevationExaggeration;

    public double ElevationExaggeration
    {
        get { return _elevationExaggeration; }
        set { _elevationExaggeration = value; OnPropertyChanged(); }
    }

    private Esri.ArcGISRuntime.UI.LightingMode mode = Esri.ArcGISRuntime.UI.LightingMode.NoLight;

    public Esri.ArcGISRuntime.UI.LightingMode SunLighting
    {
        get { return mode; }
        set { mode = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsSunTimeActive)); }
    }

    private DateTimeOffset? _sunTime;
    public DateTimeOffset SunTime
    {
        get
        {
            if (_sunTime is null)
                _sunTime = DateTimeOffset.Now;
            return _sunTime.Value;
        }
        set { _sunTime = value; OnPropertyChanged(); }
    }
    private Esri.ArcGISRuntime.UI.AtmosphereEffect _atmosphereEffect = Esri.ArcGISRuntime.UI.AtmosphereEffect.HorizonOnly;

    public Esri.ArcGISRuntime.UI.AtmosphereEffect AtmosphereEffect
    {
        get { return _atmosphereEffect; }
        set { _atmosphereEffect = value; OnPropertyChanged(); }
    }

    public bool IsSunTimeActive => SunLighting != Esri.ArcGISRuntime.UI.LightingMode.NoLight;
}
