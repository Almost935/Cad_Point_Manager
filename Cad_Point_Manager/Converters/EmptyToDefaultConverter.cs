using System.Globalization;
using System.Windows.Data;

namespace Cad_Point_Manager.Converters
{
    public class EmptyToDefaultConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var text = values[0] as string;
            var defaultText = values[1] as string;

            return string.IsNullOrWhiteSpace(text) ? defaultText : text;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[]
            {
                value,                 // goes back to Text
                Binding.DoNothing      // BaseText should NEVER be modified
            };
        }
    }
}