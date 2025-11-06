using SharpDX;
using System.Windows.Media;

using Color = System.Windows.Media.Color;

namespace Cad_Point_Manager.Helpers
{
    public static class GlobalHelperProperties
    {
        public const float ZoomFactor = 1.3f;

        public const float HoverTransparency = 0.4f;
        public static readonly Vector4 HoverColor = new(0, 0, 0, HoverTransparency);
        public const float LineGlowPixelWidth = 7;

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
        public static readonly Vector4 SelectedMouseOverGlowColor = new(127.0f / 255.0f, 1.0f, 116.0f / 255.0f, 0.4f);

        public static readonly float DxfPointToExtentsBaseFactor = 0.05f;

        public const float CogoPointCirclePixelRadius = 0.4f;
        public const float CogoPointCircleMouseOverPixelRadius = 0.6f;
        public const float CogoPointLeaderLinePixelWidth = 1;

        public static readonly Color MouseOverCogoPointColor = Color.FromArgb(255, 8, 230, 238);
        public static readonly SolidColorBrush MouseOverCogoPointBrush = new(MouseOverCogoPointColor);
        public static readonly Color SelectedCogoPointColor = Color.FromArgb(255, 16, 191, 0);
        public static readonly SolidColorBrush SelectedCogoPointBrush = new(SelectedCogoPointColor);
        public static readonly Color SelectedCogoPointMouseOverColor = Color.FromArgb(255, 127, 255, 116);
        public static readonly SolidColorBrush SelectedCogoPointMouseOverBrush = new(SelectedCogoPointMouseOverColor);
    }
}
