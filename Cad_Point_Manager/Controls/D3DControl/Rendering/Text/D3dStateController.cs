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
        private readonly HashSet<uint> _dirty = [];

        public D3dStateController(SceneIdMap ids, D3dStateBuffers bufs)
        {
            _ids = ids;
            _bufs = bufs;
        }

        public void SetPointSelected(CogoPoint cp, bool selected)
        {
            void Flip(int line)
            {
                if (!_ids.TryGetLabelId(cp, line, out var lid)) { return; }
                ref var s = ref _bufs.LabelSpan[(int)lid];
                if (selected) { s.Flags |= (uint)LabelFlags.Selected; }
                else { s.Flags &= ~(uint)LabelFlags.Selected; } 
                _dirty.Add(lid);
            }
            Flip(0); Flip(1);
            if (!string.IsNullOrEmpty(cp.Description)) { Flip(2); }
        }

        public void SetGroupVisibility(PointGroup pg, bool visible)
        {
            if (!_ids.TryGetGroupId(pg, out var gid)) return;
            ref var gs = ref _bufs.GroupSpan[(int)gid];
            if (visible) gs.Flags |= 1u; else gs.Flags &= ~1u;
            _bufs.FlushAll(); // groups are few; simplest is full (or add a group-subset flush if you prefer)
        }

        public void SetGroupScaleColor(PointGroup pg, float scale, Vector4 color)
        {
            if (!_ids.TryGetGroupId(pg, out var gid)) return;
            ref var gs = ref _bufs.GroupSpan[(int)gid];
            gs.Scale = scale; gs.Color = color;
            _bufs.FlushAll();
        }

        public void SetLabelOffset(CogoPoint cp, Vector2 offset)
        {
            void Set(int line)
            {
                if (!_ids.TryGetLabelId(cp, line, out var lid)) return;
                _bufs.LabelSpan[(int)lid].Offset = offset;
                _dirty.Add(lid);
            }
            Set(0); Set(1);
            if (!string.IsNullOrEmpty(cp.Description)) Set(2);
        }

        public void FlushLabelUpdates()
        {
            _bufs.FlushLabelSubset(_dirty);
            _dirty.Clear();
        }
    }
}
