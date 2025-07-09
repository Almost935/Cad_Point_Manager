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
        public DrawingVisual Visual { get; set; } = new();
        public CogoPoint Point { get; }
        public Rect Bounds { get; private set; }
        #endregion

        #region Constructors
        public CogoPointVisualGroup(CogoPoint point)
        {
            Point = point;
            Redraw();
        }
        #endregion

        #region Methods
        public void UpdateTransform(Matrix matrix)
        {
            Visual.Transform = new MatrixTransform(matrix);
        }
        public void Redraw()
        {
            using var dc = Visual.RenderOpen();
            var group = Point.PointGroup;
            if (!group.IsVisible) { return; }

            double scale = group.PointScale;
            double markerRadius = (Point.PointGroup.MarkerBaseSize / 2) * Point.PointGroup.PointScale;
            
            double desiredTextSize = Point.PointGroup.FontBaseSize * Point.PointGroup.PointScale;
            double textScaleFactor = desiredTextSize / _baseRenderTextSize;

            Brush brush = group.WindowsBrush;
            Brush glowBrush = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0));
            Pen textGlowPen = new(glowBrush, 0.75);
            
            dc.DrawEllipse(brush, null, Point.Position, markerRadius, markerRadius);
            if (Point.IsMouseOver)
            {
                dc.DrawEllipse(glowBrush, new Pen(glowBrush, 0.15), Point.Position, markerRadius, markerRadius);
            }

            _ellipseGeometry = new GeometryGroup();
            var ellipseGeom = new EllipseGeometry(Point.Position, markerRadius, markerRadius);
            _ellipseGeometry.Children.Add(ellipseGeom);

            Point textOrigin = Point.TextInfoPosition;
            double y = textOrigin.Y - (_baseRenderTextSize * 3);
            string[] lines =
            {
            Point.PointNumber.ToString(),
            Point.Elevation.ToString("F3"),
            Point.Description
            };

            _textHitTestGeometry = new GeometryGroup();

            dc.PushTransform(new ScaleTransform(textScaleFactor, -textScaleFactor, textOrigin.X, textOrigin.Y));
            foreach (var line in lines)
            {
                var formatted = new FormattedText(
                    line,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    _baseRenderTextSize,
                    brush,
                    VisualTreeHelper.GetDpi(Visual).PixelsPerDip);
                
                var linePos = new Point(textOrigin.X, y);
                dc.DrawText(formatted, linePos);
                var textGeom = formatted.BuildGeometry(linePos);
                var rectGeom = new RectangleGeometry(textGeom.Bounds);
                _textHitTestGeometry.Children.Add(rectGeom);

                //dc.DrawGeometry(brush, null, textGeom);
                if (Point.IsMouseOver)
                {
                    dc.DrawGeometry(glowBrush, textGlowPen, textGeom);
                }
                y += _baseRenderTextSize;
            }

            var textTransform = new ScaleTransform(textScaleFactor, -textScaleFactor, textOrigin.X, textOrigin.Y);
            _textHitTestGeometry.Transform = textTransform;

            dc.Pop();

            Bounds = Rect.Union(_ellipseGeometry.Bounds, _textHitTestGeometry.Bounds);

            dc.Close();
        }

        public bool HitTest(Point mouse)
        {
            double scale = Point.PointGroup.PointScale;
            double radius = Point.PointGroup.MarkerBaseSize / 2;
            Point center = Point.Position;

            double dx = mouse.X - center.X;
            double dy = mouse.Y - center.Y;

            return dx * dx + dy * dy <= radius * radius;
        }
        public double DistanceToPoint(Point mouse)
        {
            //Debug.WriteLine($"\nmouse: {mouse}");
            //Debug.WriteLine($"_textHitTestGeometry Left: {_textHitTestGeometry.Bounds.Left} Right: {_textHitTestGeometry.Bounds.Right} Top: {_textHitTestGeometry.Bounds.Top} Bottom: {_textHitTestGeometry.Bounds.Bottom}");
            //Debug.WriteLine($"_textHitTestGeometry.FillContains(mouse): {_textHitTestGeometry.FillContains(mouse)}");

            if (_ellipseGeometry.FillContains(mouse) || _textHitTestGeometry.FillContains(mouse))
            {
                return 0;
            }
            else
            {
                return double.MaxValue;
            }
        }
        #endregion
    }
}
