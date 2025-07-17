using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Cad_Point_Manager.Converters
{
    public class SelectionModeEnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            string paramString = parameter.ToString();
            if (!Enum.IsDefined(value.GetType(), value))
                return false;

            var enumType = value.GetType();

            try
            {
                var parsedParam = Enum.Parse(enumType, paramString);
                return value.Equals(parsedParam);
            }
            catch
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool isChecked) || !isChecked || parameter == null)
                return Binding.DoNothing;

            try
            {
                string paramString = parameter.ToString();

                if (targetType.IsEnum)
                {
                    return Enum.Parse(targetType, paramString);
                }
            }
            catch
            {
                // Optionally log the error
            }

            return Binding.DoNothing;
        }

    }
}
