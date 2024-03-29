﻿using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using System.Text;
using Windows.Storage;

namespace ArcGISRuntimeDesktop.ViewModels;

public abstract partial class Document : BaseViewModel
{
    public Document(string name, GeoModel model)
    {
        GeoDocument = model;
        if (model.LoadStatus == Esri.ArcGISRuntime.LoadStatus.Loaded)
            SubscribeToModel();
        else
            model.Loaded += (s, e) => SubscribeToModel();
        Name = name.Trim();
        GraphicsOverlays.Add(SearchResultOverlay);
        GraphicsOverlays.Add(EditorOverlay);
    }

    public GeoModel GeoDocument { get; }

    public GraphicsOverlayCollection GraphicsOverlays { get; } = new GraphicsOverlayCollection();

    public GraphicsOverlay EditorOverlay { get; } = new GraphicsOverlay();

    public GraphicsOverlay SearchResultOverlay { get; } = new GraphicsOverlay();

    public string Name { get; set; }

    public void Close() => ApplicationViewModel.Instance.RemoveDocument(this);

    public bool CanAdd(IEnumerable<IStorageItem> items)
    {
        foreach (var item in items)
        {
            var f = new FileInfo(item.Path);
            var ext = f.Extension.ToLower();
            if (Is3D && (ext == ".slpk" || ext == ".pslpk") ||
                ext == ".shp" || ext == ".geodatabase" || ext == ".kml" || ext == ".kmz" || ext == ".vtpk" || ext == ".tpk" ||
                ext == ".tif" || ext == ".tiff" || ext == ".img" || ext == ".sid")
                return true;
        }
        return false;
    }

    public bool CanAdd(Layer layer)
    {
        if (this is MapDocument && layer is not ArcGISSceneLayer && layer is not IntegratedMeshLayer && layer is not PointCloudLayer)
            return true;
        return true;
    }

    internal async void AddLayer(Layer layer)
    {
        try
        {
            await layer.LoadAsync();
            GeoDocument.OperationalLayers.Add(layer);
            if (layer.FullExtent != null)
                ZoomTo(new Viewpoint(layer.FullExtent));
        }
        catch (System.Exception ex)
        {
            //TODO
            System.Diagnostics.Debug.WriteLine("Failed to load layer: " + ex.Message);
        }
    }

    public class Selection : IDisposable
    {
        public Selection(Feature feature)
        {
            SelectedElement = feature; 
            Owner = feature.FeatureTable?.Layer;
            if (feature.FeatureTable?.Layer is FeatureLayer l)
                l.SelectFeature(feature);
        }
        
        public Selection(Graphic graphic, GraphicsOverlay overlay)
        {
            SelectedElement = graphic; Owner = overlay; graphic.IsSelected = true;
        }

        public GeoElement? SelectedElement { get; set; }
        public bool CanEdit => true;
        public object? Owner { get; set; }
        public Layer? OwnerLayer => Owner as Layer;
        public GraphicsOverlay? OwnerGraphicsOverlay=> Owner as GraphicsOverlay;

        public void Dispose()
        {
            if (SelectedElement is Graphic g)
                g.IsSelected = false;
            else if(SelectedElement is Feature feature)
                (feature.FeatureTable?.Layer as FeatureLayer)?.UnselectFeature(feature);
        }
    }

    public bool CanEdit => CurrentSelection?.CanEdit == true;

    private Selection? _selection;

    public Selection? CurrentSelection
    {
        get { return _selection; }
        set
        {
            _selection?.Dispose();
            _selection = value;
            base.OnPropertyChanged();
            OnPropertyChanged(nameof(CanEdit));
        }
    }


    public void ZoomTo(Viewpoint viewpoint)
    {
        ViewpointRequested?.Invoke(this, viewpoint);
    }

    public event EventHandler<Viewpoint>? ViewpointRequested;
    public Func<Viewpoint?>? GetCurrentViewpoint;

    public async Task<Envelope?> Add(IEnumerable<IStorageItem> items)
    {
        List<Layer> layers = new List<Layer>();
        foreach (var item in items)
        {
            var layer = GetLayer(item);
            if (layer != null)
                layers.Add(layer);
        }
        Envelope? extent = null;
        foreach (var layer in layers)
        {
            try
            {
                await layer.LoadAsync();
                GeoDocument.OperationalLayers.Add(layer);
                if (extent is null)
                    extent = layer.FullExtent;
                else if (layer.FullExtent is not null && extent.SpatialReference is not null)
                {
                    var newExtent = GeometryEngine.Project(layer.FullExtent, extent.SpatialReference)?.Extent;
                    if (newExtent is not null)
                    {
                        extent = new Envelope(Math.Min(extent.XMin, newExtent.XMin), Math.Min(extent.YMin, newExtent.YMin), Math.Max(extent.XMax, newExtent.XMax), Math.Max(extent.YMax, newExtent.YMax), extent.SpatialReference ?? newExtent.SpatialReference);
                    }
                }
            }
            catch (Exception)
            {
            }
        }
        return extent;
    }

    private Layer? GetLayer(IStorageItem storageItem)
    {
        var f = new FileInfo(storageItem.Path);
        var ext = f.Extension.ToLower();
        switch (ext)
        {
            case ".shp":
                return new FeatureLayer(new Esri.ArcGISRuntime.Data.ShapefileFeatureTable(f.FullName));
            case ".slpk":
                return new ArcGISSceneLayer(new Uri(f.FullName));
            case ".pslpk":
                return new PointCloudLayer(new Uri(f.FullName));
            case ".kml":
            case ".kmz":
                return new KmlLayer(new Uri(f.FullName));
            case ".vtpk":
                return new ArcGISVectorTiledLayer(new VectorTileCache(f.FullName));
            case ".tpk":
                return new ArcGISTiledLayer(new TileCache(f.FullName));
            case ".tiff":
            case ".tif":
            case ".img":
            case ".sid":
                return new RasterLayer(new Esri.ArcGISRuntime.Rasters.Raster(f.FullName));
        }
        return null;
    }
    public abstract bool Is3D { get; }
    public bool Is2D => !Is3D;
}

