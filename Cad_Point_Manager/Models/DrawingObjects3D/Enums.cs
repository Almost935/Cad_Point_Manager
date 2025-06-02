namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public enum DrawingObject3dType
    {
        DrawingLine3D,
        DrawingArc3D,
        DrawingCircle3D,
        DrawingPolyline3D,
        DrawingBlock3D,
        DrawingSText3D,
        DrawingMtext3D,
        DrawingSpline3D
    }

    public enum DrawingObject3dColorType
    {
        ByLayer,
        ByBlock,
        ByObject
    }
}
