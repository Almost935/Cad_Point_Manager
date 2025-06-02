using netDxf.Entities;
using Vector3 = SharpDX.Vector3;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public abstract class DrawingCurve3D : DrawingSegment3D
    {
        #region Properties
        public float Radius { get; set; }
        public Vector3 RadiusPoint { get; set; }
        public float StartAngle { get; set; }
        public float EndAngle { get; set; }
        public int NumberOfSegments { get; set; }
        public float Sweep { get; set; }
        public float Diameter => Radius * 2;
        #endregion

        #region Methods
        public abstract void UpdateVertices(EntityObject entity);

        public static int CalculateArcSegments(double radius, double angleInDegrees, double smoothnessFactor = 10)
        {
            double angleInRadians = angleInDegrees * Math.PI / 180.0;

            double segments = (angleInRadians / radius) * smoothnessFactor;

            // Return a value rounded to an integer, ensuring at least 4 segments
            return Math.Max(10, (int)Math.Ceiling(segments));
        }

        public static int CalculateSegments(double radius, double sweep, double tolerance = 0.001)
        {
            // Convert sweep angle from degrees to radians
            double sweepRadians = sweep * Math.PI / 180.0;

            // Ensure the tolerance is reasonable
            if (tolerance <= 0 || radius <= 0)
                throw new ArgumentException("Radius and tolerance must be greater than zero.");

            // Calculate the angle step based on the tolerance
            double angleStep = 2 * Math.Acos(1 - (tolerance / radius));

            // Calculate the number of segments
            int numSegments = (int)Math.Ceiling(sweepRadians / angleStep);


            // Ensure at least one segment for very small arcs
            return Math.Max(numSegments, 10);
        }
        #endregion
    }
}
