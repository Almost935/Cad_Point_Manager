using SharpDX;

namespace Cad_Point_Manager.Helpers
{
    public static class GlobalHelperProperties
    {
        public const float ZoomFactor = 1.3f;

        public const float LineGlowTransparency = 0.4f;
        public static readonly Vector4 LineGlowColor = new(0, 0, 0, LineGlowTransparency);
        public const float LineGlowPixelWidth = 5;

        public const float TextHeightToGlowOffsetFactor = 0.05f;
        public const int InitialLineVertices = 5000;
        public const int InitialTextVertices = 5000;
        public const int InitialCircleVertices = 1000;
        public const int InitialLineGlowVertices = 1000;
        public const int InitialTextGlowVertices = 2000;
        public const int InitialCircleGlowVertices = 200;

        public const float TextHeightToFontSizeFactor = 1.5f;
        public const float TextHeightToSpaceWidthFactor = 0.5f;

        public static readonly Vector4 SelectedObjectColor = new(59.0f / 255.0f, 255.0f / 255.0f, 62.0f / 255.0f, 1);
        public static readonly Vector4 SelectedMouseOverObjectColor = new(127.0f / 255.0f, 1.0f, 116.0f / 255.0f, 1);
        public static readonly Vector4 SelectedMouseOverGlowColor = new(170.0f / 255.0f, 252.0f / 255.0f, 235.0f / 255.0f, 1);
        
        public static readonly Vector4 SigPointColor = new(50.0f / 255.0f, 200.0f / 255.0f, 255.0f / 255.0f, 1);
        public static readonly float SigPointRadiusInPixels = 4.0f;

        public static readonly float DxfPointToExtentsBaseFactor = 0.05f;

        public static readonly System.Windows.Media.Color MouseOverCogoPointColor = System.Windows.Media.Color.FromArgb(255, 8, 230, 238);
        public static readonly System.Windows.Media.SolidColorBrush MouseOverCogoPointBrush = new(MouseOverCogoPointColor);
        public static readonly System.Windows.Media.Color SelectedCogoPointColor = System.Windows.Media.Color.FromArgb(255, 16, 191, 0);
        public static readonly System.Windows.Media.SolidColorBrush SelectedCogoPointBrush = new(SelectedCogoPointColor);
        public static readonly System.Windows.Media.Color SelectedCogoPointMouseOverColor = System.Windows.Media.Color.FromArgb(255, 127, 255, 116);
        public static readonly System.Windows.Media.SolidColorBrush SelectedCogoPointMouseOverBrush = new(SelectedCogoPointMouseOverColor);
    }
}
