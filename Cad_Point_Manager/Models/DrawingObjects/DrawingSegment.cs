namespace Cad_Point_Manager.Models.DrawingObjects
{
    public abstract class DrawingSegment : DrawingGeometry
    {
        #region Properties
        public bool IsPartOfPolyline { get; set; } = false;
        public DrawingPolyline DrawingPolyline3D { get; set; }
        public float Length { get; set; }
        #endregion
    }
}
