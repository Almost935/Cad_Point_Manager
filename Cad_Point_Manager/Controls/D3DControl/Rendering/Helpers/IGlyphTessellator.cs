using SharpDX.DirectWrite;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Helpers
{
    /// <summary>
    /// Contract for your existing tessellator (wrap it with this), returning triangles in DESIGN UNITS.
    /// </summary>
    public interface IGlyphTessellator
    {
        GlyphMeshCache.GlyphMesh Build(short glyphIndex, FontFace fontFace);
    }
}
