using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Cad_Point_Manager.Converters
{
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Color c ? new SolidColorBrush(c) : Brushes.Black;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is SolidColorBrush b ? b.Color : Colors.Black;
    }
}
