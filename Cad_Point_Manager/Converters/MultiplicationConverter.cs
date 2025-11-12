using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace Cad_Point_Manager.Converters
{
    public class MultiplicationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) { return Binding.DoNothing; }

            if (double.TryParse(value.ToString(), out double input) &&
                double.TryParse(parameter.ToString(), out double factor))
            {
                return input * factor;
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Optional: allow reverse conversion (divide instead)
            if (value == null || parameter == null) { return Binding.DoNothing; }

            if (double.TryParse(value.ToString(), out double result) &&
                double.TryParse(parameter.ToString(), out double factor))
            {
                return result / factor;
            }

            return Binding.DoNothing;
        }
    }
}
