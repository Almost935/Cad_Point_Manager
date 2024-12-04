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
        private const float _scaleFactor = 1.25f;
        private const float _zoomSpeed = 0.1f;
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
        private Camera _camera = new(_zoomSpeed, _rotationSpeed);
        private bool _isShiftPressed = false;
        private Vector2 _lastPanPosition;
        private Vector2 _lastRotatePosition;

        #endregion

        #region Properties
        public bool DxfIsDirty { get; set; } = true;
        public bool D3dIsDirty { get; set; } = true;
        public bool ShadersLoaded { get; set; } = false;
        public bool ConstantBufferInitialized { get; set; } = false;
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
            // Update transformation matrix
            var transformation = _worldMatrix * _viewMatrix * _projectionMatrix;

            var transformationBuffer = new TransformationBuffer
            {
                WorldViewProjection = transformation
            };

            // Update the constant buffer with the new matrix
            _d3dResCache.DeviceContext.UpdateSubresource(ref transformationBuffer, _transformationBuffer);
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

            if (_isPanning && e.MiddleButton == MouseButtonState.Pressed)
            {
                var delta = MathHelpers.ScreenToNDC(_pointerCoords - _previousMousePosition, ActualWidth, ActualHeight);

                _panOffset += delta * (1.0f / _currentZoom);
                _viewMatrix = Matrix.Translation(_panOffset.X, -_panOffset.Y, 0);

                _previousMousePosition = _pointerCoords;

                D3dIsDirty = true;

                e.Handled = true;
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            //float zoomFactor = (e.Delta > 0) ? 1.25f : 0.8f;
            //_currentZoom *= zoomFactor;

            //var pos = e.GetPosition(this);
            ////_projectionMatrix = Matrix.Scaling(_currentZoom, _currentZoom, 1);
            //var ndcX = (float)(pos.X / ActualWidth * 2 - 1);
            //var ndcY = (float)(1 - pos.Y / ActualHeight * 2);

            //var ndcPoint = MathHelpers.ScreenToNDC(_pointerCoords, ActualWidth, ActualHeight);
            //_projectionMatrix = MathHelpers.ScaleToPoint(_projectionMatrix, zoomFactor, zoomFactor, 1, new Vector3(ndcX, ndcY, 0));
            ////_projectionMatrix = MathHelpers.ScaleToPoint(_projectionMatrix, zoomFactor, zoomFactor, 1, new Vector3(ndcPoint.X, ndcPoint.Y, 0));

            _camera.ZoomCamera(e.Delta);

            D3dIsDirty = true;

            e.Handled = true;
        }

        //Camera based methods
        public void HandleInput(Camera camera, Matrix viewProjectionMatrix)
        {
            _isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

            var pos = Mouse.GetPosition(this);
            var currentMousePosition = new Vector2((float)pos.X, (float)pos.Y);

            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                if (_isShiftPressed)
                {
                    RotateCamera(currentMousePosition - _lastMousePosition, camera);
                }
                else
                {
                    // Use the new panning function
                    PanCamera(currentMousePosition, camera, viewProjectionMatrix, Matrix.Invert(viewProjectionMatrix));
                }
            }

            if (mouseState.ScrollDelta != 0)
            {
                ZoomCamera(mouseState.ScrollDelta, camera);
            }

            //_lastMousePosition = currentMousePosition;
        }


        

        private Vector2 ScreenToNDC(Vector2 screenPos, float screenWidth, float screenHeight)
        {
            return new Vector2(
                (screenPos.X / screenWidth) * 2.0f - 1.0f, // Map x from [0, screenWidth] to [-1, 1]
                1.0f - (screenPos.Y / screenHeight) * 2.0f  // Map y from [0, screenHeight] to [1, -1]
            );
        }

       
        #endregion

        #region Public Methods

        #endregion
    }
}
