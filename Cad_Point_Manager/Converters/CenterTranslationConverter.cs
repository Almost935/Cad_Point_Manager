using System.Globalization;
using System.Windows.Data;
using System.Windows;
using System.Diagnostics;

namespace Cad_Point_Manager.Converters
{
    public class CenterTranslationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not double size)
            {
                return DependencyProperty.UnsetValue;
            }

            bool parameterSet = double.TryParse(parameter.ToString(), out double factor);
            if (!parameterSet)
            {
                factor = 0.5;
            }

            return -size * factor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack is not implemented for DebugConverter.");
        }
    }
}
