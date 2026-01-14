using Cad_Point_Manager.Models.HitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Cad_Point_Manager.Models.PointRendering
{
    public class CogoPointTree
    {
        #region Fields
        private CadManager _cadManager;
        #endregion

        #region Properties
        public List<CogoPoint> CogoPoints { get; set; } = [];
        public Rect Extents { get; set; }

        public CogoPointTreeNode Root { get; set; }
        public List<CogoPointTreeNode> CurrentlyVisibleNodes { get; set; } = [];
        public int LeafCapacity { get; set; } = 10;   // 32–128 are common
        public double MinCellSize { get; set; } = 2;  // world units (≈ a few pixels)
        public int MaxLevels { get; set; } = 12;
        public List<CogoPointTreeNode> LeafNodes { get; set; } = [];
        #endregion

        #region Constructors
        public CogoPointTree(CadManager cadManager, Rect extents, int levels)
        {
            _cadManager = cadManager;
            Extents = extents;
            MaxLevels = levels;

            Initialize();
        }
        #endregion

        #region Methods
        private void Initialize()
        {
            GetCogoPoints();
            UpdateExtents();
            GetRoot();
        }
        private void UpdateExtents()
        {
            if (CogoPoints is null || CogoPoints.Count == 0)
            {
                Extents = Rect.Empty;
            }

            Rect newExtents = Rect.Empty;

            foreach (var hitTestableObject in CogoPoints)
            {
                if (newExtents.IsEmpty)
                {
                    newExtents = hitTestableObject.Bounds;
                }
                else
                {
                    newExtents.Union(hitTestableObject.Bounds);
                }
            }

            Extents = newExtents;
        }
        private void GetRoot()
        {
            Root = new(CogoPoints, MaxLevels, Extents, this);
        }
        private void GetCogoPoints()
        {
            foreach (var pg in _cadManager.CogoPointManager.PointGroups)
            {
                CogoPoints.AddRange(pg.Points);
            }
        }
        public List<CogoPointTreeNode> GetIntersectingNodes(Rect view)
        {
            List<CogoPointTreeNode> quadTreeNodes = [];

            quadTreeNodes.AddRange(Root.GetIntersectingQuadTreeNodes(view));

            return quadTreeNodes;
        }

        public List<CogoPointTreeNode> GetIntersectingNodes(Point p)
        {
            List<CogoPointTreeNode> quadTreeNodes = [];

            quadTreeNodes.AddRange(Root.GetNodesAtPoint(p));

            return quadTreeNodes;
        }
        public CogoPointTreeNode GetIntersectingNode(Point p)
        {
            return Root.GetNodeAtPoint(p);
        }
        #endregion
    }
}
