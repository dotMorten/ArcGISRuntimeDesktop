using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ArcGISRuntimeDesktop.Controls
{
    public class EnumComboBox : ContentControl
    {
        ComboBox cmb;
        public EnumComboBox()
        {
            this.Content = cmb = new ComboBox();
            cmb.SelectionChanged += Cmb_SelectionChanged;
            StringMappings = new ObservableCollection<EnumStringValue>();
        }

        private void Cmb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedItem = (cmb.SelectedItem as ComboBoxItem)?.Tag;
        }

        public object Header
        {
            get { return (object)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(nameof(Header), typeof(object), typeof(EnumComboBox), new PropertyMetadata(null, (s, e) => ((EnumComboBox)s).cmb.Header = e.NewValue));


        public object? SelectedItem
        {
            get { return (object?)GetValue(SelectedItemProperty); }
            set {
                if (object.Equals(GetValue(SelectedItemProperty), value)) return;
                SetValue(SelectedItemProperty, value); 
            }
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(EnumComboBox), new PropertyMetadata(null, (s,e) => ((EnumComboBox)s).OnSelectedItemChanged(e.OldValue, e.NewValue)));

        private void OnSelectedItemChanged(object oldValue, object newValue)
        {
            if (object.Equals(oldValue, newValue)) return;
            if (oldValue?.GetType() != newValue?.GetType())
            {
                cmb.Items.Clear();
                if (newValue?.GetType().IsEnum == true)
                {
                    var values = Enum.GetValues(newValue.GetType());
                    foreach (var value in values)
                    {
                        ComboBoxItem item = new ComboBoxItem();
                        if (StringMappings is not null && StringMappings.Where(s => s.EnumValue == (value?.ToString() ?? "")).FirstOrDefault() is EnumStringValue mapping)
                            item.Content = mapping.DisplayName;
                        else
                            item.Content = string.Join(' ', Regex.Split(value?.ToString() ?? "", "(?=[A-Z][^A-Z])"));
                        item.Tag = value;
                        cmb.Items.Add(item);
                    }
                }
            }
            var match = cmb.Items.OfType<ComboBoxItem>().Where(i => object.Equals(i.Tag, newValue)).FirstOrDefault();
            var idx = cmb.Items.IndexOf(match);
            if (idx > -1)
                cmb.SelectedIndex = idx;
        }

        public ObservableCollection<EnumStringValue> StringMappings
        {
            get { return (ObservableCollection<EnumStringValue>)GetValue(StringMappingsProperty); }
            set { SetValue(StringMappingsProperty, value); }
        }

        public static readonly DependencyProperty StringMappingsProperty =
            DependencyProperty.Register("StringMappings", typeof(ObservableCollection<EnumStringValue>), typeof(EnumComboBox), new PropertyMetadata(null));


    }
    public class EnumStringValue
    {
        public string? EnumValue { get; set; }
        public string? DisplayName { get; set; }
    }
}
