using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;
using SharpDX;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public abstract class DrawingText : DrawingObject
    {
        #region Properties
        public abstract List<TextVertex> TextVertices { get; set; }

        public string Text { get; set; }
        public float MaxWidth { get; set; }
        public int StartVertexIndex { get; set; }
        public int EndVertexIndex { get; set; }
        public Vector3 Position { get; set; }
        #endregion

        #region Methods
        public abstract void UpdateTextVertices(ResCache d3DResCache, uint layerId, SceneIdMap sceneIdMap, D3dStateBuffers stateBuffers);
        #endregion
    }
}
