using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using System.Text;
using Windows.Storage;

namespace ArcGISRuntimeDesktop.ViewModels;

public abstract partial class Document : BaseViewModel
{
    private void SubscribeToModel()
    {
        GeoDocument.PropertyChanged += Model_PropertyChanged;
        GeoDocument.OperationalLayers.CollectionChanged += OperationalLayers_CollectionChanged;
        if (string.IsNullOrEmpty(GeoDocument.Item?.ItemId))
        {
            changedBasemap = GeoDocument.Basemap?.Item?.ItemId;

            foreach (var layer in GeoDocument.OperationalLayers)
            {
                if (TrySerializeLayer(layer, out string data))
                    layersAdded.Add(data);
            }
        }
    }

    private void OperationalLayers_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems)
            {
                if (item is Layer layer)
                {
                    if (TrySerializeLayer(layer, out string data))
                        layersAdded.Add(data);
                }
            }
        }
    }
    private bool TrySerializeLayer(Layer layer, out string data)
    {
        if (!string.IsNullOrEmpty(layer.Item?.ItemId))
        {
            data = $"{layer.GetType().Name}\titemId={layer.Item.ItemId}";
            return true;
        }
        else if (layer is ArcGISSceneLayer s)
        {
            data = $"{layer.GetType().Name}\tpath={s.Source!.OriginalString}";
            return true;
        }
        else if (layer is IntegratedMeshLayer m)
        {
            data = $"{layer.GetType().Name}\tpath={m.Source!.OriginalString}";
            return true;
        }
        else if (layer is PointCloudLayer p)
        {
            data = $"{layer.GetType().Name}\tpath={p.Source!.OriginalString}";
            return true;
        }
        else if (layer is KmlLayer kml)
        {
            data = $"{layer.GetType().Name}\tpath={kml.Dataset!.Source!.OriginalString}";
            return true;
        }
        else if (layer is FeatureLayer fl)
        {
            if (fl.FeatureTable is ShapefileFeatureTable shp)
            {
                data = $"{layer.GetType().Name}\ttable={shp.GetType().Name}\tpath={shp.Path}";
                return true;
            }
            else
            {
                //TODO
                System.Diagnostics.Debug.WriteLine("Missing serialization for table" + fl.FeatureTable?.GetType().FullName);
            }
        }
        else
        {
            //TODO
            System.Diagnostics.Debug.WriteLine("Missing serialization for " + layer.GetType().FullName);
        }
        data = "";
        return false;
    }

    private void Model_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(GeoModel.Basemap):
                changedBasemap = GeoDocument.Basemap?.Item?.ItemId;
                break;
            default: break;
        }
    }

    private string? changedBasemap = null;
    private List<string> layersAdded = new List<string>();
    public bool TrySerialize(out string str)
    {
        string? itemId = GeoDocument.Item?.ItemId;
        bool result = false;
        StringBuilder sb = new StringBuilder();
        if (!string.IsNullOrEmpty(itemId))
            sb.AppendLine($"itemId:{itemId}");
        if (!string.IsNullOrEmpty(changedBasemap))
            sb.AppendLine($"basemap:{changedBasemap}");
        var vp = GetCurrentViewpoint?.Invoke() ?? GeoDocument.InitialViewpoint;
        if(vp != null)
        {
            sb.AppendLine("viewpoint:" + vp.ToJson());
            if(vp.Camera != null)
                sb.AppendLine($"camera:{vp.Camera.Location.X},{vp.Camera.Location.Y},{vp.Camera.Location.Z},{vp.Camera.Heading},{vp.Camera.Pitch},{vp.Camera.Roll}");
        }
        if (layersAdded != null)
        {
            foreach (var l in layersAdded)
                sb.AppendLine($"layer:{l}");
        }
        str = sb.ToString();
        if (sb.Length > 0)
        {
            str = this.GetType().Name + Environment.NewLine + $"{Name}" + Environment.NewLine + str;
            return true;
        }
        return result;
    }

    public static async Task<Document?> CreateFromSerialized(string data)
    {
        Document? d = null;
        var lines = data.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length > 2)
        {
            string type = lines[0];
            string name = lines[1];
            string? itemId = lines.Where(l => l.StartsWith("itemId:")).FirstOrDefault()?.Substring(7);
            if (type == typeof(SceneDocument).Name)
            {
                if (!string.IsNullOrEmpty(itemId))
                {
                    var item = await PortalItem.CreateAsync(ApplicationViewModel.Instance.PortalUser!.Portal, itemId);
                    d = new SceneDocument(name, new Scene(item));
                }
                else
                    d = new SceneDocument(name, new Scene());
            }
            else if (type == typeof(MapDocument).Name)
            {
                if (!string.IsNullOrEmpty(itemId))
                {
                    var item = await PortalItem.CreateAsync(ApplicationViewModel.Instance.PortalUser!.Portal, itemId);
                    d = new MapDocument(name, new Map(item));
                }
                else
                    d = new MapDocument(name, new Map());
            }
            if (d is null)
                return null;
            string? basemap = lines.Where(l => l.StartsWith("basemap:")).FirstOrDefault()?.Substring(8);
            if (!string.IsNullOrEmpty(basemap))
            {
                var item = await PortalItem.CreateAsync(ApplicationViewModel.Instance.PortalUser!.Portal, basemap);
                d.GeoDocument.Basemap = new Basemap(item);
                d.changedBasemap = basemap;
            }
            string? vp = lines.Where(l => l.StartsWith("viewpoint:")).FirstOrDefault()?.Substring(10);
            if (vp != null)
            {
                try
                {
                    var v = Viewpoint.FromJson(vp);
                    if (v != null)
                    {
                        if(v.Camera is null)
                        {
                            string? cam = lines.Where(l => l.StartsWith("camera:")).FirstOrDefault()?.Substring(7);
                            if (cam != null)
                            {
                                var vals = cam.Split(',').Select(c => double.Parse(c)).ToArray();
                                Camera c = new Camera(new MapPoint(vals[0], vals[1], vals[2], SpatialReferences.Wgs84), vals[3], vals[4], vals[5]);
                                if(v.ViewpointType == ViewpointType.CenterAndScale)
                                    v = new Viewpoint((MapPoint)v.TargetGeometry, v.TargetScale, v.Rotation, c);
                                else
                                    v = new Viewpoint(v.TargetGeometry, v.Rotation, c);
                            }
                        }
                        d.GeoDocument.InitialViewpoint = v;
                    }
                }
                catch { }
            }
            foreach (var addedLayer in lines.Where(l => l.StartsWith("layer:")).Select(l => l.Substring(6)))
            {
                d.layersAdded.Add(addedLayer);
                var parameters = addedLayer.Split('\t');
                itemId = parameters.Where(l => l.StartsWith("itemId:")).FirstOrDefault()?.Substring(7);
                string? path = parameters.Where(l => l.StartsWith("path:")).FirstOrDefault()?.Substring(5);
                PortalItem? item = null;
                if(!string.IsNullOrEmpty(itemId))
                {
                    try
                    {
                        item = await PortalItem.CreateAsync(ApplicationViewModel.Instance.PortalUser!.Portal, itemId);
                    }
                    catch { }
                    if (item is null) continue;
                }
                switch (parameters[0])
                {
                    case nameof(KmlLayer):
                        if (item != null)
                            d.GeoDocument.OperationalLayers.Add(new KmlLayer(item));
                        else if(path != null)
                            d.GeoDocument.OperationalLayers.Add(new KmlLayer(new Uri(path)));
                        break;
                    case nameof(FeatureLayer):
                        if (item != null)
                            d.GeoDocument.OperationalLayers.Add(new FeatureLayer(item));
                        else
                        {
                            //TODO Table ty[e
                        }
                        //else if (path != null)
                        //    d.GeoDocument.OperationalLayers.Add(new FeatureLayer(new Uri(path));
                        break;
                    case nameof(ArcGISSceneLayer):
                        if (item != null)
                            d.GeoDocument.OperationalLayers.Add(new ArcGISSceneLayer(item));
                        else if (path != null)
                            d.GeoDocument.OperationalLayers.Add(new ArcGISSceneLayer(new Uri(path)));
                        break;
                    case nameof(PointCloudLayer):
                        if (item != null)
                            d.GeoDocument.OperationalLayers.Add(new PointCloudLayer(item));
                        else if (path != null)
                            d.GeoDocument.OperationalLayers.Add(new PointCloudLayer(new Uri(path)));
                        break;
                    case nameof(IntegratedMeshLayer):
                        if (item != null)
                            d.GeoDocument.OperationalLayers.Add(new IntegratedMeshLayer(item));
                        else if (path != null)
                            d.GeoDocument.OperationalLayers.Add(new IntegratedMeshLayer(new Uri(path)));
                        break;
                    default:

                        {
                            //TODO
                            System.Diagnostics.Debug.WriteLine("Missing deserialization for " + parameters[0]);
                            break;
                        }
                }
            }
        }
        return d;
    }
}