using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.DrawingObjects.HelperClasses
{
    internal sealed class OffsetSegment
    {
        // Original centerline
        public Vector2 Start;
        public Vector2 End;

        public float StartWidth;
        public float EndWidth;

        // Offset geometry
        public Vector2 LeftStart;
        public Vector2 LeftEnd;

        public Vector2 RightStart;
        public Vector2 RightEnd;

        // Original geometry
        public bool IsArc;

        // Line data
        public Vector2 Direction;
        public Vector2 Normal;

        // Arc data
        public Vector2 Center;
        public float Radius;
        public float StartAngle;
        public float EndAngle;
        public bool Clockwise;
    }
}
