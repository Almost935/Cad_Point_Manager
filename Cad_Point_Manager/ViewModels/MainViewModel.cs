using Cad_Point_Manager.Commands;
using Direct2DDXFViewer.DrawingObjects;
using netDxf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Cad_Point_Manager.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        #region Fields
        private bool _jobFileLoaded = false;
        private ObjectLayerManager _layerManager;
        private string _dxfFilePath;
        private string _dxfFileName;
        private DxfDocument _dxfDocument;
        #endregion

        #region Properties
        public bool JobFileLoaded
        {
            get { return _jobFileLoaded; }
            set
            {
                _jobFileLoaded = value;
                OnPropertyChanged(nameof(JobFileLoaded));
            }
        }
        public ObjectLayerManager LayerManager
        {
            get { return _layerManager; }
            set
            {
                _layerManager = value;
                OnPropertyChanged(nameof(LayerManager));
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
        public ICommand AttachDxfFileCommand { get; set; }
        #endregion

        #region Constructors
        public MainViewModel()
        {
            AttachDxfFileCommand = new RelayCommand<RoutedEventArgs>(AttachDxfFile);
        }
        #endregion

        #region Methods
        public void AttachDxfFile(RoutedEventArgs e)
        {
            LayerManager?.Dispose();
            LayerManager = new();

            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".dxf";
            dlg.Filter = "DXF Files (*.dxf)|*.dxf";
            dlg.InitialDirectory = @"C:\Users\Tim\source\repos\Cad_Point_Manager\Cad_Point_Manager\Resources\DXF\";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                DxfFilePath = dlg.FileName;
                DxfFileName = dlg.SafeFileName;

                DxfDocument = DxfDocument.Load(DxfFilePath);
                if (DxfDocument is not null)
                {
                    DxfFileName = DxfDocument.Name;
                    LayerManager.LoadDxfDocument(DxfDocument);
                }
            }
        }
        #endregion
    }
}
