using Cad_Point_Manager.Controls.D3DControl;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public abstract class DrawingCurve3D : DrawingSegment3D
    {
        #region Fields
        private const float _toleranceAngle = (float)(Math.PI / 180);
        #endregion

        #region Properties
        protected float ToleranceAngle { get => _toleranceAngle; }

        public float Radius { get; set; }
        public Vector3 Center { get; set; }
        public float Length { get; set; }
        public float StartAngle { get; set; }
        public float EndAngle { get; set; }
        public List<Vertex> Vertices { get; set; } = [];
        public int NumberOfSegments { get; set; }
        public float Sweep { get; set; }
        public float Diameter => Radius * 2;
        #endregion

        #region Methods
        public abstract void UpdateVertices();
        #endregion
    }
}
