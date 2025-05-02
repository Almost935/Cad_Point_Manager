

using Cad_Point_Manager.Controls.D3DControl;
using SharpDX;

namespace Cad_Point_Manager.Helpers
{
    public static class GlobalHelperProperties
    {
        public const float _zoomFactor = 1.3f;

        public const float _lineGlowTransparency = 0.5f;
        public const float _lineGlowPixelWidth = 5;


        public const float _textHeightToGlowOffsetFactor = 0.05f;
        public const int _maxLineVertices = 10000000;
        public const int _maxTextVertices = 10000000;
        public const float _textHeightToFontSizeFactor = 1.5f;
        public const float _textHeightToSpaceWidthFactor = 0.6f;

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

        public static readonly Vector4 _selectedObjectColor = new(0.07f, 0.85f, 0, 1);
        public static readonly Vector4 _selectedMouseOverObjectColor = new(185.0f / 255.0f, 1.0f, 179.0f / 255.0f, 1);
        public static readonly Vector4 _selectedMouseOverGlowColor = new(170.0f / 255.0f, 252.0f / 255.0f, 235.0f / 255.0f, 1);

        public static readonly float _dxfPointToExtentsBaseFactor = 0.05f;
    }
}
