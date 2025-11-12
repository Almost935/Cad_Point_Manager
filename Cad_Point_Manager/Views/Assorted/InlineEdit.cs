using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Cad_Point_Manager.Views.Assorted
{
    public static class InlineEdit
    {
        // Holds which field is in edit mode on a ListViewItem: "PointNumber", "Northing", etc.
        public static readonly DependencyProperty EditingFieldProperty =
            DependencyProperty.RegisterAttached(
                "EditingField",
                typeof(string),
                typeof(InlineEdit),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

        public static void SetEditingField(DependencyObject obj, string value) => obj.SetValue(EditingFieldProperty, value);
        public static string GetEditingField(DependencyObject obj) => (string)obj.GetValue(EditingFieldProperty);
    }
}
