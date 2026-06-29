using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Models.DrawingObjects;
using netDxf.Entities;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Helpers
{
    public class WidenedGeometryRenderingHelpers
    {
        public static PathGeometry GetWidenedPolylineGeometry(ResCache resCache, DrawingWidePolyline widePolyLine, float width)
        {
            PathGeometry pathGeometry = new(resCache.D2dFactory);

            using GeometrySink sink = pathGeometry.Open();

            foreach (DrawingObject drawingObject in widePolyLine.DrawObjects)
            {

            }

            sink.Close();
            return pathGeometry;
        }
    }
}
