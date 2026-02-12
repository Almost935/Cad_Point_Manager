using Cad_Point_Manager.Models.DrawingObjects;
using Cad_Point_Manager.Models.PointRendering;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Text
{
    public sealed class SceneIdMap
    {
        private readonly Dictionary<(CogoPoint cp, int line), uint> _labelOf = [];
        private readonly Dictionary<CogoPoint, uint> _pointOf = [];
        private readonly Dictionary<PointGroup, uint> _groupOf = [];
        private readonly Dictionary<ObjectLayer, uint> _layerOf = [];
        private readonly Dictionary<DrawingObject, uint> _objectOf = [];
        private uint _nextLabelId;

        public bool TryGetLabelId(CogoPoint cp, int line, out uint id) => _labelOf.TryGetValue((cp, line), out id);
        public bool TryGetPointId(CogoPoint p, out uint id) => _pointOf.TryGetValue(p, out id);
        public bool TryGetGroupId(PointGroup pg, out uint id) => _groupOf.TryGetValue(pg, out id);
        public bool TryGetLayerId(ObjectLayer layer, out uint id) => _layerOf.TryGetValue(layer, out id);
        public bool TryGetObjectId(DrawingObject obj, out uint id) => _objectOf.TryGetValue(obj, out id);

        public bool TryRemoveLabelId(CogoPoint cp, int line) => _labelOf.Remove((cp, line));
        public bool TryRemovePointId(CogoPoint cp) => _pointOf.Remove(cp);
        public bool TryRemoveGroupId(PointGroup pg) => _groupOf.Remove(pg);
        public bool TryRemoveLayerId(ObjectLayer layer) => _layerOf.Remove(layer);

        public uint GetOrAddLabelId(CogoPoint cp, int line, out bool isNew)
        {
            if (_labelOf.TryGetValue((cp, line), out var id)) 
            {
                isNew = false;
                return id; 
            }
            id = _nextLabelId++;
            _labelOf[(cp, line)] = id;

            isNew = true;
            return id;
        }
        public uint GetOrAddPointId(CogoPoint cp, out bool isNew)
        {
            if (_pointOf.TryGetValue(cp, out var id)) 
            { 
                isNew = false;
                return id; 
            }
            id = (uint)_pointOf.Count;
            _pointOf[cp] = id;

            isNew = true;
            return id;
        }
        public uint GetOrAddGroupId(PointGroup pg, out bool isNew)
        {
            if (_groupOf.TryGetValue(pg, out var id)) 
            {
                isNew = false;
                return id; 
            }
            id = (uint)_groupOf.Count;
            _groupOf[pg] = id;

            isNew = true;
            return id;
        }
        public uint GetOrAddLayerId(ObjectLayer layer, out bool isNew)
        {
            if (_layerOf.TryGetValue(layer, out var id)) 
            {
                isNew = false;
                return id; 
            }
            id = (uint)_layerOf.Count;
            _layerOf[layer] = id;

            isNew = true;
            return id;
        }
        public uint GetOrAddObjectId(DrawingObject obj, out bool isNew)
        {
            if (_objectOf.TryGetValue(obj, out var id)) 
            {
                isNew = false;
                return id; 
            }
            id = (uint)_objectOf.Count;
            _objectOf[obj] = id;

            isNew = true;
            return id;
        }

        public int MaxLabelCount => (int)_nextLabelId;
        public int PointCount => _pointOf.Count;
        public int GroupCount => _groupOf.Count;
        public int LayerCount => _layerOf.Count;
        public int ObjectCount => _objectOf.Count;

        public void ClearLabels() { _labelOf.Clear(); _nextLabelId = 0; }
        public void ClearPoints() { _pointOf.Clear(); }
        public void ClearGroups() { _groupOf.Clear(); }
        public void ClearLayers() { _layerOf.Clear(); }
        public void ClearObjects() { _objectOf.Clear(); }
    }
}
