using Cad_Point_Manager.Commands;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.DrawingObjects;
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
        private JobFile _jobFile;
        private bool _jobFileLoaded = false;
        private DxfDocument _dxfDocument;
        #endregion

        #region Properties
        public JobFile JobFile
        {
            get { return _jobFile; }
            set
            {
                _jobFile = value;
                OnPropertyChanged(nameof(JobFile));
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
            JobFile = new();

            AttachDxfFileCommand = new RelayCommand<RoutedEventArgs>(AttachDxfFile);
        }
        #endregion

        #region Methods
        public void AttachDxfFile(RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".dxf";
            dlg.Filter = "DXF Files (*.dxf)|*.dxf";
            dlg.InitialDirectory = @"C:\Users\fcraw\source\repos\Cad_Point_Manager\Cad_Point_Manager\Resources\DXF";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                JobFile.LoadDxf(dlg.FileName);
            }
        }
        #endregion
    }
}
