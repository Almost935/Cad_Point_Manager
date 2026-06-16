namespace Cad_Point_Manager.Models.DrawingObjects
{
    public enum TextRenderStyle
    {
        Stroke,
        Triangle
    }
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
        DrawingDimension,
        DrawingSolid
    }

    public enum ColorType
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
