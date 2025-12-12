using Cad_Point_Manager.Common;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.DrawingObjects3D;
using Cad_Point_Manager.Models.HitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Cad_Point_Manager.Models.PointRendering
{
    public class CogoPointTreeNode
    {
        #region Properties
        public List<CogoPoint> CogoPoints { get; set; } = [];
        public Rect Extents { get; set; }
        public int Level { get; set; }
        public CogoPointTreeNode[] ChildNodes { get; set; }
        public bool IsLeaf => ChildNodes is null;
        public CogoPointTree Tree { get; set; }
        #endregion

        #region Constructors
        public CogoPointTreeNode(List<CogoPoint> cogoPoints, int level, Rect extents, CogoPointTree tree)
        {
            CogoPoints = cogoPoints;
            Level = level;
            Extents = extents;
            Tree = tree;

            Subdivide();

            if (IsLeaf) { Tree.LeafNodes.Add(this); }
        }
        #endregion

        #region Methods
        public List<CogoPointTreeNode> GetIntersectingQuadTreeNodes(Rect view)
        {
            List<CogoPointTreeNode> intersectingNodes = [];

            if (MathHelpers.RectsIntersect(view, Extents))
            {
                if (ChildNodes is null) { intersectingNodes.Add(this); }
                else
                {
                    foreach (var child in ChildNodes) { intersectingNodes.AddRange(child.GetIntersectingQuadTreeNodes(view)); }
                }
            }
            return intersectingNodes;
        }
        public List<CogoPointTreeNode> GetNodesAtPoint(Point p)
        {
            List<CogoPointTreeNode> nodes = [];

            if (Extents.Contains(p))
            {
                if (Level == 0) { nodes.Add(this); }
                else
                {
                    foreach (var child in ChildNodes) { nodes.AddRange(child.GetNodesAtPoint(p)); }
                }
            }
            return nodes;
        }
        public CogoPointTreeNode GetNodeAtPoint(Point p)
        {
            CogoPointTreeNode node = null;

            if (Extents.Contains(p))
            {
                if (Level == 0) { node = this; }
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

        public (double distance, CogoPoint cogoPoint) HitTestNode(Point p, float tolerance = 2)
        {
            HitTestableObject hitTestableObject = null;
            double distance = double.MaxValue;

            foreach (var cp in CogoPoints)
            {
                if (cp.PointGroup.IsVisible)
                {
                    var inflatedBounds = Rect.Inflate(cp.Bounds, tolerance, tolerance);

                    if (inflatedBounds.Contains(p))
                    {
                        double d = cp.DistanceToPoint(p);

                        if (d < distance)
                        {
                            distance = d;
                            hitTestableObject = drawingObject;
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

        public List<(double distance, CogoPoint point)> HitTest(Point p, Rect hitTestRange)
        {
            List<(double distance, CogoPoint point)> cogoPoints = [];

            foreach (var hitTestableObject in HitTestableObjects)
            {
                if (hitTestableObject is CogoPoint point)
                {
                    if (point.PointGroup.IsVisible)
                    {
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

        public List<CogoPoint> HitTestRect(Rect rect)
        {
            List<CogoPoint> cogoPoints = [];

            foreach (var hitTestableObject in HitTestableObjects)
            {
                if (hitTestableObject is CogoPoint point)
                {
                    if (point.PointGroup.IsVisible)
                    {
                        if (point.CogoPointIntersectsRect(rect)) { cogoPoints.Add(point); }
                    }
                }
            }

            return cogoPoints;
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
}
