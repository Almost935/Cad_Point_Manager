using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace Cad_Point_Manager.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; } = false;
        public bool CollapseWhenFalse { get; set; } = true;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = value is bool b && b;

            if (Invert)
            {
                boolValue = !boolValue;
            }

            if (boolValue)
            {
                return Visibility.Visible;
            }

            return CollapseWhenFalse ? Visibility.Collapsed : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility v)
            {
                return Invert ? v != Visibility.Visible : v == Visibility.Visible;
            }

            return false;
        }
    }
}
