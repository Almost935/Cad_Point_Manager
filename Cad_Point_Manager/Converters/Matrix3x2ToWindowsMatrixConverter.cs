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
    public class Matrix3x2ToWindowsMatrix : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Matrix3x2 matrix3x2)
            {
                return DependencyProperty.UnsetValue;
            }

            return new Matrix(matrix3x2.M11, matrix3x2.M12, matrix3x2.M21, matrix3x2.M22, matrix3x2.M31, matrix3x2.M32);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
