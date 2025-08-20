using Cad_Point_Manager.Models.HitTesting;
using System.Windows.Media;

namespace Cad_Point_Manager.Views.Visuals
{
    public class HitTestablePointVisualGroup
    {
        #region Fields
        #endregion

        #region Properties
        public DrawingVisual Visual { get; set; } = new();
        public HitTestablePoint Point { get; }
        #endregion

        #region Constructors
        public HitTestablePointVisualGroup(HitTestablePoint point)
        {
            Point = point;
            RedrawEllipse();
        }
        #endregion

        #region Methods
        public void RedrawEllipse()
        {
            //using var dc = Visual.RenderOpen();

            //double markerRadius = (Point.PointGroup.MarkerBaseSize / 2) * Point.PointGroup.PointScale;

            //Brush brush;
            //if (Point.IsSelected) { brush = GlobalHelperProperties.SelectedCogoPointBrush; }
            //else { brush = group.WindowsBrush; }

            //Brush glowBrush = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0));
            //Pen glowPen = new(brush, lineGlowStrokeThickness)
            //{
            //    LineJoin = PenLineJoin.Round,
            //    StartLineCap = PenLineCap.Round,
            //    EndLineCap = PenLineCap.Round
            //};

            //_ellipseHitTestGeometry = new EllipseGeometry(Point.Position, markerRadius, markerRadius);
            //dc.DrawEllipse(brush, null, Point.Position, markerRadius, markerRadius);
            //if (Point.IsMouseOver)
            //{
            //    dc.DrawEllipse(glowBrush, new Pen(glowBrush, 0.15), Point.Position, markerRadius, markerRadius);
            //}

            //dc.Close();

            //UpdateBounds();
        }
        #endregion
    }
}
