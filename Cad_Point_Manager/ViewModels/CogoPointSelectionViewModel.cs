using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.PointRendering;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
                if (_selectedPoints.Count == 0)
                    return _noneString;

                int first = _selectedPoints[0].PointNumber;

                for (int i = 1; i < _selectedPoints.Count; i++)
                {
                    if (_selectedPoints[i].PointNumber != first)
                        return _variesString;
                }

                return first.ToString();
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
                if (_selectedPoints.Count == 0)
                    return _noneString;

                string first = _selectedPoints[0].Description;

                for (int i = 1; i < _selectedPoints.Count; i++)
                {
                    if (_selectedPoints[i].Description != first)
                        return _variesString;
                }

                return first;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    return;

                foreach (var point in _selectedPoints)
                    point.Description = value;

                OnPropertyChanged(nameof(Description));
            }
        }
        public string PointGroup
        {
            get
            {
                if (_selectedPoints.Count == 0)
                    return _noneString;

                string? first = _selectedPoints[0].PointGroup?.Name;

                for (int i = 1; i < _selectedPoints.Count; i++)
                {
                    if (_selectedPoints[i].PointGroup?.Name != first)
                        return _variesString;
                }

                return first ?? _noneString;
            }

            set
            {
                if (string.IsNullOrEmpty(value) || value == _variesString || value == _noneString)
                {
                    return;
                }

                var group = CadManager?.PointGroups.FirstOrDefault(pg => pg.Name == value);

                if (group == null)
                    return;

                foreach (var p in _selectedPoints)
                {
                    if (!ReferenceEquals(p.PointGroup, group))
                    {
                        p.UpdatePointGroup(group);
                    }
                }

                OnPropertyChanged(nameof(PointGroup));
            }
        }

        private ObservableCollection<string> _displayedPointGroupsName = [];
        private CadManager _cadManager;
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
        public CadManager CadManager
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
                if (ReferenceEquals(_selectedPoints, value)) { return; }

                _selectedPoints = value;
                OnPropertyChanged(nameof(SelectedPoints));
                Refresh();
            }
        }

        public CogoPointSelectionViewModel(CadManager cadManager, ObservableCollection<CogoPoint> selectedPoints)
        {
            CadManager = cadManager;

            UpdateDisplayedPointGroups();
            _selectedPoints = selectedPoints;

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

        public void UpdateCadManager(CadManager cadManager)
        {
            if (CadManager is not null)
            {
                CadManager.PointGroups.CollectionChanged -= OnPointGroupsCollectionChanged;
            }
            CadManager = cadManager;
            CadManager.PointGroups.CollectionChanged += OnPointGroupsCollectionChanged;

            UpdateDisplayedPointGroups();
        }

        private void OnPointGroupsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateDisplayedPointGroups();
        }

        public void UpdateDisplayedPointGroups()
        {
            DisplayedPointGroupsName.Clear();

            foreach (var pg in _cadManager.PointGroups)
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
            if (_selectedPoints.Count == 0) { return _noneString; }

            double first = selector(_selectedPoints[0]);

            for (int i = 1; i < _selectedPoints.Count; i++)
            {
                if (selector(_selectedPoints[i]) != first) { return _variesString; }
            }

            return first.ToString("F3");
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
