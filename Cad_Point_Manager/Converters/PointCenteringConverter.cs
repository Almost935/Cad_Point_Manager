using System.Globalization;
using System.Windows.Data;
using System.Windows;
using SharpDX;

using Point = System.Windows.Point;
using Matrix = System.Windows.Media.Matrix;
using System.Diagnostics;

namespace Cad_Point_Manager.Converters
{
    public class PointCenteringConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts a coordinate by transforming it with the matrix parameter and then subtracting the second parameter.
        /// </summary>
        /// <param name="values">[0] = center coordinate (double), [1] = size (double)</param>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 ||
             values[0] is not double position ||
             values[1] is not double size)
            {
                return DependencyProperty.UnsetValue;
            }

            return position - size / 2;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
