using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cad_Point_Manager.Controls
{
    public class NoRevertComboBox : ComboBox
    {
        public static readonly DependencyProperty DisplayTextProperty =
            DependencyProperty.RegisterAttached(
                "DisplayText",
                typeof(string),
                typeof(NoRevertComboBox),
                new PropertyMetadata(null));

        public static void SetDisplayText(
            DependencyObject element,
            string? value)
        {
            element.SetValue(DisplayTextProperty, value);
        }

        public static string? GetDisplayText(
            DependencyObject element)
        {
            return (string?)element.GetValue(DisplayTextProperty);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                return;
            }

            base.OnPreviewKeyDown(e);
        }
    }
}