using ArcGISRuntimeDesktop.ViewModels;
using Esri.ArcGISRuntime.Ogc;

namespace ArcGISRuntimeDesktop.Controls;

public sealed partial class ContentView : UserControl
{
    public ContentView()
    {
        this.InitializeComponent();
    }

    public Document? Document
    {
        get { return (Document)GetValue(DocumentProperty); }
        set { SetValue(DocumentProperty, value); }
    }

    public static readonly DependencyProperty DocumentProperty =
        DependencyProperty.Register("Document", typeof(Document), typeof(ContentView), new PropertyMetadata(null));

    private void contentFlyout_Opening(object sender, object e)
    {
        var flyout = (MenuFlyout)sender;
        flyout.Items.Clear();
        var target = flyout.Target.DataContext as Layer;
        if(target is null)
        {
            return;
        }
        if (target.FullExtent is not null)
        {
            var zoomto = new MenuFlyoutItem() { Text = "Zoom to" };
            zoomto.Click += (s, e) =>
            {
                ApplicationViewModel.Instance.ActiveDocument?.ZoomTo(new Viewpoint(target.FullExtent));
            };
            flyout.Items.Add(zoomto);
        }
        var props = new MenuFlyoutItem() { Text = "Properties" };
        props.Click += (s, e) =>
        {
            AppDialog.Instance.ShowAsync("Layer Properties", new Views.LayerPropertiesView(target));
        };
        flyout.Items.Add(props);
    }
}
public sealed class LayerTemplateSelector : DataTemplateSelector
{
    public DataTemplate? LayerTemplate { get; set; }
    public DataTemplate? KmlNodeTemplate { get; set; }
    public DataTemplate? KmlContainerTemplate { get; set; }
    public DataTemplate? KmlNetworkLinkTemplate { get; set; }
    public DataTemplate? KmlLayerTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        if (item is KmlLayer)
            return KmlLayerTemplate;
        if (item is KmlContainer con && con.ListItemType != KmlListItemType.CheckHideChildren)
            return KmlContainerTemplate;
        if (item is KmlNetworkLink link && link.ListItemType != KmlListItemType.CheckHideChildren)
            return KmlNetworkLinkTemplate;
        if (item is KmlNode node)
            return KmlNodeTemplate;
        return LayerTemplate ?? base.SelectTemplateCore(item);
    }
}