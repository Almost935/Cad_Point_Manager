using Cad_Point_Manager.Common;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Cad_Point_Manager.Converters
{
    public class SelectionModeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not SelectionMode selectionMode ||
                parameter is not string selectionParam)
            {
                return Visibility.Collapsed;
            }
            else
            {
                bool isMatch = selectionMode.ToString() == selectionParam;
                return isMatch ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
