using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class TextBox
    {
        #region Properties
        public Point InsertionPoint { get; set; } = new Point(0, 0);
        public Point TopLeft { get; set; }
        public Point BottomRight { get; set; }

        public Vector Translation { get; set; } = new(0, 0);
        public double CurrentScale { get; set; } = 1.0;
        public double Rotation { get; set; } = 0.0;

        public double Left => TopLeft.X;
        public double Right => BottomRight.X;
        public double Top => TopLeft.Y;
        public double Bottom => BottomRight.Y;
        public double Width => BottomRight.X - TopLeft.X;
        public double Height => BottomRight.Y - TopLeft.Y;
        #endregion

        #region Static Properties
        public static TextBox Empty => new TextBox(new Point(0, 0),  new Point(0, 0), new Point(0, 0));
        #endregion

        #region Constructors
        public TextBox(Point insertionPoint, Point topLeft, Point bottomRight)
        {
            InsertionPoint = insertionPoint;
            TopLeft = topLeft;
            BottomRight = bottomRight;
        }
        #endregion

        #region Public Methods
        public override string ToString()
        {
            return $"Left: {Left} Right: {Right} Top: {Top} Bottom: {Bottom}";
        }


        public void Expand(double expandRight, double expandBottom, double expandLeft, double expandTop)
        {
            BottomRight = new Point(BottomRight.X + expandRight, BottomRight.Y + expandBottom);
            TopLeft = new Point(TopLeft.X - expandLeft, TopLeft.Y - expandTop);
        }

        public void Translate(Vector offset)
        {
            TopLeft += offset;
            BottomRight += offset;
            Translation += offset;
        }

        public void Scale(double factor)
        {
            var center = new Point(Left + Width / 2, Top + Height / 2);
            ScaleTo(factor, center);
        }

        public void ScaleTo(double factor, Point pivot)
        {
            TopLeft = ScalePoint(TopLeft, pivot, factor);
            BottomRight = ScalePoint(BottomRight, pivot, factor);
            CurrentScale *= factor;
        }

        public void Rotate(double angleDegrees, Point pivot)
        {
            TopLeft = ApplyCurrentRotationToPoint(TopLeft, pivot, angleDegrees);
            BottomRight = ApplyCurrentRotationToPoint(BottomRight, pivot, angleDegrees);
            Rotation += angleDegrees;
        }

        public void Union(Point point)
        {
            double left = Math.Min(Left, point.X);
            double right = Math.Max(Right, point.X);
            double top = Math.Min(Top, point.Y);
            double bottom = Math.Max(Bottom, point.Y);
            TopLeft = new Point(left, top);
            BottomRight = new Point(right, bottom);
        }

        public void Union(TextBox other)
        {
            double left = Math.Min(Left, other.Left);
            double right = Math.Max(Right, other.Right);
            double top = Math.Min(Top, other.Top);
            double bottom = Math.Max(Bottom, other.Bottom);
            TopLeft = new Point(left, top);
            BottomRight = new Point(right, bottom);
        }

        #endregion

        #region Private Methods
        private Point ApplyCurrentRotationToPoint(Point pt, Point pivot, double angleDegrees)
        {
            double radians = angleDegrees * Math.PI / 180;
            double cos = Math.Cos(radians);
            double sin = Math.Sin(radians);

            double dx = pt.X - pivot.X;
            double dy = pt.Y - pivot.Y;

            double xNew = dx * cos - dy * sin + pivot.X;
            double yNew = dx * sin + dy * cos + pivot.Y;

            return new Point(xNew, yNew);
        }

        private static Point UnapplyCurrentRotationToPoint(Point pt, Point pivot, double angleDegrees)
        {
            double radians = angleDegrees * Math.PI / 180;
            double cos = Math.Cos(radians);
            double sin = Math.Sin(radians);

            double dx = pt.X - pivot.X;
            double dy = pt.Y - pivot.Y;

            double xNew = dx * cos - dy * sin + pivot.X;
            double yNew = dx * sin + dy * cos + pivot.Y;

            return new Point(xNew, yNew);
        }

        private static Point ScalePoint(Point pt, Point pivot, double factor)
        {
            double dx = pt.X - pivot.X;
            double dy = pt.Y - pivot.Y;

            return new Point(pivot.X + dx * factor, pivot.Y + dy * factor);
        }

        #endregion
    }
}
