using SharpDX;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace Cad_Point_Manager.Views.UserControls
{
    /// <summary>
    /// Interaction logic for ColorToggle.xaml
    /// </summary>
    public partial class ColorToggle : UserControl
    {
        #region Fields
        #endregion

        #region Properties
        public Vector4 SelectedColor
        {
            get => (Vector4)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }
        #endregion

        #region Dependency Properties
        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register(nameof(SelectedColor), typeof(Vector4), typeof(ColorToggle),
                new PropertyMetadata(new Vector4(0, 0, 0, 1), OnSelectedColorChanged));
        #endregion

        #region Constructors
        public ColorToggle()
        {
            InitializeComponent();
            Loaded += (s, e) => ColorCanvas.SelectedColor = Vector4ToColor(SelectedColor);
        }
        #endregion

        #region Methods
        #endregion

        public Brush ColorBrush => new SolidColorBrush(Vector4ToColor(SelectedColor));

        private void ColorCanvas_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<System.Windows.Media.Color?> e)
        {
            if (e.NewValue.HasValue)
            {
                SelectedColor = ColorToVector4(e.NewValue.Value);
                OnPropertyChanged(nameof(ColorBrush));
            }
        }

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColorToggle control)
            {
                control.ColorCanvas.SelectedColor = Vector4ToColor(control.SelectedColor);
                control.OnPropertyChanged(nameof(ColorBrush));
            }
        }

        private static Color Vector4ToColor(Vector4 vec)
        {
            return Color.FromScRgb(vec.W, vec.X, vec.Y, vec.Z); // W = A, X = R, Y = G, Z = B
        }

        private static Vector4 ColorToVector4(Color color)
        {
            return new Vector4(color.ScR, color.ScG, color.ScB, color.ScA);
        }

        protected void OnPropertyChanged(string name)
        {
            Dispatcher.Invoke(() =>
            {
                var propChanged = GetType().GetProperty(name);
                propChanged?.SetValue(this, propChanged.GetValue(this));
            });
        }
    }
}
