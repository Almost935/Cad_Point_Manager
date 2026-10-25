using Cad_Point_Manager.Common;
using System.Windows;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class TextBox
    {
        #region Properties
        public Vector2d InsertionPoint { get; set; } = Vector2d.Zero;
        public Vector2d TopLeft { get; set; }
        public Vector2d BottomRight { get; set; }

        public Vector2d Translation { get; set; } = Vector2d.Zero;
        public double CurrentScale { get; set; } = 1.0;
        public double Rotation { get; set; } = 0.0;

        public double Left => TopLeft.X;
        public double Right => BottomRight.X;
        public double Top => TopLeft.Y;
        public double Bottom => BottomRight.Y;
        public double Width => BottomRight.X - TopLeft.X;
        public double Height => TopLeft.Y - BottomRight.Y;
        #endregion

        #region Static Properties
        public static TextBox Empty => new TextBox(Vector2d.Zero, Vector2d.Zero, Vector2d.Zero);
        #endregion

        #region Constructors
        public TextBox(Vector2d insertionPoint, Vector2d topLeft, Vector2d bottomRight)
        {
            InsertionPoint = insertionPoint;
            TopLeft = topLeft;
            BottomRight = bottomRight;
        }
        #endregion

        #region Public Methods
        public override string ToString()
        {
            return $"Left: {Left} Right: {Right} Top: {Top} Bottom: {Bottom} Width: {Width} Height: {Height}";
        }


        public void Expand(double expandRight, double expandBottom, double expandLeft, double expandTop)
        {
            BottomRight = new(BottomRight.X + expandRight, BottomRight.Y + expandBottom);
            TopLeft = new(TopLeft.X - expandLeft, TopLeft.Y - expandTop);
        }

        public void Translate(Vector2d offset)
        {
            TopLeft += offset;
            BottomRight += offset;
            Translation += offset;
        }

        public void Scale(double factor)
        {
            var center = new Vector2d(Left + Width / 2, Top + Height / 2);
            ScaleTo(factor, center);
        }

        public void ScaleTo(double factor, Vector2d pivot)
        {
            TopLeft = ScalePoint(TopLeft, pivot, factor);
            BottomRight = ScalePoint(BottomRight, pivot, factor);
            CurrentScale *= factor;
        }
        #endregion

        #region Private Methods
        private static Vector2d ScalePoint(Vector2d pt, Vector2d pivot, double factor)
        {
            double dx = pt.X - pivot.X;
            double dy = pt.Y - pivot.Y;

            return new Vector2d(pivot.X + dx * factor, pivot.Y + dy * factor);
        }

        #endregion
    }
}
