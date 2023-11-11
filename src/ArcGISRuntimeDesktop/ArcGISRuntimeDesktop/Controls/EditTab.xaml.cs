using CommunityToolkit.Mvvm.Input;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Editing;
using Microsoft.UI.Xaml.Data;
using System.Diagnostics;
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

    private void ReshapeEditor_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GeometryEditor.Geometry))
            ReshapeAcceptCommand.NotifyCanExecuteChanged();
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

    public Document Document
    {
        get { return (Document)GetValue(DocumentProperty); }
        set { SetValue(DocumentProperty, value); }
    }

    public static readonly DependencyProperty DocumentProperty =
        DependencyProperty.Register("Document", typeof(Document), typeof(EditTab), new PropertyMetadata(null, (s, e) => ((EditTab)s).OnDocumentPropertyChanged(e.OldValue as Document, e.NewValue as Document)));

    public Document.Selection Selection
    {
        get { return (Document.Selection)GetValue(SelectionProperty); }
        set { SetValue(SelectionProperty, value); }
    }

    private static readonly DependencyProperty SelectionProperty =
        DependencyProperty.Register("Selection", typeof(Document.Selection), typeof(EditTab), new PropertyMetadata(null, (s, e) => ((EditTab)s).OnSelectionPropertyChanged()));

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
        if (oldDocument is MapDocument mapdoc)
            mapdoc.GeometryEditor = null;
        if (newDocument is MapDocument newmapdoc)
            newmapdoc.GeometryEditor = editor;
    }

    private bool CanEditVertices => Document is MapDocument mapdoc && mapdoc.CurrentSelection?.CanEdit == true && (mapdoc.CurrentSelection?.SelectedElement?.Geometry is Polygon || (mapdoc.CurrentSelection?.SelectedElement?.Geometry is Polyline));

    [RelayCommand(CanExecute = nameof(CanEditVertices))]
    public void EditVertices()
    {
        if(Document is MapDocument mapdoc)
        {
            mapdoc.GeometryEditor = null;
            mapdoc.GeometryEditor = editor;
        }
        SetElementVisibility(false);
        editor.EditVertices();
    }
    private bool CanMove => Document is MapDocument mapdoc && mapdoc.CurrentSelection?.CanEdit == true && mapdoc.CurrentSelection?.SelectedElement?.Geometry is Geometry;

    [RelayCommand(CanExecute = nameof(CanMove))]
    public void Move()
    {
        if (Document is MapDocument mapdoc)
        {
            mapdoc.GeometryEditor = editor;
        }
        SetElementVisibility(false);
        editor.Move();
    }

    private bool CanUndo() => editor.CanUndo;
    [RelayCommand(CanExecute = nameof(CanUndo))]
    public void Undo() => editor.Undo();

    private bool CanRedo() => editor.CanRedo;
    [RelayCommand(CanExecute = nameof(CanRedo))]
    public void Redo() => editor.Undo();

    [RelayCommand(CanExecute = nameof(CanEditVertices))]
    public void Rotate()
    {
        if (Document is MapDocument mapdoc)
        {
            mapdoc.GeometryEditor = editor;
        }
        SetElementVisibility(false);
        editor.Rotate();
    }
    private void SetElementVisibility(bool visible)
    {
        if (Selection.SelectedElement is Graphic g)
            g.IsVisible = visible;
        else if(Selection.SelectedElement is Feature f && f.FeatureTable?.Layer is FeatureLayer l)
        {
            l.SetFeatureVisible(f, visible);
        }
    }

    private bool CanReshape => !reshapeEditor.IsStarted && editor.Geometry?.IsEmpty == false && editor.Geometry is Polygon; //TODO

    [RelayCommand(CanExecute = nameof(CanReshape))]
    public void Reshape()
    {
        if (Document is MapDocument mapdoc)
        {
            mapdoc.GeometryEditor = reshapeEditor;
            reshapeEditor.Start(GeometryType.Polyline);
            ReshapeAcceptCommand.NotifyCanExecuteChanged();
            SetElementVisibility(true);
            ReshapeCommand.NotifyCanExecuteChanged();
            ReshapeAcceptCommand.NotifyCanExecuteChanged();
        }
    }
    private bool CanAcceptReshape => reshapeEditor.IsStarted &&
        this.editor.Geometry is Multipart mp && !mp.IsEmpty && reshapeEditor.Geometry is Polyline line && !line.IsEmpty &&
        GeometryEngine.Reshape(mp, line) != null;

    [RelayCommand(CanExecute = nameof(CanAcceptReshape))]
    public void ReshapeAccept()
    {
        var geometry = reshapeEditor.Stop();
        if (editor.Geometry is Multipart mp && geometry is Polyline line && !mp.IsEmpty && !line.IsEmpty)
        {
            var reshapedGeometry = GeometryEngine.Reshape(mp, line);
            if (reshapedGeometry != null)
                editor.ReplaceGeometry(reshapedGeometry);
        }
        if (Document is MapDocument mapdoc)
        {
            SetElementVisibility(false);
            mapdoc.GeometryEditor = editor;
        }
        ReshapeCommand.NotifyCanExecuteChanged();
        ReshapeAcceptCommand.NotifyCanExecuteChanged();
    }

    public class MyGeometryEditor : GeometryEditor
    {
        private VertexTool _moveTool = new VertexTool()
        {
            Configuration = new InteractionConfiguration()
            {
                //AllowMovingSelectedElement = true,
                AllowMidVertexSelection = false,
                AllowDeletingSelectedElement = false,
                AllowPartCreation = false,
                AllowGeometrySelection = true,
                AllowPartSelection = true,
                AllowScalingSelectedElement = false,
                AllowVertexCreation = false,
                AllowVertexSelection = false,
                AllowRotatingSelectedElement = false,
                RequireSelectionBeforeMove = false
            }, Style = new GeometryEditorStyle
            {
                 MidVertexSymbol = null, VertexSymbol = null, SelectedVertexSymbol = null, SelectedMidVertexSymbol = null,
                FeedbackVertexSymbol = null
            }
        };
        private VertexTool _rotateTool = new VertexTool()
        {
            Configuration = new InteractionConfiguration()
            {
                AllowMovingSelectedElement = false,
                AllowMidVertexSelection = false,
                AllowDeletingSelectedElement = false,
                AllowPartCreation = false,
                AllowGeometrySelection = true,
                AllowPartSelection = true,
                AllowScalingSelectedElement = false,
                AllowVertexCreation = false,
                AllowVertexSelection = false,
                AllowRotatingSelectedElement = true,
                RequireSelectionBeforeMove = false
            },
            Style = new GeometryEditorStyle
            {
                MidVertexSymbol = null,
                VertexSymbol = null,
                SelectedVertexSymbol = null,
                SelectedMidVertexSymbol = null, FeedbackVertexSymbol = null
            }
        };
        private VertexTool _vertexTool = new VertexTool()
        {
            Configuration = new InteractionConfiguration()
            {
                AllowMovingSelectedElement = true,
                AllowMidVertexSelection = true,
                AllowDeletingSelectedElement = true,
                AllowPartCreation = false,
                AllowGeometrySelection = false,
                AllowPartSelection = false,
                AllowScalingSelectedElement = false,
                AllowVertexCreation = true,
                AllowVertexSelection = true,
                AllowRotatingSelectedElement = false,
                RequireSelectionBeforeMove = false
            },
            Style = new GeometryEditorStyle
            {
                VertexTextSymbol = null,
                VertexSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Square, System.Drawing.Color.Black, 9),
                SelectedVertexSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Square, System.Drawing.Color.Black, 9),
                MidVertexSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Gray, 8),
                SelectedMidVertexSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Gray, 8),
                FeedbackVertexSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Cross, System.Drawing.Color.Black, 10),
            }
        };
        private VertexTool _inactiveTool = new VertexTool()
        {

            Configuration = new InteractionConfiguration()
            {
                AllowMovingSelectedElement = false,
                AllowMidVertexSelection = false,
                AllowDeletingSelectedElement = false,
                AllowPartCreation = false,
                AllowGeometrySelection = false,
                AllowPartSelection = true,
                AllowScalingSelectedElement = false,
                AllowVertexCreation = false,
                AllowVertexSelection = false,
                AllowRotatingSelectedElement = false,
                RequireSelectionBeforeMove = false
            },
            Style = new GeometryEditorStyle
            {
                MidVertexSymbol = null,
                VertexSymbol = null,
                SelectedVertexSymbol = null,
                SelectedMidVertexSymbol = null,
                FeedbackVertexSymbol = null
            }
        };

        private Geometry? _geometry;
        private Symbol? _symbol;

        public void Initialize(Geometry geometry, Symbol? symbol)
        {
            _geometry = geometry;
            _symbol = symbol;
            PropertyChanged += OnPropertyChanged;
            if (symbol is not null)
            {
                if (geometry is Polygon)
                {
                    _vertexTool.Style.FillSymbol = _rotateTool.Style.FillSymbol = _moveTool.Style.FillSymbol = _inactiveTool.Style.FillSymbol = symbol;
                }
                else if (geometry is Polyline)
                {
                    _vertexTool.Style.LineSymbol = _rotateTool.Style.LineSymbol = _moveTool.Style.LineSymbol = _inactiveTool.Style.LineSymbol = symbol;
                }
                else if (geometry is MapPoint || geometry is Multipoint)
                {
                }
            }
            Tool = _inactiveTool;
            Start(geometry);
        }

        //public Geometry? ActiveGeometry => Geometry ?? _geometry;

        private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(IsStarted):
                    break;
                case nameof(SelectedElement):
                    break;
                case nameof(Tool):
                    break;
                case nameof(Geometry):
                 //   OnPropertyChanged(nameof(ActiveGeometry));
                    break;
            }
        }

        private void EnsureStarted()
        {
            Debug.Assert(_geometry != null, "Geometry not set");
            if (!IsStarted)
                Start(_geometry);
        }
        public void Move() {
            Tool = _moveTool;
            EnsureStarted();
            SelectGeometry();
        }
        public void EditVertices() {
            Tool = _vertexTool;
            EnsureStarted();
            ClearSelection();
        }
        public void Rotate() {
            Tool = _rotateTool;
            EnsureStarted();
            SelectGeometry();
        }
    }
}