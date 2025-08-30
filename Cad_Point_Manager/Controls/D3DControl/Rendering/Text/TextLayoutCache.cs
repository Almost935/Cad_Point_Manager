using SharpDX;
using SharpDX.DirectWrite;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Text
{
    /// <summary>
    /// Shapes strings into glyph indices + design-unit advances and caches by string.
    /// </summary>
    public sealed class TextLayoutCache : IDisposable
    {
        private readonly Factory _dwFactory;
        private readonly TextFormat _format;                 // single font family/weight/style; size irrelevant
        private readonly Dictionary<string, GlyphLayout> _cache = new(StringComparer.Ordinal);
        private FontFace _fontFace;
        private bool _disposed;

        public TextLayoutCache(Factory dwFactory, TextFormat format, FontFace fontFace)
        {
            _dwFactory = dwFactory ?? throw new ArgumentNullException(nameof(dwFactory));
            _format = format ?? throw new ArgumentNullException(nameof(format));
            _fontFace = fontFace ?? throw new ArgumentNullException(nameof(fontFace));
        }

        public FontFace FontFace => _fontFace;

        public void ReplaceFontFace(FontFace face) => _fontFace = face ?? throw new ArgumentNullException(nameof(face));

        /// <summary>Return shaped glyphs + design-unit advances for the string (cached).</summary>
        public GlyphLayout Get(string s)
        {
            if (string.IsNullOrEmpty(s)) return GlyphLayout.Empty;
            if (_cache.TryGetValue(s, out var L)) return L;

            // Optional: we still make a TextLayout because it's fast and future-proof (features, bidi, etc.).
            using var layout = new TextLayout(_dwFactory, s, _format, float.MaxValue, float.MaxValue);
            var collector = new GlyphRunCollector();
            layout.Draw(null, collector, 0, 0);
            // (We don't strictly need runs here since we use design metrics, but leave it for completeness.)

            // Map chars→glyphs, then pull design metrics for advances.
            var cps = new int[s.Length];
            for (int i = 0; i < s.Length; i++) cps[i] = s[i];

            short[] glyphIndices = _fontFace.GetGlyphIndices(cps);
            var metrics = _fontFace.GetDesignGlyphMetrics(glyphIndices, false /* sideways */);

            var advDU = new float[glyphIndices.Length];
            float width = 0;
            for (int i = 0; i < glyphIndices.Length; i++)
            {
                float a = metrics[i].AdvanceWidth; // design units
                advDU[i] = a;
                width += a;
            }

            var layoutOut = new GlyphLayout
            {
                GlyphIndices = glyphIndices,
                AdvanceDU = advDU,
                TotalWidthDU = width
            };
            _cache[s] = layoutOut;
            return layoutOut;
        }

        public sealed class GlyphLayout
        {
            public static readonly GlyphLayout Empty = new()
            {
                GlyphIndices = Array.Empty<short>(),
                AdvanceDU = Array.Empty<float>(),
                TotalWidthDU = 0
            };

            public short[] GlyphIndices;
            public float[] AdvanceDU;
            public float TotalWidthDU;

            public int Count => GlyphIndices?.Length ?? 0;
        }

        /// <summary>
        /// Minimal TextRenderer to satisfy Draw(); only collects runs. You already hit the correct override shape.
        /// </summary>
        private sealed class GlyphRunCollector : TextRendererBase
        {
            public override Result DrawGlyphRun(
                object clientDrawingContext,
                float baselineOriginX,
                float baselineOriginY,
                SharpDX.Direct2D1.MeasuringMode measuringMode,
                GlyphRun glyphRun,
                GlyphRunDescription glyphRunDescription,
                ComObject clientDrawingEffect)
            {
                // No-op; kept for completeness. You can inspect glyphRun if you want.
                return Result.Ok;
            }

            public override Result DrawUnderline(object ctx, float x, float y, ref Underline underline, ComObject effect)
                => Result.Ok;

            public override Result DrawStrikethrough(object ctx, float x, float y, ref Strikethrough strike, ComObject effect)
                => Result.Ok;

            public override Result DrawInlineObject(object ctx, float x, float y, InlineObject inlineObject, bool isSideways, bool isRightToLeft, ComObject effect)
                => Result.Ok;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _fontFace?.Dispose();
            _fontFace = null;
        }
    }
}
