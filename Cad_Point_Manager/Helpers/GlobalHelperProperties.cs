using SharpDX;

namespace Cad_Point_Manager.Helpers
{
    public static class GlobalHelperProperties
    {
        public const float _zoomFactor = 1.3f;

        public const float _mouseOverGlowTransparency = 0.5f;
        public const float _mouseOverGlowPixelWidth = 5;


        public const float _textHeightToGlowOffsetFactor = 0.05f;
        public const int _initialLineVertices = 5000;
        public const int _initialTriangleVertices = 20000;
        public const int _maxCircleVertices = 1000;
        public const float _textHeightToFontSizeFactor = 1.5f;
        public const float _textHeightToSpaceWidthFactor = 0.5f;

        public static readonly Vector4 _selectedObjectColor = new(0.07f, 0.85f, 0, 1);
        public static readonly Vector4 _selectedMouseOverObjectColor = new(185.0f / 255.0f, 1.0f, 179.0f / 255.0f, 1);
        public static readonly Vector4 _selectedMouseOverGlowColor = new(170.0f / 255.0f, 252.0f / 255.0f, 235.0f / 255.0f, 1);

        public static readonly float _dxfPointToExtentsBaseFactor = 0.05f;
    }
}
