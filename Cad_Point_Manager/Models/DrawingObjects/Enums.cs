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
        DrawingSpline,
        DrawingDimension
    }

    public enum DrawingObject3dColorType
    {
        ByLayer,
        ByBlock,
        ByObject
    }

    public enum DrawingDimensionType
    {
        Linear,
        Aligned,
        Angular,
        Diameter,
        Radius,
        Ordinate
    }
}
