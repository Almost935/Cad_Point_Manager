// Rendering/D3D/D3dStateController.cs
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

        public void SetPointOffset(CogoPoint cp, Vector2 offset, bool? hasLeaderLine = null)
        {
            if (!_ids.TryGetPointId(cp, out var pid)) { return; }

            ref var s = ref _bufs.PointSpan[(int)pid];
            s.Offset = offset;
            //_bufs.PointSpan[(int)pid].Offset = offset;
            if (hasLeaderLine is not null)
            {
                if ((bool)hasLeaderLine) { s.Flags |= (uint)CogoPointFlags.HasLeaderLine; }
                else { s.Flags &= ~(uint)CogoPointFlags.HasLeaderLine; }
            }
            _dirtyPoints.Add(pid);
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
