﻿using Microsoft.UI.Xaml.Markup;
using Microsoft.Windows.ApplicationModel.Resources;

namespace ArcGISRuntimeDesktop
{
    [MarkupExtensionReturnType(ReturnType = typeof(string))]
    public class StringResourceExtension : MarkupExtension
    {
        public StringResourceExtension() { }

        public string Key { get; set; } = "";

        protected override object ProvideValue()
        {
            return Resources.GetString(Key);
        }
    }
    public static class Resources
    {
        private static readonly ResourceLoader _resourceLoader = new();
        public static string GetString(string key)
        {
            return _resourceLoader.GetString(key);
        }
    }
}
