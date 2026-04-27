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
        private ObservableCollection<ImportConflict> _importConflicts = [];
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
            DataContext = this;
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

        private void ListView_Loaded(object sender, RoutedEventArgs e)
        {
            ListView listview = sender as ListView;

            GridView gridView = listview.View as GridView;
            double totalWidth = ConflictsListView.ActualWidth;
            double columnWidth = totalWidth / gridView.Columns.Count;
            if (columnWidth > 0)
            {
                gridView.Columns[0].Width = columnWidth * 1.0;
                gridView.Columns[1].Width = columnWidth * 1.0;
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
