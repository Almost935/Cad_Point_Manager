using System.ComponentModel;

namespace Cad_Point_Manager.Common
{
    public enum FilterType
    {
        [Description("Point Number Filter")]
        PointNumberFilter,

        [Description("Northing Filter")]
        NorthingFilter,

        [Description("Easting Filter")]
        EastingFilter,

        [Description("Elevation Filter")]
        ElevationFilter,

        [Description("Description Filter")]
        DescriptionFilter,

        [Description("Point Group Filter")]
        PointGroupFilter,
    }

    public enum SelectionMode
    {
        Points,
        Geometries,
        All,
        CogoPoints
    }

    /// <summary>
    /// Represents the type of significant point on the CAD geometry. Midpoint represents midway along a geometry between two endpoints, 
    /// EndPoint represents the end of a geometry, Intersection represents the point where two geometries cross, and MousePosition 
    /// represents the current position of the mouse cursor when no other significant point is within range.
    /// </summary>
    public enum SignificantPointType
    {
        MidPoint,
        EndPoint,
        Intersection,
        MousePosition
    }

    public enum EllipseType
    {
        FullEllipse,
        Arc
    }

    public enum TextAttachmentPoint
    {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }

    public enum TextAlignment
    {
        Left,
        Center,
        Right,
        Justified,
        Distributed
    }
}