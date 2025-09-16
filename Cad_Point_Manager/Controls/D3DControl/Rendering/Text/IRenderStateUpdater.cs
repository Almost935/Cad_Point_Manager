// Rendering/D3D/IRenderStateUpdater.cs
using Cad_Point_Manager.Models.PointRendering;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Text
{
    public interface IRenderStateUpdater
    {
        void SetPointSelected(CogoPoint cp, bool selected);
        void SetGroupVisibility(PointGroup pg, bool visible);
        void SetGroupScaleColor(PointGroup pg, float scale, SharpDX.Vector4 color);
        void SetLabelOffset(CogoPoint cp, SharpDX.Vector2 offset);
        void FlushLabelUpdates();
    }
}
