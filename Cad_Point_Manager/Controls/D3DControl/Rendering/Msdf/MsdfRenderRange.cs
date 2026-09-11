using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Msdf
{
    public readonly struct MsdfRenderRange(int startInstance, int instanceCount)
    {
        public readonly int StartInstance = startInstance;
        public readonly int InstanceCount = instanceCount;
    }
}
