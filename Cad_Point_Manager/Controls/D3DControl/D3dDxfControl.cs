using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

using Buffer = SharpDX.Direct3D11.Buffer;
using Point = System.Windows.Point;

namespace Cad_Point_Manager.Controls.D3DControl
{
    public class D3dDxfControl : Direct3DControl
    {
        #region Fields
        private const float _scaleFactor = 1.25f;

        private Buffer _vertexBuffer;
        private Buffer _transformationBuffer;
        private Vertex[] _vertices;
        private VertexShader _vertexShader;
        private PixelShader _pixelShader;
        private InputLayout _inputLayout;

        //Panning and Zooming Fields
        private bool _isPanning = false;
        private Point _lastTranslatePos;
        private Point _pointerCoords;

        private Matrix _transformMatrix = new(
            1,0,0,0,
            0,1,0,0,
            0,0,1,0,
            0,0,0,1
            );
        #endregion

        #region Properties
        public bool DxfIsDirty { get; set; } = true;
        public bool D3dIsDirty { get; set; } = true;
        public bool ShadersLoaded { get; set; } = false;
        public bool ConstantBufferInitialized { get; set; } = false;

        public Matrix TransformMatrix
        {
            get { return _transformMatrix; }
            set
            {
                _transformMatrix = value;
                D3dIsDirty = true;
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
            if (!ConstantBufferInitialized) { InitializeConstantBuffer(); }
            if (DxfIsDirty) { GetDxfLines(); }
            if (D3dIsDirty) { DrawDxf(); }
        }

        private void DrawDxf()
        {
            var context = _d3dResCache.DeviceContext;

            // Set render target and clear it
            context.OutputMerger.SetRenderTargets(_d3dResCache.RenderTargetView);
            context.ClearRenderTargetView(_d3dResCache.RenderTargetView, new SharpDX.Mathematics.Interop.RawColor4(0, 0, 0, 0));

            // Update constant buffer
            UpdateConstantBuffer();

            // Set shaders
            context.VertexShader.Set(_vertexShader);
            context.PixelShader.Set(_pixelShader);
            context.InputAssembler.InputLayout = _inputLayout;

            // Set the constant buffer to the vertex shader
            context.VertexShader.SetConstantBuffer(0, _transformationBuffer);

            // Bind vertex buffer and draw
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertexBuffer, Utilities.SizeOf<Vertex>(), 0));
            context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.LineList;
            context.Draw(_vertices.Length, 0);

            D3dIsDirty = false;
        }

        private void GetDxfLines()
        {
            if (_d3dResCache is null) { return; }

            int numLines = 100;

            _vertices = new Vertex[numLines * 2];
            float factor = 2f / numLines;
            float redStart = 0;
            float blueStart = 1;

            for (int i = 0; i < numLines; i++)
            {
                float x = -1 + factor * i;

                Vertex startVertex = new(new Vector3(x, 1, 0f), new Vector4((redStart + factor * i), 0f, (blueStart - factor * i), 1f));
                Vertex endVertex = new(new Vector3(x, -1, 0f), new Vector4((redStart + factor * i), 0f, (blueStart - factor * i), 1f));
                _vertices[i * 2] = startVertex;
                _vertices[i * 2 + 1] = endVertex;
            }

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

        private void InitializeConstantBuffer()
        {
            var bufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<TransformationBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            _transformationBuffer = new Buffer(_d3dResCache.Device, bufferDesc);
            ConstantBufferInitialized = true;
        }

        private void UpdateConstantBuffer()
        {
            // Create the MatrixBuffer structure and assign the current transformation matrix
            var transposedMatrix = Matrix.Transpose(_transformMatrix);

            var transformationBuffer = new TransformationBuffer
            {
                WorldViewProjection = transposedMatrix
            };

            // Update the constant buffer with the new matrix
            _d3dResCache.DeviceContext.UpdateSubresource(ref transformationBuffer, _transformationBuffer);
        }


        protected override void OnMouseMove(MouseEventArgs e)
        {
            _pointerCoords = e.GetPosition(this);

            if (_isPanning)
            { 
                var translate = _lastTranslatePos - _pointerCoords;

                //if (translate.LengthSquared < 1) { return; } //Prevent unneccessary translations

                //Matrix.Translation((float)translate.X, (float)translate.Y, 0, out Matrix translationMatrix);
                //UpdateTransformMatrix(translationMatrix);

                TranslateTransformMatrix((float)translate.X, (float)translate.Y);

                _lastTranslatePos = _pointerCoords;
            }

            e.Handled = true;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                _isPanning = true;
                _lastTranslatePos = e.GetPosition(this);
                this.Cursor = Cursors.Hand;
            }

            e.Handled = true;
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                _isPanning = false;
                this.Cursor = null;

                var translate = _pointerCoords - _lastTranslatePos;
                Debug.WriteLine($"translate: {translate}");
            }

            e.Handled = true;
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            float scale = e.Delta > 0 ? 1.25f : 0.75f;
            Matrix.Scaling(scale, scale, 1, out Matrix scaleMatrix);
            UpdateTransformMatrix(scaleMatrix);
        }

        public void ResetTransformMatrix()
        {
            TransformMatrix = Matrix.Identity;
            D3dIsDirty = true;
        }
        public void TranslateTransformMatrix(float x, float y)
        {
            Debug.WriteLine($"Translate: {x} {y}");

            Matrix.Translation(x / 100, y / 100, 0, out Matrix translationMatrix);
            UpdateTransformMatrix(translationMatrix);
        }
        public void UpdateTransformMatrix(Matrix matrix)
        {
            Matrix.Multiply(ref _transformMatrix, ref matrix, out Matrix newTransformMatrix);

            // Ensure that we are not changing the Z-axis, using only 2D transformations
            //Matrix adjustedMatrix = new Matrix(newTransformMatrix.M11, newTransformMatrix.M12, 0, 0,
            //                                   newTransformMatrix.M21, newTransformMatrix.M22, 0, 0,
            //                                   0, 0, 1, 0,
            //                                   newTransformMatrix.M41, newTransformMatrix.M42, 0, 1);


            //Debug.WriteLine($"{adjustedMatrix.M11} {adjustedMatrix.M12} {adjustedMatrix.M21} {adjustedMatrix.M22}");

            _transformMatrix = newTransformMatrix;
            D3dIsDirty = true;
        }
        #endregion

        #region Public Methods

        #endregion
    }
}
