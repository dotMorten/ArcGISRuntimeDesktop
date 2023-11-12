using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Editing;
using Microsoft.UI.Xaml.Data;
using Symbol = Esri.ArcGISRuntime.Symbology.Symbol;

namespace ArcGISRuntimeDesktop.Controls;
public sealed partial class EditTab : UserControl
{
    private readonly MyGeometryEditor editor = new MyGeometryEditor();
    private readonly GeometryEditor reshapeEditor = new GeometryEditor();
    public EditTab()
    {
        reshapeEditor.PropertyChanged += ReshapeEditor_PropertyChanged;
        this.InitializeComponent();
        Binding b = new Binding() { Path = new PropertyPath(nameof(Document) + "." + nameof(Document.CurrentSelection)), Source = this, Mode = BindingMode.OneWay };
        SetBinding(SelectionProperty, b);
        editor.PropertyChanged += Editor_PropertyChanged;
    }

    private void Editor_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(GeometryEditor.CanUndo): UndoCommand.NotifyCanExecuteChanged(); break;
            case nameof(GeometryEditor.CanRedo): RedoCommand.NotifyCanExecuteChanged(); break;
            case nameof(MyGeometryEditor.Geometry):
                {
                    ReshapeCommand.NotifyCanExecuteChanged();
                    break;
                }
        }
    }


    private void ReshapeEditor_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GeometryEditor.Geometry))
            ReshapeAcceptCommand.NotifyCanExecuteChanged();
    }

    public Document Document
    {
        get { return (Document)GetValue(DocumentProperty); }
        set { SetValue(DocumentProperty, value); }
    }

    public static readonly DependencyProperty DocumentProperty =
        DependencyProperty.Register(nameof(Document), typeof(Document), typeof(EditTab), new PropertyMetadata(null, (s, e) => ((EditTab)s).OnDocumentPropertyChanged(e.OldValue as Document, e.NewValue as Document)));

    public Document.Selection Selection
    {
        get { return (Document.Selection)GetValue(SelectionProperty); }
        set { SetValue(SelectionProperty, value); }
    }

    private static readonly DependencyProperty SelectionProperty =
        DependencyProperty.Register(nameof(Selection), typeof(Document.Selection), typeof(EditTab), new PropertyMetadata(null, (s, e) => ((EditTab)s).OnSelectionPropertyChanged()));

    private void OnSelectionPropertyChanged()
    {
        editor.Stop();
        if (Selection?.SelectedElement?.Geometry != null)
        {
            Symbol? symbol = null;
            if (Selection.SelectedElement is Graphic g)
                symbol = g.Symbol ?? Selection.OwnerGraphicsOverlay?.Renderer?.GetSymbol(g);
            else if(Selection.SelectedElement is Feature f)
            {
                symbol = (f.FeatureTable?.Layer as FeatureLayer)?.Renderer?.GetSymbol(f, true) ??
                    (f.FeatureTable as ArcGISFeatureTable)?.LayerInfo?.DrawingInfo?.Renderer?.GetSymbol(f, true);
            }
            editor.Initialize(Selection.SelectedElement.Geometry, symbol);
        }
        EditVerticesCommand.NotifyCanExecuteChanged();
        RotateCommand.NotifyCanExecuteChanged();
        MoveCommand.NotifyCanExecuteChanged();
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
        ReshapeCommand.NotifyCanExecuteChanged();
        ReshapeAcceptCommand.NotifyCanExecuteChanged();
    }

    private void OnDocumentPropertyChanged(Document? oldDocument, Document? newDocument)
    {
        if (oldDocument is MapDocument mapDoc)
        {
            mapDoc.EditorOverlay.Graphics.Clear();
            mapDoc.GeometryEditor = null;
        }
        if (newDocument is MapDocument newMapDoc)
        {
            newMapDoc.GeometryEditor = editor;
        }
    }
}