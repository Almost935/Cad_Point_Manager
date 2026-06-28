using netDxf.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Extensions
{
    public static class NetDxfPolylineVertexExtensions
    {
        public static double DistanceTo(this Polyline2DVertex start, Polyline2DVertex end)
        {
            var dx = end.Position.X - start.Position.X;
            var dy = end.Position.Y - start.Position.Y;

            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
