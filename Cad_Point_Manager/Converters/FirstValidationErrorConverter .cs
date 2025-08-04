using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace Cad_Point_Manager.Converters
{
    public class FirstValidationErrorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var hasError = values[0] as bool?;
            var errors = values[1] as ReadOnlyObservableCollection<ValidationError>;
            var originalTooltip = values[2]?.ToString();

            if (hasError == true && errors != null && errors.Count > 0)
            {
                return errors[0].ErrorContent?.ToString();
            }

            return originalTooltip;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
