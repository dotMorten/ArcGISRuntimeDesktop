using CommunityToolkit.Mvvm.Input;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;

namespace ArcGISRuntimeDesktop.Controls;
public sealed partial class EditTab 
{
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
            mapdoc.EditorOverlay.Graphics.Add(new Graphic(editor.Geometry, editor.Symbol));
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
        Document?.EditorOverlay.Graphics.Clear();
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
}