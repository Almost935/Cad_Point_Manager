using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.PointRendering;
using netDxf;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        private CadManager _cadManager3D;
        private Rect _extents = RectExtensions.Zero;
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
            get { return _cadManager3D; }
            set
            {
                _cadManager3D = value;
                OnPropertyChanged();
            }
        }
        public Rect Extents
        {
            get { return _extents; }
            set
            {
                _extents = value;
                OnPropertyChanged(nameof(Extents));
            }
        }

        public bool JobPathSet { get; set; } = false;
        public bool DxfLoaded { get { return CadManager is not null; } }
        public string DxfFileName { get; set; }
        #endregion

        #region Constructors
        public JobFileManager()
        {
            CadManager = new CadManager();
        }
        #endregion

        #region Methods
        public void NewJobFile()
        {
            CadManager.ClearDxf();
            CadManager.ClearDxfPoints();
            CadManager.Layouts.Clear();

            JobName = string.Empty;
            JobFilePath = string.Empty;
            DxfFilePath = string.Empty;
            DxfDoc = null;
            Extents = Rect.Empty;
            JobPathSet = false;
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

            bool? result = dlg.ShowDialog();

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
            JobFileData jobFileData = new() { JobName = JobName, DxfFileName = Dxf };
            string jsonString = JsonSerializer.Serialize(jobFileData);
            File.WriteAllText(path, jsonString);
        }

        public bool TryLoadJobFile()
        {
            Microsoft.Win32.OpenFileDialog dlg = new()
            {
                DefaultExt = ".cpm",
                Filter = "Cad Point Manager Files (*.cpm)|*.cpm"
            };
            //dlg.InitialDirectory = @"C:\Users\fcraw\source\repos\Cad_Point_Manager\Cad_Point_Manager\Resources\DXF";

            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                string jsonString = File.ReadAllText(dlg.FileName);
                var options = new JsonSerializerOptions { IncludeFields = true, Converters = { new JsonStringEnumConverter() } };
                JobFileData jobFileData = JsonSerializer.Deserialize<JobFileData>(jsonString, options);
                bool isLoaded = LoadJobFile(jobFileData);

                if (isLoaded)
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
            else
            {
                return false;
            }
        }
        public bool LoadJobFile(JobFileData jobFileData)
        {
            if (jobFileData is null) { return false; }

            JobName = jobFileData.JobName;
            JobFilePath = jobFileData.JobFilePath;
            DxfFilePath = jobFileData.DxfFilePath;
            Extents = jobFileData.Extents;

            return true;
        }
        public void LoadDxf(DxfDocument dxfDoc)
        {
            if (dxfDoc is not null)
            {
                DxfDoc = dxfDoc;
                DxfFileName = DxfDoc.Name;
                CadManager.LoadDxf(dxfDoc);
            }
        }
    }
        #endregion
}

public sealed class JobFileData
{
    public int FileVersion { get; set; } = 1;
    public string JobName { get; set; } = string.Empty;
    public string JobFilePath { get; set; } = string.Empty;
    public string DxfFileName { get; set; } = string.Empty;
    public string DxfFilePath { get; set; } = string.Empty;
    public Rect Extents { get; set; }

    public List<PointGroupDto> PointGroups { get; set; } = new();
    public List<CogoPointDto> CogoPoints { get; set; } = new();

    public JobFileData() { }

    public JobFileData(JobFileManager jobFile)
    {
        JobName = jobFile.JobName;
        JobFilePath = jobFile.JobFilePath;
        DxfFileName = jobFile.DxfFileName;
        DxfFilePath = jobFile.DxfFilePath;
        Extents = jobFile.Extents;
        PointGroups = jobFile.CadManager.CogoPointManager.GetPointGroupDtos();
        CogoPoints = jobFile.CadManager.CogoPointManager.GetCogoPointDtos();
    }
}
