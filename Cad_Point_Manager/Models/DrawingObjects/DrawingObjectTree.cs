using System.Windows;
using Point = System.Windows.Point;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingObjectTree
    {
        #region Fields
        private const float _viewInflationFactor = 1.1f;

        private CadManager _cadManager;
        #endregion

        #region Properties
        public List<DrawingObject> DrawingObjects { get; set; } = [];
        public Rect Extents { get; set; }
        public int Levels { get; set; }
        public DrawingObjectNode Root { get; set; }
        public List<DrawingObjectNode> CurrentlyVisibleNodes { get; set; } = new();

        /// <summary>
        /// Consists of all the 0 level nodes in the tree.
        /// </summary>
        public List<DrawingObjectNode> BaseLevelNodes { get; set; } = new();
        #endregion

        #region Constructors
        public DrawingObjectTree(CadManager cadManager, Rect extents, int levels)
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
            Root = new(DrawingObjects, Levels, Extents, this);
        }
        private void GetDrawingObjects()
        {
            foreach (var layer in _cadManager.Layers.Values)
            {
                DrawingObjects.AddRange(layer.DrawingObjects);
            }
        }
        public List<DrawingObjectNode> GetIntersectingNodes(Rect view)
        {
            List<DrawingObjectNode> quadTreeNodes = [];

            quadTreeNodes.AddRange(Root.GetIntersectingQuadTreeNodes(view));

            return quadTreeNodes;
        }
        public List<DrawingObjectNode> GetIntersectingNodes(Point p)
        {
            List<DrawingObjectNode> quadTreeNodes = [];

            quadTreeNodes.AddRange(Root.GetNodesAtPoint(p));

            return quadTreeNodes;
        }
        public DrawingObjectNode GetIntersectingNode(Point p)
        {
            return Root.GetNodeAtPoint(p);
        }

        //public void UpdateCurrentlyVisibleNodes(Rect view)
        //{
        //    view.Inflate(new System.Windows.Size);
        //    CurrentlyVisibleNodes = GetIntersectingNodes(view);
        //}
        #endregion
    }
}
