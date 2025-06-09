using SharpDX;

namespace Cad_Point_Manager.Helpers
{
    public static class GlobalHelperProperties
    {
        public const float _zoomFactor = 1.3f;

        public const float _lineGlowTransparency = 0.4f;
        public static readonly Vector4 _lineGlowColor = new(0, 0, 0, _lineGlowTransparency);
        public const float _lineGlowPixelWidth = 5;

        public const float _textHeightToGlowOffsetFactor = 0.05f;
        public const int _initialLineVertices = 5000;
        public const int _initialTextVertices = 5000;
        public const int _initialCircleVertices = 1000;
        public const int _initialLineGlowVertices = 1000;
        public const int _initialTextGlowVertices = 2000;
        public const int _initialCircleGlowVertices = 200;

        public const float _textHeightToFontSizeFactor = 1.5f;
        public const float _textHeightToSpaceWidthFactor = 0.5f;

        public static readonly Vector4 _selectedObjectColor = new(16.0f / 255.0f, 191.0f / 255.0f, 0, 1);
        public static readonly Vector4 _selectedMouseOverObjectColor = new(127.0f / 255.0f, 1.0f, 116.0f / 255.0f, 1);
        public static readonly Vector4 _selectedMouseOverGlowColor = new(170.0f / 255.0f, 252.0f / 255.0f, 235.0f / 255.0f, 1);
        
        public static readonly Vector4 _sigPointColor = new(50.0f / 255.0f, 200.0f / 255.0f, 255.0f / 255.0f, 1);
        public static readonly float _sigPointRadiusInPixels = 5.0f;

        public static readonly float _dxfPointToExtentsBaseFactor = 0.05f;
    }
}
