using SharpDX;
using Cad_Point_Manager.Controls.D3DControl;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public abstract class DrawingText3D : DrawingObject3D
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
        public abstract void UpdateTextVertices(D3dResCache d3DResCache);
        #endregion
    }
}
