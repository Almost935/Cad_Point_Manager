using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using System.Windows.Media;
using SharpDX;
using Cad_Point_Manager.Extensions;
using Point = System.Windows.Point;

namespace Cad_Point_Manager.Converters
{
    public class CogoPointBaseToggleCenterConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts a coordinate by transforming it with the matrix parameter and then subtracting the second parameter.
        /// </summary>
        /// <param name="values">[0] = center coordinate (double), [1] = size (double)</param>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 ||
             values[0] is not Point dxfPoint ||
             values[1] is not Matrix3x2 matrix ||
             parameter is not string axis)
            {
                return DependencyProperty.UnsetValue;
            }

            var transformed = matrix.ToWindowsMatrix().Transform(dxfPoint);

            return axis == "X" ? transformed.X : transformed.Y;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
