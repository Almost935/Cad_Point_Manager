namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public enum DrawingObject3dType
    {
        DrawingLine3D,
        DrawingArc3D,
        DrawingCircle3D,
        DrawingPolyline3D,
        DrawingBlock3D,
        DrawingText3D,
        DrawingMtext3D
    }

    public enum DrawingObject3dColorType
    {
        ByLayer,
        ByBlock,
        ByObject
    }
}
