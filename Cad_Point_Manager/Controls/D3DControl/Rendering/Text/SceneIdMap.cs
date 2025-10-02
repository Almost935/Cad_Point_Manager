using Cad_Point_Manager.Models.DrawingObjects3D;
using Cad_Point_Manager.Models.PointRendering;
using System.Collections.Generic;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Text
{
    public sealed class SceneIdMap
    {
        private readonly Dictionary<(CogoPoint cp, int line), uint> _labelOf = [];
        private readonly Dictionary<CogoPoint, uint> _pointOf = [];
        private readonly Dictionary<PointGroup, uint> _groupOf = [];
        private readonly Dictionary<ObjectLayer3D, uint> _layerOf = [];
        private uint _nextLabelId;

        public bool TryGetLabelId(CogoPoint cp, int line, out uint id) => _labelOf.TryGetValue((cp, line), out id);
        public bool TryGetPointId(CogoPoint p, out uint id) => _pointOf.TryGetValue(p, out id);
        public bool TryGetGroupId(PointGroup pg, out uint id) => _groupOf.TryGetValue(pg, out id);
        public bool TryGetLayerId(ObjectLayer3D layer, out uint id) => _layerOf.TryGetValue(layer, out id);

        public bool TryRemoveLabelId(CogoPoint cp, int line) => _labelOf.Remove((cp, line));
        public bool TryRemovePointId(CogoPoint cp) => _pointOf.Remove(cp);
        public bool TryRemoveGroupId(PointGroup pg) => _groupOf.Remove(pg);
        public bool TryRemoveLayerId(ObjectLayer3D layer) => _layerOf.Remove(layer);

        public uint GetOrAddLabelId(CogoPoint cp, int line)
        {
            if (_labelOf.TryGetValue((cp, line), out var id)) { return id; }
            id = _nextLabelId++;
            _labelOf[(cp, line)] = id;
            return id;
        }
        public uint GetOrAddPointId(CogoPoint cp)
        {
            if (_pointOf.TryGetValue(cp, out var id)) return id;
            id = (uint)_pointOf.Count;
            _pointOf[cp] = id;
            return id;
        }
        public uint GetOrAddGroupId(PointGroup pg)
        {
            if (_groupOf.TryGetValue(pg, out var id)) return id;
            id = (uint)_groupOf.Count;
            _groupOf[pg] = id;
            return id;
        }
        public uint GetOrAddLayerId(ObjectLayer3D layer)
        {
            if (_layerOf.TryGetValue(layer, out var id)) { return id; }
            id = (uint)_layerOf.Count;
            _layerOf[layer] = id;
            return id;
        }

        public int MaxLabelCount => (int)_nextLabelId;
        public int PointCount => _pointOf.Count;
        public int GroupCount => _groupOf.Count;
        public int LayerCount => _layerOf.Count;

        public void Clear() { _labelOf.Clear(); _groupOf.Clear(); _pointOf.Clear(); _nextLabelId = 0; }
    }
}
