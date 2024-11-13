using netDxf;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace Cad_Point_Manager.Models
{
    public class JobFileManager : BaseModel
    {
        #region Fields
        private string _jobName = "Unsaved";
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
            get { return _dxfDoc; }
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
            CadManager.ClearDxfDocument();

            JobName = string.Empty;
            JobFilePath = string.Empty;
            DxfFilePath = string.Empty;
            DxfDoc = null;
            Extents = new Rect();
        }
        public bool TrySaveJobFile()
        {
            if (!JobPathSet)
            {
                bool result = TryGetJobFilePath();

                if (result == true)
                {
                    JobPathSet = true;
                    SaveJobFile(JobFilePath);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                SaveJobFile(JobFilePath);
                return true;
            }
        }
        public bool TryGetJobFilePath()
        {
            Microsoft.Win32.SaveFileDialog dlg = new()
            {
                DefaultExt = ".cpm",
                Filter = "Cad Point Manager Files (*.cpm)|*.cpm"
            };
            //dlg.InitialDirectory = @"C:\Users\fcraw\source\repos\Cad_Point_Manager\Cad_Point_Manager\Resources\DXF";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                JobFilePath = dlg.FileName;
                JobName = Path.GetFileName(JobFilePath);
                JobPathSet = true;
                return true;
            }
            else
            {
                return false;
            }
        }
        public void SaveJobFile(string path)
        {
            JobFileData jobFileData = new(this);
            string jsonString = JsonSerializer.Serialize(jobFileData);
            System.IO.File.WriteAllText(path, jsonString);
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
