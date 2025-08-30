using Cad_Point_Manager.Controls.D3DControl;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DirectWrite;
using System.Collections.Generic;
using static Cad_Point_Manager.Helpers.TextRenderingHelpers;

using Buffer = SharpDX.Direct3D11.Buffer;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Text
{
    /// <summary>
    /// Produces and caches glyph triangle meshes (positions in DESIGN UNITS).
    /// You inject your existing tessellator here.
    /// </summary>
    public sealed class GlyphMeshCache
    {
        public struct GlyphMesh
        {
            public static readonly GlyphMesh Empty = new()
            {
                PositionsDU = System.Array.Empty<Vector2>(),
                Indices = System.Array.Empty<int>(),
                BoundsDU = RectangleF.Empty
            };

            public Vector2[] PositionsDU;   // triangle list in DU: v0,v1,v2, v3,v4,v5 ...
            public int[] Indices;       // typically sequential (0..N-1)
            public RectangleF BoundsDU;
            public bool IsEmpty => PositionsDU == null || PositionsDU.Length == 0;
        }

        private readonly FontFace _fontFace;
        private readonly IGlyphTessellator _tess;
        private readonly Dictionary<short, GlyphMesh> _cache = new();

        public GlyphMeshCache(FontFace face, IGlyphTessellator tess)
        {
            _fontFace = face;
            _tess = tess;
        }

        public GlyphMesh Get(short glyphIndex)
        {
            if (glyphIndex == 0) return GlyphMesh.Empty;
            if (_cache.TryGetValue(glyphIndex, out var m)) return m;

            m = _tess.Build(glyphIndex, _fontFace);
            _cache[glyphIndex] = m;
            return m;
        }

        public void Clear() => _cache.Clear();
    }
}
