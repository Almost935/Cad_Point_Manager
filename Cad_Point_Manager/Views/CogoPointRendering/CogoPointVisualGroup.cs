using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.PointRendering;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace Cad_Point_Manager.Views
{
    public class CogoPointVisualGroup
    {
        #region Fields
        private const double _baseRenderTextSize = 6;

        private GeometryGroup? _ellipseGeometry;
        private GeometryGroup? _textHitTestGeometry;
        #endregion

        #region Properties
        public DrawingVisual MarkerVisual { get; set; } = new();
        public DrawingVisual TextVisual { get; set; } = new();
        public CogoPoint Point { get; }
        public Rect Bounds { get; private set; }
        #endregion

        #region Constructors
        public CogoPointVisualGroup(CogoPoint point)
        {
            Point = point;
            RedrawAll();
            UpdateBounds();
        }
        #endregion

        #region Methods
        public void UpdateMarkerTransform(Matrix matrix)
        {
            MarkerVisual.Transform = new MatrixTransform(matrix);
        }
        public void UpdateTextTransform(Matrix matrix)
        {
            TextVisual.Transform = new MatrixTransform(matrix);
        }
        public void RedrawAll()
        {
            RedrawEllipse();
            RedrawText();
        }
        public void RedrawEllipse()
        {
            using var dc = MarkerVisual.RenderOpen();
            var group = Point.PointGroup;
            if (!group.IsVisible) { return; }

            double scale = group.PointScale;
            double markerRadius = (Point.PointGroup.MarkerBaseSize / 2) * Point.PointGroup.PointScale;

            Brush brush;
            if (Point.IsSelected) { brush = GlobalHelperProperties.SelectedCogoPointBrush; }
            else { brush = group.WindowsBrush; }

            Brush glowBrush = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0));

            dc.DrawEllipse(brush, null, Point.Position, markerRadius, markerRadius);
            if (Point.IsMouseOver)
            {
                dc.DrawEllipse(glowBrush, new Pen(glowBrush, 0.15), Point.Position, markerRadius, markerRadius);
            }

            _ellipseGeometry = new GeometryGroup();
            var ellipseGeom = new EllipseGeometry(Point.Position, markerRadius, markerRadius);
            _ellipseGeometry.Children.Add(ellipseGeom);

            dc.Close();
        }
        public void RedrawText()
        {
            using var dc = TextVisual.RenderOpen();
            var group = Point.PointGroup;
            if (!group.IsVisible) { return; }

            double scale = group.PointScale;

            double desiredTextSize = Point.PointGroup.FontBaseSize * Point.PointGroup.PointScale;
            double textScaleFactor = desiredTextSize / _baseRenderTextSize;

            Brush brush;
            if (Point.IsSelected) { brush = GlobalHelperProperties.SelectedCogoPointBrush; }
            else { brush = group.WindowsBrush; }
            Pen pen = new(brush, 0.1);

            Brush glowBrush = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0));
            Pen textGlowPen = new(glowBrush, 0.75);

            Point textOrigin = Point.Position;
            double y = textOrigin.Y - (_baseRenderTextSize * 3);
            string[] lines =
            {
            Point.PointNumber.ToString(),
            Point.Elevation.ToString("F3"),
            Point.Description
            };

            _textHitTestGeometry = new GeometryGroup();

            bool isFlipped = false;
            if (!Point.TextInfoInBasePosition)
            {
                var start = Point.Position;
                var end = Point.TextToggleButtonPosition;
                dc.DrawLine(pen, start, end);

                if (end.X - start.X < 0) { isFlipped = true; }
            }
            Matrix textMatrix = new();
            double xOffset = Point.TextInfoBaseOffset * Point.PointGroup.PointScale;
            textMatrix.ScaleAt(textScaleFactor, -textScaleFactor, textOrigin.X, textOrigin.Y);
            textMatrix.Translate(Point.TextInfoCurrentOffset.X + (xOffset), Point.TextInfoCurrentOffset.Y);

            dc.PushTransform(new MatrixTransform(textMatrix));
            foreach (var line in lines)
            {
                var formatted = new FormattedText(
                    line,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    _baseRenderTextSize,
                    brush,
                    VisualTreeHelper.GetDpi(TextVisual).PixelsPerDip);

                Point linePos;
                if (isFlipped)
                {
                    linePos = new Point(textOrigin.X - formatted.Width - xOffset, y);
                }
                else
                {
                    linePos = new(textOrigin.X + (xOffset), y);
                }
                
                dc.DrawText(formatted, linePos);
                var textGeom = formatted.BuildGeometry(linePos);
                var rectGeom = new RectangleGeometry(textGeom.Bounds);
                _textHitTestGeometry.Children.Add(rectGeom);

                if (Point.IsMouseOver)
                {
                    dc.DrawGeometry(glowBrush, textGlowPen, textGeom);
                }
                y += _baseRenderTextSize;
            }

            var textTransform = new ScaleTransform(textScaleFactor, -textScaleFactor, textOrigin.X, textOrigin.Y);
            _textHitTestGeometry.Transform = textTransform;

            dc.Close();
        }
        public void UpdateBounds()
        {
            Bounds = Rect.Union(_ellipseGeometry.Bounds, _textHitTestGeometry.Bounds);
        }

        public double DistanceToPoint(Point mouse)
        {
            if (_ellipseGeometry.FillContains(mouse) || _textHitTestGeometry.FillContains(mouse))
            {
                return 0;
            }
            else
            {
                return double.MaxValue;
            }
        }
        public double DistanceToPointText(Point mouse)
        {
            if (_textHitTestGeometry.FillContains(mouse))
            {
                return 0;
            }
            else
            {
                return MathHelpers.PointToRectDistance(_textHitTestGeometry.Bounds, mouse);
            }
        }
        #endregion
    }
}
