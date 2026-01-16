// Rendering/D3D/D3dStateController.cs
using Cad_Point_Manager.Models.DrawingObjects3D;
using Cad_Point_Manager.Models.PointRendering;
using SharpDX;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Text
{
    public sealed class D3dStateController
    {
        #region Fields
        private readonly SceneIdMap _ids;
        private readonly D3dStateBuffers _bufs;
        private readonly HashSet<uint> _dirtyObjects = [];
        private readonly HashSet<uint> _dirtyGroups = [];
        private readonly HashSet<uint> _dirtyLabels = [];
        private readonly HashSet<uint> _dirtyPoints = [];
        private readonly HashSet<uint> _dirtyLayers = [];
        #endregion

        #region Constructors
        public D3dStateController(SceneIdMap ids, D3dStateBuffers bufs)
        {
            _ids = ids;
            _bufs = bufs;
        }
        #endregion

        #region Methods
        public ReadOnlySpan<LabelState> GetLabelStates() => _bufs.LabelSpan;
        public ReadOnlySpan<PointState> GetPointStates() => _bufs.PointSpan;
        public ReadOnlySpan<GroupState> GetGroupStates() => _bufs.GroupSpan;
        public ReadOnlySpan<LayerState> GetLayerStates() => _bufs.LayerSpan;
        public ReadOnlySpan<ObjectState> GetObjectStates() => _bufs.ObjectSpan;

        public LabelState[] GetLabelStatesSnapshot()
        {
            var src = _bufs.LabelSpan;
            var dst = new LabelState[src.Length];
            src.CopyTo(dst);
            return dst;
        }
        public PointState[] GetPointStatesSnapshot()
        {
            var src = _bufs.PointSpan;
            var dst = new PointState[src.Length];
            src.CopyTo(dst);
            return dst;
        }
        public GroupState[] GetGroupStatesSnapshot()
        {
            var src = _bufs.GroupSpan;
            var dst = new GroupState[src.Length];
            src.CopyTo(dst);
            return dst;
        }
        public LayerState[] GetLayerStatesSnapshot()
        {
            var src = _bufs.LayerSpan;
            var dst = new LayerState[src.Length];
            src.CopyTo(dst);
            return dst;
        }
        public ObjectState[] GetObjectStatesSnapshot()
        {
            var src = _bufs.ObjectSpan;
            var dst = new ObjectState[src.Length];
            src.CopyTo(dst);
            return dst;
        }

        public void SetObjectSelected(DrawingObject obj, bool selected)
        {
            if (!_ids.TryGetObjectId(obj, out var oId)) { return; }
            ref var s = ref _bufs.ObjectSpan[(int)oId];
            if (selected) { s.Flags |= (uint)ObjectFlags.Selected; }
            else { s.Flags &= ~(uint)ObjectFlags.Selected; }
            _dirtyObjects.Add(oId);
        }
        public void SetObjectMouseOver(DrawingObject obj, bool mouseOver)
        {
            if (!_ids.TryGetObjectId(obj, out var oId)) { return; }
            ref var s = ref _bufs.ObjectSpan[(int)oId];
            if (mouseOver) { s.Flags |= (uint)ObjectFlags.MouseOver; }
            else { s.Flags &= ~(uint)ObjectFlags.MouseOver; }
            _dirtyObjects.Add(oId);
        }

        public void SetLayerVisibility(ObjectLayer layer, bool visible)
        {
            if (!_ids.TryGetLayerId(layer, out var lid)) { return; }
            ref var ls = ref _bufs.LayerSpan[(int)lid];
            if (visible) ls.Flags |= 1u; else ls.Flags &= ~1u;
            _dirtyLayers.Add(lid);
        }
        public void SetLayerColor(ObjectLayer layer, Vector4 color)
        {
            if (!_ids.TryGetLayerId(layer, out var lid)) { return; }
            ref var ls = ref _bufs.LayerSpan[(int)lid];
            ls.Color = color;
            _dirtyLayers.Add(lid);
        }

        public void SetPointAnchorMouseOver(CogoPoint cp, bool mouseOver)
        {
            if (!_ids.TryGetPointId(cp, out var pid)) { return; }
            ref var s = ref _bufs.PointSpan[(int)pid];
            if (mouseOver) { s.Flags |= (uint)CogoPointFlags.MouseOverAnchor; }
            else { s.Flags &= ~(uint)CogoPointFlags.MouseOverAnchor; }
            _dirtyPoints.Add(pid);
        }
        public void SetPointSelected(CogoPoint cp, bool selected)
        {
            if (!_ids.TryGetPointId(cp, out var pid)) { return; }
            ref var s = ref _bufs.PointSpan[(int)pid];
            if (selected) { s.Flags |= (uint)CogoPointFlags.Selected; }
            else { s.Flags &= ~(uint)CogoPointFlags.Selected; }
            _dirtyPoints.Add(pid);
        }
        public void SetPointMouseOver(CogoPoint cp, bool mouseOver)
        {
            if (!_ids.TryGetPointId(cp, out var pid)) { return; }
            ref var s = ref _bufs.PointSpan[(int)pid];
            if (mouseOver) { s.Flags |= (uint)CogoPointFlags.MouseOver; }
            else { s.Flags &= ~(uint)CogoPointFlags.MouseOver; }
            _dirtyPoints.Add(pid);
        }
        public void SetPointOffset(CogoPoint cp, Vector2 offset)
        {
            if (!_ids.TryGetPointId(cp, out var pid)) { return; }

            ref var s = ref _bufs.PointSpan[(int)pid];
            s.Offset = offset;
            _dirtyPoints.Add(pid);
        }
        public void SetPointInfoOffset(CogoPoint cp, Vector2 offset, bool? hasLeaderLine = null, bool? isFlippedY = null, bool? isFlippedX = null)
        {
            if (!_ids.TryGetPointId(cp, out var pid)) { return; }

            ref var s = ref _bufs.PointSpan[(int)pid];
            s.PointInfoOffset = offset;
            if (hasLeaderLine is not null)
            {
                if ((bool)hasLeaderLine) { s.Flags |= (uint)CogoPointFlags.HasLeaderLine; }
                else { s.Flags &= ~(uint)CogoPointFlags.HasLeaderLine; }
            }
            if (isFlippedY is not null)
            {
                if ((bool)isFlippedY) { s.Flags |= (uint)CogoPointFlags.IsFlippedY; }
                else { s.Flags &= ~(uint)CogoPointFlags.IsFlippedY; }
            }
            if (isFlippedX is not null)
            {
                if ((bool)isFlippedX) { s.Flags |= (uint)CogoPointFlags.IsFlippedX; }
                else { s.Flags &= ~(uint)CogoPointFlags.IsFlippedX; }
            }
            _dirtyPoints.Add(pid);
        }
        public void SetPointGroupId(CogoPoint cp, uint gId)
        {
            if (!_ids.TryGetPointId(cp, out var pid)) { return; }

            ref var s = ref _bufs.PointSpan[(int)pid];
            s.GroupId = gId;
            _dirtyPoints.Add(pid);
        }
        public void SetPointVisible(CogoPoint cp, bool visible)
        {
            if (!_ids.TryGetPointId(cp, out var pid)) return;
            ref var s = ref _bufs.PointSpan[(int)pid];
            if (visible) s.Flags |= (uint)CogoPointFlags.Visible;
            else s.Flags &= ~(uint)CogoPointFlags.Visible;
            _dirtyPoints.Add(pid);
        }

        public void SetGroupVisibility(PointGroup pg, bool visible)
        {
            if (!_ids.TryGetGroupId(pg, out var gid)) { return; }
            ref var gs = ref _bufs.GroupSpan[(int)gid];
            if (visible) gs.Flags |= 1u; else gs.Flags &= ~1u;
            _dirtyGroups.Add(gid);
        }
        public void SetGroupScaleColorBaseOffset(PointGroup pg, float scale, Vector4 color, float baseXoffset)
        {
            if (!_ids.TryGetGroupId(pg, out var gid)) { return; }
            ref var gs = ref _bufs.GroupSpan[(int)gid];
            gs.Scale = scale; gs.Color = color; gs.TextInfoBaseXoffset = baseXoffset;
            _dirtyGroups.Add(gid);
        }

        public void SetLabelOffsets(CogoPoint cp, Vector2 pointNumOffset, Vector2 elevOffset, Vector2 descrOffset)
        {
            void Set(int line)
            {
                if (!_ids.TryGetLabelId(cp, line, out var lid)) { return; }
                _bufs.LabelSpan[(int)lid].Offset = line switch
                {
                    0 => pointNumOffset,
                    1 => elevOffset,
                    2 => descrOffset,
                    _ => Vector2.Zero
                };
                _dirtyLabels.Add(lid);
            }
            Set(0); Set(1);
            if (!string.IsNullOrEmpty(cp.Description)) Set(2);
        }
        public void SetLabelVisible(CogoPoint cp, int line, bool visible)
        {
            if (!_ids.TryGetLabelId(cp, line, out var lid)) return;
            ref var ls = ref _bufs.LabelSpan[(int)lid];
            if (visible) ls.Flags |= 1u;   // LabelFlags.Visible
            else ls.Flags &= ~1u;
            _dirtyLabels.Add(lid);
        }

        public void FlushObjectUpdates()
        {
            _bufs.FlushObjectSubset(_dirtyObjects);
            _dirtyObjects.Clear();
        }
        public void FlushGroupUpdates()
        {
            _bufs.FlushGroupSubset(_dirtyGroups);
            _dirtyGroups.Clear();
        }
        public void FlushLayerUpdates()
        {
            _bufs.FlushLayerSubset(_dirtyLayers);
            _dirtyLayers.Clear();
        }
        public void FlushPointUpdates()
        {
            _bufs.FlushPointSubset(_dirtyPoints);
            _dirtyPoints.Clear();
        }
        public void FlushLabelUpdates()
        {
            _bufs.FlushLabelSubset(_dirtyLabels);
            _dirtyLabels.Clear();
        }
        #endregion
    }
}
