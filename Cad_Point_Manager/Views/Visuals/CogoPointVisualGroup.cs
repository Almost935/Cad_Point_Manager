using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.PointRendering;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace Cad_Point_Manager.Views
{
    public class CogoPointVisualGroup : INotifyPropertyChanged
    {
        #region Fields
        private const double _baseRenderTextSize = 6;
        private const double _baseLineStrokeThickness = 0.2;
        private const double _baseGlowLineStrokeThickness = 0.6;
        private const double _textGlowStrokeThickness = 0.75;

        private EllipseGeometry? _ellipseHitTestGeometry;
        private GeometryGroup? _textHitTestGeometry;
        private LineGeometry? _lineHitTestGeometry;
        private Pen _hittestPen;

        private Rect _bounds = Rect.Empty;
        #endregion

        #region Properties
        public DrawingVisual MarkerVisual { get; set; } = new();
        public DrawingVisual TextVisual { get; set; } = new();
        public CogoPoint Point { get; }

        public Rect Bounds
        {
            get => _bounds;
            private set
            {
                if (_bounds != value)
                {
                    _bounds = value;
                    OnPropertyChanged(nameof(Bounds));
                }
            }
        }
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
            double lineGlowStrokeThickness = _baseGlowLineStrokeThickness * scale;
            double markerRadius = (Point.PointGroup.MarkerBaseSize / 2) * Point.PointGroup.PointScale;

            Brush brush;
            if (Point.IsSelected) { brush = GlobalHelperProperties.SelectedCogoPointBrush; }
            else { brush = group.WindowsBrush; }

            Brush glowBrush = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0));
            Pen glowPen = new(brush, lineGlowStrokeThickness)
            {
                LineJoin = PenLineJoin.Round,
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };

            _ellipseHitTestGeometry = new EllipseGeometry(Point.Position, markerRadius, markerRadius);
            dc.DrawEllipse(brush, null, Point.Position, markerRadius, markerRadius);
            if (Point.IsMouseOver)
            {
                dc.DrawEllipse(glowBrush, new Pen(glowBrush, 0.15), Point.Position, markerRadius, markerRadius);
            }

            dc.Close();

            UpdateBounds();
        }
        public void RedrawText()
        {
            using var dc = TextVisual.RenderOpen();
            var group = Point.PointGroup;
            if (!group.IsVisible) { return; }

            double scale = group.PointScale;
            double lineStrokeThickness = _baseLineStrokeThickness * scale;
            double lineGlowStrokeThickness = _baseGlowLineStrokeThickness * scale;

            double desiredTextSize = Point.PointGroup.FontBaseSize * Point.PointGroup.PointScale;
            double textScaleFactor = desiredTextSize / _baseRenderTextSize;

            Brush brush;
            if (Point.IsSelected) { brush = GlobalHelperProperties.SelectedCogoPointBrush; }
            else { brush = group.WindowsBrush; }
            Pen pen = new(brush, lineStrokeThickness)
            {
                LineJoin = PenLineJoin.Round,
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            _hittestPen = pen.Clone();

            Brush glowBrush = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0));
            Pen textGlowPen = new(glowBrush, _textGlowStrokeThickness) 
            {
                LineJoin = PenLineJoin.Round,
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            Pen lineGlowPen = new(glowBrush, lineGlowStrokeThickness)
            {
                LineJoin = PenLineJoin.Round,
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };

            Point textOrigin = Point.TextInfoCurrentPosition;
            double y = textOrigin.Y - (_baseRenderTextSize * 3);
            string[] lines =
            {
            Point.PointNumber.ToString(),
            Point.Elevation.ToString("F3"),
            Point.Description
            };

            _textHitTestGeometry = new GeometryGroup();

            bool isFlippedOnY = false;
            bool isFlippedOnX = false;
            if (!Point.TextInfoInBasePosition)
            {
                dc.DrawLine(pen, Point.Position, Point.TextToggleButtonPosition);
                if (Point.IsMouseOver)
                {
                    dc.DrawLine(lineGlowPen, Point.Position, Point.TextToggleButtonPosition);
                }

                if (Point.TextToggleButtonPosition.X - Point.Position.X < 0) { isFlippedOnY = true; }
                if (Point.TextToggleButtonPosition.Y - Point.Position.Y < 0) { isFlippedOnX = true; }
            }
            Matrix textMatrix = new();
            double xOffset = Point.TextInfoBaseOffset * Point.PointGroup.PointScale;
            textMatrix.ScaleAt(textScaleFactor, -textScaleFactor, textOrigin.X, textOrigin.Y);
            if (isFlippedOnY) { textMatrix.Translate(-xOffset, 0); }
            else { textMatrix.Translate(xOffset, 0); }

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
                if (isFlippedOnY) { linePos = new Point(textOrigin.X - formatted.Width, y); }
                else { linePos = new(textOrigin.X, y); }

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

            _textHitTestGeometry.Transform = new MatrixTransform(textMatrix);
            _lineHitTestGeometry = new(Point.Position, Point.TextToggleButtonPosition);

            dc.Pop();
            Brush testBrush = new SolidColorBrush(Color.FromArgb(50, 0, 255, 0));
            Pen testPen = new(testBrush, 0.1);
            dc.DrawGeometry(testBrush, testPen, _textHitTestGeometry);

            dc.Close();

            UpdateBounds();
        }
        public void UpdateBounds()
        {
            if (_ellipseHitTestGeometry == null || _textHitTestGeometry == null)
            {
                Bounds = Rect.Empty;
                return;
            }
            Bounds = Rect.Union(_ellipseHitTestGeometry.Bounds, _textHitTestGeometry.Bounds);
        }

        public double DistanceToPoint(Point mouse)
        {
            if (_ellipseHitTestGeometry.FillContains(mouse) || _textHitTestGeometry.FillContains(mouse) ||
                _lineHitTestGeometry.StrokeContains(_hittestPen, mouse))
            {
                return 0;
            }
            else
            {
                return double.MaxValue;
            }
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
