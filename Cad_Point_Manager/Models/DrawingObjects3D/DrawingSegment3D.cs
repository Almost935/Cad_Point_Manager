using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public abstract class DrawingSegment3D : DrawingGeometry3D
    {
        #region Properties
        public bool IsPartOfPolyline { get; set; } = false;
        public DrawingPolyline3D DrawingPolyline3D { get; set; }
        public float Length { get; set; }
        #endregion
    }
}
