using Cad_Point_Manager.Controls.D3DControl.Rendering.Msdf;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.PointRendering
{
    public sealed class GlyphPlacement
    {
        public MsdfGlyph Glyph { get; init; }
        public RectangleF Bounds { get; init; }
        public float PenX { get; init; }
    }
}
