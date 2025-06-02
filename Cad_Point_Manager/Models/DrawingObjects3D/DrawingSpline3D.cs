using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using netDxf.Entities;
using SharpDX;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class DrawingSpline3D : DrawingSegment3D
    {
        #region Fields
        private int _polylineApproximationPrecision = 32;
        #endregion

        #region Properties
        public DrawingPolyline3D PolylineApproximation { get; set; }
        #endregion

        #region Constructors
        public DrawingSpline3D(Spline spline, ObjectLayer3D layer, bool isPartOfBlock = false, DrawingBlock3D block = null)
        {
            Type = DrawingObject3dType.DrawingSpline3D;
            Layer = layer;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock3D = block;

            UpdateColor();
            UpdateData(spline);
        }
        #endregion

        #region Methods
        public override void UpdateData(EntityObject entity)
        {
            if (entity is Spline spline)
            {
                var polyline = spline.ToPolyline2D(_polylineApproximationPrecision);
                DrawingPolyline3D = new(polyline, Layer, isPartOfBlock: IsPartOfBlock, block: DrawingBlock3D);
            }
            else
            {
                throw new ArgumentException("entity must be of type Spline");
            }
        }
    #endregion
}
}
