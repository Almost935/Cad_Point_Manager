using SharpDX;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace Cad_Point_Manager.Converters
{
    public class Vector4ToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Vector4 vector)
            {
                Color color = Color.FromScRgb(
                vector.W,
                vector.X,
                vector.Y,
               vector.Z);

                return new SolidColorBrush(color);
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                Color c = brush.Color;
                return new Vector4(brush.Color.ScR, brush.Color.ScG, brush.Color.ScB, brush.Color.ScA);
            }

            return Binding.DoNothing;
        }
    }
}
