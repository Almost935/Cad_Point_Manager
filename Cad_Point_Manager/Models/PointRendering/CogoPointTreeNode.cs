using Cad_Point_Manager.Helpers;
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
        public List<CogoPointTreeNode>                                                                             GetIntersectingQuadTreeNodes(Rect view)
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

        public List<(double distance, CogoPoint cogoPoint)> HitTestPoint(Point p, Rect hitTestRange)
        {
            List<(double distance, CogoPoint cogoPoint)> hits = [];

            foreach (var potentialHit in CogoPoints)
            {
                if (potentialHit.PointGroup.IsVisible)
                {
                    if (potentialHit.BoundsInRect(hitTestRange))
                    {
                        double d = potentialHit.DistanceToPoint(p);
                        hits.Add((d, potentialHit));
                    }
                }
            }
            return hits;
        }

        public List<CogoPoint> HitTestRect(Rect rect)
        {
            List<CogoPoint> cogoPoints = [];

            foreach (var potentialHit in CogoPoints)
            {
                if (potentialHit.PointGroup.IsVisible)
                {
                    if (potentialHit.CogoPointIntersectsRect(rect)) { cogoPoints.Add(potentialHit); }
                }
            }
            return cogoPoints;
        }

        private void Subdivide()
        {
            if (Level <= 0) { return; }
            if (CogoPoints.Count <= Tree.LeafCapacity) { return; }
            if (Extents.Width < Tree.MinCellSize || Extents.Height < Tree.MinCellSize) { return; }

            ChildNodes = new CogoPointTreeNode[4];

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

            List<CogoPoint> cogoPoints1 = [];
            List<CogoPoint> cogoPoints2 = [];
            List<CogoPoint> cogoPoints3 = [];
            List<CogoPoint> cogoPoints4 = [];

            foreach (var cogoPoint in CogoPoints)
            {
                if (cogoPoint.Bounds.IsEmpty) { continue; }

                var bounds = Rect.Inflate(cogoPoint.Bounds, 0.5, 0.5);

                if (bounds.IntersectsWith(extents1))
                {
                    cogoPoints1.Add(cogoPoint);
                }
                if (bounds.IntersectsWith(extents2))
                {
                    cogoPoints2.Add(cogoPoint);
                }
                if (bounds.IntersectsWith(extents3))
                {
                    cogoPoints3.Add(cogoPoint);
                }
                if (bounds.IntersectsWith(extents4))
                {
                    cogoPoints4.Add(cogoPoint);
                }
            }

            ChildNodes[0] = new(cogoPoints1, Level - 1, extents1, Tree);
            ChildNodes[1] = new(cogoPoints2, Level - 1, extents2, Tree);
            ChildNodes[2] = new(cogoPoints3, Level - 1, extents3, Tree);
            ChildNodes[3] = new(cogoPoints4, Level - 1, extents4, Tree);
        }
        #endregion
    }
}
