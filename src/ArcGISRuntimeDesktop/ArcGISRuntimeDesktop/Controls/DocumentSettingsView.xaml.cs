using ArcGISRuntimeDesktop.ViewModels;
using Esri.ArcGISRuntime.Ogc;
using System.ComponentModel;

namespace ArcGISRuntimeDesktop.Controls;

public sealed partial class DocumentSettingsView : UserControl
{
    public DocumentSettingsView()
    {
        this.InitializeComponent();
        timeofyear.Date = DateTimeOffset.Now;
    }

    public Document? Document
    {
        get { return (Document)GetValue(DocumentProperty); }
        set { SetValue(DocumentProperty, value); }
    }

    public static readonly DependencyProperty DocumentProperty =
        DependencyProperty.Register("Document", typeof(Document), typeof(ContentView), new PropertyMetadata(null, (s,e) => ((DocumentSettingsView)s).OnDocumentChanged(e.NewValue as Document)));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public MapDocument MapDocument
    {
        get { return (MapDocument)GetValue(MapDocumentProperty); }
        set { SetValue(MapDocumentProperty, value); }
    }

    public static readonly DependencyProperty MapDocumentProperty =
        DependencyProperty.Register("MapDocument", typeof(MapDocument), typeof(DocumentSettingsView), new PropertyMetadata(null));


    [EditorBrowsable(EditorBrowsableState.Never)]
    public SceneDocument SceneDocument
    {
        get { return (SceneDocument)GetValue(SceneDocumentProperty); }
        set { SetValue(SceneDocumentProperty, value); }
    }

    public static readonly DependencyProperty SceneDocumentProperty =
        DependencyProperty.Register("SceneDocument", typeof(SceneDocument), typeof(DocumentSettingsView), new PropertyMetadata(null));


    private void OnDocumentChanged(Document? document)
    {
        MapDocument = document as MapDocument;
        SceneDocument = document as SceneDocument;
    }

    private void UpdateTime()
    {
        if (SceneDocument is null) return;
        var time = timeofyear.Date.Value.Date.AddHours(timeofday.Value);
        SceneDocument.SunTime = time;
    }

    private void SuntimeChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args) => UpdateTime();

    private void SuntimeChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e) => UpdateTime();

}