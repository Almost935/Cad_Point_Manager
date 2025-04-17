

using SharpDX;

namespace Cad_Point_Manager.Helpers
{
    public static class GlobalHelperProperties
    {
        public const float _zoomFactor = 1.3f;
        public const float _glowTransparency = 0.3f;
        public const float _textHeightToGlowOffsetFactor = 0.05f;

        public static readonly List<Vector2> _glowOffsetDirections =
        [
            new Vector2(-1, 0), 
            new Vector2(0, 1),
            new Vector2(1, 0),
            new Vector2(0, -1),
            new Vector2((float)Math.Sqrt(2), (float)Math.Sqrt(2)),
            new Vector2((float)Math.Sqrt(2), -(float)Math.Sqrt(2)),
            new Vector2(-(float)Math.Sqrt(2), (float)Math.Sqrt(2)),
            new Vector2(-(float)Math.Sqrt(2), -(float)Math.Sqrt(2)),
        ];
    }
}
