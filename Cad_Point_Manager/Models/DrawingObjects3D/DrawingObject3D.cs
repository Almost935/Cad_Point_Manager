using Cad_Point_Manager.Controls.D3DControl;
using SharpDX;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class DrawingObject3D
    {
        public DrawingObject3dType Type { get; set; }
        public Vector4 Color { get; set; }
        public Vector4 LayerColor { get; set; }
    }
}
