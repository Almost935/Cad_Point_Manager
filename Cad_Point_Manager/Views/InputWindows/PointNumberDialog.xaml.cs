using Cad_Point_Manager.Models.Importing;
using Cad_Point_Manager.Models.PointRendering;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;

namespace Cad_Point_Manager.Views.InputWindows
{
    /// <summary>
    /// Interaction logic for PointNumberDialog.xaml
    /// </summary>
    public partial class PointNumberDialog : Window, INotifyPropertyChanged
    {
        #region Fields
        private ObservableCollection<CogoPoint> _importConflicts = [];
        #endregion

        #region Properties
        public ObservableCollection<ImportConflict> ImportConflicts
        {
            get { return _importConflicts; }
            set
            {
                if (_importConflicts != value)
                {
                    _importConflicts = value;
                    OnPropertyChanged(nameof(ImportConflicts));
                }
            }
        }
        #endregion

        #region Constructors
        public PointNumberDialog()
        {
            InitializeComponent();
        }
        #endregion

        #region Methods
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
                return;
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
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
