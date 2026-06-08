using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.DxfImport;
using Cad_Point_Manager.Models.PointRendering;
using netDxf;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;

namespace Cad_Point_Manager.Models
{
    public class JobFileManager : BaseModel
    {
        #region Fields
        private string _jobName = "Unsaved";
        private string _jobFilePath;
        private string _dxfFilePath;
        private DxfDocument _dxfDoc;
        private CadManager _CadManager;
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
            get { return _CadManager; }
            set
            {
                _CadManager = value;
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
            CadManager = new();
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
            try
            {
                if (!JobPathSet)
                {
                    if (!TryGetJobFilePath())
                    {
                        return false;
                    }
                }

                SaveJobFile(JobFilePath);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to save job file.\n\n{ex.Message}",
                    "Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }
        }
        public bool TryGetJobFilePath()
        {
            Microsoft.Win32.SaveFileDialog dlg = new()
            {
                DefaultExt = ".cpm",
                Filter = "Cad Point Manager Files (*.cpm)|*.cpm"
            };

            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                JobFilePath = dlg.FileName;
                JobName = Path.GetFileNameWithoutExtension(JobFilePath);
                JobPathSet = true;
                return true;
            }

            return false;
        }
        public void SaveJobFile(string path)
        {
            if (DxfDoc is null)
                throw new InvalidOperationException("Cannot save job because no DXF document is loaded.");

            JobFileData jobFileData = BuildJobFileData();

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };

            using FileStream fileStream = File.Create(path);
            using ZipArchive archive = new(fileStream, ZipArchiveMode.Create);

            // Save job.json
            ZipArchiveEntry jsonEntry = archive.CreateEntry("job.json", CompressionLevel.Optimal);
            using (Stream jsonStream = jsonEntry.Open())
            using (StreamWriter writer = new(jsonStream))
            {
                string jsonString = JsonSerializer.Serialize(jobFileData, jsonOptions);
                writer.Write(jsonString);
            }

            // Save drawing.dxf
            ZipArchiveEntry dxfEntry = archive.CreateEntry("drawing.dxf", CompressionLevel.Optimal);
            using (Stream dxfStream = dxfEntry.Open())
            {
                bool success = DxfDoc.Save(dxfStream);
                if (!success)
                {
                    throw new InvalidOperationException("Failed to save DXF into the job file.");
                }
            }
        }

        public bool TryLoadJobFile()
        {
            Microsoft.Win32.OpenFileDialog dlg = new()
            {
                DefaultExt = ".cpm",
                Filter = "Cad Point Manager Files (*.cpm)|*.cpm"
            };

            bool? result = dlg.ShowDialog();

            if (result != true)
            {
                return false;
            }

            try
            {
                bool loaded = LoadJobFileFromPath(dlg.FileName);
                if (!loaded)
                {
                    return false;
                }

                JobFilePath = dlg.FileName;
                JobName = Path.GetFileNameWithoutExtension(dlg.FileName);
                JobPathSet = true;

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to load job file.\n\n{ex.Message}",
                    "Load Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

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
        public void LoadDxf(DxfImportResult dxfimportResult)
        {
            if (dxfDoc is not null)
            {
                DxfDoc = dxfDoc;
                DxfFileName = DxfDoc.Name;


                CadManager.LoadDxf(dxfDoc);
            }
        }

        #endregion

        #region Private Save Helpers
        private JobFileData BuildJobFileData()
        {
            List<PointGroupDto> pointGroupDtos = [];
            List<CogoPointDto> cogoPointDtos = [];

            if (CadManager?.PointGroups is not null)
            {
                foreach (var pointGroup in CadManager.PointGroups)
                {
                    pointGroupDtos.Add(new(pointGroup));
                }
            }

            if (CadManager?.CogoPoints is not null)
            {
                foreach (var point in CadManager.CogoPoints)
                {
                    cogoPointDtos.Add(new CogoPointDto(point));
                }
            }

            return new JobFileData(this);
        }
        #endregion

        #region Private Load Helpers
        private bool LoadJobFileFromPath(string filePath)
        {
            using FileStream fileStream = File.OpenRead(filePath);
            using ZipArchive archive = new(fileStream, ZipArchiveMode.Read);

            ZipArchiveEntry? jsonEntry = archive.GetEntry("job.json");
            ZipArchiveEntry? dxfEntry = archive.GetEntry("drawing.dxf");

            if (jsonEntry is null || dxfEntry is null)
            {
                return false;
            }

            JobFileData? jobFileData;
            using (Stream jsonStream = jsonEntry.Open())
            using (StreamReader reader = new(jsonStream))
            {
                string jsonString = reader.ReadToEnd();

                var options = new JsonSerializerOptions
                {
                    Converters = { new JsonStringEnumConverter() }
                };

                jobFileData = JsonSerializer.Deserialize<JobFileData>(jsonString, options);
            }

            if (jobFileData is null)
            {
                return false;
            }

            DxfDocument? loadedDxf;
            string tempDxfPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dxf");

            try
            {
                using (Stream dxfStream = dxfEntry.Open())
                using (FileStream tempFile = File.Create(tempDxfPath))
                {
                    dxfStream.CopyTo(tempFile);
                }

                loadedDxf = DxfDocument.Load(tempDxfPath);
            }
            finally
            {
                if (File.Exists(tempDxfPath))
                {
                    File.Delete(tempDxfPath);
                }
            }

            if (loadedDxf is null)
            {
                return false;
            }

            return LoadJobFile(jobFileData, loadedDxf);
        }

        private bool LoadJobFile(JobFileData jobFileData, DxfDocument dxfDoc)
        {
            if (jobFileData is null || dxfDoc is null)
            {
                return false;
            }

            NewJobFile();

            JobName = jobFileData.JobName;
            DxfDoc = dxfDoc;
            Extents = jobFileData.Extents;

            // Rebuild DXF-derived runtime objects
            CadManager.LoadDxf(dxfDoc);

            // Important: if LoadDxf() currently creates test points, remove that call there.
            // This clear acts as a safeguard in case that call is still present.
            CadManager.ClearDxfPoints();

            Dictionary<string, PointGroup> pointGroupsByName = new(StringComparer.OrdinalIgnoreCase);

            // Restore PointGroups first
            foreach (PointGroupDto pgDto in jobFileData.PointGroups)
            {
                bool created = CadManager.TryCreatePointGroup(
                    pgDto.Name,
                    Color.FromArgb(pgDto.A, pgDto.R, pgDto.G, pgDto.B),
                    out PointGroup? pointGroup);

                if (!created || pointGroup is null)
                {
                    continue;
                }

                pointGroup.IsVisible = pgDto.IsVisible;
                pointGroup.PointScale = pgDto.PointScale;

                pointGroupsByName[pointGroup.Name] = pointGroup;
            }

            // Restore CogoPoints
            foreach (CogoPointDto cpDto in jobFileData.CogoPoints)
            {
                if (!pointGroupsByName.TryGetValue(cpDto.PointGroupName, out PointGroup? pointGroup))
                {
                    continue;
                }

                CadManager.TryCreatePoint(
                    cpDto.PointNumber,
                    new SharpDX.Vector3(cpDto.X, cpDto.Y, cpDto.Z),
                    pointGroup,
                    out _,
                    cpDto.Elevation,
                    cpDto.Description);
            }

            // Rebuild runtime-only state
            CadManager.UpdateExtents();
            CadManager.UpdateCogoPointTree();

            Extents = CadManager.Extents;

            CadManager.LineVerticesDirty = true;
            CadManager.TextVerticesDirty = true;
            CadManager.CogoPointTextVerticesDirty = true;
            CadManager.CogoPointCircleVerticesDirty = true;
            CadManager.HitTestableObjectTreeDirty = true;
            CadManager.DxfNeedsReload = true;

            return true;
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
            PointGroups = jobFile.CadManager.GetPointGroupDtos();
            CogoPoints = jobFile.CadManager.GetCogoPointDtos();
        }
    }
}
