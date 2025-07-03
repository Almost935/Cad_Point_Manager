using Cad_Point_Manager.Models.HitTesting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
