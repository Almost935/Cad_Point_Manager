using SharpDX.DirectWrite;
using System.Diagnostics;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Text
{
    public sealed class AdvanceWidthCache
    {
        private readonly Dictionary<short, float> _byGlyph = [];

        public AdvanceWidthCache(FontFace fontFace, IEnumerable<short> glyphIds)
        {
            // Make a stable, distinct list (filter out 0 if you don’t want the missing/default glyph)
            var ids = glyphIds.Distinct().Where(gid => gid != 0).ToArray();
            if (ids.Length == 0) return;

            // Metrics come back 1:1 with order of ids[]
            var metrics = fontFace.GetDesignGlyphMetrics(ids, isSideways: false);
            for (int i = 0; i < ids.Length; i++) { _byGlyph[ids[i]] = metrics[i].AdvanceWidth; } 
        }

        public float this[short glyphId] =>
            _byGlyph.TryGetValue(glyphId, out var w) ? w : 0f;

        public static AdvanceWidthCache CreateForAscii(FontFace fontFace)
        {
            var glyphIds = GlyphSets.Ascii32To126(fontFace); // your existing helper
            return new AdvanceWidthCache(fontFace, glyphIds);
        }
    }
}
