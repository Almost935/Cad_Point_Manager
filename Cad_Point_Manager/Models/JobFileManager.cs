using Cad_Point_Manager.DrawingObjects;
using netDxf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Cad_Point_Manager.Models
{
    public class JobFileManager : BaseModel
    {
        #region Fields
        private string _jobName;
        private string _jobFilePath;
        private string _dxfFilePath;
        private DxfDocument _dxfDoc;
        private CadManager _cadManager = new();
        private Rect _extents = new();
        #endregion

        #region Properties
        public string JobName
        {
            get { return _jobName; }
            set
            {
                _jobName = value;
                OnPropertyChanged();
            }
        }
        public string JobFilePath
        {
            get { return _jobFilePath; }
            set
            {
                _jobFilePath = value;
                OnPropertyChanged();
            }
        }
        public string DxfFilePath
        {
            get { return _dxfFilePath; }
            set
            {
                _dxfFilePath = value;
                OnPropertyChanged();
            }
        }
        public DxfDocument DxfDoc
        {
            get { return  _dxfDoc; }
            set
            {
                _dxfDoc = value;
                OnPropertyChanged();
            }
        }
        public CadManager CadManager 
        {
            get { return _cadManager; }
            set
            {
                _cadManager = value;
                OnPropertyChanged();
            }
        }
        public Rect Extents
        {
           get { return _extents; }
            set
            {
                _extents = value;
                OnPropertyChanged();
            }
        }

        public bool JobPathSet { get; set; } = false;
        public bool DxfLoaded { get { return CadManager is not null; } }
        #endregion

        #region Constructors
        public JobFileManager() { }
        #endregion

        #region Methods
        public void NewJobFile()
        {
            CadManager?.Dispose();

            JobName = string.Empty;
            JobFilePath = string.Empty;
            DxfFilePath = string.Empty;
            DxfDoc = null;
            CadManager = new CadManager();
            Extents = new Rect();
        }
        public bool TrySaveJobFile()
        {
            if (!JobPathSet)
            {
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.DefaultExt = ".cpm";
                dlg.Filter = "Cad Point Manager Files (*.cpm)|*.cpm";
                dlg.InitialDirectory = @"C:\Users\fcraw\source\repos\Cad_Point_Manager\Cad_Point_Manager\Resources\DXF";

                Nullable<bool> result = dlg.ShowDialog();

                if (result == true)
                {

                }
                else
                {
                    return false;
                }
            }
        }
        public void SaveJobFile()
        {
           JobFileData jobFileData = new JobFileData(this);
        }
        public void LoadJobFile(JobFileData jobFileData)
        {
            JobName = jobFileData.JobName;
            JobFilePath = jobFileData.JobFilePath;
            DxfFilePath = jobFileData.DxfFilePath;
            Extents = jobFileData.Extents;
            
            if (DxfFilePath is not null)
            {
               DxfDoc = DxfDocument.Load(DxfFilePath);
               
            }
        }
        public void LoadDxf(DxfDocument dxfDoc)
        {
            if (dxfDoc is not null)
            {
                CadManager.LoadDxfDocument(dxfDoc);
            }
        }
        #endregion
    }

    public class JobFileData
    {
        public string JobName { get; set; }
        public string JobFilePath { get; set; }
        public string DxfFilePath { get; set; }
        public Rect Extents { get; set; }
        public CadManagerData CadManagerData { get; set; }

        public JobFileData(JobFileManager jobFile)
        {
            { 
                JobName = jobFile.JobName;
                JobFilePath = jobFile.JobFilePath;
                DxfFilePath = jobFile.DxfFilePath;
                Extents = jobFile.Extents;
                CadManagerData = new CadManagerData(jobFile.CadManager);
            }
        }
    }
}
