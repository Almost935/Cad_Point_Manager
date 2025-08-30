using Cad_Point_Manager.Common;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.DrawingObjects3D;
using Cad_Point_Manager.Models.PointRendering;
using SharpDX;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Windows;

using Point = System.Windows.Point;

namespace Cad_Point_Manager.Models.HitTesting
{
    public class HitTestableObjectNode
    {
        #region Properties
        public List<HitTestableObject> HitTestableObjects { get; set; } = [];
        public Rect Extents { get; set; }
        public int Level { get; set; }
        public HitTestableObjectNode[] ChildNodes { get; set; }
        public bool IsLeaf => ChildNodes is null;
        public HitTestableObjectTree Tree { get; set; }
        #endregion

        #region Constructors
        public HitTestableObjectNode(List<HitTestableObject> hitTestableObjects, int level, Rect extents, HitTestableObjectTree tree)
        {
            HitTestableObjects = hitTestableObjects;
            Level = level;
            Extents = extents;
            Tree = tree;

            Subdivide();

            if (IsLeaf) { Tree.LeafNodes.Add(this); }
        }
        #endregion

        #region Methods
        public List<HitTestableObjectNode> GetIntersectingQuadTreeNodes(Rect view)
        {
            List<HitTestableObjectNode> intersectingNodes = [];

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
        public List<HitTestableObjectNode> GetNodesAtPoint(Point p)
        {
            List<HitTestableObjectNode> nodes = new();

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
        public HitTestableObjectNode GetNodeAtPoint(Point p)
        {
            HitTestableObjectNode node = null;

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
        public (double distance, HitTestableObject hitTestableObject) HitTestNode(Point p, float tolerance = 2)
        {
            HitTestableObject hitTestableObject = null;
            double distance = double.MaxValue;

            foreach (var obj in HitTestableObjects)
            {
                if (obj is DrawingObject3D drawingObject)
                {
                    if (drawingObject.Layer.IsVisible)
                    {
                        var inflatedBounds = Rect.Inflate(drawingObject.Bounds, tolerance, tolerance);

                        if (inflatedBounds.Contains(p))
                        {
                            double d = drawingObject.DistanceToPoint(p);

                            if (d < distance)
                            {
                                distance = d;
                                hitTestableObject = drawingObject;
                            }
                        }
                    }
                }
                if (obj is CogoPoint dxfPoint)
                {
                    if (dxfPoint.PointGroup.IsVisible)
                    {
                        var inflatedBounds = Rect.Inflate(dxfPoint.Bounds, tolerance, tolerance);

                        if (inflatedBounds.Contains(p))
                        {
                            double d = dxfPoint.DistanceToPoint(p);

                            if (d < distance)
                            {
                                distance = d;
                                hitTestableObject = dxfPoint;
                            }
                        }
                    }
                }
            }
            return (distance, hitTestableObject);
        }

        /// <summary>
        /// Retunrs a list of objects sorted by distance from the point p.
        /// </summary>
        /// <param name="p">The point from which the distance to the objects is determined</param>
        /// <param name="hitTestRange">The bounds that define the minimum distance from the point that the object can lie.</param>
        /// <returns></returns>
        public List<(double distance, HitTestableObject hitTestableObject)> HitTestAll(Point p, Rect hitTestRange)
        {
            List<(double distance, HitTestableObject hitTestableObject)> hits = [];

            foreach (var hitTestableObject in HitTestableObjects)
            {
                if (hitTestableObject is DrawingObject3D drawingObject)
                {
                    if (drawingObject.Layer.IsVisible)
                    {
                        if (drawingObject.BoundsInRect(hitTestRange))
                        {
                            double d = drawingObject.DistanceToPoint(p);
                            hits.Add((d, drawingObject));
                        }
                    }
                }
                if (hitTestableObject is CogoPoint dxfPoint)
                {
                    if (dxfPoint.PointGroup.IsVisible)
                    {
                        if (dxfPoint.BoundsInRect(hitTestRange))
                        {
                            double d = dxfPoint.DistanceToPoint(p);
                            hits.Add((d, dxfPoint));
                        }
                    }
                }
            }
            return hits;
        }
        public List<(double distance, DrawingGeometry3D geometry)> HitTestGeometries(Point p, Rect hitTestRange)
        {
            List<(double distance, DrawingGeometry3D geometry)> geometries = [];

            foreach (var hitTestableObject in HitTestableObjects)
            {
                if (hitTestableObject is DrawingGeometry3D drawingGeometry3D)
                {
                    if (drawingGeometry3D.Layer.IsVisible)
                    {
                        if (drawingGeometry3D.BoundsInRect(hitTestRange))
                        {
                            double d = drawingGeometry3D.DistanceToPoint(p);
                            geometries.Add((d, drawingGeometry3D));
                        }
                    }
                }
            }
            return geometries;
        }
        public List<(double distance, CogoPoint point)> HitTestCogoPoints(Point p, Rect hitTestRange)
        {
            List<(double distance, CogoPoint point)> cogoPoints = [];

            foreach (var hitTestableObject in HitTestableObjects)
            {
                //Debug.WriteLine($"HitTestCogoPoints: hitTestableObject is CogoPoint: {hitTestableObject is CogoPoint}");
                if (hitTestableObject is CogoPoint point)
                {
                    //Debug.WriteLine($"HitTestCogoPoints: point.PointGroup.IsVisible: {point.PointGroup.IsVisible}");
                    if (point.PointGroup.IsVisible)
                    {
                        //Debug.WriteLine($"HitTestCogoPoints: point.BoundsInRect(hitTestRange): {point.BoundsInRect(hitTestRange)}");
                        if (point.BoundsInRect(hitTestRange))
                        {
                            double d = point.DistanceToPoint(p);
                            cogoPoints.Add((d, point));
                        }
                    }
                }
            }
            return cogoPoints;
        }
        public List<(Enums.SignificantPointType pointType, double distance, Vector2 coordinate)> HitTestSignificantPoints(Point p, Rect hitTestRange)
        {
            Vector2 pos = new((float)p.X, (float)p.Y);
            ConcurrentBag<DrawingSegment3D> segments = [];

            Parallel.ForEach(HitTestableObjects, hitTestableObject =>
            {
                if (hitTestableObject is DrawingGeometry3D geometry && geometry.Layer.IsVisible)
                {
                    if (geometry is DrawingSegment3D segment && segment.BoundsInRect(hitTestRange))
                    {
                        segments.Add(segment);
                    }
                    else if (geometry is DrawingPolyline3D polyline)
                    {
                        foreach (var plineSegment in polyline.DrawingSegments)
                        {
                            if (plineSegment.BoundsInRect(hitTestRange))
                            {
                                segments.Add(plineSegment);
                            }
                        }
                    }
                    else if (geometry is DrawingSpline3D spline)
                    {
                        foreach (var splineSegment in spline.PolylineApproximation.DrawingSegments)
                        {
                            if (splineSegment.BoundsInRect(hitTestRange))
                            {
                                segments.Add(splineSegment);
                            }
                        }
                    }
                }
                else if (hitTestableObject is DrawingBlock3D block)
                {
                    foreach (var blockGeometry in block.DrawingObjects.OfType<DrawingGeometry3D>())
                    {
                        if (blockGeometry is DrawingSegment3D segment && segment.BoundsInRect(hitTestRange))
                        {
                            segments.Add(segment);
                        }
                        else if (blockGeometry is DrawingPolyline3D polyline)
                        {
                            foreach (var plineSegment in polyline.DrawingSegments)
                            {
                                if (plineSegment.BoundsInRect(hitTestRange))
                                {
                                    segments.Add(plineSegment);
                                }
                            }
                        }
                    }
                }
            });

            var coords = GeometryHelpers.GetSignificantPointsList(segments.ToList());

            List<(Enums.SignificantPointType pointType, double distance, Vector2 coordinate)> hits = [];
            foreach (var (pointType, position) in coords)
            {
                var vector2Pos = position.ToSharpDXVector2();
                float d = Vector2.Distance(vector2Pos, pos);
                hits.Add((pointType, d, vector2Pos));
            }
            hits.Sort((a, b) => a.distance.CompareTo(b.distance));

            return hits.ToList();
        }

        public List<CogoPoint> HitTestCogoPointsInRect(Rect rect)
        {
            List<CogoPoint> cogoPoints = [];

            foreach (var hitTestableObject in HitTestableObjects)
            {
                if (hitTestableObject is CogoPoint point)
                {
                    if (point.PointGroup.IsVisible)
                    {
                        //if (point.RectContainsRect(rect)) { cogoPoints.Add(point); }
                        if (point.BoundsInRect(rect)) { cogoPoints.Add(point); }
                    }
                }
            }
            return cogoPoints;
        }
        public List<DrawingGeometry3D> HitTestGeometriesInRect(Rect rect)
        {
            List<DrawingGeometry3D> geometries = [];

            foreach (var hitTestableObject in HitTestableObjects)
            {
                if (hitTestableObject is DrawingGeometry3D geometry)
                {
                    if (geometry.Layer.IsVisible)
                    {
                        if (geometry.BoundsInRect(rect)) { geometries.Add(geometry); }
                    }
                }
            }
            return geometries;
        }

        private void Subdivide()
        {
            if (Level <= 0) { return; }
            if (HitTestableObjects.Count <= Tree.LeafCapacity) { return; }
            if (Extents.Width < Tree.MinCellSize || Extents.Height < Tree.MinCellSize) { return; }

            ChildNodes = new HitTestableObjectNode[4];

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

            List<HitTestableObject> hitTestableObjects1 = [];
            List<HitTestableObject> hitTestableObjects2 = [];
            List<HitTestableObject> hitTestableObjects3 = [];
            List<HitTestableObject> hitTestableObjects4 = [];

            foreach (var hitTestableObject in HitTestableObjects)
            {
                if (hitTestableObject.Bounds.IsEmpty) { continue; }

                var bounds = Rect.Inflate(hitTestableObject.Bounds, 0.5, 0.5);

                if (bounds.IntersectsWith(extents1))
                {
                    hitTestableObjects1.Add(hitTestableObject);
                }
                if (bounds.IntersectsWith(extents2))
                {
                    hitTestableObjects2.Add(hitTestableObject);
                }
                if (bounds.IntersectsWith(extents3))
                {
                    hitTestableObjects3.Add(hitTestableObject);
                }
                if (bounds.IntersectsWith(extents4))
                {
                    hitTestableObjects4.Add(hitTestableObject);
                }
            }

            ChildNodes[0] = new(hitTestableObjects1, Level - 1, extents1, Tree);
            ChildNodes[1] = new(hitTestableObjects2, Level - 1, extents2, Tree);
            ChildNodes[2] = new(hitTestableObjects3, Level - 1, extents3, Tree);
            ChildNodes[3] = new(hitTestableObjects4, Level - 1, extents4, Tree);
        }
        #endregion
    }
}
