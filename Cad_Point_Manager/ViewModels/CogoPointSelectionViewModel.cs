using Cad_Point_Manager.Models.PointRendering;
using Cad_Point_Manager.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Specialized;

namespace Cad_Point_Manager.ViewModels
{
    public class CogoPointSelectionViewModel : INotifyPropertyChanged
    {
        private const string _noneString = "<None>";
        private const string _variesString = "<Varies>";

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsSingleSelection => SelectedPoints.Count == 1;
        public bool IsPointNumberEditable => SelectedPoints.Count == 1;
        public bool IsCoordinateEditable => SelectedPoints.Count > 0;
        public bool IsDescriptionEditable => SelectedPoints.Count > 0;
        public bool IsPointGroupEditable => SelectedPoints.Count > 0;

        public string PointNumber
        {
            get
            {
                if (_selectedPoints.Count() == 0) { return _noneString; }
                if (_selectedPoints.Count() == 1)
                { return _selectedPoints[0].PointNumber.ToString(); }

                var value = _selectedPoints.Select(p => p.PointNumber).Distinct().ToList();
                return value.Count() == 1 ? value[0].ToString() : _variesString;
            }
            set
            {
                if (int.TryParse(value, out int result))
                {
                    foreach (var p in _selectedPoints)
                        p.PointNumber = result;
                    OnPropertyChanged(nameof(PointNumber));
                }
            }
        }
        public string Northing
        {
            get => GetCommonValueOrVaries(p => p.Northing);
            set => SetDoubleProperty(p => p.Northing = double.Parse(value));
        }

        public string Easting
        {
            get => GetCommonValueOrVaries(p => p.Easting);
            set => SetDoubleProperty(p => p.Easting = double.Parse(value));
        }

        public string Elevation
        {
            get => GetCommonValueOrVaries(p => p.Elevation);
            set => SetDoubleProperty(p => p.Elevation = double.Parse(value));
        }

        public string Description
        {
            get
            {
                if (_selectedPoints.Count == 0) { return _noneString; }
                var values = _selectedPoints.Select(p => p.Description).Distinct().ToList();
                return values.Count == 1 ? values[0] : _variesString;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) return;

                foreach (var point in _selectedPoints)
                    point.Description = value;

                OnPropertyChanged(nameof(Description));
            }
        }

        public string PointGroup
        {
            get
            {
                if (_selectedPoints.Count == 0) { return _noneString; }
                var names = _selectedPoints.Select(p => p.PointGroup?.Name).Distinct().ToList();
                var pg = names.Count() == 1 ? names[0] : _variesString;
                return pg;
            }
            set
            {
                if (string.IsNullOrEmpty(value) || value == _variesString) { return; }

                var group = _selectedPoints.FirstOrDefault()?.CogoPointManager?.PointGroups
                    .FirstOrDefault(pg => pg.Name == value);

                if (group != null)
                {
                    foreach (var p in _selectedPoints) { p.UpdatePointGroup(group); }
                    OnPropertyChanged(nameof(PointGroup));
                }
            }
        }

        private ObservableCollection<string> _displayedPointGroupsName = [];
        private CadManager3D _cadManager;
        private ObservableCollection<CogoPoint> _selectedPoints;

        public ObservableCollection<string> DisplayedPointGroupsName
        {
            get => _displayedPointGroupsName;
            set
            {
                _displayedPointGroupsName = value;
                OnPropertyChanged(nameof(DisplayedPointGroupsName));
            }
        }
        public CadManager3D CadManager
        {
            get => _cadManager;
            set
            {
                if (_cadManager != value)
                {
                    _cadManager = value;
                    OnPropertyChanged(nameof(CadManager));
                }
            }
        }
        public ObservableCollection<CogoPoint> SelectedPoints
        {
            get => _selectedPoints;
            set
            {
                _selectedPoints = value;
                OnPropertyChanged(nameof(SelectedPoints));
            }
        }

        public CogoPointSelectionViewModel(CadManager3D cadManager, ObservableCollection<CogoPoint> selectedPoints)
        {
            CadManager = cadManager;

            UpdateDisplayedPointGroups();
            _selectedPoints = selectedPoints;
            _selectedPoints.CollectionChanged += (s, e) => Refresh();
            Refresh();
        }

        public void Refresh()
        {
            OnPropertyChanged(nameof(IsSingleSelection));
            OnPropertyChanged(nameof(PointNumber));
            OnPropertyChanged(nameof(Northing));
            OnPropertyChanged(nameof(Easting));
            OnPropertyChanged(nameof(Elevation));
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(PointGroup));
            OnPropertyChanged(nameof(IsPointNumberEditable));
            OnPropertyChanged(nameof(IsCoordinateEditable));
            OnPropertyChanged(nameof(IsDescriptionEditable));
            OnPropertyChanged(nameof(IsPointGroupEditable));
        }

        public void UpdateCadManager(CadManager3D cadManager)
        {
            if (CadManager is not null)
            {
                CadManager.CogoPointManager.PointGroups.CollectionChanged -= OnPointGroupsCollectionChanged;
            }
            CadManager = cadManager;
            CadManager.CogoPointManager.PointGroups.CollectionChanged += OnPointGroupsCollectionChanged;

            UpdateDisplayedPointGroups();
        }

        private void OnPointGroupsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateDisplayedPointGroups();
        }

        public void UpdateDisplayedPointGroups()
        {
            DisplayedPointGroupsName.Clear();

            foreach (var pg in _cadManager.CogoPointManager.PointGroups)
            {
                if (pg != null && !string.IsNullOrEmpty(pg.Name))
                {
                    DisplayedPointGroupsName.Add(pg.Name);
                }
            }

            DisplayedPointGroupsName.Add(_noneString);
            DisplayedPointGroupsName.Add(_variesString);
        }
        private string GetCommonValueOrVaries(Func<CogoPoint, double> selector)
        {
            if (_selectedPoints.Count() == 0) { return _noneString; }

            var values = _selectedPoints.Select(selector).Distinct().ToList();
            return values.Count() == 1 ? values[0].ToString("F3") : _variesString;
        }

        private void SetDoubleProperty(Action<CogoPoint> setter)
        {
            foreach (var p in _selectedPoints) { setter(p); }
            Refresh();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
