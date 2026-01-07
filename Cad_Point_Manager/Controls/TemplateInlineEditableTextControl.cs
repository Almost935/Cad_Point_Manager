using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Views.UserControls;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Cad_Point_Manager.Controls
{
    public class TemplateInlineEditableTextControl : ContentControl
    {
        #region Fields
        private string? _originalText;
        private TextBox? _overlayEditor;
        private LayoutsViewControl? _host;
        private bool _overlayUpdatePending;
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

        public static readonly DependencyProperty ScaleFontWithViewProperty =
            DependencyProperty.Register(
                nameof(ScaleFontWithView),
                typeof(bool),
                typeof(TemplateInlineEditableTextControl),
                new PropertyMetadata(true));
        public bool ScaleFontWithView
        {
            get => (bool)GetValue(ScaleFontWithViewProperty);
            set => SetValue(ScaleFontWithViewProperty, value);
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
        //public override void OnApplyTemplate()
        //{
        //    base.OnApplyTemplate();

        //    // Wire textbox events (template part names must match)
        //    if (GetTemplateChild("EditView") is TextBox tb)
        //    {
        //        tb.LostKeyboardFocus += (_, __) => EndEdit(commit: true);
        //        tb.PreviewKeyDown += (s, e) =>
        //        {
        //            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
        //            {
        //                EndEdit(commit: true);
        //                e.Handled = true;
        //            }
        //            else if (e.Key == Key.Escape)
        //            {
        //                EndEdit(commit: false);
        //                e.Handled = true;
        //            }
        //        };

        //        MouseDoubleClick += (s, e) =>
        //        {
        //            BeginEdit();
        //            e.Handled = true;
        //        };
        //    }
        //}

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            Debug.WriteLine($"OnPreviewMouseLeftButtonUp");
            base.OnPreviewMouseLeftButtonUp(e);

            BeginEdit();
            e.Handled = true;
        }

        private void BeginEdit()
        {
            if (_overlayEditor is not null) { return; }

            _host = FindAncestor<LayoutsViewControl>(this);
            if (_host == null) { return; }

            _originalText = Text;

            _overlayEditor = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalContentAlignment = VerticalAlignment.Top,
                Padding = new Thickness(0),
                BorderThickness = new Thickness(0),
                Background = Brushes.White,
                Foreground = Brushes.Black
            };

            // Bind overlay editor text to this control
            _overlayEditor.SetBinding(TextBox.TextProperty, new Binding(nameof(Text))
            {
                Source = this,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });

            _overlayEditor.LostKeyboardFocus += OverlayEditor_LostKeyboardFocus;
            _overlayEditor.PreviewKeyDown += OverlayEditor_PreviewKeyDown;

            // Add to overlay layer (NOT scaled)
            _host.EditorOverlay.IsHitTestVisible = true;
            _host.EditorOverlay.Children.Add(_overlayEditor);

            // Keep in sync with pan/zoom
            _host.ViewMatrixChanged += Host_ViewMatrixChanged;

            // Initial placement
            UpdateOverlayEditorRect();

            _overlayEditor.Focus();
            _overlayEditor.SelectAll();
        }

        private void EndEdit(bool commit)
        {
            if (_overlayEditor == null) { return; }

            if (!commit && _originalText != null) { Text = _originalText; }

            // Unhook from host + remove overlay editor
            if (_host != null)
            {
                _host.ViewMatrixChanged -= Host_ViewMatrixChanged;
                _host.EditorOverlay.Children.Remove(_overlayEditor);
                _host.EditorOverlay.IsHitTestVisible = false;
            }

            _overlayEditor.LostKeyboardFocus -= OverlayEditor_LostKeyboardFocus;
            _overlayEditor.PreviewKeyDown -= OverlayEditor_PreviewKeyDown;

            _overlayEditor = null;
            _originalText = null;
            _host = null;
            _overlayUpdatePending = false;
        }
        private void OverlayEditor_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            EndEdit(commit: true);
        }

        private void OverlayEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                EndEdit(commit: false);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                EndEdit(commit: true);
                e.Handled = true;
                return;
            }
        }

        private void Host_ViewMatrixChanged(object? sender, EventArgs e)
        {
            // Throttle updates to render priority so panning stays smooth.
            if (_overlayUpdatePending) { return; }

            _overlayUpdatePending = true;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                _overlayUpdatePending = false;
                UpdateOverlayEditorRect();
            }), DispatcherPriority.Render);
        }

        private void UpdateOverlayEditorRect()
        {
            if (_overlayEditor == null || _host == null) { return; }

            // Transform THIS control’s bounds into BackgroundCanvas coords.
            // This automatically reflects PageHost's RenderTransform (matrix).
            var t = TransformToAncestor(_host.BackgroundCanvas);
            Rect bounds = t.TransformBounds(new Rect(new Size(ActualWidth, ActualHeight)));

            Canvas.SetLeft(_overlayEditor, bounds.Left);
            Canvas.SetTop(_overlayEditor, bounds.Top);
            _overlayEditor.Width = Math.Max(1, bounds.Width);
            _overlayEditor.Height = Math.Max(1, bounds.Height);

            // Font sizing:
            // Your control's FontSize is in "page units". We multiply by view scale
            // so it visually matches the zoomed content.
            _overlayEditor.FontFamily = FontFamily;

            double s = _host.ViewMatrix.M11; // uniform scale assumption
            _overlayEditor.FontSize = ScaleFontWithView
                ? Math.Max(1, FontSize * s)
                : Math.Max(1, FontSize * 12); // adjust if you want a different baseline
        }

        private static T? FindAncestor<T>(DependencyObject d) where T : DependencyObject
        {
            DependencyObject? cur = d;
            while (cur != null)
            {
                if (cur is T match)
                    return match;

                cur = VisualTreeHelper.GetParent(cur);
            }
            return null;
        }
        #endregion
    }
}
