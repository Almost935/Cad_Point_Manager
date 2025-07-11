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
using System.Diagnostics;
using Cad_Point_Manager.ViewModels;
using Matrix = System.Windows.Media.Matrix;

namespace Cad_Point_Manager.Converters
{
    public class CogoPointToggleButtonConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts a coordinate by transforming it with the matrix parameter and then subtracting the second parameter.
        /// </summary>
        /// <param name="values">[0] = center coordinate (double), [1] = size (double)</param>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 ||
             values[0] is not Point dxfPoint ||
             values[1] is not Matrix3x2 matrix3x2 ||
             parameter is not string axis)
            {
                return DependencyProperty.UnsetValue;
            }

            Matrix matrix = new(matrix3x2.M11, matrix3x2.M12, matrix3x2.M21, matrix3x2.M22, matrix3x2.M31, matrix3x2.M32);
            var translatedPoint = matrix.Transform(dxfPoint);

            return axis == "X" ? translatedPoint.X : translatedPoint.Y;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
