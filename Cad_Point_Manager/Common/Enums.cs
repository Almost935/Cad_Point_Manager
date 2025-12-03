namespace Cad_Point_Manager.Common
{
    public class Enums
    {
        public enum LineType
        {
            Solid,
            Dash,
            Dot,
            DashDot,
            DashDotDot
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

        public enum DrawingObjectType
        {
            Line,
            Circle,
            Arc,
            Ellipse,
            Text,
            MText,
            Polyline,
            LwPolyline,
            Spline,
            Hatch,
            Image,
            Insert,
            Block,
            MLine,
            Dimension,
            Leader,
            Table,
            Tolerance,
            Viewport,
            Mesh,
            Face3D,
            Solid,
            Trace,
            Underlay,
            XLine,
            Ray,
            PolyfaceMesh,
            MTextAttribute,
            Polyline2D,
            Polyline3D,
            PolyfaceMeshVertex,
            PolylinePface,
            PolylineMesh,
            PolylineMeshVertex,
            PolylineMeshFace,
            PolylineMeshEdge,
            PolylineMeshSeqend,
            PolylineMeshSeqendVertex,
            PolylineMeshSeqendFace,
            PolylineMeshSeqendEdge,
            PolylineMeshSeqendSeqend,
            PolylineMeshSeqendSeqendVertex,
            PolylineMeshSeqendSeqendFace,
            PolylineMeshSeqendSeqendEdge,
            PolylineMeshSeqendSeqendSeqend,
            PolylineMeshSeqendSeqendSeqendVertex,
            PolylineMeshSeqendSeqendSeqendFace,
            PolylineMeshSeqendSeqendSeqendEdge,
            PolylineMeshSeqendSeqendSeqendSeqend,
            PolylineMeshSeqendSeqendSeqendSeqendVertex,
            PolylineMeshSeqendSeqendSeqendSeqendFace,
            PolylineMeshSeqendSeqendSeqendSeqendEdge,
            PolylineMeshSeqendSeqendSeqendSeqendSeqend,
            PolylineMeshSeqendSeqendSeqendSeqendSeqendVertex,
            PolylineMeshSeqendSeqendSeqendSeqendSeqendFace,
            PolylineMeshSeqendSeqendSeqendSeqendSeqendEdge,
            PolylineMeshSeqendSeqendSeqendSeqendSeqendSeqend,
            PolylineMeshSeqendSeqendSeqendSeqendSeqendSeqendVertex,
            PolylineMeshSeqendSeqendSeqendSeqendSeqendSeqendFace
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
}
