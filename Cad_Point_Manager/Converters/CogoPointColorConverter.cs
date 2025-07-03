using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using SharpDX;
using System.Windows.Media.Converters;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using Cad_Point_Manager.Helpers;

namespace Cad_Point_Manager.Converters
{
    public class CogoPointColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 3 ||
                values[0] is not Color baseColor ||
                values[1] is not bool isMouseOver ||
                values[2] is not bool isSelected)
            {
                return DependencyProperty.UnsetValue;
            }

            if (isMouseOver)
            {
                if (isSelected)
                {
                    return new SolidColorBrush(GlobalHelperProperties._selectedCogoPointMouseOverColor);
                }
                else
                {
                     return new SolidColorBrush(GlobalHelperProperties._mouseOverCogoPointColor);
                }
            }
            else if (isSelected)
            {
                return new SolidColorBrush(GlobalHelperProperties._selectedCogoPointColor);
            }
            else
            {
                return new SolidColorBrush(baseColor);
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
