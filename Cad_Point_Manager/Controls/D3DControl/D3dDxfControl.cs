using SharpDX;
using SharpDX.Direct3D11;

using Buffer = SharpDX.Direct3D11.Buffer;

namespace Cad_Point_Manager.Controls.D3DControl
{
    public class D3dDxfControl : Direct3DControl
    {
        #region Fields
        private Buffer _vertexBuffer;
        private Vertex[] _vertices;
        #endregion

        #region Properties
        public bool DrawingIsDirty { get; set; } = true;
        #endregion

        #region Constructors
        public D3dDxfControl() { }
        #endregion

        #region Methods
        public override void Render(D3dResCache d3DResCache)
        {
            ArgumentNullException.ThrowIfNull(d3DResCache);

            _d3dResCache = d3DResCache;
            d3DResCache.DeviceContext.ClearRenderTargetView(d3DResCache.RenderTargetView, SharpDX.Color.Bisque);

            if (DrawingIsDirty) { GetDxfLines(); }
        }

        private void DrawDxfLine()
        {
            _d3dResCache.DeviceContext.OutputMerger.SetRenderTargets(_d3dResCache.RenderTargetView);
            _d3dResCache.DeviceContext.ClearRenderTargetView(_d3dResCache.RenderTargetView, new SharpDX.Mathematics.Interop.RawColor4(0, 0, 0, 1));

            // Bind vertex buffer
            _d3dResCache.DeviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertexBuffer, Utilities.SizeOf<Vertex>(), 0));
            _d3dResCache.DeviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.LineList;

            // Draw lines
            _d3dResCache.DeviceContext.Draw(_vertices.Length, 0);
        }

        private void GetDxfLines()
        {
            if (_d3dResCache is null) { return; }

            var vector11 = new Vector3(0, 0, 0);
            var vector12 = new Vector3((float)ActualWidth, (float)ActualHeight, 0);
            var vector21 = new Vector3(0, (float)ActualHeight, 0);
            var vector22 = new Vector3((float)ActualWidth, 0, 0);

            _vertices =
            [
                new Vertex { Position = vector11, Color = new Vector4(1f, 0f, 0f, 1f) },
                new Vertex { Position = vector12, Color = new Vector4(1f, 0f, 0f, 1f) }
            ];

            _vertexBuffer = SharpDX.Direct3D11.Buffer.Create(
                _d3dResCache.Device,
                BindFlags.VertexBuffer,
                _vertices
            );

            DrawingIsDirty = false;
        }
        #endregion
    }
}
