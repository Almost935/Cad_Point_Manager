using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.DrawingObjects3D;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using System.ComponentModel;
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
        
        private float _width;
        private float _height;

        private Buffer _vertexBuffer;
        private Buffer _transformationBuffer;
        private Vertex[] _vertices = [];
        private VertexShader _vertexShader;
        private PixelShader _pixelShader;
        private InputLayout _inputLayout;
        private Point _pointerCoords;

        //Panning and Zooming Fields
        private Matrix _viewMatrix = Matrix.Identity;
        private Matrix _projectionMatrix = Matrix.Identity;
        private Matrix _worldMatrix = Matrix.Identity;

        private Point _previousMousePosition;
        private bool _isPanning = false;

        private float _currentZoom = 1.0f;
        private Vector2 _panOffset = new(0, 0);

        //Camera based fields
        private Camera _camera;
        private bool _isShiftPressed = false;
        private Vector2 _prevMousePos;

        // Fields for testing
        private (int startIndex, int endIndex) _mouseLineIndices;
        #endregion

        #region Properties
        public bool DxfInitialized { get; set; } = false;
        public bool DxfIsDirty { get; set; } = false;
        public bool D3dIsDirty { get; set; } = true;
        public bool ShadersLoaded { get; set; } = false;
        public bool ConstantBufferInitialized { get; set; } = false;
        public Bounds DxfBounds { get; set; } = Bounds.Empty;
        #endregion

        #region Dependency Properties
        public CadManager3D CadManager3D
        {
            get { return (CadManager3D)GetValue(CadManager3DProperty); }
            set { SetValue(CadManager3DProperty, value); }
        }

        public static readonly DependencyProperty CadManager3DProperty =
        DependencyProperty.Register(
            nameof(CadManager3D),
            typeof(CadManager3D),
            typeof(D3dDxfControl),
            new PropertyMetadata(null, OnCadManager3DChanged));
        #endregion

        #region Constructors 
        public D3dDxfControl() { }
        #endregion

        #region Methods
        public override void Render()
        {
            if (_d3dResCache is null) { return; }

            if (DxfIsDirty) 
            { 
                GetDxfGeometries();
                GetDxfBounds();
                _camera.UpdateBounds(DxfBounds);
            }
            if (_camera is null)
            {
                _width = (float)ActualWidth;
                _height = (float)ActualHeight;
                GetDxfBounds();

                _camera = new(_width, _height, DxfBounds);
            }
            if (!ShadersLoaded) { InitializeShaders(); }
            if (!ConstantBufferInitialized) { InitializeConstantBuffer(); }
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

        private void GetDxfGeometries()
        {
            if (_d3dResCache is null) { return; }

            List<Vertex> vertices = [];
            foreach (var layer in CadManager3D.LayerManager.Layers.Values)
            {
                foreach (var drawingObject3D in layer.DrawingObject3Ds)
                {
                    if (drawingObject3D is DrawingLine3D line)
                    {
                        vertices.Add(line.StartVertex);
                        vertices.Add(line.EndVertex);
                    }
                    if (drawingObject3D is DrawingArc3D arc)
                    {
                        foreach (var vertex in arc.IntermediateVertices)
                        {
                            vertices.Add(vertex);
                        }
                    }
                    if (drawingObject3D is DrawingPolyline3D polyline)
                    {
                        foreach (var drawingObj in polyline.DrawingObject3Ds)
                        {
                            if (drawingObject3D is DrawingLine3D line2)
                            {
                                vertices.Add(line2.StartVertex);
                                vertices.Add(line2.EndVertex);
                            }
                            if (drawingObject3D is DrawingArc3D arc2)
                            {
                                foreach (var vertex in arc2.IntermediateVertices)
                                {
                                    vertices.Add(vertex);
                                }
                            }
                        }
                    }
                }
            }

            // Add center lines to check zooming
            Vertex verticalStart = new(new Vector3((_width * 0.5f), _height, 0), new Vector4(0, 1, 0, 1));
            Vertex verticalEnd = new(new Vector3((_width * 0.5f), 0, 0), new Vector4(0, 1, 0, 1));
            Vertex horizontalStart = new(new Vector3(_width, (_height * 0.5f), 0), new Vector4(0, 1, 0, 1));
            Vertex horizontalEnd = new(new Vector3(0, (_height * 0.5f), 0), new Vector4(0, 1, 0, 1));
            vertices.Add(verticalStart);
            vertices.Add(verticalEnd);
            vertices.Add(horizontalStart);
            vertices.Add(horizontalEnd);

            _vertices = vertices.ToArray();

            _vertexBuffer = Buffer.Create(
                _d3dResCache.Device,
                BindFlags.VertexBuffer,
                _vertices
            );

            DxfInitialized = true;
            DxfIsDirty = false;
            D3dIsDirty = true;
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
            var transformation = _camera.ViewMatrix * _camera.ProjectionMatrix;

            var transformationBuffer = new TransformationBuffer
            {
                WorldViewProjection = transformation
            };

            // Update the constant buffer with the new matrix
            _d3dResCache.DeviceContext.UpdateSubresource(ref transformationBuffer, _transformationBuffer);
        }

        private void GetDxfBounds()
        {
            if (!DxfInitialized)
            {
                DxfBounds = new(_width, 0, 0, _height);
            }
            else
            {
                float centerX = (float)(CadManager3D.Extents.Left + CadManager3D.Extents.Right) * 0.5f;
                float centerY = (float)(CadManager3D.Extents.Bottom + CadManager3D.Extents.Top) * 0.5f;

                DxfBounds = new(centerX + _width / 2, centerX - _width / 2, centerY - _height / 2, centerY + _height / 2);
                //DxfBounds = new((float)CadManager3D.Extents.Right, (float)CadManager3D.Extents.Left, (float)CadManager3D.Extents.Top, (float)CadManager3D.Extents.Bottom);

                //DxfBounds = new(-1000 + _width, -1000, 5000, 5000 + _height);
            }
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

            _isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                if (_isShiftPressed)
                {

                }
                else
                {
                    _camera.Pan(currentMousePos, _prevMousePos);
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

            _camera.Zoom(zoomSteps, new Vector2((float)_pointerCoords.X, (float)_pointerCoords.Y));

            D3dIsDirty = true;

            e.Handled = true;
        }

        private static void OnCadManager3DChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not D3dDxfControl control) { return; }

            if (e.OldValue is CadManager3D oldCadManager3D)
            {
                oldCadManager3D.PropertyChanged -= control.CadManager3D_PropertyChanged;
            }

            if (e.NewValue is CadManager3D newCadManager3D)
            {
                newCadManager3D.PropertyChanged += control.CadManager3D_PropertyChanged;
            }
        }

        private void CadManager3D_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CadManager3D.DxfDirty) && CadManager3D.DxfDirty) { DxfIsDirty = true; }
        }
        #endregion
    }
}
