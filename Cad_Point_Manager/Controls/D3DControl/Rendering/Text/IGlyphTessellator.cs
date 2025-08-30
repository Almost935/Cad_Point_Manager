using SharpDX.DirectWrite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Text
{
    /// <summary>
    /// Contract for your existing tessellator (wrap it with this), returning triangles in DESIGN UNITS.
    /// </summary>
    public interface IGlyphTessellator
    {
        GlyphMeshCache.GlyphMesh Build(short glyphIndex, FontFace fontFace);
    }
}
