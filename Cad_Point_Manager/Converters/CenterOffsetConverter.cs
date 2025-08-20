using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace Cad_Point_Manager.Converters
{
    public class CenterOffsetConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts a center coordinate and a control size (width or height) to top-left aligned Canvas.Left or Canvas.Top.
        /// </summary>
        /// <param name="values">[0] = center coordinate (double), [1] = size (double)</param>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2 ||
                values[0] is not double center ||
                values[1] is not double size)
            {
                return DependencyProperty.UnsetValue;
            }

            double newCoord = center - (size / 2);

            return newCoord;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
