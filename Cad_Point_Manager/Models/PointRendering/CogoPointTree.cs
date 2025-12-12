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
        private CadManager3D _cadManager;
        #endregion

        #region Properties
        public List<CogoPoint> CogoPoints { get; set; } = [];
        public Rect Extents { get; set; }

        public CogoPointTreeNode Root { get; set; }
        public List<CogoPointTreeNode> CurrentlyVisibleNodes { get; set; } = [];
        public int LeafCapacity { get; set; } = 64;   // 32–128 are common
        public double MinCellSize { get; set; } = 2;  // world units (≈ a few pixels)
        public int MaxLevels { get; set; } = 12;
        public List<CogoPointTreeNode> LeafNodes { get; set; } = [];
        #endregion

        #region Constructors
        public CogoPointTree(CadManager3D cadManager, Rect extents, int levels)
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
            GetDrawingObjects();
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
        private void GetDrawingObjects()
        {
            foreach (var keyValue in _cadManager.Layers)
            {
                HitTestableObjects.AddRange(keyValue.Value.DrawingObject3Ds);
            }
            foreach (var keyValue in _cadManager.CogoPointManager.PointGroups)
            {
                HitTestableObjects.AddRange(keyValue.Points);
            }
        }
        public List<HitTestableObjectNode> GetIntersectingNodes(Rect view)
        {
            List<HitTestableObjectNode> quadTreeNodes = [];

            quadTreeNodes.AddRange(Root.GetIntersectingQuadTreeNodes(view));

            return quadTreeNodes;
        }

        public List<HitTestableObjectNode> GetIntersectingNodes(Point p)
        {
            List<HitTestableObjectNode> quadTreeNodes = [];

            quadTreeNodes.AddRange(Root.GetNodesAtPoint(p));

            return quadTreeNodes;
        }
        public HitTestableObjectNode GetIntersectingNode(Point p)
        {
            return Root.GetNodeAtPoint(p);
        }
        #endregion
    }
}
