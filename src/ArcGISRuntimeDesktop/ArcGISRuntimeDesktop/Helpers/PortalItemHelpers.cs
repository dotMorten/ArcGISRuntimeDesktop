using Esri.ArcGISRuntime.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArcGISRuntimeDesktop.Helpers
{
    public static class PortalItemHelpers
    {
        public static bool TryCreateLayer(this PortalItem item, [NotNullWhen(true)] out Layer? layer)
        {
            switch (item.Type)
            {
                case PortalItemType.FeatureService:
                    layer = new FeatureLayer(item);
                    return true;
                case PortalItemType.FeatureCollection:
                    layer = new FeatureCollectionLayer(new FeatureCollection(item));
                    return true;
                case PortalItemType.VectorTileService:
                    layer = new ArcGISVectorTiledLayer(item);
                    return true;
                case PortalItemType.SceneService:
                    {
                        if(item.TypeKeywords.Contains("PointCloud"))
                            layer = new PointCloudLayer(item);
                        else
                            layer = new ArcGISSceneLayer(item);
                    }
                    return true;
                case PortalItemType.WMS:
                    layer = new WmsLayer(item);
                    return true;
                case PortalItemType.KML:
                    layer = new KmlLayer(item);
                    return true;
                // TODO:
                case PortalItemType.MapDocument:
                case PortalItemType.SceneDocument:
                case PortalItemType.MapService:
                case PortalItemType.WMTS:
                    //layer = new WmtsLayer(item);
                    //return true;
                case PortalItemType.WFS:
                    //new WfsFeatureTable(item.ServiceUrl, item.)
                default:
                    System.Diagnostics.Debug.WriteLine($"Type {item.TypeName} not implemented");
                    break;
            }
            layer = null;
            return false;
        }

        public static bool TryCreateDocument(this PortalItem item, [NotNullWhen(true)] out Document? document)
        {
            switch (item.Type)
            {
                case PortalItemType.WebMap:
                    document = new MapDocument(item.Title, new Map(item));
                    return true;
                case PortalItemType.WebScene:
                    document = new SceneDocument(item.Title, new Scene(item));
                    return true;
            }
            document = null;
            return false;
        }
    }
}
