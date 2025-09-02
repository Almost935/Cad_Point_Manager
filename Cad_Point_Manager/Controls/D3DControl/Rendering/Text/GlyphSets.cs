using SharpDX.DirectWrite;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Text
{
    public static class GlyphSets
    {
        /// <summary>
        /// Maps ASCII codepoints [32..126] to glyph indices for this font.
        /// Skips missing glyphs (id==0). Optionally include a fallback ('?') if present.
        /// </summary>
        public static IReadOnlyList<short> Ascii32To126(FontFace fontFace, bool includeFallbackQuestionMark = true)
        {
            // ASCII range 32..126 inclusive has 95 codepoints
            var cps = Enumerable.Range(32, 95).ToArray();      // 32..126
            var ids = fontFace.GetGlyphIndices(cps);           // parallel to cps

            var set = new HashSet<short>();
            for (int i = 0; i < ids.Length; i++)
            {
                short gid = ids[i];
                if (gid != 0) set.Add(gid);                    // 0 = missing glyph, skip
            }

            if (includeFallbackQuestionMark)
            {
                // Ensure '?' is present for unexpected characters in your data
                int qm = (int)'?';
                var qid = fontFace.GetGlyphIndices(new[] { qm })[0];
                if (qid != 0) set.Add(qid);
            }

            return set.ToList();
        }
    }
}
