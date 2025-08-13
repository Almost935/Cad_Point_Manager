using System.Globalization;
using System.Windows.Data;
using System.Windows;
using System.Windows.Media;

using Point = System.Windows.Point;

namespace Cad_Point_Manager.Converters
{
    public class PointMatrixTransformationConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2 ||
                values[0] is not Point point ||
                values[1] is not Matrix matrix)
            {
                return DependencyProperty.UnsetValue;
            }
            
            return matrix.Transform(point);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
