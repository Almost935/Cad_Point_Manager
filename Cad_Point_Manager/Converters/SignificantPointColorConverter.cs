using System.Globalization;
using System.Windows.Data;
using System.Windows;
using System.Windows.Media;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.HitTesting;

namespace Cad_Point_Manager.Converters
{
    public class SignificantPointColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not HitTestablePoint hitTestablePoint) { return DependencyProperty.UnsetValue; }

            if (hitTestablePoint.IsSelected)
            {
                if (hitTestablePoint.IsMouseOver) { return new SolidColorBrush(GlobalHelperProperties._selectedCogoPointMouseOverColor);
                }
                else
                {
                    return new SolidColorBrush(GlobalHelperProperties._selectedCogoPointColor);
                }
            }
            else
            {
                return new SolidColorBrush(GlobalHelperProperties._mouseOverCogoPointColor); 
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack is not implemented for SignificantPointColorConverter.");
        }
    }
}
