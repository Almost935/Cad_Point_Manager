using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public enum DrawingObject3dType
    {
        DrawingLine3D,
        DrawingArc3D,
        DrawingCircle3D,
        DrawingPolyline3D,
        DrawingBlock3D
    }

    public enum DrawingObject3dColorType
    {
        ByLayer,
        ByBlock,
        ByObject
    }
}
