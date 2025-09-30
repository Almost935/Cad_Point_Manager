// Rendering/D3D/D3dStateController.cs
using Cad_Point_Manager.Models.DrawingObjects3D;
using Cad_Point_Manager.Models.PointRendering;
using SharpDX;
using System.Collections.Generic;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Text
{
    public sealed class D3dStateController : IRenderStateUpdater
    {
        private readonly SceneIdMap _ids;
        private readonly D3dStateBuffers _bufs;
        private readonly HashSet<uint> _dirtyLabels = [];
        private readonly HashSet<uint> _dirtyPoints = [];
        private readonly HashSet<uint> _dirtyLayers = [];

        public void SetLayerVisibility(ObjectLayer3D layer, bool visible)
        {
            if (!_ids.TryGetLayerId(layer, out var lid)) { return; }
            ref var ls = ref _bufs.LayerSpan[(int)lid];
            if (visible) ls.Flags |= 1u; else ls.Flags &= ~1u;
            _dirtyLayers.Add(lid);
        }

        public void SetLayerColor(ObjectLayer3D layer, Vector4 color)
        {
            if (!_ids.TryGetLayerId(layer, out var lid)) { return; }
            ref var ls = ref _bufs.LayerSpan[(int)lid];
            ls.Color = color;
            _dirtyLayers.Add(lid);
        }

        public D3dStateController(SceneIdMap ids, D3dStateBuffers bufs)
        {
            _ids = ids;
            _bufs = bufs;
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
        public void SetPointOffset(CogoPoint cp, Vector2 offset, bool? hasLeaderLine = null)
        {
            if (!_ids.TryGetPointId(cp, out var pid)) { return; }

            ref var s = ref _bufs.PointSpan[(int)pid];
            s.Offset = offset;
            if (hasLeaderLine is not null)
            {
                if ((bool)hasLeaderLine) { s.Flags |= (uint)CogoPointFlags.HasLeaderLine; }
                else { s.Flags &= ~(uint)CogoPointFlags.HasLeaderLine; }
            }
            _dirtyPoints.Add(pid);
        }
        public void SetPointLeaderLineAngle(CogoPoint cp, float angle)
        {
            if (!_ids.TryGetPointId(cp, out var pid)) { return; }

            ref var s = ref _bufs.PointSpan[(int)pid];
            s.LeaderLineAngle = angle;
            _dirtyPoints.Add(pid);
        }

        public void SetGroupVisibility(PointGroup pg, bool visible)
        {
            if (!_ids.TryGetGroupId(pg, out var gid)) { return; }
            ref var gs = ref _bufs.GroupSpan[(int)gid];
            if (visible) gs.Flags |= 1u; else gs.Flags &= ~1u;
            _bufs.FlushAll();
        }

        public void SetGroupScaleColor(PointGroup pg, float scale, Vector4 color)
        {
            if (!_ids.TryGetGroupId(pg, out var gid)) { return; }
            ref var gs = ref _bufs.GroupSpan[(int)gid];
            gs.Scale = scale; gs.Color = color;
            _bufs.FlushAll();
        }

        public void SetLabelOffset(CogoPoint cp, Vector2 offset)
        {
            void Set(int line)
            {
                if (!_ids.TryGetLabelId(cp, line, out var lid)) { return; }
                _bufs.LabelSpan[(int)lid].Offset = offset;
                _dirtyLabels.Add(lid);
            }
            Set(0); Set(1);
            if (!string.IsNullOrEmpty(cp.Description)) Set(2);
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
    }
}
