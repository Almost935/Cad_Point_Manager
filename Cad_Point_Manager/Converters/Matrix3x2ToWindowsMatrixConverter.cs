using System.Globalization;
using System.Windows.Data;
using System.Windows;
using SharpDX;

namespace Cad_Point_Manager.Converters
{
    public class Matrix3x2ToWindowsMatrixConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Matrix3x2 matrix3x2) { return DependencyProperty.UnsetValue; }

            System.Windows.Media.Matrix windowsMatrix = new System.Windows.Media.Matrix(
                matrix3x2.M11, matrix3x2.M12,
                matrix3x2.M21, -matrix3x2.M22,
                matrix3x2.M31,-matrix3x2.M32);

            return windowsMatrix;

            //return new System.Windows.Media.Matrix(
            //    matrix3x2.M11, matrix3x2.M12,
            //    matrix3x2.M21, matrix3x2.M22,
            //    -1000, -5000);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
