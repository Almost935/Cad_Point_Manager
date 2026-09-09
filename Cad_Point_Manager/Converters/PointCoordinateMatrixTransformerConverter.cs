using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Cad_Point_Manager.Converters
{
    public class PointCoordinateMatrixTransformerConverter : IMultiValueConverter
    {
        public object Convert(
            object[] values,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (values.Length < 3 ||
                values[0] is not Point dxfPoint ||
                values[1] is not Matrix matrix ||
                values[2] is not Visual visual ||
                parameter is not string axis)
            {
                return DependencyProperty.UnsetValue;
            }

            // World/DXF -> physical D3D pixels
            var pixelPoint = matrix.Transform(dxfPoint);

            // Physical pixels -> WPF DIPs
            var dpi = VisualTreeHelper.GetDpi(visual);

            var dipPoint = new Point(
                pixelPoint.X / dpi.DpiScaleX,
                pixelPoint.Y / dpi.DpiScaleY);

            return axis == "X"
                ? dipPoint.X

                : dipPoint.Y;
        }

        public object[] ConvertBack(
            object value,
            Type[] targetTypes,
            object parameter,
            CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}