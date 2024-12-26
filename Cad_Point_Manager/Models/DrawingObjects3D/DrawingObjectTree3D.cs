using Cad_Point_Manager.Models.DrawingObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class DrawingObjectTree3D
    {
        #region Fields
        private const float _viewInflationFactor = 1.1f;

        private CadManager3D _cadManager;
        #endregion

        #region Properties
        public List<DrawingObject3D> DrawingObjects { get; set; } = [];
        public Rect Extents { get; set; }
        public int Levels { get; set; }
        public DrawingObjectNode3D Root { get; set; }
        public List<DrawingObjectNode3D> CurrentlyVisibleNodes { get; set; } = [];

        /// <summary>
        /// Consists of all the 0 level nodes in the tree.
        /// </summary>
        public List<DrawingObjectNode3D> BaseLevelNodes { get; set; } = new();
        #endregion

        #region Constructors
        public DrawingObjectTree3D(CadManager3D cadManager, Rect extents, int levels)
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
                DrawingObjects.AddRange(layer.DrawingObject3Ds);
            }
        }
        public List<DrawingObjectNode3D> GetIntersectingNodes(Rect view)
        {
            List<DrawingObjectNode3D> quadTreeNodes = [];

            quadTreeNodes.AddRange(Root.GetIntersectingQuadTreeNodes(view));

            return quadTreeNodes;
        }
        public List<DrawingObjectNode3D> GetIntersectingNodes(Point p)
        {
            List<DrawingObjectNode3D> quadTreeNodes = [];

            quadTreeNodes.AddRange(Root.GetNodesAtPoint(p));

            return quadTreeNodes;
        }
        public DrawingObjectNode3D GetIntersectingNode(Point p)
        {
            return Root.GetNodeAtPoint(p);
        }
        #endregion
    }
}
