using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using SharpDX;

namespace Cad_Point_Manager.Converters
{
    public class CogoPointScaleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2 ||
                values[0] is not Matrix3x2 matrix ||
                values[1] is not double pointScale)
            {
                return DependencyProperty.UnsetValue;
            }

            return matrix.M11 * pointScale;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
