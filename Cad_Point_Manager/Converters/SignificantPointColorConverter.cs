using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.HitTesting;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Cad_Point_Manager.Converters
{
    public class SignificantPointColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not HitTestablePoint hitTestablePoint) { return DependencyProperty.UnsetValue; }

            if (hitTestablePoint.IsSelected)
            {
                if (hitTestablePoint.IsMouseOver)
                {
                    return new SolidColorBrush(GlobalHelperProperties.SelectedCogoPointMouseOverColor);
                }
                else
                {
                    return new SolidColorBrush(GlobalHelperProperties.SelectedCogoPointColor);
                }
            }
            else
            {
                return new SolidColorBrush(GlobalHelperProperties.MouseOverCogoPointColor);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack is not implemented for SignificantPointColorConverter.");
        }
    }
}
