using Cad_Point_Manager.Models.PointRendering;
using SharpDX;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xceed.Wpf.Toolkit;
using Color = System.Windows.Media.Color;

namespace Cad_Point_Manager.Views.UserControls
{
    /// <summary>
    /// Interaction logic for ColorToggle.xaml
    /// </summary>
    public partial class ColorToggle : UserControl, INotifyPropertyChanged
    {
        #region Fields
        #endregion

        #region Properties
        public Vector4 SelectedColor
        {
            get => (Vector4)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        public bool IsPopupOpen
        {
            get => (bool)GetValue(IsPopupOpenProperty);
            set => SetValue(IsPopupOpenProperty, value);
        }

        public double ToggleButtonWidth
        {
            get => (double)GetValue(ToggleButtonWidthProperty);
            set => SetValue(ToggleButtonWidthProperty, value);
        }
        public double ToggleButtonHeight
        {
            get => (double)GetValue(ToggleButtonHeightProperty);
            set => SetValue(ToggleButtonHeightProperty, value);
        }

        public Thickness ToggleButtonMargin
        {
            get => (Thickness)GetValue(ToggleButtonMarginProperty);
            set => SetValue(ToggleButtonMarginProperty, value);
        }

        public Brush ColorBrush => new SolidColorBrush(Vector4ToColor(SelectedColor));
        #endregion

        #region Dependency Properties
        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register(
                nameof(SelectedColor), 
                typeof(Vector4), 
                typeof(ColorToggle),
                new PropertyMetadata(new Vector4(0, 0, 0, 1), OnSelectedColorChanged));

        public static readonly DependencyProperty IsPopupOpenProperty =
            DependencyProperty.Register(
                nameof(IsPopupOpen),
                typeof(bool),
                typeof(ColorToggle),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsPopupOpenChanged));


        public static readonly DependencyProperty ToggleButtonWidthProperty =
            DependencyProperty.Register(
                nameof(ToggleButtonWidth),
                typeof(double),
                typeof(ColorToggle),
                new PropertyMetadata(20.0));

        public static readonly DependencyProperty ToggleButtonHeightProperty =
            DependencyProperty.Register(
                nameof(ToggleButtonHeight),
                typeof(double),
                typeof(ColorToggle),
                new PropertyMetadata(20.0));

        public static readonly DependencyProperty ToggleButtonMarginProperty =
           DependencyProperty.Register(
               nameof(ToggleButtonMargin),
               typeof(Thickness),
               typeof(ColorToggle),
               new PropertyMetadata(new Thickness(0,0,0,0)));
        #endregion

        #region Events
        public event EventHandler<bool> IsPopupOpenChanged;
        public event EventHandler<Vector4> IsColorChanged;
        #endregion

        #region Constructors
        public ColorToggle()
        {
            InitializeComponent();
            
            Loaded += (s, e) => ColorCanvas.SelectedColor = Vector4ToColor(SelectedColor);
        }
        #endregion

        #region Methods
        private void ColorCanvas_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (e.NewValue.HasValue)
            {
                SelectedColor = ColorToVector4(e.NewValue.Value);
                OnPropertyChanged(nameof(ColorBrush));
            }
        }

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement fe && fe.DataContext is PointGroup pg && e.NewValue is Vector4 color)
            {
               if (d is ColorToggle control)
                {
                    control.ColorCanvas.SelectedColor = Vector4ToColor(control.SelectedColor);
                    control.OnPropertyChanged(nameof(ColorBrush));
                    control.IsColorChanged?.Invoke(control, control.SelectedColor);
                }
            }
        }

        private static Color Vector4ToColor(Vector4 vec)
        {
            return Color.FromScRgb(vec.W, vec.X, vec.Y, vec.Z); 
        }

        private static Vector4 ColorToVector4(Color color)
        {
            return new Vector4(color.ScR, color.ScG, color.ScB, color.ScA);
        }

        public static void OnIsPopupOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColorToggle control && e.NewValue is bool newValue)
            {
                control.OnPropertyChanged(nameof(IsPopupOpen));
                control.IsPopupOpenChanged?.Invoke(control, newValue);
            }
        }

        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion
    }
}
