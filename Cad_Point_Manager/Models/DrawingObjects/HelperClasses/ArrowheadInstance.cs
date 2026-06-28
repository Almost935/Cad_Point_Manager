using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.DrawingObjects.HelperClasses
{
    public class ArrowheadInstance
    {
        public DrawingObject DrawingObject { get; init; }
        public Vector3 Translation { get; init; }
        public float RotationRadians { get; init; }
        public float Scale { get; init; }

        public Matrix Transform =>
            Matrix.Scaling(Scale)
            * Matrix.RotationZ(RotationRadians)
            * Matrix.Translation(Translation);
    }
}
