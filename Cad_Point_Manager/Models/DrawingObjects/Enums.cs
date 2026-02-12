namespace Cad_Point_Manager.Models.DrawingObjects
{
    public enum DrawingObjectType
    {
        DrawingLine,
        DrawingArc,
        DrawingCircle,
        DrawingPolyline,
        DrawingBlock,
        DrawingSText,
        DrawingMtext,
        DrawingMtextSegment,
        DrawingSpline
    }

    public enum DrawingObject3dColorType
    {
        ByLayer,
        ByBlock,
        ByObject
    }
}
