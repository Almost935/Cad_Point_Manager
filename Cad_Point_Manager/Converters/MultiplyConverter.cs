using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Cad_Point_Manager.Converters
{
    public class MultiplyConverter : IValueConverter
    {
        public double Factor { get; set; } = 1.0;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is double d ? d * Factor : value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is double d ? d / Factor : value;
    }
}
