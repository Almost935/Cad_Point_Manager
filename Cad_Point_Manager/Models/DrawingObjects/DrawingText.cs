using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;
using SharpDX;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public abstract class DrawingText : DrawingObject
    {
        #region Properties
        public List<TextVertex> TextVertices { get; set; } = [];
        public List<LineVertex> LineVertices { get; set; } = [];
        public TextRenderStyle TextRenderStyle { get; set; } = TextRenderStyle.Triangle;

        public string Text { get; set; }
        public float MaxWidth { get; set; }
        public int StartLineVertexIndex { get; set; }
        public int EndLineVertexIndex { get; set; }
        public int StartTextVertexIndex { get; set; }
        public int EndTextVertexIndex { get; set; }
        public Vector3 Position { get; set; }
        #endregion

        #region Methods
        public abstract void UpdateVertices(ResCache d3DResCache, uint layerId, SceneIdMap sceneIdMap, D3dStateBuffers stateBuffers);
        #endregion
    }
}
