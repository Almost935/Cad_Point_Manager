using Cad_Point_Manager.Common.Collections;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Models.PointRendering;
using SharpDX;
using SharpDX.DirectWrite;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using Matrix = System.Windows.Media.Matrix;

namespace Cad_Point_Manager.Models
{
    public class CogoPointManager : INotifyPropertyChanged
    {
        #region Fields
        private const string _fontName = "Arial";

        private ObservableCollection<KeyValuePair<string, PointGroup>> _pointGroups = [];
        private PointGroup _activePointGroup;
        private CadManager3D _cadManager;
        private ObservableCollection<CogoPoint> _cogoPoints = [];
        #endregion

        #region Properties
        public ObservableCollection<KeyValuePair<string, PointGroup>> PointGroups
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
        public ObservableCollection<CogoPoint> CogoPoints
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

        public Rect Extents { get; set; } = Rect.Empty;
        public Matrix CurrentlyAppliedMatrix { get; set; } = Matrix.Identity;

        public List<int> UsedPointNumbers => PointGroups.SelectMany(pg => pg.Value.Points).Select(p => p.PointNumber).ToList();
        public bool PointExists(int pointNumber) => PointGroups.SelectMany(pg => pg.Value.Points).Any(p => p.PointNumber == pointNumber);
        #endregion

        #region Constructor
        public CogoPointManager(CadManager3D cadManager)
        {
            _cadManager = cadManager;
        }
        #endregion

        #region Methods
        public int GetNextAvailablePointNumber(int startCount)
        {
            int num = startCount;
            while (PointNumberExists(num)){ num++; }
            return num;
        }
        private bool PointNumberExists(int num)
        {
            return PointGroups.SelectMany(pg => pg.Value.Points).Any(p => p.PointNumber == num);
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

        public void AddPoint(CogoPoint cogoPoint)
        {
            CogoPoints.Add(cogoPoint);
            cogoPoint.PointGroup.Points.Add(cogoPoint);
        }

        public bool TryCreatePointGroup(string groupName, Vector4 color, double pointScale, out PointGroup pointGroup)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                pointGroup = null;
                return false;
            }
            if (PointGroups.Any(pg => pg.Key.Equals(groupName, StringComparison.OrdinalIgnoreCase)))
            {
                pointGroup = null;
                return false;
            }

            pointGroup = new(groupName, color, this, CadManager.PointBaseScale);
            PointGroups.Add(new KeyValuePair<string, PointGroup>(groupName, pointGroup));
            return true;
        }

        public bool TryGetPointGroup(string groupName, out PointGroup pointGroup)
        {
            var pair = PointGroups.FirstOrDefault(pg => pg.Key.Equals(groupName, StringComparison.OrdinalIgnoreCase));
            if (pair.Equals(default(KeyValuePair<string, PointGroup>)))
            {
                pointGroup = null;
                return false;
            }

            pointGroup = pair.Value;
            return true;
        }
        public bool PointGroupExists(PointGroup pg)
        {
            return PointGroups.Any(p => p.Value == pg);
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
            if (PointGroups.Any(pg => pg.Key.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                errorMessage = "A point group with this name already exists.";
                return false;
            }

            return true;
        }

        public void MergePointGroups(List<PointGroup> mergePGs, PointGroup destinationPG)
        {
            var copy = mergePGs.ToList();
            foreach (var pg in copy) // Enumerate a copy
            {
                bool removed = PointGroups.Remove(PointGroups.FirstOrDefault(p => p.Value == pg));
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
                    var group = PointGroups[g].Value;
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
                foreach (var point in pointGroup.Value.Points)
                {
                    CogoPoints.Add(point);
                }
            }
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
