using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Models.PointRendering;
using SharpDX;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Matrix = System.Windows.Media.Matrix;

namespace Cad_Point_Manager.Models
{
    public class CogoPointManager : INotifyPropertyChanged
    {
        #region Fields
        private List<int> _usedPointNumbers = [];

        private ObservableCollection<KeyValuePair<string, PointGroup>> _pointGroups = [];
        private PointGroup _activePointGroup;
        private bool _pointsDirty = false;
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
        public bool PointsDirty
        {
            get => _pointsDirty;
            set
            {
                if (_pointsDirty != value)
                {
                    _pointsDirty = value;
                    OnPropertyChanged(nameof(PointsDirty));
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

        public Bounds Extents { get; set; } = Bounds.Empty;

        public List<int> UsedPointNumbers => PointGroups.SelectMany(pg => pg.Value.Points).Select(p => p.PointNumber).ToList();
        #endregion

        #region Constructor
        public CogoPointManager(CadManager3D cadManager)
        {
            _cadManager = cadManager;
        }
        #endregion

        #region Methods
        private bool PointNumberExists(int num)
        {
            return PointGroups.SelectMany(pg => pg.Value.Points).Any(p => p.PointNumber == num);
        }

        public void UpdateVisualTransforms(Matrix matrix)
        {
            foreach (var pg in PointGroups)
            {
                pg.Value.UpdateVisualTransforms(matrix);
            }
            //Parallel.ForEach(PointGroups, pointGroup =>
            //{
            //    pointGroup.Value.UpdateScreenSpaceCoordinates(matrix);
            //});
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

        public bool TryAddPointToActiveGroup(int pointNum, Vector3 position, float elevation = 0, string description = "")
        {
            if (ActivePointGroup == null || PointNumberExists(pointNum))
            {
                return false;
            }

            ActivePointGroup.AddPoint(pointNum, position, elevation, description);
            UpdateCogoPointsList();
            return true;
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

            pointGroup = new(groupName, color, this, pointScale);
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

        public void UpdatePointExtents()
        {
            if (PointGroups == null || PointGroups.Count == 0) { Extents = Bounds.Empty; }

            int processorCount = Environment.ProcessorCount;
            var partialResults = new Rect[processorCount];

            Parallel.For(0, processorCount, i =>
            {
                Bounds localUnion = Bounds.Empty;

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

            Bounds finalUnion = Bounds.Empty;
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

        public void SetCadManagerPointVerticesDirty()
        {
            if (!CadManager.PointTextVerticesDirty)
            {
                CadManager.PointTextVerticesDirty = true;
            }
            if (!CadManager.PointCircleVerticesDirty)
            {
                CadManager.PointCircleVerticesDirty = true;
            }
        }

        private void UpdateCogoPointsList()
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
