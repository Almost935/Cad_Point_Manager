using Cad_Point_Manager.Models.PointRendering;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Cad_Point_Manager.Converters
{
    public class KeyValueToValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is KeyValuePair<string, PointGroup> kvp)
                return kvp.Value;
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Not supported
            return Binding.DoNothing;
        }
    }

}
