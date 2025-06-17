using System.Globalization;
using System.Windows.Data;
using System.Windows;
using System.Diagnostics;

namespace Cad_Point_Manager.Converters
{
    public class DebugConverter : IValueConverter
    {
        public bool Invert { get; set; } = false;
        public bool CollapseWhenFalse { get; set; } = true;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not double height)
            {
                return DependencyProperty.UnsetValue;
            }

            Debug.WriteLine($"\n\nheight: {height}");
            return height;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack is not implemented for DebugConverter.");
        }
    }
}
