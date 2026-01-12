using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Views.UserControls;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        private MouseButtonEventHandler? _hostPreviewMouseDownHandler;
        private TextBlock? _readView;
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

        public static readonly DependencyProperty TextAlignmentProperty =
            DependencyProperty.Register(nameof(TextAlignment), typeof(TextAlignment),
                typeof(TemplateInlineEditableTextControl), new PropertyMetadata(TextAlignment.Left));
        public TextAlignment TextAlignment
        {
            get => (TextAlignment)GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
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
            _readView = GetTemplateChild("ReadView") as TextBlock;
        }
        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);

            BeginEdit();
            e.Handled = true;
        }
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
        }

        private void BeginEdit()
        {
            if (_overlayEditor is not null) { return; }

            _host = VisualTreeHelpers.FindAncestor<LayoutsViewControl>(this);
            if (_host == null) { return; }

            ApplyTemplate();
            _readView?.UpdateLayout();

            _originalText = Text;

            _overlayEditor = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalContentAlignment = VerticalAlignment.Top,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 0, 0),
                Padding = new Thickness(0),
                MinHeight = 0,
                Background = Brushes.White
            };
            _overlayEditor.SetBinding(TextBox.FontFamilyProperty, new Binding(nameof(FontFamily)) { Source = this });
            _overlayEditor.SetBinding(TextBox.FontStyleProperty, new Binding(nameof(FontStyle)) { Source = this });
            _overlayEditor.SetBinding(TextBox.FontWeightProperty, new Binding(nameof(FontWeight)) { Source = this });
            _overlayEditor.SetBinding(TextBox.FontStretchProperty, new Binding(nameof(FontStretch)) { Source = this });
            _overlayEditor.SetBinding(TextBox.ForegroundProperty, new Binding(nameof(Foreground)) { Source = this });

            _hostPreviewMouseDownHandler = Host_PreviewMouseDown;
            _host.AddHandler(UIElement.PreviewMouseDownEvent, _hostPreviewMouseDownHandler, handledEventsToo: true);

            _overlayEditor.SetBinding(TextBox.TextProperty, new Binding(nameof(Text))
            {
                Source = this,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            _overlayEditor.LostKeyboardFocus += OverlayEditor_LostKeyboardFocus;
            _overlayEditor.PreviewKeyDown += OverlayEditor_PreviewKeyDown;

            _host.EditorOverlay.IsHitTestVisible = true;
            _host.EditorOverlay.Children.Add(_overlayEditor);

            _host.ViewMatrixChanged += Host_ViewMatrixChanged;

            UpdateOverlayEditorRect();

            _overlayEditor.Focus();
            _overlayEditor.SelectAll();
        }

        private void EndEdit(bool commit)
        {
            if (_overlayEditor == null) { return; }

            if (!commit && _originalText != null) { Text = _originalText; }

            // Unhook from host + remove overlay editor
            if (_host is not null)
            {
                if (_hostPreviewMouseDownHandler is not null)
                {
                    _host.RemoveHandler(UIElement.PreviewMouseDownEvent, _hostPreviewMouseDownHandler);
                    _hostPreviewMouseDownHandler = null;
                }

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

        private void Host_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_overlayEditor == null) { return; }

            // If click happened inside the overlay editor, do nothing
            if (e.OriginalSource is DependencyObject d)
            {
                if (_overlayEditor == d || VisualTreeHelpers.IsDescendantOf(_overlayEditor, d)) { return; }
            }

            // Click was outside -> commit and close
            EndEdit(commit: true);
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

            FrameworkElement anchor = _readView ?? (FrameworkElement)this;

            var t = anchor.TransformToAncestor(_host.BackgroundCanvas);
            Rect bounds = t.TransformBounds(new Rect(0, 0, anchor.ActualWidth, anchor.ActualHeight));

            Canvas.SetLeft(_overlayEditor, bounds.Left);
            Canvas.SetTop(_overlayEditor, bounds.Top);
            _overlayEditor.Width = Math.Max(1, bounds.Width);
            _overlayEditor.Height = Math.Max(1, bounds.Height);

            _overlayEditor.FontFamily = FontFamily;
            _overlayEditor.Foreground = Foreground;

            if (_readView != null) { _overlayEditor.TextAlignment = _readView.TextAlignment; }

            _overlayEditor.VerticalContentAlignment = _readView?.VerticalAlignment ?? VerticalAlignment.Top;
            _overlayEditor.HorizontalContentAlignment = _readView?.HorizontalAlignment ?? HorizontalAlignment.Left;

            double s = _host.ViewMatrix.M11;
            _overlayEditor.FontSize = ScaleFontWithView ? Math.Max(1, FontSize * s) : Math.Max(1, FontSize * 12);
        }
        #endregion
    }
}
