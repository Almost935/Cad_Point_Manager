using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.Importing;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cad_Point_Manager.Views.InputWindows
{
    /// <summary>
    /// Interaction logic for PointNumberDialog.xaml
    /// </summary>
    public partial class PointNumberDialog : Window, INotifyPropertyChanged
    {
        #region Fields
        private HashSet<int> _existingPointNumbers;
        private CadManager _cadManager;

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

        public bool HasErrors => ImportConflicts.Any(x => x.HasErrors);
        #endregion

        #region Constructors
        public PointNumberDialog(CadManager cadManager)
        {
            InitializeComponent();
            DataContext = this;

            _cadManager = cadManager;
            ImportConflicts.CollectionChanged += ImportConflicts_CollectionChanged;
        }
        #endregion

        #region Methods
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

        private void ConflictsListView_SizeChanged(object sender, SizeChangedEventArgs e)
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

        private void ImportConflicts_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (ImportConflict item in e.NewItems)
                    item.ErrorsChanged += Item_ErrorsChanged;
            }

            if (e.OldItems != null)
            {
                foreach (ImportConflict item in e.OldItems)
                    item.ErrorsChanged -= Item_ErrorsChanged;
            }

            OnPropertyChanged(nameof(HasErrors));
        }

        private void Item_ErrorsChanged(object sender, DataErrorsChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasErrors));
        }

        public void InitializeConflicts(CadManager cadManager)
        {
            _existingPointNumbers = cadManager
                .UsedPointNumbers
                .ToHashSet();

            foreach (var item in ImportConflicts)
            {
                item.GetAllConflicts = () => ImportConflicts;
                item.GetExistingPointNumbers = () => _existingPointNumbers;

                item.ErrorsChanged += Item_ErrorsChanged;
                item.PropertyChanged += Item_PropertyChanged;

                item.ValidateAll();
            }
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ImportConflict.NewPointNumberText))
            {
                foreach (var item in ImportConflicts)
                {
                    item.ValidateAll();
                }

                OnPropertyChanged(nameof(HasErrors));
            }
        }

        private void AutoNumberButton_Click(object sender, RoutedEventArgs e)
        {
            List<int> usedNums = _cadManager.UsedPointNumbers;

            foreach (var conflict in ImportConflicts)
            {
                int currentNum = conflict.ExistingPointNumber;
                while (usedNums.Contains(currentNum))
                {
                    currentNum++;
                }
                conflict.NewPointNumberText = currentNum.ToString();
                usedNums.Add(currentNum);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            if (HasErrors) { return; }

            DialogResult = true;
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
