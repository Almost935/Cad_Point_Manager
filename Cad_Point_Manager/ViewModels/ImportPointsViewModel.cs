using Cad_Point_Manager.Commands;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.Importing;
using Cad_Point_Manager.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Cad_Point_Manager.Models.PointRendering;
using Cad_Point_Manager.Views.InputWindows;

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

            List<CogoPoint> potPoints = [];
            foreach (var row in _rows)
            {
                (int num, double n, double e, double? elev, string? desc, string? pg) = _service.ParseRow(row, mappings);
                if (pointGroupsInFile)
                {
                    var pgName = _service.GetMappedValue(row, mappings, CogoFieldType.PointGroup);

                    if (pgName is null) { throw new Exception("Unexpected null point group name."); }

                    var pg = _cadManager.CogoPointManager.GetPointGroup(pgName, Colors.Black, _cadManager.PointBaseScale);
                    var p = _service.CreatePoint(row, mappings, pg, _cadManager.CogoPointManager);

                    potPoints.Add(p);
                }
                else
                {
                    if (_cadManager.CogoPointManager.ActivePointGroup is null)
                    {
                        MessageBox.Show("You must select an active point group to create new points.");
                        return;
                    }
                    var p = _service.CreatePoint(row, mappings, _cadManager.CogoPointManager.ActivePointGroup, _cadManager.CogoPointManager);
                    _cadManager.CogoPointManager.TryAddPoint(p, _cadManager.CogoPointManager.ActivePointGroup);

                    potPoints.Add(p);
                }
            }

            List<ImportConflict> conflictPoints = [];
            List<CogoPoint> pointsToAdd = [];
            for (int i = 0; i < potPoints.Count; i++)
            {
                if (_cadManager.CogoPointManager.PointNumberExists(potPoints[i].PointNumber) ||
                pointsToAdd.Any(p => p.PointNumber == potPoints[i].PointNumber))
                {

                    conflictPoints.Add(new ImportConflict(potPoints[i]));
                }
                else
                {
                    pointsToAdd.Add(potPoints[i]);
                    //_cadManager.CogoPointManager.AddPoint(potPoints[i]);
                }
            }

            if (conflictPoints.Count > 0)
            {
                var dlg = new PointNumberDialog() { ImportConflicts = new(conflictPoints) };
                if (dlg.ShowDialog() == true)
                {
                    //_cadManager.CogoPointManager.OverwritePoint(potPoints[i]);

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
