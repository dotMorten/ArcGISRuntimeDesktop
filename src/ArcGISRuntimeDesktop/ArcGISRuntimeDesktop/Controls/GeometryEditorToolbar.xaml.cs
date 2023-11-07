using CommunityToolkit.Mvvm.Input;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.UI.Editing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace ArcGISRuntimeDesktop.Controls
{
    public sealed partial class GeometryEditorToolbar : UserControl
    {
        public GeometryEditorToolbar()
        {
            this.InitializeComponent();
            this.Visibility = Visibility.Collapsed;
        }

        public GeoView? GeoView
        {
            get { return (GeoView)GetValue(GeoViewProperty); }
            set { SetValue(GeoViewProperty, value); }
        }

        public static readonly DependencyProperty GeoViewProperty =
            DependencyProperty.Register(nameof(GeoView), typeof(GeoView), typeof(GeometryEditorToolbar), new PropertyMetadata(null, (s, e) =>
            {
                ((GeometryEditorToolbar)s).GeometryEditor = (e.NewValue as MapView)?.GeometryEditor;
            }));

        public GeometryEditor? GeometryEditor
        {
            get { return (GeometryEditor)GetValue(GeometryEditorProperty); }
            set { SetValue(GeometryEditorProperty, value); }
        }

        public static readonly DependencyProperty GeometryEditorProperty =
            DependencyProperty.Register(nameof(GeometryEditor), typeof(GeometryEditor), typeof(GeometryEditorToolbar), new PropertyMetadata(null, (s, e) =>
            {
                ((GeometryEditorToolbar)s).OnGeometryEditorChanged(e.OldValue as GeometryEditor, e.NewValue as GeometryEditor);
            }));

        private void OnGeometryEditorChanged(GeometryEditor? oldEditor, GeometryEditor? newEditor)
        {
            if (oldEditor is not null)
                oldEditor.PropertyChanged -= Editor_PropertyChanged;
            if (newEditor is not null)
                newEditor.PropertyChanged += Editor_PropertyChanged;
            UndoCommand.NotifyCanExecuteChanged();
            RedoCommand.NotifyCanExecuteChanged();
            DeleteSelectedCommand.NotifyCanExecuteChanged();
            ClearSelectionCommand.NotifyCanExecuteChanged();
            AddPartCommand.NotifyCanExecuteChanged();
            FinishCommand.NotifyCanExecuteChanged();
            this.Visibility = GeometryEditor?.IsStarted == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Editor_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GeometryEditor.CanUndo))
                UndoCommand.NotifyCanExecuteChanged();
            if (e.PropertyName == nameof(GeometryEditor.CanRedo))
                RedoCommand.NotifyCanExecuteChanged();
            if (e.PropertyName == nameof(GeometryEditor.SelectedElement))
            {
                DeleteSelectedCommand.NotifyCanExecuteChanged();
                ClearSelectionCommand.NotifyCanExecuteChanged();
            }
            if (e.PropertyName == nameof(GeometryEditor.Geometry))
            {
                AddPartCommand.NotifyCanExecuteChanged();
                FinishCommand.NotifyCanExecuteChanged();
            }
            if (e.PropertyName == nameof(GeometryEditor.IsStarted))
            {
                this.Visibility = GeometryEditor?.IsStarted == true ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private bool CanUndo => this.GeometryEditor?.CanUndo == true;

        [RelayCommand(CanExecute = nameof(CanUndo))]
        private void Undo()
        {
            if (this.GeometryEditor?.CanUndo == true) GeometryEditor?.Undo();
        }

        private bool CanRedo => this.GeometryEditor?.CanRedo == true;

        [RelayCommand(CanExecute = nameof(CanRedo))]
        private void Redo()
        {
            if (this.GeometryEditor?.CanRedo == true) GeometryEditor?.Redo();
        }

        private bool CanDeleteSelected => this.GeometryEditor?.SelectedElement != null && this.GeometryEditor?.SelectedElement is not GeometryEditorMidVertex;

        [RelayCommand(CanExecute = nameof(CanDeleteSelected))]
        private void DeleteSelected()
        {
            GeometryEditor?.DeleteSelectedElement();
        }

        private bool CanClearSelection => this.GeometryEditor?.SelectedElement != null;

        [RelayCommand(CanExecute = nameof(CanClearSelection))]
        private void ClearSelection()
        {
            GeometryEditor?.ClearSelection();
        }

        private bool CanAddPart => false; // TODO CanFinish && GeometryEditor != null && (GeometryEditor.Geometry is Polygon || GeometryEditor.Geometry is Polyline polyline);

        [RelayCommand(CanExecute = nameof(CanAddPart))]
        private void AddPart()
        {
            // TODO
            // if (GeometryEditor?.Geometry is Polygon polygon)
            // {
            //     var builder = new PolygonBuilder(polygon);
            //     builder.Parts.Add(new Part(polygon.SpatialReference));
            //     GeometryEditor.ReplaceGeometry(builder.ToGeometry());
            // }
            // else if (GeometryEditor?.Geometry is Polyline polyline)
            // {
            //     var builder = new PolylineBuilder(polyline);
            //     builder.Parts.Add(new Part(polyline.SpatialReference));
            //     GeometryEditor.ReplaceGeometry(builder.ToGeometry());
            // }
        }

        [RelayCommand()]
        private void Stop()
        {
            GeometryEditor?.Stop();
        }

        private bool CanFinish => this.GeometryEditor?.Geometry?.IsEmpty == false;
       
        [RelayCommand(CanExecute = nameof(CanFinish))]
        private void Finish()
        {
            var geometry = GeometryEditor?.Stop();
            if (geometry is not null)
                GeometryCreated?.Invoke(this, geometry);
        }

        public event EventHandler<Esri.ArcGISRuntime.Geometry.Geometry> GeometryCreated;
    }
}