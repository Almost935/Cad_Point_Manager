using SharpDX.Direct3D;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Controls.D3DControl
{
    public class D3dDxfControl : D3dControl
    {
        public override void Render(DeviceContext deviceContext)
        {
            // Clear the render target
            deviceContext.ClearRenderTargetView(_renderTargetView, SharpDX.Color.CornflowerBlue);

            // Set the vertex buffer
            var vertexBufferBinding = new VertexBufferBinding(_vertexBuffer, Utilities.SizeOf<Vertex>(), 0);
            deviceContext.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);

            // Set primitive topology
            deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
            
            // Draw the lines
            deviceContext.Draw(2, 0);
        }
    }
}
