using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.HitTesting;
using SharpDX;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.TextFormatting;

namespace Cad_Point_Manager.Models.PointRendering
{
    public class DxfPoint : HitTestableObject
    {
        #region Fields
        private float _markerToTextOffset = 0.25f;
        #endregion

        #region Properties
        public int PointNumber { get; set; }
        public Vector3 Position { get; set; } = Vector3.Zero;
        public float TextHeight { get; set; }
        public float MarkerSize { get; set; }
        public PointGroup PointGroup { get; set; }
        public TextVertex[] TextVertices { get; set; } = [];
        public CircleVertex[] MarkerVertices { get; set; } = new CircleVertex[1];
        public int TextStartIndex { get; set; }
        public int TextEndIndex { get; set; }
        public int MarkerStartIndex { get; set; }
        public int MarkerEndIndex { get; set; }
        #endregion

        #region Constructors
        public DxfPoint(PointGroup pointGroup, int pointNum, Vector3 position, float textHeight, float markerSize)
        {
            PointGroup = pointGroup;
            PointNumber = pointNum;
            Position = position;
            TextHeight = textHeight;
            MarkerSize = markerSize;
        }
        #endregion

        #region Methods
        public override double DistanceToPoint(System.Windows.Point p)
        {
            if (MathHelpers.PointToPointDistance(p, Position.ToPoint()) < MarkerSize) { return 0.0; }  
            
            if (TextVertices.Length < 3) { return double.MaxValue; };

            Vector2 testPoint = new((float)p.X, (float)p.Y);
            double minDistance = double.MaxValue;
            bool pointInside = false;
            object locker = new();

            Parallel.For(0, TextVertices.Length / 3, (i, state) =>
            {
                if (pointInside) return;

                Vector2 v0 = TextVertices[i * 3 + 0].Position.ToVector2();
                Vector2 v1 = TextVertices[i * 3 + 1].Position.ToVector2();
                Vector2 v2 = TextVertices[i * 3 + 2].Position.ToVector2();

                if (MathHelpers.IsPointInTriangle(testPoint, v0, v1, v2))
                {
                    lock (locker)
                    {
                        pointInside = true;
                        minDistance = 0.0;
                    }

                    state.Stop();
                }
                else
                {
                    double dist = MathHelpers.DistanceToTriangle(testPoint, v0, v1, v2);

                    lock (locker)
                    {
                        if (dist < minDistance)
                            minDistance = dist;
                    }
                }
            });

            return minDistance;
        }
        public override void UpdateBounds()
        {
            if (TextVertices.Length == 0)
            {
                Bounds = Rect.Empty;
                return;
            }

            Span<TextVertex> span = TextVertices.AsSpan();

            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            for (int i = 0; i < span.Length; i++)
            {
                var pos = span[i].Position;
                minX = Math.Min(minX, pos.X);
                minY = Math.Min(minY, pos.Y);
                maxX = Math.Max(maxX, pos.X);
                maxY = Math.Max(maxY, pos.Y);
            }

            Bounds = new Rect(new System.Windows.Point(minX, minY), new System.Windows.Point(maxX, maxY));

            Rect circleBounds = new Rect(new System.Windows.Point(Position.X - MarkerSize, Position.Y - MarkerSize),
                new System.Windows.Point(Position.X + MarkerSize, Position.Y + MarkerSize));
            Bounds.Union(circleBounds);
        }

        public override void MouseEnter()
        { 
            this.IsMouseOver = true;

            Span<TextVertex> textSpan = TextVertices;
            for (int i = 0; i < textSpan.Length; i++)
            {
                textSpan[i].SetIsMouseOver(true);
            }

            Span<CircleVertex> markerSpan = MarkerVertices;
            for (int i = 0; i < markerSpan.Length; i++)
            {
                markerSpan[i].SetIsMouseOver(true);
            }
        }
        public override void MouseLeave()
        {
            this.IsMouseOver = false;

            Span<TextVertex> textSpan = TextVertices;
            for (int i = 0; i < textSpan.Length; i++)
            {
                textSpan[i].SetIsMouseOver(false);
            }

            Span<CircleVertex> markerSpan = MarkerVertices;
            for (int i = 0; i < markerSpan.Length; i++)
            {
                markerSpan[i].SetIsMouseOver(false);
            }
        }

        public override void Select()
        {
            this.IsSelected = true;

            Span<TextVertex> textSpan = TextVertices;
            for (int i = 0; i < textSpan.Length; i++)
            {
                textSpan[i].SetIsSelected(true);
            }
        }
        public override void Deselect()
        {
            this.IsSelected = false;

            Span<TextVertex> textSpan = TextVertices;
            for (int i = 0; i < textSpan.Length; i++)
            {
                textSpan[i].SetIsSelected(false);
            }
        }

        public void UpdateTextVertices(DxfPointTextVerticesDict textDict)
        {
            TextVertices ??= Array.Empty<TextVertex>();
            Array.Clear(TextVertices);

            TextVertices = textDict.GetIntTextVertices(PointNumber, TextHeight, new Vector2(Position.X + _markerToTextOffset, Position.Y), PointGroup.Color);

            UpdateBounds();
        }
        public void UpdateMarkerVertices()
        {
            MarkerVertices[0] = new(Position, PointGroup.Color, MarkerSize, PointGroup.IsVisible ? 1 : 0,
                IsMouseOver ? 1 : 0, IsSelected ? 1 : 0);
        }
        #endregion
    }
}
