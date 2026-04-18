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
using System.Windows;
using System.Windows.Input;

namespace Cad_Point_Manager.ViewModels
{
    public class ImportPointsViewModel : BaseViewModel
    {
        private readonly PointImportService _service;
        private readonly CadManager _cadManager;

        private List<List<string>> _rows;

        public ObservableCollection<ColumnAnalysis> Columns { get; } = new();
        public ObservableCollection<ColumnMapping> Mappings { get; } = new();

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

            Columns.Clear();
            Mappings.Clear();

            var analyzed = _service.AnalyzeColumns(_rows);

            foreach (var col in analyzed)
            {
                Columns.Add(col);
                Mappings.Add(new ColumnMapping { ColumnIndex = col.Index });
            }
        }

        private bool CanImport()
        {
            return Mappings.Any(m => m.AssignedField == CogoFieldType.PointNumber)
                && Mappings.Count(m => m.AssignedField == CogoFieldType.Northing) == 1
                && Mappings.Count(m => m.AssignedField == CogoFieldType.Easting) == 1;
        }

        private void ImportPoints()
        {
            var group = _cadManager.CogoPointManager.ActivePointGroup;
            if (group == null)
            {
                MessageBox.Show("Select an active point group.");
                return;
            }

            var points = _service.CreatePoints(
                _rows,
                Mappings.ToList(),
                group,
                _cadManager.CogoPointManager);

            foreach (var p in points)
            {
                _cadManager.CogoPointManager.TryAddPoint(
                    p.PointNumber,
                    new SharpDX.Vector3((float)p.Easting, (float)p.Northing, 0),
                    group,
                    out _,
                    (float)p.Elevation,
                    p.Description);
            }

            _cadManager.CogoPointCircleVerticesDirty = true;
            _cadManager.CogoPointTextVerticesDirty = true;
        }
    }
}
