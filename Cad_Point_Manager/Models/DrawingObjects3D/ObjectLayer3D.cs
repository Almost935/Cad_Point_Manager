using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class ObjectLayer3D
    {
        public string LayerName { get; set; }
        public List<DrawingObject3D> DrawingObject3Ds { get; set; } = [];

        public ObjectLayer3D()
        {
            
        }
    }
}
