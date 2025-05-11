using SharpDX;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace Cad_Point_Manager.Converters
{
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Color color;
            // If value is a SharpDX.Vector4, interpret as ScRGB
            if (value is Vector4 vec)
            {
                color = Color.FromScRgb(vec.W, vec.X, vec.Y, vec.Z); // W = A, X = R, Y = G, Z = B
                return new SolidColorBrush(color);
            }

            // If it's already a Color, wrap in brush
            if (value is Color)
            {
                color = (Color)value;
                return new SolidColorBrush(color);
            }

            // If it's already a Brush, just return it
            if (value is Brush brush)
            {
                return brush;
            }

            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                Color color = brush.Color;
                return new Vector4(color.ScR, color.ScG, color.ScB, color.ScA);
            }

            return new Vector4(0, 0, 0, 1); 
        }
    }
}
