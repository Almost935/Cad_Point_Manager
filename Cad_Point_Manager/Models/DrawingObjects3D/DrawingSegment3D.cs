using Cad_Point_Manager.Controls.D3DControl;
using netDxf;
using netDxf.Entities;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Vector3 = SharpDX.Vector3;
using Vector4 = SharpDX.Vector4;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public abstract class DrawingSegment3D : DrawingGeometry3D
    {
        #region Properties
        public bool IsPartOfPolyline { get; set; } = false;
        public DrawingPolyline3D DrawingPolyline3D { get; set; }
        public float Length { get; set; }
        #endregion
    }
}
