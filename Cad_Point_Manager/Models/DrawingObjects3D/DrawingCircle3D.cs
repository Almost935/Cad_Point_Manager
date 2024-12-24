using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Models.DrawingObjects;
using Cad_Point_Manager.Models.DrawingObjects3D;
using Cad_Point_Manager.Models.SerializableObjects;
using netDxf.Entities;
using netDxf.Tables;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System.Windows;
using static netDxf.Entities.HatchBoundaryPath;
using Ellipse = SharpDX.Direct2D1.Ellipse;

namespace Cad_Point_Manager.DrawingObjects
{
    public class DrawingCircle3D : DrawingCurve3D
    {
        #region Fields
        private Circle _dxfCircle;
        #endregion

        #region Properties
        public Circle DxfCircle
        {
            get { return _dxfCircle; }
            set
            {
                _dxfCircle = value;
                OnPropertyChanged(nameof(DxfCircle));
            }
        }

        public float Radius { get; set; }
        public RawVector2 Center { get; set; }
        public List<Vertex> IntermediateVertices { get; set; } = [];
        public float Circumference { get; set; }
        #endregion

        #region Constructor
        public DrawingCircle3D(Circle circle, ObjectLayer3D layer, bool isPartOfBlock = false, DrawingBlock3D block = null)
        {
            Type = DrawingObject3dType.DrawingArc3D;
            Layer = layer;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock3D = block;
            EntityObject = circle;

            UpdateColor();
        }
        #endregion

        #region Methods
        public override void UpdateData(EntityObject entity)
        {
            if (entity is Circle circle)
            {
                Radius = (float)circle.Radius;
                StartAngle = (float)circle.StartAngle;
                EndAngle = (float)circle.EndAngle;

                Sweep = EndAngle - StartAngle;
                if (Sweep < 0) { Sweep += 360; }
                IsLargeArc = Sweep >= 180;

                Length = (float)((Sweep / 360) * (2 * Math.PI * Radius));
            }
            else
            {
                throw new ArgumentException("entity must be of type Circle");
            }
        }
        #endregion
    }
}
