using Cad_Point_Manager.Commands;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.Importing;
using Cad_Point_Manager.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Cad_Point_Manager.Views.InputWindows;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Models.PointRendering;

namespace Cad_Point_Manager.ViewModels
{
    public class ImportPointsViewModel : BaseViewModel
    {
        private readonly PointImportService _service;
        private readonly CadManager _cadManager;

        private List<List<string>> _rows;

        public ObservableCollection<ColumnAnalysis> Columns { get; } = new();

        private string _importFilePath;
        public string ImportFilePath
        {
            get => _importFilePath;
            set
            {
                _importFilePath = value;
                OnPropertyChanged(nameof(ImportFilePath));
            }

        }

        private bool _hasHeaderRow;
        public bool HasHeaderRow
        {
            get => _hasHeaderRow;
            set
            {
                _hasHeaderRow = value;
                OnPropertyChanged();
                Reanalyze(); // 👈 important
            }
        }

        public Array AvailableFields => Enum.GetValues(typeof(CogoFieldType));

        public ICommand LoadFileCommand { get; }
        public ICommand ImportCommand { get; }

        public ImportPointsViewModel(CadManager cadManager)
        {
            _service = new PointImportService();
            _cadManager = cadManager;

            LoadFileCommand = new RelayCommand(LoadFile);
            ImportCommand = new RelayCommand(ImportPoints, CanImport);
        }

        private void LoadFile()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Data Files (*.csv;*.txt;*.xlsx)|*.csv;*.txt;*.xlsx"
            };

            if (dlg.ShowDialog() != true) { return; }

            ImportFilePath = dlg.FileName;
            _rows = _service.ReadFile(dlg.FileName);

            HasHeaderRow = _service.DetectHeaderRow(_rows);

            Columns.Clear();

            var analyzed = _service.AnalyzeColumns(_rows, HasHeaderRow);

            foreach (var col in analyzed)
            {
                Columns.Add(col);
            }
        }

        private bool CanImport()
        {
            var mappings = Columns.Select(c => c.Mapping);

            return mappings.Any(m => m.AssignedField == CogoFieldType.PointNumber)
                && mappings.Any(m => m.AssignedField == CogoFieldType.Northing)
                && mappings.Any(m => m.AssignedField == CogoFieldType.Easting);
        }

        private bool PointGroupInImportFile()
        {
            var mappings = Columns.Select(c => c.Mapping);

            return mappings.Any(m => m.AssignedField == CogoFieldType.PointGroup);
        }

        private void ImportPoints()
        {
            var pointGroupsInFile = PointGroupInImportFile();
            var mappings = Columns.Select(c => c.Mapping).ToList();

            List<(int num, double n, double e, double? elev, string? desc, string? pg)> approvedPoints = [];
            List<ImportConflict> conflictPoints = [];
            foreach (var row in _rows)
            {
                (int num, double n, double e, double? elev, string? desc, string? pg) parsedRow = _service.ParseRow(row, mappings);

                if (parsedRow.num <= 0) { continue; }
                if (pointGroupsInFile && parsedRow.pg is not null)
                {
                    var errorMessage = _service.ValidatePointNumber(parsedRow.num, _cadManager.CogoPointManager);
                    if (approvedPoints.Any(p => p.num == parsedRow.num))
                    {
                        conflictPoints.Add(new ImportConflict(new List<(int, double, double, double?, string?, string?)> { parsedRow }, parsedRow.num, "Point number already exists"));
                    }
                    else if (errorMessage is not null)
                    { conflictPoints.Add(new ImportConflict(new List<(int, double, double, double?, string?, string?)> { parsedRow }, parsedRow.num, errorMessage)); }
                    else { approvedPoints.Add(parsedRow); }
                }
                else
                {
                    if (_cadManager.CogoPointManager.ActivePointGroup is null)
                    {
                        MessageBox.Show("You must select an active point group to create new points.");
                        return;
                    }

                }
            }

            if (conflictPoints.Count > 0)
            {
                var dlg = new PointNumberDialog(_cadManager);
                dlg.ImportConflicts.AddRange(conflictPoints);
                dlg.InitializeConflicts(_cadManager);
                if (dlg.ShowDialog() == true)
                {
                    foreach (var approvedPoint in approvedPoints)
                    {
                        var pos = new SharpDX.Vector3((float)approvedPoint.e, (float)approvedPoint.n, 0f);
                        _cadManager.CogoPointManager.TryAddPoint(approvedPoint.num, pos, approvedPoint.pg, out _, (float)approvedPoint.elev, approvedPoint.desc);
                    }
                    foreach (var conflictPoint in conflictPoints)
                    {
                        var row = conflictPoint.Row;
                        var pos = new SharpDX.Vector3((float)row.e, (float)row.n, 0f);
                        _cadManager.CogoPointManager.TryAddPoint(row., pos, row.pg, out _, (float)approvedPoint.elev, approvedPoint.desc);
                    }
                }
            }

            _cadManager.CogoPointCircleVerticesDirty = true;
            _cadManager.CogoPointTextVerticesDirty = true;
        }

        private void Reanalyze()
        {
            Columns.Clear();

            var analyzed = _service.AnalyzeColumns(_rows, HasHeaderRow);

            foreach (var col in analyzed)
            {
                Columns.Add(col);
            }
        }
    }
}
