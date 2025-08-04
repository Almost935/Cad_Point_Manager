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
using Cad_Point_Manager.Common;
using System.Diagnostics;
using System.Windows.Controls;

namespace Cad_Point_Manager.Converters
{
    public class SnapToggleEnumToBoolConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2 ||
                values[0] is not string tag ||
                values[1] is not Enums.SelectionMode selectionMode)
            {
                return DependencyProperty.UnsetValue;
            }

            if (tag.Contains("Point"))
            {
                if (selectionMode ==Enums.SelectionMode.Points) { return true; }
                return false;
            }
            else
            {
                if (selectionMode == Enums.SelectionMode.Geometries) { return true; }
                return false;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // ConvertBack is only needed if you want two-way binding.
            if (value is bool isChecked && isChecked && parameter is Enums.SelectionMode selectionMode)
            {
                return new object[] { Binding.DoNothing, selectionMode };
            }

            return new object[] { Binding.DoNothing, Binding.DoNothing };
        }
    }
}
