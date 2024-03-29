﻿using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using Microsoft.UI.Xaml.Input;

namespace ArcGISRuntimeDesktop.Controls;

public sealed partial class DocumentView : UserControl
{
    public DocumentView()
    {
        this.InitializeComponent();
    }

    public Document? Document
    {
        get { return (Document)GetValue(DocumentProperty); }
        set { SetValue(DocumentProperty, value); }
    }

    public static readonly DependencyProperty DocumentProperty =
        DependencyProperty.Register(nameof(Document), typeof(Document), typeof(DocumentView), new PropertyMetadata(null, (s,e) => ((DocumentView)s).OnDocumentPropertyChanged(e)));

    private void OnDocumentPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        var document = e.NewValue as Document;
        if (e.NewValue is MapDocument mapDocument)
        {
            ContentArea.ContentTemplate = (DataTemplate)Resources["mapTemplate"];
        }
        else if (e.NewValue is SceneDocument sceneDocument)
        {
            ContentArea.ContentTemplate = (DataTemplate)Resources["sceneTemplate"];
        }
        else
            ContentArea.Content = null;
        if (e.OldValue is Document old)
        {
            old.GetCurrentViewpoint = null;
            old.ViewpointRequested -= Document_ViewpointRequested;
        }
        if (document != null)
        {
            document.ViewpointRequested += Document_ViewpointRequested;
            document.GetCurrentViewpoint = () =>
            {
                var vp = activeView?.GetCurrentViewpoint(ViewpointType.CenterAndScale);
                if(vp is null) return null;
                if (activeView is SceneView sv && vp.Camera is null)
                    vp = new Viewpoint((MapPoint)vp.TargetGeometry, vp.TargetScale, vp.Rotation, sv.Camera);
                return vp;
            };
        }
    }

    private void Document_ViewpointRequested(object? sender, Viewpoint e)
    {
        var g = ContentArea.Content as GeoView;
        _ = activeView?.SetViewpointAsync(e);
    }

    private void GeoView_PointerMoved(object? sender, PointerRoutedEventArgs e)
    {
        MapPoint? point = null;
        if (sender is MapView mv)
        {
            point = mv.ScreenToLocation(e.GetCurrentPoint(mv).Position);
        }
        else if (sender is SceneView sv)
        {
            point = sv.ScreenToBaseSurface(e.GetCurrentPoint(sv).Position);
        }
        if (point != null && !point.IsEmpty)
            PointerPosition.Text = CoordinateFormatter.ToLatitudeLongitude(point, LatitudeLongitudeFormat.DecimalDegrees, 6);
        else
            PointerPosition.Text = String.Empty;
    }
    
    private async void GeoView_Drop(object sender, DragEventArgs e)
    {
        if (Document is null)
            return;
        GeoView geoView = (GeoView)sender;
        var deferral = e.GetDeferral();
        
        Envelope? extent = null;
        try
        {
            var items = await e.DataView.GetStorageItemsAsync();
            extent = await Document.Add(items);
        }
        finally
        {
            deferral.Complete();
        }
        if(extent != null)
            _ = geoView.SetViewpointAsync(new Viewpoint(extent));
    }

    private async void GeoView_DragOver(object sender, DragEventArgs e)
    {
        var dv = e.DataView;
        if (dv is null)
            return;
        var deferral = e.GetDeferral();
        try
        {
            var items = await dv.GetStorageItemsAsync();
            if(Document?.CanAdd(items) == true)
                e.AcceptedOperation = global::Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
        }
        finally
        {
            deferral.Complete();
        }
    }

    private GeoView? activeView;

    private void GeoView_Loaded(object sender, RoutedEventArgs e)
    {
        activeView = sender as GeoView;
        if(activeView is MapView mapView)
        {
            mapView.LocationDisplay.DataSource = ApplicationViewModel.Instance.LocationDataSource;
            if (Document is MapDocument mapdoc)
                mapdoc.ActiveLocationDisplay = mapView.LocationDisplay;
        }
        editorToolbar.GeoView = activeView;
    }

    private async void GeoView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var geoview = (GeoView)sender;
        var pos = e.GetPosition(geoview);
        flyout.Items.Clear();
        //MenuFlyout f = new MenuFlyout();
        //f.XamlRoot = geoview.XamlRoot;
        //geoview.ContextFlyout = f;
        if (geoview is MapView mapview && mapview.GeometryEditor?.IsStarted == true)
        {
            flyout.Items.Add(new MenuFlyoutItem()
            {
                Text = "Complete drawing"
            });
            ((MenuFlyoutItem)flyout.Items[0]).Click += (s,e) => mapview.GeometryEditor.Stop();
            if(mapview.GeometryEditor.SelectedElement is Esri.ArcGISRuntime.UI.Editing.GeometryEditorVertex)
            {
                flyout.Items.Add(new MenuFlyoutItem()
                {
                    Text = "Delete selected vertex"
                });
                ((MenuFlyoutItem)flyout.Items.Last()).Click += (s, e) => mapview.GeometryEditor.DeleteSelectedElement();
            }
            if (mapview.GeometryEditor.SelectedElement is Esri.ArcGISRuntime.UI.Editing.GeometryEditorPart)
            {
                flyout.Items.Add(new MenuFlyoutItem()
                {
                    Text = "Delete select part"
                });
                ((MenuFlyoutItem)flyout.Items.Last()).Click += (s, e) => mapview.GeometryEditor.DeleteSelectedElement();
            }
            if (mapview.GeometryEditor.SelectedElement is Esri.ArcGISRuntime.UI.Editing.GeometryEditorGeometry)
            {
                flyout.Items.Add(new MenuFlyoutItem()
                {
                    Text = "Delete geometry"
                });
                ((MenuFlyoutItem)flyout.Items.Last()).Click += (s, e) => mapview.GeometryEditor.DeleteSelectedElement();
            }
            if (mapview.GeometryEditor.CanUndo)
            {
                flyout.Items.Add(new MenuFlyoutItem()
                {
                    Text = "Undo"
                });
                ((MenuFlyoutItem)flyout.Items.Last()).Click += (s, e) => mapview.GeometryEditor.Undo();
            }
            if (mapview.GeometryEditor.CanRedo)
            {
                flyout.Items.Add(new MenuFlyoutItem()
                {
                    Text = "Redo"
                });
                ((MenuFlyoutItem)flyout.Items.Last()).Click += (s, e) => mapview.GeometryEditor.Redo();
            }
        }
        else {
            flyout.Items.Add(new MenuFlyoutItem() { Text = "Identify" /*LocalizedStrings.GetString("GeoView_ContextMenu_Identify")*/ });

            //flyout.Items.Add(new MenuFlyoutItem() { Text = "Select", IsEnabled = false });
            //flyout.Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Auto;


            ((MenuFlyoutItem)flyout.Items[0]).Click += async (s, e) =>
            {
                try
                {
                    var result = await geoview.IdentifyLayersAsync(pos, 2, false, 1);
                    var elm = result.FirstOrDefault()?.GeoElements?.FirstOrDefault();
                    if (elm != null)
                    {
                        if (elm is Esri.ArcGISRuntime.ILoadable iload)
                            await iload.LoadAsync();
                        var mp = elm.Geometry as MapPoint;
                        if (mp is null)
                            mp = (geoview as MapView)?.ScreenToLocation(pos) ?? (geoview as SceneView)?.ScreenToBaseSurface(pos);
                        if (mp is not null)
                        {
                            var popup = new GeoElementView() { Element = elm };
                            popup.OnFeatureEditRequested += (s, f) => this.Document.CurrentSelection = new Document.Selection(f);
                            popup.Close += (s, e) => geoview.DismissCallout();
                            geoview.ShowCalloutAt(mp, popup);
                        }
                    }
                    else
                    {
                        MapPoint? loc = (geoview as MapView)?.ScreenToLocation(pos) ?? await ((geoview as SceneView)?.ScreenToLocationAsync(pos) ?? Task.FromResult<MapPoint?>(null));
                        if (loc != null)
                        {
                            string coordinate = CoordinateFormatter.ToLatitudeLongitude(loc, LatitudeLongitudeFormat.DegreesDecimalMinutes, 3);
                            if (loc.HasZ)
                            {
                                coordinate += " Z=" + loc.Z.ToString("0.###");
                            }
                            geoview.ShowCalloutAt(loc, new Esri.ArcGISRuntime.UI.CalloutDefinition(coordinate));
                        }
                    }
                }
                catch { }
            };
        }
        flyout.ShowAt(geoview, pos);
    }

    TaskCompletionSource EditFeatureCompletion;
    private void EditFeature(Feature feature)
    {
        EditFeatureCompletion?.SetResult();
        var source = EditFeatureCompletion = new TaskCompletionSource();
        
        if (feature.FeatureTable.Layer is FeatureLayer flayer)
        {
            flayer.SetFeatureVisible(feature, false);
            //flayer.ClearSelection();
            //flayer.SelectFeature(feature);
            Graphic g = new Graphic(feature.Geometry, flayer.Renderer.GetSymbol(feature, true));
            var editOverlay = new GraphicsOverlay();
            editOverlay.Graphics.Add(g);
            var overlays = activeView.GraphicsOverlays;
            overlays.Add(editOverlay);
            source.Task.ContinueWith((t) =>
            {
                overlays.Remove(editOverlay);
                flayer.SetFeatureVisible(feature, true);
            });
            if (this.activeView is MapView mv)
            {
                mv.GeometryEditor.Start(feature.Geometry);
            }
        }
    }
}
