using Cad_Point_Manager.Models.PointRendering;
using SharpDX;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Cad_Point_Manager.Models
{
    public class CogoPointManager : INotifyPropertyChanged
    {
        #region Fields
        private List<int> _usedPointNumbers = [];

        private ObservableCollection<KeyValuePair<string, PointGroup>> _pointGroups = [];
        private PointGroup _activePointGroup;
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

        public List<int> UsedPointNumbers => PointGroups.SelectMany(pg => pg.Value.Points).Select(p => p.PointNumber).ToList();
        #endregion

        #region Methods
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

        public bool TryAddPointToActiveGroup(int pointNum, Vector3 position, float elevation = 0, string description = "")
        {
            if (ActivePointGroup == null || PointNumberExists(pointNum))
            {
                return false;
            }

            ActivePointGroup.AddPoint(pointNum, position, elevation, description);
            return true;
        }

        public bool TryCreatePointGroup(string groupName, Vector4 color, float textHeight, float markerSize, out PointGroup pointGroup)
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

            pointGroup = new(groupName, color, textHeight, markerSize, this);
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

        public void Reset()
        {
            PointGroups.Clear();
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
