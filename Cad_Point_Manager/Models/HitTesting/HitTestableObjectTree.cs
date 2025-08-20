using System.Windows;

namespace Cad_Point_Manager.Models.HitTesting
{
    public class HitTestableObjectTree
    {
        #region Fields
        private const float _viewInflationFactor = 1.1f;

        private CadManager3D _cadManager;
        #endregion

        #region Properties
        public List<HitTestableObject> HitTestableObjects { get; set; } = [];
        public Rect Extents { get; set; }

        public HitTestableObjectNode Root { get; set; }
        public List<HitTestableObjectNode> CurrentlyVisibleNodes { get; set; } = [];
        public int LeafCapacity { get; set; } = 64;   // 32–128 are common
        public double MinCellSize { get; set; } = 2;  // world units (≈ a few pixels)
        public int MaxLevels { get; set; } = 12;
        public List<HitTestableObjectNode> LeafNodes { get; set; } = [];
        #endregion

        #region Constructors
        public HitTestableObjectTree(CadManager3D cadManager, Rect extents, int levels)
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
            if (HitTestableObjects is null || HitTestableObjects.Count == 0)
            {
                Extents = Rect.Empty;
            }

            Rect newExtents = Rect.Empty;

            foreach (var hitTestableObject in HitTestableObjects)
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
            Root = new(HitTestableObjects, MaxLevels, Extents, this);
        }
        private void GetDrawingObjects()
        {
            foreach (var keyValue in _cadManager.Layers)
            {
                HitTestableObjects.AddRange(keyValue.Value.DrawingObject3Ds);
            }
            foreach (var keyValue in _cadManager.CogoPointManager.PointGroups)
            {
                HitTestableObjects.AddRange(keyValue.Value.Points);
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
