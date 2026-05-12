using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Cad_Point_Manager.Converters
{
    public class PointCoordinateMatrixTransformerConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 ||
             values[0] is not Point dxfPoint ||
             values[1] is not Matrix matrix ||
             parameter is not string axis)
            {
                return DependencyProperty.UnsetValue;
            }

            var translatedPoint = matrix.Transform(dxfPoint);

            return axis == "X" ? translatedPoint.X : translatedPoint.Y;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
