using Esri.ArcGISRuntime.Data;
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
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace ArcGISRuntimeDesktop.Controls
{
    public sealed partial class GeoElementView : UserControl
    {
        public GeoElementView()
        {
            this.InitializeComponent();
        }

        public GeoElement Element
        {
            get { return (GeoElement)GetValue(ElementProperty); }
            set { SetValue(ElementProperty, value); }
        }

        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register(nameof(Element), typeof(GeoElement), typeof(GeoElementView), new PropertyMetadata(null, (s, e) => ((GeoElementView)s).OnGeoElementViewPropertyChanged(e)));

        private async void OnGeoElementViewPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            ItemsView.ItemsSource = null;
            DisplayField.Text = "";
            webView.Visibility = Visibility.Collapsed;
            ItemsView.Visibility = Visibility.Visible;
            if (e.NewValue is GeoElement elm)
            {
                List<GeoElementViewItemData> data = new List<GeoElementViewItemData>();
                foreach (var item in elm.Attributes)
                {
                    data.Add(new GeoElementViewItemData(item.Key, item.Value));
                }
                if (elm is Esri.ArcGISRuntime.Ogc.KmlGeoElement kmlelm)
                {
                    if(elm.Attributes.ContainsKey("html"))
                    {
                        try
                        {
                            await webView.EnsureCoreWebView2Async();
                            webView.NavigateToString((string)elm.Attributes["html"]);
                            ItemsView.Visibility = Visibility.Collapsed;
                            webView.Visibility = Visibility.Visible;
                        }
                        catch (System.Exception ex)
                        {
                        }
                    }
                }
                else if (elm is Feature f && f.FeatureTable is not null)
                {
                    var displayFieldName = (f.FeatureTable as ArcGISFeatureTable)?.LayerInfo?.DisplayFieldName;
                    foreach (var field in f.FeatureTable.Fields)
                    {
                        if (data.FirstOrDefault(d=>d.Key == field.Name) is GeoElementViewItemData item)
                        {
                            if(field.FieldType == FieldType.Blob || field.FieldType == FieldType.OID || field.FieldType == FieldType.GlobalID)
                            {
                                data.Remove(item);
                                continue;
                            }
                            if (!string.IsNullOrEmpty(field.Alias))
                                item.Name = field.Alias;
                            if (field.Domain is CodedValueDomain cvd && cvd.CodedValues.FirstOrDefault(c => c.Code == item.Value) is object o)
                                item.Value = o;

                            if (field.Name == displayFieldName)
                            {
                                DisplayField.Text = item.Value?.ToString();
                                data.Remove(item);
                            }
                        }
                    }
                }
                ItemsView.ItemsSource = data;
            }
        }
        private void Close_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Close?.Invoke(this, EventArgs.Empty);
        }
        public event EventHandler? Close;

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (Element is Feature feature)
            {
                OnFeatureEditRequested?.Invoke(this, feature);
            }
            Close?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler<Feature> OnFeatureEditRequested;
    }

    public class GeoElementViewItemData
    {
        public GeoElementViewItemData(string key, object? value)
        {
            Key = Name = key;
            Value = value;
        }
        public string Key { get; }
        public string Name { get; set; }
        public object? Value { get; set; }
    }
}
