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
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

using Buffer = SharpDX.Direct3D11.Buffer;
using Point = System.Windows.Point;

namespace Cad_Point_Manager.Controls.D3DControl
{
    public class D3dDxfControl : Direct3DControl, INotifyPropertyChanged
    {
        #region Fields
        private const float _rotationSpeed = 0.005f;
        private const float _panThreshold = 1.0f;

        private float _width;
        private float _height;

        private Buffer _vertexBuffer;
        private Buffer _transformationBuffer;
        private Vertex[] _vertices = [];
        private VertexShader _vertexShader;
        private PixelShader _pixelShader;
        private InputLayout _inputLayout;
        private Point _pointerCoords;
        private bool _dxfInitialized = false;

        // Panning and Zooming Fields
        private Matrix _viewMatrix = Matrix.Identity;
        private Matrix _projectionMatrix = Matrix.Identity;

        private Point _previousMousePosition;
        private bool _isPanning = false;

        private float _currentZoom = 1.0f;
        private Vector2 _panOffset = new(0, 0);

        // Camera based fields
        private Camera _camera;
        private bool _isShiftPressed = false;
        private Vector2 _prevMousePos;

        private Vector2 _dxfCoords = new();
        private string _dxfCoordsString = $"X: {0:F3}   Y: {0:F3}";

        // Hittesting Fields
        private bool _isHitTesting = false;
        private float _hittestStrokeThickness;
        #endregion

        #region Properties
        public bool DxfInitialized { get; set; } = false;
        public bool DxfIsDirty { get; set; } = false;
        private bool DxfNeedsReload { get; set; } = false;
        public bool D3dIsDirty { get; set; } = true;
        public bool ShadersLoaded { get; set; } = false;
        public bool ConstantBufferInitialized { get; set; } = false;
        public Bounds DxfBounds { get; set; } = Bounds.Empty;

        public Vector2 DxfCoords
        {
            get => _dxfCoords;
            set
            {
                _dxfCoords = value;
                OnPropertyChanged();
            }
        }
        public string DxfCoordsString
        {
            get => _dxfCoordsString;
            set
            {
                _dxfCoordsString = value;
                OnPropertyChanged();
            }
        }
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

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Methods
        public override void Render()
        {
            if (_d3dResCache is null) { return; }

            if (DxfIsDirty)
            {
                GetDxfGeometries();
            }
            if (DxfNeedsReload)
            {
                GetDxfBounds();
                _camera.UpdateBounds(DxfBounds);
                DxfNeedsReload = false;
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

            _vertices = CadManager3D.Vertices.ToArray();

            if (_vertices is not null && _vertices.Length > 0)
            {
                _vertexBuffer = Buffer.Create(
                _d3dResCache.Device,
                BindFlags.VertexBuffer,
                _vertices);
            }

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
            var transformation = _camera.ViewProjectionMatrix;

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
                float centerX = (CadManager3D.Extents.Left + CadManager3D.Extents.Right) * 0.5f;
                float centerY = (CadManager3D.Extents.Bottom + CadManager3D.Extents.Top) * 0.5f;

                float left = centerX - _width * 0.5f;
                float right = centerX + _width * 0.5f;
                float bottom = centerY - _height * 0.5f;
                float top = centerY + _height * 0.5f;

                Bounds dxfBounds = new(left, right, bottom, top);
                float scale = Math.Min(_width / CadManager3D.Extents.Width, _height / CadManager3D.Extents.Height);
                float newWidth = dxfBounds.Width / scale;
                float newHeight = dxfBounds.Height / scale;
                DxfBounds = new(dxfBounds.Center.X - newWidth / 2, dxfBounds.Center.X + newWidth / 2, dxfBounds.Center.Y - newHeight / 2, dxfBounds.Center.Y + newHeight / 2);
            }
        }

        private Bounds ScaleBoundsToFit(Bounds dxfBounds)
        {
            float scaleX = _width / dxfBounds.Width;
            float scaleY = _height / dxfBounds.Height;
            float scale = Math.Min(scaleX, scaleY);

            return Bounds.ScaleToCenter(dxfBounds, scale);
        }

        private void UpdateDxfCoords(Vector2 mousePos)
        {
            var ndcCoords = Camera.ScreenToNDC(mousePos, _width, _height);
            var vector3MouseCoords = Camera.Unproject(ndcCoords, _camera.InverseViewProjectionMatrix);

            DxfCoords = new(vector3MouseCoords.X, vector3MouseCoords.Y);
            DxfCoordsString = $"X: {DxfCoords.X:F3}   Y: {DxfCoords.Y:F3}";
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

            if (Vector2.Distance(currentMousePos, _prevMousePos) > _panThreshold)
            {
                if (!_isPanning)
                {
                    //Task.Run(() => UpdateDxfCoords(currentMousePos));
                    UpdateDxfCoords(currentMousePos);

                    if (!_isHitTesting)
                    {
                        CadManager3D.HitTestPoint(new Point(DxfCoords.X, DxfCoords.Y));
                    }
                }

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
            DxfIsDirty = CadManager3D.DxfDirty;
            DxfNeedsReload = CadManager3D.DxfNeedsReload;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
