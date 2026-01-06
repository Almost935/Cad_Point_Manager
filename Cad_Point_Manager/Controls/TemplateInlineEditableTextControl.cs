using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Cad_Point_Manager.Controls
{
    public class TemplateInlineEditableTextControl : ContentControl
    {
        #region Fields
        private string? _originalText;
        #endregion

        #region Dependency Properties
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(TemplateInlineEditableTextControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty IsEditingProperty =
            DependencyProperty.Register(nameof(IsEditing), typeof(bool), typeof(TemplateInlineEditableTextControl),
                new PropertyMetadata(false));
        public bool IsEditing
        {
            get => (bool)GetValue(IsEditingProperty);
            set => SetValue(IsEditingProperty, value);
        }
        #endregion

        #region Constructors
        static TemplateInlineEditableTextControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TemplateInlineEditableTextControl),
                new FrameworkPropertyMetadata(typeof(TemplateInlineEditableTextControl)));
        }
        #endregion

        #region Methods
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Wire textbox events (template part names must match)
            if (GetTemplateChild("EditView") is TextBox tb)
            {
                tb.LostKeyboardFocus += (_, __) => EndEdit(commit: true);
                tb.PreviewKeyDown += (s, e) =>
                {
                    if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
                    {
                        EndEdit(commit: true);
                        e.Handled = true;
                    }
                    else if (e.Key == Key.Escape)
                    {
                        EndEdit(commit: false);
                        e.Handled = true;
                    }
                };

                MouseDoubleClick += (s, e) =>
                {
                    BeginEdit();
                    e.Handled = true;
                };
            }
        }

        private void BeginEdit()
        {
            if (IsEditing) { return; }

            _originalText = Text;
            IsEditing = true;

            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                if (GetTemplateChild("EditView") is TextBox tb)
                {
                    tb.Focus();
                    Keyboard.Focus(tb);
                    Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
                    {
                        if (GetTemplateChild("EditView") is TextBox tb)
                        {
                            tb.Focus();
                            Keyboard.Focus(tb);

                            Debug.WriteLine("FocusedElement = " + Keyboard.FocusedElement);
                            Debug.WriteLine("tb.IsKeyboardFocusWithin = " + tb.IsKeyboardFocusWithin);
                            Debug.WriteLine("tb.IsFocused = " + tb.IsFocused);
                        }
                    }));

                    tb.SelectAll();
                }
            }));
        }

        private void EndEdit(bool commit)
        {
            if (!IsEditing) { return; }

            if (!commit && _originalText != null) { Text = _originalText; }

            IsEditing = false;
            _originalText = null;
        }
        #endregion
    }
}
