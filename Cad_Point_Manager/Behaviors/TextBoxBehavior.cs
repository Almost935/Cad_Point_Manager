using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cad_Point_Manager.Behaviors
{
    public static class TextBoxBehavior
    {
        public static readonly DependencyProperty SelectAllOnFocusProperty =
                DependencyProperty.RegisterAttached(
                    "SelectAllOnFocus",
                    typeof(bool),
                    typeof(TextBoxBehavior),
                    new PropertyMetadata(false, OnSelectAllOnFocusChanged));

        [AttachedPropertyBrowsableForType(typeof(TextBox))]
        public static bool GetSelectAllOnFocus(DependencyObject obj)
            => (bool)obj.GetValue(SelectAllOnFocusProperty);

        public static void SetSelectAllOnFocus(DependencyObject obj, bool value)
            => obj.SetValue(SelectAllOnFocusProperty, value);

        private static void OnSelectAllOnFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    textBox.GotKeyboardFocus += TextBox_GotKeyboardFocus;
                    textBox.PreviewMouseLeftButtonDown += TextBox_PreviewMouseLeftButtonDown;
                }
                else
                {
                    textBox.GotKeyboardFocus -= TextBox_GotKeyboardFocus;
                    textBox.PreviewMouseLeftButtonDown -= TextBox_PreviewMouseLeftButtonDown;
                }
            }
        }

        private static void TextBox_GotKeyboardFocus(object sender, RoutedEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }

        private static void TextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (!textBox.IsKeyboardFocusWithin)
            {
                textBox.Focus();
                e.Handled = true;
            }
        }
    }
}