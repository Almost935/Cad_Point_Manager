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
    public class DrawingCircle3D : DrawingSegment3D
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

        #endregion
    }
}
