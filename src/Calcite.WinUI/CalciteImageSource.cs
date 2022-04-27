using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Esri.Calcite.WinUI
{
    internal class CalciteImageSource : SvgImageSource
    {
        public CalciteImageSource() : base()
        {
            RegisterPropertyChangedCallback(RasterizePixelHeightProperty, RasterizePixelSizeChanged);
            RegisterPropertyChangedCallback(RasterizePixelWidthProperty, RasterizePixelSizeChanged);
            RegisterPropertyChangedCallback(UriSourceProperty, UriSourcePropertyChanged);
        }

        private void UriSourcePropertyChanged(DependencyObject sender, DependencyProperty dp)
        {
            CalciteImageSource src = (CalciteImageSource)sender;
            
        }

        private void RasterizePixelSizeChanged(DependencyObject sender, DependencyProperty dp)
        {
            CalciteImageSource src = (CalciteImageSource)sender;
        }
    }
}
