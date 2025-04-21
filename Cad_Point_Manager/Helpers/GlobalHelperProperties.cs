

using Cad_Point_Manager.Controls.D3DControl;
using SharpDX;

namespace Cad_Point_Manager.Helpers
{
    public static class GlobalHelperProperties
    {
        public const float _zoomFactor = 1.3f;

        public const float _lineGlowTransparency = 0.4f;
        public const float _lineGlowPixelWidth = 5;

        public const float _textGlowTransparency = 0.4f;
        public const float _textGlowPixelWidth = 20;
        public const float _textHeightToTextGlowOffsetFactor = 0.15f;

        public const float _textHeightToGlowOffsetFactor = 0.05f;
        public const int _maxLineVertices = 10000000;
        public const int _maxTextVertices = 10000000;

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

        public static float GetGlowOffset(Matrix dxfInitialViewMatrix)
        {
            return 0;
        }
    }
}
