using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
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

        private Matrix _transformMatrix = Matrix.Identity;
        #endregion

        #region Properties
        public bool DxfIsDirty { get; set; } = true;
        public bool DrawingIsDirty { get; set; } = true;
        public bool ShadersLoaded { get; set; } = false;

        public Matrix TransformMatrix
        {
            get { return _transformMatrix; }
            set 
            { 
                _transformMatrix = value;
                DrawingIsDirty = true;
            }
        }
        #endregion

        #region Constructors
        public D3dDxfControl() { }
        #endregion

        #region Private Methods
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
            context.ClearRenderTargetView(_d3dResCache.RenderTargetView, new SharpDX.Mathematics.Interop.RawColor4(0, 0, 0, 0));

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

            _vertices =
            [
                new Vertex { Position = new Vector3(-0.5f, 0.5f, 0f), Color = new Vector4(1f, 0f, 0f, 1f) },
                new Vertex { Position = new Vector3(0.5f, 0.5f, 0f), Color = new Vector4(1f, 0f, 0f, 1f) },

                new Vertex { Position = new Vector3(0.5f, 0.5f, 0f), Color = new Vector4(0f, 1f, 0f, 1f) },
                new Vertex { Position = new Vector3(0.5f, -0.5f, 0f), Color = new Vector4(0f, 1f, 0f, 1f) },

                new Vertex { Position = new Vector3(0.5f, -0.5f, 0f), Color = new Vector4(0f, 0f, 1f, 1f) },
                new Vertex { Position = new Vector3(-0.5f, -0.5f, 0f), Color = new Vector4(0f, 0f, 1f, 1f) },

                new Vertex { Position = new Vector3(-0.5f, -0.5f, 0f), Color = new Vector4(1f, 0f, 1f, 1f) },
                new Vertex { Position = new Vector3(-0.5f, 0.5f, 0f), Color = new Vector4(1f, 0f, 1f, 1f) }
            ];

            _vertexBuffer = Buffer.Create(
                _d3dResCache.Device,
                BindFlags.VertexBuffer,
                _vertices
            );

            DxfIsDirty = false;
        }

        private void InitializeShaders()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            while (Path.GetFileName(path) != "Cad_Point_Manager")
            {
                path = Path.GetDirectoryName(path);
                if (path == null)
                {
                    throw new DirectoryNotFoundException("The 'Cad_Point_Manager' directory could not be found in the path.");
                }
            }
            string shadersPath = path + @"\Controls\D3DControl\Shaders.hlsl";

            var vertexShaderByteCode = ShaderBytecode.CompileFromFile(shadersPath, "VSMain", "vs_4_0");
            _vertexShader = new VertexShader(_d3dResCache.Device, vertexShaderByteCode);

            var pixelShaderByteCode = ShaderBytecode.CompileFromFile(shadersPath, "PSMain", "ps_4_0");
            _pixelShader = new PixelShader(_d3dResCache.Device, pixelShaderByteCode);

            _inputLayout = new InputLayout(
                _d3dResCache.Device,
                ShaderSignature.GetInputSignature(vertexShaderByteCode),
                [
                    new InputElement("POSITION", 0, SharpDX.DXGI.Format.R32G32B32_Float, 0, 0),
                    new InputElement("COLOR", 0, SharpDX.DXGI.Format.R32G32B32A32_Float, 12, 0)
                ]);

            ShadersLoaded = true;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(this);
            
            e.Handled = true;
        }
        #endregion

        #region Public Methods
        public void UpdateTransformMatrix(Matrix matrix)
        {
            TransformMatrix = matrix;
            DrawingIsDirty = true;
        }
        #endregion
    }
}
