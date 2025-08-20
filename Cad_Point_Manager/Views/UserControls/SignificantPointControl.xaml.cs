using Cad_Point_Manager.Models.HitTesting;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Cad_Point_Manager.Views.UserControls
{
    /// <summary>
    /// Interaction logic for SignificantPointControl.xaml
    /// </summary>
    public partial class SignificantPointControl : UserControl, INotifyPropertyChanged
    {
        #region Properties
        public static readonly DependencyProperty HitTestablePointProperty =
           DependencyProperty.Register(nameof(HitTestablePoint), typeof(HitTestablePoint), typeof(SignificantPointControl),
               new PropertyMetadata(null));
        public HitTestablePoint HitTestablePoint
        {
            get => (HitTestablePoint)GetValue(HitTestablePointProperty);
            set => SetValue(HitTestablePointProperty, value);
        }
        #endregion

        #region Constructors
        public SignificantPointControl()
        {
            InitializeComponent();
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
