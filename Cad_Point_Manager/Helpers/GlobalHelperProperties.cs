using SharpDX;
using System.Windows.Media;

using Color = System.Windows.Media.Color;

namespace Cad_Point_Manager.Helpers
{
    public static class GlobalHelperProperties
    {
        public static readonly int PopOutCloseDelay = 700;

        public const double ZoomFactor = 1.3;

        public const double HoverTransparency = 0.5;
        public static readonly Vector4 HoverColor = new(0, 0, 0, (float)HoverTransparency);
        public const double GlowPixelOffset = 5;

        public const double TextHeightToGlowOffsetFactor = 0.05;
        public const int InitialLineVertices = 5000;
        public const int InitialTextVertices = 5000;
        public const int InitialCircleVertices = 1000;
        public const int InitialLineGlowVertices = 1000;
        public const int InitialTextGlowVertices = 2000;
        public const int InitialCircleGlowVertices = 200;

        public const double TextHeightToFontSizeFactor = 1.5;
        public const double TextHeightToSpaceWidthFactor = 0.5;

        public static readonly Vector4 SelectedObjectColor = new(36.0f / 255.0f, 250.0f / 255.0f, 255.0f / 255.0f, 0.7f);
        public static readonly Vector4 SelectedMouseOverObjectColor = new(127.0f / 255.0f, 1.0f, 116.0f / 255.0f, 1);
        public static readonly Vector4 SelectedMouseOverGlowColor = new(127.0f / 255.0f, 1.0f, 116.0f / 255.0f, 0.4f);

        public const double SignificantPointPixelRadius = 6.0;
        public static readonly Vector4 SelectedSigPointColor = new(59.0f / 255.0f, 255.0f / 255.0f, 62.0f / 255.0f, 0.5f);

        public static readonly double DxfPointToExtentsBaseFactor = 0.05;

        public const double CogoPointCirclePixelRadius = 0.2;
        public const double CogoPointCircleMouseOverPixelRadius = 0.4;
        public const double CogoPointLeaderLinePixelWidth = 1;

        public static readonly Color MouseOverCogoPointColor = Color.FromArgb(255, 8, 230, 238);
        public static readonly SolidColorBrush MouseOverCogoPointBrush = new(MouseOverCogoPointColor);
        public static readonly Color SelectedCogoPointColor = Color.FromArgb(255, 16, 191, 0);
        public static readonly SolidColorBrush SelectedCogoPointBrush = new(SelectedCogoPointColor);
        public static readonly Color SelectedCogoPointMouseOverColor = Color.FromArgb(255, 127, 255, 116);
        public static readonly SolidColorBrush SelectedCogoPointMouseOverBrush = new(SelectedCogoPointMouseOverColor);

        // Drag rect properties
        public static readonly Vector4 DragRectBorderColor = new(0f, 0.749f, 1f, 1f);
        public static readonly Vector4 DragRectFillColor = new(0f, 0.749f, 1f, 0.15f);

        public const float PdfPreviewDpi = 96;
    }
}
