using Cad_Point_Manager.Helpers;
using System.Windows;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class DrawingObjectNode3D
    {
        #region Fields
        #endregion

        #region Properties
        public List<DrawingObject3D> DrawingObjects { get; set; } = [];
        public Rect Extents { get; set; }
        public int Level { get; set; }
        public DrawingObjectNode3D[] ChildNodes { get; set; }
        public DrawingObjectTree3D Tree { get; set; }
        #endregion

        #region Constructors
        public DrawingObjectNode3D(List<DrawingObject3D> drawingObjects, int level, Rect extents, DrawingObjectTree3D tree)
        {
            DrawingObjects = drawingObjects;
            Level = level;
            Extents = extents;
            Tree = tree;

            if (Level == 0) { Tree.BaseLevelNodes.Add(this); }

            Subdivide();
        }
        #endregion

        #region Methods
        public List<DrawingObjectNode3D> GetIntersectingQuadTreeNodes(Rect view)
        {
            List<DrawingObjectNode3D> intersectingNodes = [];

            if (MathHelpers.RectsIntersect(view, Extents))
            {
                if (ChildNodes is null)
                {
                    intersectingNodes.Add(this);
                }
                else
                {
                    foreach (var child in ChildNodes)
                    {
                        intersectingNodes.AddRange(child.GetIntersectingQuadTreeNodes(view));
                    }
                }
            }
            return intersectingNodes;
        }
        public List<DrawingObjectNode3D> GetNodesAtPoint(Point p)
        {
            List<DrawingObjectNode3D> nodes = new();

            if (Extents.Contains(p))
            {
                if (Level == 0)
                {
                    nodes.Add(this);
                }
                else
                {
                    foreach (var child in ChildNodes)
                    {
                        nodes.AddRange(child.GetNodesAtPoint(p));
                    }
                }
            }
            return nodes;
        }
        public DrawingObjectNode3D GetNodeAtPoint(Point p)
        {
            DrawingObjectNode3D node = null;

            if (Extents.Contains(p))
            {
                if (Level == 0)
                {
                    node = this;
                }
                else
                {
                    foreach (var child in ChildNodes)
                    {
                        node = child.GetNodeAtPoint(p);
                        if (node != null) { break; }
                    }
                }
            }
            return node;
        }

        /// <summary>
        /// Gets the closest object to the point p within the tolerance.
        /// </summary>
        /// <param name="p">The point n from which the closest objects are found.</param>
        /// <param name="tolerance">The minimum distance from the point to the object</param>
        /// <returns></returns>
        public (double distance, DrawingObject3D obj) HitTestNode(Point p, float tolerance = 2)
        {
            DrawingObject3D drawingObject3D = null;
            double distance = double.MaxValue;

            foreach (var obj in DrawingObjects)
            {
                if (obj.IsVisible && obj.Layer.IsVisible)
                {
                    var inflatedBounds = Rect.Inflate(obj.Bounds, tolerance, tolerance);

                    if (inflatedBounds.Contains(p))
                    {
                        double d = obj.DistanceToPoint(p);

                        if (d < distance)
                        {
                            distance = d;
                            drawingObject3D = obj;
                        }
                    }
                }
            }
            return (distance, drawingObject3D);
        }

        /// <summary>
        /// Retunrs a list of objects sorted by distance from the point p.
        /// </summary>
        /// <param name="p">The point from which the distance to the objects is determined</param>
        /// <param name="hitTestRange">The bounds that define the minimum distance from the point that the object can lie.</param>
        /// <returns></returns>
        public List<(double distance, DrawingObject3D obj)> HitTestNode(Point p, Rect hitTestRange)
        {
            List<(double distance, DrawingObject3D obj)> hits = [];

            foreach (var obj in DrawingObjects)
            {
                if (obj.IsVisible && obj.Layer.IsVisible)
                {
                    if (obj.BoundsInRect(hitTestRange))
                    {
                        double d = obj.DistanceToPoint(p);
                        hits.Add((d, obj));
                    }
                }
            }
            return hits;
        }

        private void Subdivide()
        {
            if (Level > 0)
            {
                ChildNodes = new DrawingObjectNode3D[4];

                // Represents which quandrant each of the 1-4 is in
                Point factor1 = new(0, 0);
                Point factor2 = new(1, 0);
                Point factor3 = new(0, 1);
                Point factor4 = new(1, 1);

                // Represents the dxf coordinate bounds of each quadrant.
                Size halfBoundsSize = new(Extents.Width / 2, Extents.Height / 2);
                Rect extents1 = new(Extents.Left + halfBoundsSize.Width * factor1.X, Extents.Top + halfBoundsSize.Height * factor1.Y, halfBoundsSize.Width, halfBoundsSize.Height);
                Rect extents2 = new(Extents.Left + halfBoundsSize.Width * factor2.X, Extents.Top + halfBoundsSize.Height * factor2.Y, halfBoundsSize.Width, halfBoundsSize.Height);
                Rect extents3 = new(Extents.Left + halfBoundsSize.Width * factor3.X, Extents.Top + halfBoundsSize.Height * factor3.Y, halfBoundsSize.Width, halfBoundsSize.Height);
                Rect extents4 = new(Extents.Left + halfBoundsSize.Width * factor4.X, Extents.Top + halfBoundsSize.Height * factor4.Y, halfBoundsSize.Width, halfBoundsSize.Height);

                List<DrawingObject3D> objects1 = [];
                List<DrawingObject3D> objects2 = [];
                List<DrawingObject3D> objects3 = [];
                List<DrawingObject3D> objects4 = [];

                foreach (var drawingObject in DrawingObjects)
                {
                    if (drawingObject.Bounds.IsEmpty) { continue; }

                    var bounds = Rect.Inflate(drawingObject.Bounds, 0.5, 0.5);

                    if (bounds.IntersectsWith(extents1))
                    {
                        objects1.Add(drawingObject);
                    }
                    if (bounds.IntersectsWith(extents2))
                    {
                        objects2.Add(drawingObject);
                    }
                    if (bounds.IntersectsWith(extents3))
                    {
                        objects3.Add(drawingObject);
                    }
                    if (bounds.IntersectsWith(extents4))
                    {
                        objects4.Add(drawingObject);
                    }
                }

                ChildNodes[0] = new(objects1, Level - 1, extents1, Tree);
                ChildNodes[1] = new(objects2, Level - 1, extents2, Tree);
                ChildNodes[2] = new(objects3, Level - 1, extents3, Tree);
                ChildNodes[3] = new(objects4, Level - 1, extents4, Tree);
            }
        }
        #endregion
    }
}
