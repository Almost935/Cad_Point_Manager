using Cad_Point_Manager.Helpers;
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
        private const float _rotationSpeed = 0.005f;

        private Buffer _vertexBuffer;
        private Buffer _transformationBuffer;
        private Vertex[] _vertices;
        private VertexShader _vertexShader;
        private PixelShader _pixelShader;
        private InputLayout _inputLayout;
        private Point _pointerCoords;

        //Panning and Zooming Fields
        private Matrix _viewMatrix = Matrix.Identity;
        //private Matrix _projectionMatrix = Matrix.OrthoLH(2, 2, 0.1f, 1000f);
        private Matrix _projectionMatrix = Matrix.Identity;
        private Matrix _worldMatrix = Matrix.Identity;

        private Point _previousMousePosition;
        private bool _isPanning = false;

        private float _currentZoom = 1.0f;
        private Vector2 _panOffset = new(0, 0);

        //Camera based fields
        private Camera _camera;
        private TestCamera _testCamera;
        private bool _isShiftPressed = false;
        private Vector2 _prevMousePos;

        #endregion

        #region Properties
        public bool DxfIsDirty { get; set; } = true;
        public bool D3dIsDirty { get; set; } = true;
        public bool ShadersLoaded { get; set; } = false;
        public bool ConstantBufferInitialized { get; set; } = false;
        public Bounds DxfBounds { get; set; } = Bounds.Empty;
        #endregion

        #region Constructors
        public D3dDxfControl() { }
        #endregion

        #region Methods
        public override void Render()
        {
            if (_d3dResCache is null) { return; }

            if (_camera is null)
            {
                _camera = new(_rotationSpeed, (float)ActualWidth, (float)ActualHeight)
                {
                    Position = new Vector3(0, 0, 1), // Position the camera above the 2D plane, looking down
                    Target = new Vector3(0, 0, 0),     // Look at the origin
                    Up = Vector3.UnitY                 // Up direction is the Y-axis
                };
                _camera.UpdateView(); // Update the view matrix to ensure all matrices are current
                _camera.SetOrthographic(); // Set the orthographic projection
            }
            if (_testCamera is null)
            {
                GetDxfBounds();
                _testCamera = new((float)ActualWidth, (float)ActualHeight, DxfBounds);
            }
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

            UpdateConstantBuffer();

            // Set shaders
            context.VertexShader.Set(_vertexShader);
            context.PixelShader.Set(_pixelShader);
            context.InputAssembler.InputLayout = _inputLayout;
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

            //_vertices = new Vertex[numLines * 2];
            //float factor = 2f / numLines;
            //float redStart = 0;
            //float blueStart = 1;

            //for (int i = 0; i < numLines; i++)
            //{
            //    float x = 0 + factor * i;

            //    Vertex startVertex = new(new Vector3(-x, 2, 0f), new Vector4((redStart + factor * i), 0f, (blueStart - factor * i), 1f));
            //    Vertex endVertex = new(new Vector3(-x, 0, 0f), new Vector4((redStart + factor * i), 0f, (blueStart - factor * i), 1f));
            //    _vertices[i * 2] = startVertex;
            //    _vertices[i * 2 + 1] = endVertex;
            //    int z = 0;
            //}

            _vertices = new Vertex[numLines * 2 + 8];
            float factor = (float)ActualWidth / numLines;
            float blueStart = 1;

            for (int i = 0; i < numLines; i++)
            {
                float x = 0 + factor * i;
                float colorFactor = ((float)i / (float)numLines);

                Vertex startVertex = new(new Vector3(x, (float)ActualHeight, 0), new Vector4(colorFactor, 0f, (blueStart - colorFactor), 1f));
                Vertex endVertex = new(new Vector3(x, 0, 0), new Vector4(colorFactor, 0f, (blueStart - colorFactor), 1f));
                _vertices[i * 2] = startVertex;
                _vertices[i * 2 + 1] = endVertex;
            }

            // Add center lines to check zooming
            Vertex verticalStart = new(new Vector3(((float)ActualWidth * 0.5f), (float)ActualHeight, 0), new Vector4(0, 1, 0, 1));
            Vertex verticalEnd = new(new Vector3(((float)ActualWidth * 0.5f), 0, 0), new Vector4(0, 1, 0, 1));
            Vertex horizontalStart = new(new Vector3((float)ActualWidth, ((float)ActualHeight * 0.5f), 0), new Vector4(0, 1, 0, 1));
            Vertex horizontalEnd = new(new Vector3(0, ((float)ActualHeight * 0.5f), 0), new Vector4(0, 1, 0, 1));
            _vertices[numLines * 2] = verticalStart;
            _vertices[numLines * 2 + 1] = verticalEnd;
            _vertices[numLines * 2 + 2] = horizontalStart;
            _vertices[numLines * 2 + 3] = horizontalEnd;

            // Add lines at zero
            Vertex zeroVerticalStart = new(new Vector3(0, (float)ActualHeight, 0), new Vector4(0, 1, 0, 1));
            Vertex zeroVerticalEnd = new(new Vector3(0, 0, 0), new Vector4(0, 1, 0, 1));
            Vertex zeroHorizontalStart = new(new Vector3((float)ActualWidth, 0, 0), new Vector4(0, 1, 0, 1));
            Vertex zeroHorizontalEnd = new(new Vector3(0, 0, 0), new Vector4(0, 1, 0, 1));
            _vertices[numLines * 2 + 4] = zeroVerticalStart;
            _vertices[numLines * 2 + 5] = zeroVerticalEnd;
            _vertices[numLines * 2 + 6] = zeroHorizontalStart;
            _vertices[numLines * 2 + 7] = zeroHorizontalEnd;

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

        //private void UpdateConstantBuffer()
        //{
        //    // Update transformation matrix
        //    var transformation = _worldMatrix * _viewMatrix * _projectionMatrix;

        //    var transformationBuffer = new TransformationBuffer
        //    {
        //        WorldViewProjection = transformation
        //    };

        //    // Update the constant buffer with the new matrix
        //    _d3dResCache.DeviceContext.UpdateSubresource(ref transformationBuffer, _transformationBuffer);
        //}
        private void UpdateConstantBuffer()
        {
            // Update transformation matrix
            //var transformation = _camera.ViewMatrix * _camera.ProjectionMatrix;
            //var testTransformation = _camera.ViewMatrix * _camera.ProjectionMatrix;
            var transformation = _testCamera.ViewMatrix * _testCamera.ProjectionMatrix;

            var transformationBuffer = new TransformationBuffer
            {
                WorldViewProjection = transformation
            };

            // Update the constant buffer with the new matrix
            _d3dResCache.DeviceContext.UpdateSubresource(ref transformationBuffer, _transformationBuffer);
        }

        private void GetDxfBounds()
        {
            DxfBounds = new(0, (float)ActualWidth, 0, (float)ActualHeight);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                _isPanning = true;
                _previousMousePosition = e.GetPosition(this);
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Released)
            {
                _isPanning = false;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            _pointerCoords = e.GetPosition(this);
            var currentMousePos = new Vector2((float)_pointerCoords.X, (float)_pointerCoords.Y);

            //if (_isPanning && e.MiddleButton == MouseButtonState.Pressed)
            //{
            //    var delta = MathHelpers.ScreenToNDC(_pointerCoords - _previousMousePosition, ActualWidth, ActualHeight);

            //    _panOffset += delta * (1.0f / _currentZoom);
            //    _viewMatrix = Matrix.Translation(_panOffset.X, -_panOffset.Y, 0);

            //    _previousMousePosition = _pointerCoords;

            //    D3dIsDirty = true;

            //    e.Handled = true;
            //}

            _isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                var delta = currentMousePos - _prevMousePos;
                if (_isShiftPressed)
                {
                    _camera.RotateCamera(delta);
                } 
                else
                {
                    //_camera.PanCamera(currentMousePos, _prevMousePos, _viewMatrix * _projectionMatrix, Matrix.Invert(_viewMatrix * _projectionMatrix));
                    //_testCamera.Pan((float)delta.X, (float)delta.Y);
                    _testCamera.Pan(currentMousePos, _prevMousePos);
                }

                D3dIsDirty = true;
                e.Handled = true;
            }

            _prevMousePos = currentMousePos;
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            int zoomSteps;
            if (e.Delta > 0)
            {
                zoomSteps = 1;
            }
            else
            {
                zoomSteps = -1;
            }

            //_camera.ZoomCamera(zoom, new Vector2((float)_pointerCoords.X, (float)_pointerCoords.Y), (float)ActualWidth, (float)ActualHeight);
            _testCamera.Zoom(zoomSteps, new Vector2((float)_pointerCoords.X, (float)_pointerCoords.Y));

            D3dIsDirty = true;

            e.Handled = true;
        }
        #endregion
    }
}
