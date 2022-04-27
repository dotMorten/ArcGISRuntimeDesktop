using ArcGISRuntimeDesktop.ViewModels;

namespace ArcGISRuntimeDesktop;

public class DocumentViewTemplateSelector : DataTemplateSelector
{
    public DataTemplate? MapTemplate { get; set; }

    public DataTemplate? SceneTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
    {
        if (item is MapDocument)
            return MapTemplate;
        if (item is SceneDocument)
            return SceneTemplate;
        return base.SelectTemplateCore(item);
    }
}

