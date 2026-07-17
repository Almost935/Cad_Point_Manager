using SharpDX;
using SixLabors.Fonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Msdf
{
    public sealed class MsdfGlyph
    {
        public char Character { get; init; }
        public uint GlyphIndex { get; init; }
        public float Advance { get; init; }

        // Font-space bounds
        public Vector2 PlaneMin { get; init; }
        public Vector2 PlaneMax { get; init; }

        // Texture-space bounds
        public Vector2 UvMin { get; init; }
        public Vector2 UvMax { get; init; }

        public Vector2 PlaneSize => new(PlaneMax.X - PlaneMin.X, PlaneMin.Y - PlaneMax.Y);
        public Vector2 UvSize => UvMax - UvMin;
    }
}
