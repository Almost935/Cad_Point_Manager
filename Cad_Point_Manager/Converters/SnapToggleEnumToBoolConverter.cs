using Cad_Point_Manager.Common;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Cad_Point_Manager.Converters
{
    public class SnapToggleEnumToBoolConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2 ||
                values[0] is not string tag ||
                values[1] is not SelectionMode selectionMode)
            {
                return DependencyProperty.UnsetValue;
            }

            if (tag.Contains("Point"))
            {
                if (selectionMode == SelectionMode.Points) { return true; }
                return false;
            }
            else
            {
                if (selectionMode == SelectionMode.Geometries) { return true; }
                return false;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // ConvertBack is only needed if you want two-way binding.
            if (value is bool isChecked && isChecked && parameter is SelectionMode selectionMode)
            {
                return new object[] { Binding.DoNothing, selectionMode };
            }

            return new object[] { Binding.DoNothing, Binding.DoNothing };
        }
    }
}
