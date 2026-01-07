using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Views.UserControls;
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

        public static readonly DependencyProperty ViewMatrixProperty =
            DependencyProperty.Register(nameof(ViewMatrix), typeof(Matrix), typeof(TemplateInlineEditableTextControl),
                new PropertyMetadata(Matrix.Identity));
        public Matrix ViewMatrix
        {
            get => (Matrix)GetValue(ViewMatrixProperty);
            set => SetValue(ViewMatrixProperty, value);
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
            // The element you want to edit (usually the TextBlock area inside the titleblock)
            var target = this; // or a named element inside your control

            var layoutsView = VisualTreeHelpers.FindAncestor<LayoutsViewControl>(target);
            if (layoutsView == null) { return; }

            var overlay = layoutsView.EditorOverlay;
            var background = layoutsView.BackgroundCanvas;

            // Get target bounds in BackgroundCanvas coordinates
            var t = target.TransformToAncestor(background);
            Rect bounds = t.TransformBounds(new Rect(new Size(target.ActualWidth, target.ActualHeight)));
            
            _overlayEditor = new TextBox
            {
                Width = Math.Max(1, bounds.Width),
                Height = Math.Max(1, bounds.Height),
                FontFamily = this.FontFamily,
                FontSize = 12,
                Padding = new Thickness(2),
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalContentAlignment = VerticalAlignment.Top,
                Background = new SolidColorBrush(Color.FromArgb(64, 255, 0, 0)),
                Foreground = Brushes.Black
            };

            // Put it on overlay (NOT scaled)
            Canvas.SetLeft(_overlayEditor, bounds.Left);
            Canvas.SetTop(_overlayEditor, bounds.Top);

            // Make overlay interactive while editor is active
            overlay.IsHitTestVisible = true;

            // Bind to same Text DP
            _overlayEditor.SetBinding(TextBox.TextProperty, new Binding(nameof(Text))
            {
                Source = this,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });

            _overlayEditor.LostFocus += (_, __) => EndEdit(commit: true);
            _overlayEditor.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None) { EndEdit(true); e.Handled = true; }
                else if (e.Key == Key.Escape) { EndEdit(false); e.Handled = true; }
            };

            overlay.Children.Add(_overlayEditor);
            _overlayEditor.Focus();
            _overlayEditor.SelectAll();
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
