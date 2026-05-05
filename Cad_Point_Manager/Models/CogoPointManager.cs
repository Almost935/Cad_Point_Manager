using Cad_Point_Manager.Common.Collections;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Models.PointRendering;
using SharpDX;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using Matrix = System.Windows.Media.Matrix;

namespace Cad_Point_Manager.Models
{
    public class CogoPointManager : INotifyPropertyChanged
    {
        #region Fields
        private BatchableObservableCollection<PointGroup> _pointGroups = [];
        private PointGroup _activePointGroup;
        private CadManager _cadManager;
        private BatchableObservableCollection<CogoPoint> _cogoPoints = [];
        private double _pointBaseScale = 1;
        #endregion

        #region Properties
        public BatchableObservableCollection<PointGroup> PointGroups
        {
            get => _pointGroups;
            private set
            {
                _pointGroups = value;
                OnPropertyChanged(nameof(PointGroups));
            }
        }
        public PointGroup ActivePointGroup
        {
            get => _activePointGroup;
            set
            {
                if (_activePointGroup != value)
                {
                    _activePointGroup = value;
                    OnPropertyChanged(nameof(ActivePointGroup));
                }
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
        public BatchableObservableCollection<CogoPoint> CogoPoints
        {
            get => _cogoPoints;
            set
            {
                if (_cogoPoints != value)
                {
                    _cogoPoints = value;
                    OnPropertyChanged(nameof(CogoPoints));
                }
            }
        }
        public double PointBaseScale
        {
            get => _pointBaseScale;
            set
            {
                _pointBaseScale = value;
                OnPropertyChanged(nameof(PointBaseScale));
            }
        }

        public Rect Extents { get; set; } = Rect.Empty;
        public Matrix CurrentlyAppliedMatrix { get; set; } = Matrix.Identity;
        public CogoPointTree CogoPointTree { get; set; }
        public Color DefaultPointGroupColor => Colors.Black;

        public List<int> UsedPointNumbers => PointGroups.SelectMany(pg => pg.Points).Select(p => p.PointNumber).ToList();
        public bool PointExists(int pointNumber) => PointGroups.SelectMany(pg => pg.Points).Any(p => p.PointNumber == pointNumber);
        #endregion

        #region Constructor
        public CogoPointManager(CadManager cadManager)
        {
            _cadManager = cadManager;
        }
        #endregion

        #region Methods
        public int GetNextAvailablePointNumber(int startCount)
        {
            int num = startCount;
            while (PointNumberExists(num)) { num++; }
            return num;
        }
        public bool PointNumberExists(int num)
        {
            return PointGroups.SelectMany(pg => pg.Points).Any(p => p.PointNumber == num);
        }
        public bool ValidatePointNameChange(int pointNumber, CogoPoint p, out string? errorMessage)
        {
            errorMessage = null;

            if (pointNumber == p.PointNumber) { return true; }

            if (!IsValidPointName(pointNumber, out errorMessage))
            {
                return false;
            }
            return true;
        }
        public bool IsValidPointName(int pointNumber, out string? errorMessage)
        {
            errorMessage = null;
            if (pointNumber <= 0)
            {
                errorMessage = "Point number must be greater than zero.";
                return false;
            }
            if (CogoPoints.Any(p => p.PointNumber == pointNumber))
            {
                errorMessage = $"Point number \"{pointNumber}\" already exists.";
                return false;
            }
            return true;
        }

        public string GetTempPointGroupName()
        {
            string baseName = "New Group";
            int counter = 1;
            string groupName = baseName + $" {counter}";
            while (PointGroupNameExists(groupName))
            {
                groupName = $"{baseName} {counter}";
                counter++;
            }
            return groupName;
        }

        public bool TrySetActivePointGroup(string groupName)
        {
            bool exists = TryGetPointGroup(groupName, out PointGroup pointGroup);
            if (exists)
            {
                ActivePointGroup = pointGroup;
                return true;
            }
            return false;
        }
        public bool TrySetActivePointGroup(PointGroup pointGroup)
        {
            bool exists = TryGetPointGroup(pointGroup.Name, out PointGroup verifiedPointGroup);
            if (exists)
            {
                ActivePointGroup = verifiedPointGroup;
                return true;
            }
            return false;
        }

        public bool TryAddPointToActiveGroup(int pointNum, Vector3 position, out CogoPoint cogoPoint, float elevation = 0, string description = "")
        {
            if (ActivePointGroup == null || PointNumberExists(pointNum))
            {
                cogoPoint = null;
                return false;
            }

            cogoPoint = ActivePointGroup.AddPoint(pointNum, position, elevation, description);
            CogoPoints.Add(cogoPoint);

            return true;
        }
        public bool TryAddPoint(int pointNum, Vector3 position, PointGroup pg, out CogoPoint cogoPoint, float elevation = 0, string description = "")
        {
            if (pg == null || PointNumberExists(pointNum) || !PointGroupExists(pg))
            {
                cogoPoint = null;
                return false;
            }

            cogoPoint = pg.AddPoint(pointNum, position, elevation, description);
            CogoPoints.Add(cogoPoint);

            return true;
        }
        public bool TryAddPoint(int pointNum, Vector3 position, string pgName, out CogoPoint cogoPoint, float elevation = 0, string description = "")
        {
            if (PointNumberExists(pointNum))
            {
                cogoPoint = null;
                return false;
            }

            var pg = GetPointGroup(pgName, DefaultPointGroupColor, PointBaseScale);

            cogoPoint = pg.AddPoint(pointNum, position, elevation, description);
            CogoPoints.Add(cogoPoint);

            return true;
        }
        public bool TryAddPoint(CogoPoint p, PointGroup pg)
        {
            if (pg == null || !PointGroupExists(pg))
            {
                return false;
            }

            if (PointNumberExists(p.PointNumber) || !IsValidPointName(p.PointNumber, out _))
            {
                return false;
            }

            var isAdded = pg.TryAddPoint(p);
            CogoPoints.Add(p);

            return isAdded;
        }
        public void AddPoint(CogoPoint cogoPoint)
        {
            CogoPoints.Add(cogoPoint);
            cogoPoint.PointGroup.Points.Add(cogoPoint);
        }
        public void RemovePoint(CogoPoint cogoPoint)
        {
            CogoPoints.Remove(cogoPoint);
            cogoPoint.PointGroup.Points.Remove(cogoPoint);
        }
        public void OverwritePoint(CogoPoint newPoint)
        {
            var existingPoint = CogoPoints.FirstOrDefault(p => p.PointNumber == newPoint.PointNumber);
            if (existingPoint != null)
            {
                var group = existingPoint.PointGroup;
                RemovePoint(existingPoint);
                TryAddPoint(newPoint, group);
            }
        }

        public bool TryCreatePointGroup(string groupName, Color color, out PointGroup pointGroup)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                pointGroup = null;
                return false;
            }
            if (PointGroups.Any(pg => pg.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase)))
            {
                pointGroup = null;
                return false;
            }

            pointGroup = new(groupName, color, this, CadManager.CogoPointManager.PointBaseScale);
            PointGroups.Add(pointGroup);
            return true;
        }
        public bool TryCreatePointGroup(PointGroup pointGroup)
        {
            if (string.IsNullOrWhiteSpace(pointGroup.Name))
            {
                pointGroup = null;
                return false;
            }
            if (PointGroups.Any(pg => pg.Name.Equals(pointGroup.Name, StringComparison.OrdinalIgnoreCase)))
            {
                pointGroup = null;
                return false;
            }
            PointGroups.Add(pointGroup);
            return true;
        }

        public void DeletePointGroup(PointGroup pg)
        {
            if (pg.Points.Count > 0)
            {
                var copy = pg.Points.ToList();
                foreach (var p in copy)
                {
                    DeletePoint(p);
                }
            }
            PointGroups.Remove(pg);
        }
        public void TryDeletePointGroup(PointGroup pg)
        {
            if (pg.Points.Count > 0)
            {
                var result = MessageBox.Show(
                    "This will delete all points associated with this group. Continue?",
                    "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

                if (result != MessageBoxResult.Yes)
                {
                    foreach (var p in pg.Points)
                    {
                        DeletePoint(p);
                    }
                }
                else { return; }
            }
            PointGroups.Remove(pg);
        }

        public bool TryGetPointGroup(string groupName, out PointGroup pointGroup)
        {
            pointGroup = PointGroups.FirstOrDefault(pg => pg.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));
            if (pointGroup is null)
            {
                return false;
            }
            return true;
        }
        public PointGroup GetPointGroup(string groupName, Color color, double scale)
        {
            var pgExists = TryGetPointGroup(groupName, out PointGroup pointGroup);
            if (!pgExists)
            {
            var isValidName = IsValidPointGroupName(groupName, out string? errorMessage);
                if (!isValidName)
                {
                    throw new ArgumentException($"Invalid point group name: {errorMessage}");
                }
                pointGroup = new(groupName, color, this, scale);
                PointGroups.Add(pointGroup);
            }
            return pointGroup;
        }
        public bool PointGroupExists(PointGroup pg)
        {
            return PointGroups.Any(p => p == pg);
        }

        public bool DeletePoint(CogoPoint point)
        {
            bool deleted = false;
            if (point != null && point.PointGroup != null)
            {
                deleted = point.PointGroup.DeletePoint(point);
                if (deleted)
                {
                    CogoPoints.Remove(point);
                }
            }
            return deleted;
        }

        public bool IsValidPointGroupName(string name, out string? errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(name))
            {
                errorMessage = "Name cannot be empty or whitespace.";
                return false;
            }

            // Trim spaces just for validation purposes
            name = name.Trim();

            // Disallowed characters
            char[] invalidChars = Path.GetInvalidFileNameChars(); // includes \ / : * ? " < > | and control characters
            if (name.IndexOfAny(invalidChars) >= 0)
            {
                errorMessage = $"Name contains invalid characters: {string.Join(" ", invalidChars)}";
                return false;
            }

            // Optional: Disallow other problematic characters
            if (name.Any(c => c == '#' || c == '%'))
            {
                errorMessage = "Name contains disallowed characters like # or %.";
                return false;
            }

            // Verify uniqueness
            if (PointGroups.Any(pg => pg.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                errorMessage = "A point group with this name already exists.";
                return false;
            }

            return true;
        }
        public bool PointGroupNameExists(string name)
        {
            return PointGroups.Any(pg => pg.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsValidPointScale(string input, out string? errorMessage)
        {
            errorMessage = null;
            if (string.IsNullOrWhiteSpace(input))
            {
                errorMessage = "Point scale cannot be empty.";
                return false;
            }
            if (!double.TryParse(input, out double scale))
            {
                errorMessage = "Point scale must be a valid number.";
                return false;
            }
            if (scale <= 0)
            {
                errorMessage = "Point scale must be greater than zero.";
                return false;
            }
            return true;
        }

        public void MergePointGroups(List<PointGroup> mergePGs, PointGroup destinationPG)
        {
            var copy = mergePGs.ToList();
            foreach (var pg in copy) // Enumerate a copy
            {
                bool removed = PointGroups.Remove(pg);
                if (removed)
                {
                    pg.MergeToPointGroup(destinationPG);
                }
            }
        }

        public void UpdatePointExtents()
        {
            if (PointGroups == null || PointGroups.Count == 0) { Extents = Rect.Empty; }

            int processorCount = Environment.ProcessorCount;
            var partialResults = new Rect[processorCount];

            Parallel.For(0, processorCount, i =>
            {
                Rect localUnion = Rect.Empty;

                // Use stride to balance uneven group sizes
                for (int g = i; g < PointGroups.Count; g += processorCount)
                {
                    var group = PointGroups[g];
                    if (group?.Points == null) { continue; }

                    foreach (var point in group.Points)
                    {
                        localUnion.Union(point.Bounds);
                    }
                }
                partialResults[i] = localUnion;
            });

            Rect finalUnion = Rect.Empty;
            foreach (var r in partialResults)
            {
                finalUnion.Union(r);
            }

            Extents = finalUnion;
        }

        public void UpdateCogoPointTree()
        {
            UpdatePointExtents();
            CogoPointTree = new(CadManager, Extents, 5);
        }

        public void Reset()
        {
            PointGroups.Clear();
            CogoPoints.Clear();
        }

        public void UpdateCogoPointsList()
        {
            CogoPoints.Clear();
            foreach (var pointGroup in PointGroups)
            {
                foreach (var point in pointGroup.Points)
                {
                    CogoPoints.Add(point);
                }
            }
        }

        public List<PointGroupDto> GetPointGroupDtos()
        {
            return PointGroups.Select(pg => new PointGroupDto(pg)).ToList();
        }
        public List<CogoPointDto> GetCogoPointDtos()
        {
            return CogoPoints.Select(p => new CogoPointDto(p)).ToList();
        }
        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
