using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Msdf
{
    public sealed class MsdfJsonRoot
    {
        [JsonPropertyName("atlas")]
        public required AtlasInfo Atlas { get; init; }

        [JsonPropertyName("metrics")]
        public required FontMetrics Metrics { get; init; }

        [JsonPropertyName("glyphs")]
        public required List<GlyphInfo> Glyphs { get; init; }

        [JsonPropertyName("kerning")]
        public List<KerningPair>? Kerning { get; init; }
    }

    public sealed class AtlasInfo
    {
        public required string Type { get; init; }
        public float DistanceRange { get; init; }
        public float DistanceRangeMiddle { get; init; }
        public float Size { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public required string YOrigin { get; init; }
    }

    public sealed class FontMetrics
    {
        public float EmSize { get; init; }
        public float LineHeight { get; init; }
        public float Ascender { get; init; }
        public float Descender { get; init; }
        public float UnderlineY { get; init; }
        public float UnderlineThickness { get; init; }
    }

    public sealed class GlyphInfo
    {
        public int Unicode { get; init; }

        public float Advance { get; init; }

        public PlaneBounds? PlaneBounds { get; init; }

        public AtlasBounds? AtlasBounds { get; init; }
    }

    public sealed class PlaneBounds
    {
        public float Left { get; init; }
        public float Bottom { get; init; }
        public float Right { get; init; }
        public float Top { get; init; }
    }

    public sealed class AtlasBounds
    {
        public float Left { get; init; }
        public float Bottom { get; init; }
        public float Right { get; init; }
        public float Top { get; init; }
    }

    public sealed class KerningPair
    {
        public int Unicode1 { get; init; }
        public int Unicode2 { get; init; }
        public float Advance { get; init; }
    }
}
