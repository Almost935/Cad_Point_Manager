using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;

namespace Cad_Point_Manager.Models.DrawingObjects.Dimensioning
{
    public abstract class DrawingDimension : DrawingObject
    {
        #region Fields
        protected DrawingBlock _dimensionBlock;
        //protected readonly List<DrawingObject> _drawingObjects = [];
        protected readonly List<LineVertex> _lineVertices = [];
        protected readonly List<TextVertex> _textVertices = [];
        #endregion

        #region Properties
        public DrawingDimensionType DimensionType { get; set; }
        public int StartLineVertexIndex { get; set; }
        public int EndLineVertexIndex { get; set; }
        public int StartTextVertexIndex { get; set; }
        public int EndTextVertexIndex { get; set; }

        public DrawingBlock DimensionBlock => _dimensionBlock;
        //public IReadOnlyList<DrawingObject> DrawingObjects => _drawingObjects;
        public IReadOnlyList<LineVertex> LineVertices => _lineVertices;
        public IReadOnlyList<TextVertex> TextVertices => _textVertices;
        #endregion

        #region Methods
        public abstract void UpdateGeometryVertices(uint layerId, uint objectId);
        public abstract void UpdateTextVertices(ResCache resCache, uint layerId, SceneIdMap sceneIdMap, D3dStateBuffers stateBuffers);
        #endregion
    }
}
