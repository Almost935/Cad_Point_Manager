using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
    /// Interaction logic for BaseTitleblock.xaml
    /// </summary>
    public partial class BaseTitleblock : UserControl, INotifyPropertyChanged
    {
        #region Fields
        private string _notesText = "notes";
        #endregion

        #region Properties
        public string NotesText
        {
            get => _notesText;
            set
            {
                if (_notesText != value)
                {
                    _notesText = value;
                    OnPropertyChanged(nameof(NotesText));
                }
            }
        }
        #endregion

        #region Dependency Properties
        public static readonly DependencyProperty ViewportWidthProperty =
            DependencyProperty.Register(
                nameof(ViewportWidth),
                typeof(double),
                typeof(BaseTitleblock),
                new PropertyMetadata(28.9375));
        public double ViewportWidth
        {
            get => (double)GetValue(ViewportWidthProperty);
            set => SetValue(ViewportWidthProperty, value);
        }

        public static readonly DependencyProperty ViewportHeightProperty =
            DependencyProperty.Register(
                nameof(ViewportHeight),
                typeof(double),
                typeof(BaseTitleblock),
                new PropertyMetadata(23.0));
        public double ViewportHeight
        {
            get => (double)GetValue(ViewportHeightProperty);
            set => SetValue(ViewportHeightProperty, value);
        }

        public static readonly DependencyProperty ViewportLeftProperty =
            DependencyProperty.Register(
                nameof(ViewportLeft),
                typeof(double),
                typeof(BaseTitleblock),
                new PropertyMetadata(0.5));
        public double ViewportLeft
        {
            get => (double)GetValue(ViewportLeftProperty);
            set => SetValue(ViewportLeftProperty, value);
        }

        public static readonly DependencyProperty ViewportTopProperty =
            DependencyProperty.Register(
                nameof(ViewportTop),
                typeof(double),
                typeof(BaseTitleblock),
                new PropertyMetadata(0.5));
        public double ViewportTop
        {
            get => (double)GetValue(ViewportTopProperty);
            set => SetValue(ViewportTopProperty, value);
        }
        #endregion

        #region Constructors
        public BaseTitleblock()
        {
            InitializeComponent();
        }
        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
