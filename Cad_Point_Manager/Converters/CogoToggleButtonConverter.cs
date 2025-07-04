using System.Globalization;
using System.Windows.Data;
using System.Windows;
using SharpDX;

using Point = System.Windows.Point;
using Matrix = System.Windows.Media.Matrix;
using System.Diagnostics;

namespace Cad_Point_Manager.Converters
{
    public class CogoToggleButtonConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts a coordinate by transforming it with the matrix parameter and then subtracting the second parameter.
        /// </summary>
        /// <param name="values">[0] = center coordinate (double), [1] = size (double)</param>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 ||
             values[0] is not Point position ||
             values[1] is not Matrix3x2 matrix3x2 ||
             values[2] is not double size ||
             parameter is not string axis)
            {
                return DependencyProperty.UnsetValue;
            }

            if (axis == "X")
            {
                Matrix matrix = new(matrix3x2.M11, matrix3x2.M12, matrix3x2.M21, matrix3x2.M22, matrix3x2.M31, matrix3x2.M32);
                var translatedPoint = matrix.Transform(position);
                return translatedPoint.X - (size / 2);
            }
            else
            {
                Matrix matrix = new(matrix3x2.M11, matrix3x2.M12, matrix3x2.M21, matrix3x2.M22, matrix3x2.M31, matrix3x2.M32);
                var translatedPoint = matrix.Transform(position);
                return translatedPoint.Y - (size / 2);
            }
            //Matrix matrix = new(matrix3x2.M11, matrix3x2.M12, matrix3x2.M21, matrix3x2.M22, matrix3x2.M31, matrix3x2.M32);
            //var translatedPoint = matrix.Transform(dxfPoint);

            //return axis == "X" ? translatedPoint.X : translatedPoint.Y;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
