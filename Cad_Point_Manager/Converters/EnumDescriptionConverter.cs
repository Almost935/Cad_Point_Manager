using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace Cad_Point_Manager.Converters
{
    public class EnumDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "";

            var field = value.GetType().GetField(value.ToString());
            var attr = field?.GetCustomAttribute<DescriptionAttribute>();

            return attr?.Description ?? value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
