using Cad_Point_Manager.Commands;
using Cad_Point_Manager.Models;
using netDxf;
using System.Windows;
using System.Windows.Input;

namespace Cad_Point_Manager.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        #region Fields
        private JobFileManager _jobFileManager = new();
        private bool _jobFileLoaded = false;
        private string _dxfFilePath;
        private string _dxfFileName;
        private DxfDocument _dxfDocument;
        #endregion

        #region Properties
        public JobFileManager JobFileManager
        {
            get { return _jobFileManager; }
            set
            {
                _jobFileManager = value;
                OnPropertyChanged(nameof(JobFileManager));
            }
        }
        public bool JobFileLoaded
        {
            get { return _jobFileLoaded; }
            set
            {
                _jobFileLoaded = value;
                OnPropertyChanged(nameof(JobFileLoaded));
            }
        }
        public string DxfFilePath
        {
            get { return _dxfFilePath; }
            set
            {
                _dxfFilePath = value;
                OnPropertyChanged(nameof(DxfFilePath));
            }
        }
        public string DxfFileName
        {
            get { return _dxfFileName; }
            set
            {
                _dxfFileName = value;
                OnPropertyChanged(nameof(DxfFileName));
            }
        }
        public DxfDocument DxfDocument
        {
            get { return _dxfDocument; }
            set
            {
                _dxfDocument = value;
                OnPropertyChanged(nameof(DxfDocument));
            }
        }
        #endregion

        #region Commands
        public ICommand NewJobCommand { get; set; }
        public ICommand LoadJobCommand { get; set; }
        public ICommand AttachDxfFileCommand { get; set; }
        public ICommand SaveJobCommand { get; set; }
        public ICommand SaveAsJobCommand { get; set; }
        #endregion

        #region Constructors
        public MainViewModel()
        {
            NewJobCommand = new RelayCommand<RoutedEventArgs>(NewJob);
            LoadJobCommand = new RelayCommand<RoutedEventArgs>(LoadJob);
            AttachDxfFileCommand = new RelayCommand<RoutedEventArgs>(AttachDxfFile);
            SaveJobCommand = new RelayCommand<RoutedEventArgs>(SaveJob);
            SaveAsJobCommand = new RelayCommand<RoutedEventArgs>(SaveJobAs);
        }
        #endregion

        #region Methods
        public void NewJob(RoutedEventArgs e)
        {
            var result = MessageBox.Show("Save current job before exiting?", "Warning", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                bool saved = JobFileManager.TrySaveJobFile();
                if (saved)
                {
                    JobFileManager.NewJobFile();
                }
                else { return; }
            }
            else if (result == MessageBoxResult.No)
            {
                JobFileManager.NewJobFile();
            }
            else
            {
                return;
            }
        }
        public void LoadJob(RoutedEventArgs e)
        {
            JobFileManager.TryLoadJobFile();
        }
        public void AttachDxfFile(RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".dxf";
            dlg.Filter = "DXF Files (*.dxf)|*.dxf";
            dlg.InitialDirectory = @"C:\Users\fcraw\source\repos\Cad_Point_Manager\Cad_Point_Manager\Resources\DXF";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                DxfFilePath = dlg.FileName;
                DxfFileName = dlg.SafeFileName;

                DxfDocument = DxfDocument.Load(DxfFilePath);
                if (DxfDocument is not null)
                {
                    DxfFileName = DxfDocument.Name;
                    JobFileManager.LoadDxf(DxfDocument);
                }
            }
        }
        public void SaveJob(RoutedEventArgs e)
        {
            JobFileManager.TrySaveJobFile();
        }
        public void SaveJobAs(RoutedEventArgs e)
        {

        }
        #endregion
    }
}
