using System.Windows;
using Cad_Point_Manager.Models.DrawingObjects3D;

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
        public int Levels { get; set; }
        public HitTestableObjectNode Root { get; set; }
        public List<HitTestableObjectNode> CurrentlyVisibleNodes { get; set; } = [];

        /// <summary>
        /// Consists of all the 0 level nodes in the tree.
        /// </summary>
        public List<HitTestableObjectNode> BaseLevelNodes { get; set; } = [];
        #endregion

        #region Constructors
        public HitTestableObjectTree(CadManager3D cadManager, Rect extents, int levels)
        {
            _cadManager = cadManager;
            Extents = extents;
            Levels = levels;

            Initialize();
        }
        #endregion

        #region Methods
        private void Initialize()
        {
            GetDrawingObjects();
            GetRoot();
        }
        private void GetRoot()
        {
            Root = new(HitTestableObjects, Levels, Extents, this);
        }
        private void GetDrawingObjects()
        {
            foreach (var keyValue in _cadManager.Layers)
            {
                HitTestableObjects.AddRange(keyValue.Value.DrawingObject3Ds);
            }
            foreach (var keyValue in _cadManager.PointGroups)
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
