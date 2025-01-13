using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace Cad_Point_Manager.Converters
{
    public class LeftRightMarginConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 4 &&
                values[0] is double left &&
                values[1] is double right &&
                values[2] is double top &&
                values[3] is double bottom)
            {
                return new Thickness(left, top, right, bottom);
            }

            return new Thickness(0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
