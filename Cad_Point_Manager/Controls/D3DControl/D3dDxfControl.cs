using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;

using Buffer = SharpDX.Direct3D11.Buffer;

namespace Cad_Point_Manager.Controls.D3DControl
{
    public class D3dDxfControl : Direct3DControl
    {
        #region Fields
        private Buffer _vertexBuffer;
        private Vertex[] _vertices;
        private VertexShader _vertexShader;
        private PixelShader _pixelShader;
        private InputLayout _inputLayout;
        #endregion

        #region Properties
        public bool DxfIsDirty { get; set; } = true;
        public bool DrawingIsDirty { get; set; } = true;
        public bool ShadersLoaded { get; set; } = false;
        #endregion

        #region Constructors
        public D3dDxfControl() { }
        #endregion

        #region Methods
        public override void Render()
        {
            if (_d3dResCache is null) { return; }

            if (!ShadersLoaded) { InitializeShaders(); }
            if (DxfIsDirty) { GetDxfLines(); }
            if (DrawingIsDirty) { DrawDxf(); }
        }

        private void DrawDxf()
        {
            var context = _d3dResCache.DeviceContext;

            // Set render target
            context.OutputMerger.SetRenderTargets(_d3dResCache.RenderTargetView);
            context.ClearRenderTargetView(_d3dResCache.RenderTargetView, new SharpDX.Mathematics.Interop.RawColor4(0, 0, 0, 1));

            // Set shaders
            context.VertexShader.Set(_vertexShader);
            context.PixelShader.Set(_pixelShader);
            context.InputAssembler.InputLayout = _inputLayout;

            // Bind vertex buffer
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertexBuffer, Utilities.SizeOf<Vertex>(), 0));
            context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.LineList;

            // Draw
            context.Draw(_vertices.Length, 0);

            DrawingIsDirty = false;
        }

        private void GetDxfLines()
        {
            if (_d3dResCache is null) { return; }

            var vector11 = new Vector3(0, 0, 0);
            var vector12 = new Vector3((float)ActualWidth, (float)ActualHeight, 0);
            var vector21 = new Vector3(0, (float)ActualHeight, 0);
            var vector22 = new Vector3((float)ActualWidth, 0, 0);

            //_vertices =
            //[
            //    new Vertex { Position = vector11, Color = new Vector4(1f, 0f, 0f, 1f) },
            //    new Vertex { Position = vector12, Color = new Vector4(1f, 0f, 0f, 1f) }
            //];

            _vertices = new[]
            {
                new Vertex { Position = new Vector3(-0.5f, 0.5f, 0f), Color = new Vector4(1f, 0f, 0f, 1f) },
                new Vertex { Position = new Vector3(0.5f, -0.5f, 0f), Color = new Vector4(0f, 1f, 0f, 1f) }
            };

            _vertexBuffer = Buffer.Create(
                _d3dResCache.Device,
                BindFlags.VertexBuffer,
                _vertices
            );

            DxfIsDirty = false;
        }

        private void InitializeShaders()
        {
            //var vertexShaderByteCode = ShaderBytecode.CompileFromFile("Shaders/VertexShader.hlsl", "VSMain", "vs_5_0");
            //_vertexShader = new VertexShader(_d3dResCache.Device, vertexShaderByteCode);

            //var pixelShaderByteCode = ShaderBytecode.CompileFromFile("Shaders/PixelShader.hlsl", "PSMain", "ps_4_0");
            //_pixelShader = new PixelShader(_d3dResCache.Device, pixelShaderByteCode);

            var path = @"C:\Users\Tim\Desktop\Temp CadPointManager\Cad_Point_Manager\Controls\D3DControl\Shaders.hlsl";

            var vertexShaderByteCode = ShaderBytecode.CompileFromFile("Shaders.hlsl", "VSMain", "vs_4_0");
            _vertexShader = new VertexShader(_d3dResCache.Device, vertexShaderByteCode);

            var pixelShaderByteCode = ShaderBytecode.CompileFromFile("Shaders.hlsl", "PSMain", "ps_4_0");
            _pixelShader = new PixelShader(_d3dResCache.Device, pixelShaderByteCode);

            _inputLayout = new InputLayout(
                _d3dResCache.Device,
                ShaderSignature.GetInputSignature(vertexShaderByteCode),
                new[]
                {
                    new InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float, 0, 0),
                    new InputElement("COLOR", 0, SharpDX.DXGI.Format.R32G32B32A32_Float, 12, 0)
                });

            ShadersLoaded = true;
        }
        #endregion
    }
}
