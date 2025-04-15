using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.DrawingObjects3D;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Brush = SharpDX.Direct2D1.Brush;
using Buffer = SharpDX.Direct3D11.Buffer;
using InputElement = SharpDX.Direct3D11.InputElement;
using Matrix = SharpDX.Matrix;
using Point = System.Windows.Point;
using RectangleGeometry = System.Windows.Media.RectangleGeometry;
using SolidColorBrush = SharpDX.Direct2D1.SolidColorBrush;

namespace Cad_Point_Manager.Controls.D3DControl
{
    public class D3dDxfControl : Direct3DControl, INotifyPropertyChanged, IDisposable
    {
        #region Fields
        private const float _rotationSpeed = 0.005f;
        private const float _zoomFactor = 1.3f;

        private Buffer _transformationBuffer;
        private Point _pointerCoords;
        private Matrix _dxfInitialMatrix = Matrix.Identity;
        private bool _clipSet = false;
        private bool _isMouseInside;

        // Line shader related fields
        private Buffer _lineVertexBuffer;
        private VertexShader _lineVertexShader;
        private PixelShader _linePixelShader;
        private InputLayout _lineInputLayout;
        private LineVertex[] _lineVertices = [];
        private bool _lineShaderLoaded = false;

        // Text shader related fields
        private Buffer _textVertexBuffer;
        private VertexShader _textVertexShader;
        private PixelShader _textPixelShader;
        private InputLayout _textInputLayout;
        private bool _textShaderLoaded = false;
        private TextVertex[] _textVertices = [];

        //// Text glow shader related fields
        //private Buffer _textGlowVertexBuffer;
        //private VertexShader _textGlowVertexShader;
        //private PixelShader _textGlowPixelShader;
        //private InputLayout _textGlowInputLayout;
        //private bool _textGlowShaderLoaded = false;
        //private TextVertex[] _textGlowVertices = [];

        // Panning and Zooming Fields
        private float _panThreshold = 1.0f;
        private Point _previousMousePosition;
        private bool _isPanning = false;

        // Camera based fields
        private Camera _camera;
        private bool _isShiftPressed = false;
        private Vector2 _prevMousePos;

        private Vector2 _dxfCoords = new();
        private string _dxfCoordsString = $"X: {0:F3}   Y: {0:F3}";

        // Hit Testing Fields
        private Task _hittestTask;
        private bool _hitTestIsRunning = false;
        private float _hittestStrokeThickness;
        private Point _lastHitTestCoords = new();
        private CancellationTokenSource _hitTestCancellationTokenSource;

        private List<(double distance, DrawingObject3D obj)> _nearestDrawingObjects = [];

        // Interactive features fields
        private DrawingObject3D _snappedObject;
        private DrawingObject3D _highlightedObject;

        // Direct2D Fields
        private Brush _highlightedBrush;
        private Brush _highlightedOuterEdgeBrush;
        private StrokeStyle1 _interactiveObjectStrokeStyle;
        private bool _d2dInitialized = false;
        private RawMatrix3x2 _d2dMatrix = new();
        private Dictionary<(float r, float g, float b, float a), Brush> _brushes = [];
        #endregion

        #region Properties 
        /// <summary>
        /// Determines if the DXF file needs to be reloaded to the vertices list.
        /// </summary>
        public bool DxfIsDirty { get; set; } = true;

        /// <summary>
        /// Determines if the vertex buffer needs to be reloaded.
        /// </summary>
        public bool VertexBufferDirty { get; set; } = true;

        /// <summary>
        /// Determines if the view matrix needs to be reloaded. Occurs when the Dxf file is changed.
        /// </summary>
        private bool DxfNeedsReload { get; set; } = false;

        /// <summary>
        /// Determines if the Direct3D control needs to be redrawn. Occurs when the camera is panned or zoomed.
        /// </summary>
        public bool D3dIsDirty { get; set; } = true;
        public bool ConstantBufferInitialized { get; set; } = false;
        public ViewportF Viewport { get; set; }
        public SnapMode CurrentSnapMode { get; set; } = SnapMode.Object;

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

        public enum SnapMode
        {
            Point,
            Object
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

        #region Functions
        public readonly Func<Vector2, string> formatVectorString = (vector) => $"X: {vector.X:F3}   Y: {vector.Y:F3}";
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
                UpdateLineVertices();
                UpdateTextVertices();
            }
            if (VertexBufferDirty) { SetVertexBuffers(); }
            if (_camera is null)
            {
                GetInitialMatrix();
                _camera = new(Viewport, _zoomFactor);
            }
            if (DxfNeedsReload)
            {
                GetInitialMatrix();
                _camera.ResetView(_dxfInitialMatrix);
                DxfNeedsReload = false;
                CadManager3D.DxfNeedsReload = false;
            }
            if (!_clipSet) { SetClip(); _clipSet = true; }
            if (!_lineShaderLoaded) { InitializeLineShader(); }
            if (!_textShaderLoaded) { InitializeTextShader(); }
            if (!ConstantBufferInitialized) { InitializeConstantBuffer(); }
            if (!_d2dInitialized) { InitializeDirect2D(); }
            if (D3dIsDirty) { DrawDxf(); }
            if (!_hitTestIsRunning)
            {
                _hitTestIsRunning = true;
                _hittestTask = Task.Run(() => RunHitTestingAsync());
            }
        }

        private void DrawDxf()
        {
            var context = _d3dResCache.DeviceContext;

            // Set render target and clear it
            context.OutputMerger.SetRenderTargets(_d3dResCache.RenderTargetView);
            context.ClearRenderTargetView(_d3dResCache.RenderTargetView, new RawColor4(1, 1, 1, 1));

            DrawInteractiveObjects();
            UpdateConstantBuffer();

            DrawLinesWithShader();
            DrawTextWithShader();
            //DrawTextGlowWithShader();

            D3dIsDirty = false;
        }

        private void DrawLinesWithShader()
        {
            var context = _d3dResCache.DeviceContext;

            if (_lineVertexBuffer is null) { return; }

            // Set shaders
            context.VertexShader.Set(_lineVertexShader);
            context.PixelShader.Set(_linePixelShader);
            context.InputAssembler.InputLayout = _lineInputLayout;
            context.VertexShader.SetConstantBuffer(0, _transformationBuffer);

            // Bind vertex buffer and draw
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_lineVertexBuffer, Utilities.SizeOf<LineVertex>(), 0));
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;

            context.Draw(_lineVertices.Length, 0);
        }
        private void DrawTextWithShader()
        {
            var context = _d3dResCache.DeviceContext;

            if (_textVertexBuffer is null) { return; }

            context.VertexShader.Set(_textVertexShader);
            context.PixelShader.Set(_textPixelShader);
            context.InputAssembler.InputLayout = _textInputLayout;
            context.VertexShader.SetConstantBuffer(0, _transformationBuffer);

            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_textVertexBuffer, Marshal.SizeOf<TextVertex>(), 0));

            context.Draw(_textVertices.Length, 0);
        }
        //private void DrawTextGlowWithShader()
        //{
        //    var context = _d3dResCache.DeviceContext;

        //    if (_textVertexBuffer is null) { return; }

        //    context.VertexShader.Set(_textGlowVertexShader);
        //    context.PixelShader.Set(_textGlowPixelShader);
        //    context.InputAssembler.InputLayout = _textGlowInputLayout;
        //    context.VertexShader.SetConstantBuffer(0, _transformationBuffer);

        //    context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
        //    context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_textGlowVertexBuffer, Marshal.SizeOf<TextVertex>(), 0));

        //    context.Draw(_textGlowVertices.Length, 0);
        //}

        private void DrawInteractiveObjects()
        {
            if (CadManager3D is null || CadManager3D.DrawingObjectTree3D is null || _camera is null) { return; }

            _d3dResCache.D2DDeviceContext.Transform = _d2dMatrix;

            if (_snappedObject is not null)
            {
                var copy = _snappedObject;
                if (copy is DrawingSegment3D segment)
                {
                    Brush innerBrush = new SolidColorBrush(_d3dResCache.D2DDeviceContext, new RawColor4(segment.Color.X, segment.Color.Y, segment.Color.Z, segment.Color.W));
                    //SharpDX.Direct2D1.Brush outerBrush = new SharpDX.Direct2D1.SolidColorBrush(_d3dResCache.D2DDeviceContext, new RawColor4(segment.Color.X, segment.Color.Y, segment.Color.Z, 0.3f));
                    Brush outerBrush = new SolidColorBrush(_d3dResCache.D2DDeviceContext, new RawColor4(0, 0, 0, 0.25f));

                    segment.DrawToD2dDeviceContext(_d3dResCache.D2DDeviceContext, _d3dResCache.D2dFactory, outerBrush, 5, _interactiveObjectStrokeStyle);
                    segment.DrawToD2dDeviceContext(_d3dResCache.D2DDeviceContext, _d3dResCache.D2dFactory, innerBrush, 1.5f, _interactiveObjectStrokeStyle);

                    innerBrush.Dispose();
                    outerBrush.Dispose();
                }

                if (copy is DrawingPolyline3D polyline)
                {
                    Brush innerBrush = new SolidColorBrush(_d3dResCache.D2DDeviceContext, new RawColor4(polyline.Color.X, polyline.Color.Y, polyline.Color.Z, polyline.Color.W));
                    //SharpDX.Direct2D1.Brush outerBrush = new SharpDX.Direct2D1.SolidColorBrush(_d3dResCache.D2DDeviceContext, new RawColor4(polyline.Color.X, polyline.Color.Y, polyline.Color.Z, 0.3f));
                    Brush outerBrush = new SolidColorBrush(_d3dResCache.D2DDeviceContext, new RawColor4(0, 0, 0, 0.25f));

                    polyline.DrawToD2dDeviceContext(_d3dResCache.D2DDeviceContext, _d3dResCache.D2dFactory, outerBrush, 5, _interactiveObjectStrokeStyle);
                    polyline.DrawToD2dDeviceContext(_d3dResCache.D2DDeviceContext, _d3dResCache.D2dFactory, innerBrush, 1.5f, _interactiveObjectStrokeStyle);

                    innerBrush.Dispose();
                    outerBrush.Dispose();
                }

                if (copy is DrawingBlock3D block)
                {
                    foreach (var e in block.DrawingObjects)
                    {
                        Brush innerBrush = new SolidColorBrush(_d3dResCache.D2DDeviceContext, new RawColor4(e.Color.X, e.Color.Y, e.Color.Z, e.Color.W));
                        Brush outerBrush = new SolidColorBrush(_d3dResCache.D2DDeviceContext, new RawColor4(0, 0, 0, 0.25f));

                        e.DrawToD2dDeviceContext(_d3dResCache.D2DDeviceContext, _d3dResCache.D2dFactory, outerBrush, 5, _interactiveObjectStrokeStyle);
                        e.DrawToD2dDeviceContext(_d3dResCache.D2DDeviceContext, _d3dResCache.D2dFactory, innerBrush, 1.5f, _interactiveObjectStrokeStyle);

                        innerBrush.Dispose();
                        outerBrush.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Resets the _vertices field to the current list of vertices in the CadManager3D.
        /// </summary>
        private void UpdateLineVertices()
        {
            if (_d3dResCache is null) { return; }

            CadManager3D.UpdateLineVerticesList();

            _lineVertices = CadManager3D.LineVertices.ToArray();

            SetVertexBuffers();

            DxfIsDirty = false;
            D3dIsDirty = true;
        }

        /// <summary>
        /// Resets the _vertices field to the current list of vertices in the CadManager3D.
        /// </summary>
        private void UpdateTextVertices()
        {
            if (_d3dResCache is null) { return; }

            CadManager3D.UpdateTextVerticesList(_d3dResCache);

            _textVertices = CadManager3D.TextVertices.ToArray();

            SetVertexBuffers();

            DxfIsDirty = false;
            D3dIsDirty = true;
        }

        /// <summary>
        /// Sets the vertex buffer to the field _vertices. Does not update _vertices
        /// </summary>
        private void SetVertexBuffers()
        {
            if (_d3dResCache is null) { return; }

            _lineVertexBuffer?.Dispose();
            _textVertexBuffer?.Dispose();

            if (_lineVertices is not null && _lineVertices.Length > 0)
            {
                _lineVertexBuffer = Buffer.Create(
                _d3dResCache.Device,
                BindFlags.VertexBuffer,
                _lineVertices);
            }

            if (_textVertices.Length > 0 && _textVertices.Length > 0)
            {
                _textVertexBuffer = Buffer.Create(
                    _d3dResCache.Device,
                    BindFlags.VertexBuffer,
                    _textVertices);
            }

            //if (_textGlowVertices.Length > 0 && _textGlowVertices.Length > 0)
            //{
            //    // Create the vertex buffer
            //    _textGlowVertexBuffer = Buffer.Create(
            //        _d3dResCache.Device,
            //        BindFlags.VertexBuffer,
            //        _textGlowVertices);
            //}

            VertexBufferDirty = false;
        }

        private void InitializeDirect2D()
        {
            _highlightedBrush?.Dispose();
            _highlightedOuterEdgeBrush?.Dispose();
            _interactiveObjectStrokeStyle?.Dispose();

            _highlightedBrush = new SolidColorBrush(_d3dResCache.D2DDeviceContext, new RawColor4((97 / 255), 1.0f, 0.0f, 1.0f));
            _highlightedOuterEdgeBrush = new SolidColorBrush(_d3dResCache.D2DDeviceContext, new RawColor4((97 / 255), 1.0f, 0.0f, 1.0f))
            { Opacity = 0.2f };

            StrokeStyleProperties1 props = new()
            {
                StartCap = CapStyle.Round,
                EndCap = CapStyle.Round,
                DashCap = CapStyle.Round,
                LineJoin = LineJoin.Miter,
                MiterLimit = 10,
                DashStyle = SharpDX.Direct2D1.DashStyle.Solid,
                DashOffset = 0,
                TransformType = StrokeTransformType.Fixed
            };
            _interactiveObjectStrokeStyle = new(_d3dResCache.D2dFactory, props);
        }

        private void InitializeLineShader()
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
            string shadersPath = path + @"\Controls\D3DControl\Shaders\LineShader.hlsl";

            var lineVertexShaderByteCode = ShaderBytecode.CompileFromFile(shadersPath, "VSMain", "vs_4_0");
            _lineVertexShader = new VertexShader(_d3dResCache.Device, lineVertexShaderByteCode);

            var linePixelShaderByteCode = ShaderBytecode.CompileFromFile(shadersPath, "PSMain", "ps_4_0");
            _linePixelShader = new PixelShader(_d3dResCache.Device, linePixelShaderByteCode);

            _lineInputLayout = new InputLayout(
                _d3dResCache.Device,
                ShaderSignature.GetInputSignature(lineVertexShaderByteCode),
                [
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                    new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 12, 0),
                    new InputElement("ISVISIBLE", 0, Format.R32_Float, 28, 0)
                ]);

            _lineShaderLoaded = true;
        }
        private void InitializeTextShader()
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
            string textShadersPath = path + @"\Controls\D3DControl\Shaders\TextShader.hlsl";

            // Compile Vertex Shader
            var textVertexShaderByteCode = ShaderBytecode.CompileFromFile(textShadersPath, "VSMain", "vs_4_0");
            _textVertexShader = new VertexShader(_d3dResCache.Device, textVertexShaderByteCode);

            // Compile Pixel Shader
            var textPixelShaderByteCode = ShaderBytecode.CompileFromFile(textShadersPath, "PSMain", "ps_4_0");
            _textPixelShader = new PixelShader(_d3dResCache.Device, textPixelShaderByteCode);

            // Create Input Layout
            _textInputLayout = new InputLayout(
                _d3dResCache.Device,
                ShaderSignature.GetInputSignature(textVertexShaderByteCode),
                [
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                    new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 12, 0),
                    new InputElement("ISVISIBLE", 0, Format.R32_Float, 28, 0),
                    new InputElement("ISMOUSEOVER", 0, Format.R32_Float, 32, 0),
                    new InputElement("GLOWDIRECTION", 0, Format.R32G32_Float, 36, 0)
                ]);

            _textShaderLoaded = true;
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

        private void GetInitialMatrix()
        {
            if (!CadManager3D.DxfLoaded)
            {
                _dxfInitialMatrix = Matrix.Identity;
            }
            else
            {
                float centerX = (CadManager3D.Extents.Left + CadManager3D.Extents.Right) * 0.5f;
                float centerY = (CadManager3D.Extents.Bottom + CadManager3D.Extents.Top) * 0.5f;

                float scale = Math.Min(Viewport.Width / CadManager3D.Extents.Width, Viewport.Height / CadManager3D.Extents.Height);

                _dxfInitialMatrix = Matrix.Scaling(scale, scale, 1) * Matrix.Translation(-centerX, -centerY, 0);

                if (_camera is not null)
                {
                    _camera.ResetView(_dxfInitialMatrix);
                    UpdateD2dMatrix();
                }
            }
        }

        private void UpdateD2dMatrix()
        {
            if (_camera is null) { return; }

            var matrix = _camera.Get2DTransformationMatrix();
            _d2dMatrix = new(matrix.M11, matrix.M12, matrix.M21, -matrix.M22, (Viewport.Width / 2) - _camera.InverseViewProjectionMatrix.M41 * matrix.M11, _camera.InverseViewProjectionMatrix.M42 * matrix.M22 + (Viewport.Height / 2));
            _hittestStrokeThickness = 7.0f / _d2dMatrix.M11;
        }

        private void UpdateDxfCoords(Vector2 mousePos)
        {
            DxfCoords = _camera.ScreenToWorld(mousePos);
            DxfCoordsString = formatVectorString(DxfCoords);
        }


        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                _isPanning = true;

                ResetSnappedObjects();

                VertexBufferDirty = true;
                D3dIsDirty = true;

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
                        UpdateD2dMatrix();
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
            UpdateD2dMatrix();

            D3dIsDirty = true;
            e.Handled = true;
        }
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            _isMouseInside = false;
            _hitTestCancellationTokenSource.Cancel();
            _isPanning = false;

            ResetSnappedObjects();
            VertexBufferDirty = true;
            D3dIsDirty = true;
        }
        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);

            _isMouseInside = true;
            _hitTestCancellationTokenSource = new CancellationTokenSource();
            _ = RunHitTestingAsync();
        }
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            Viewport = new(0, 0, (float)ActualWidth, (float)ActualHeight);
            CadManager3D.ViewportSize = new((float)ActualWidth, (float)ActualHeight);

            GetInitialMatrix();

            if (_camera is not null)
            {
                _camera.UpdateViewportSize(Viewport);
            }

            D3dIsDirty = true;
        }

        public void ZoomToExtents()
        {
            if (_camera is null) { return; }

            _camera.ResetView(_dxfInitialMatrix);
            ResetSnappedObjects();
            D3dIsDirty = true;
        }

        public async Task RunHitTestingAsync()
        {
            while (_isMouseInside)
            {
                if (_hitTestCancellationTokenSource.Token.IsCancellationRequested)
                {
                    break;
                }

                // Perform hit testing logic here
                RunObjectHitTest(_hitTestCancellationTokenSource.Token);
                await Task.Delay(50); // Adjust the delay as needed
            }
        }
        private void RunObjectHitTest(CancellationToken token)
        {
            // Check for cancellation
            if (token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();
            }

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => RunObjectHitTest(token));

                return;
            }

            if (!CadManager3D.DxfLoaded) { return; }

            _lastHitTestCoords = new(DxfCoords.X, DxfCoords.Y);

            bool vertexBufferDirty = false;
            bool d3dIsDirty = false;
            Rect rect = new(_lastHitTestCoords.X - _hittestStrokeThickness, _lastHitTestCoords.Y - _hittestStrokeThickness, _hittestStrokeThickness * 2, _hittestStrokeThickness * 2);
            var snappedCopy = _snappedObject;

            if (snappedCopy is not null)
            {
                if (snappedCopy.DistanceToPoint(_lastHitTestCoords) > _hittestStrokeThickness)
                {
                    DeselectObject(snappedCopy);
                    _snappedObject = null;

                    _nearestDrawingObjects = CadManager3D.GetNearestDrawingObjects(_lastHitTestCoords, _hittestStrokeThickness);

                    if (_nearestDrawingObjects.Count > 0)
                    {
                        var (distance, obj) = _nearestDrawingObjects.First();

                        if (distance <= _hittestStrokeThickness && obj is not DrawingText3D)
                        {
                            _snappedObject = obj;
                            SelectObject(_snappedObject);
                        }
                    }

                    vertexBufferDirty = true;
                    d3dIsDirty = true;
                }
                else
                {
                    return;
                }
            }
            else
            {
                _nearestDrawingObjects = CadManager3D.GetNearestDrawingObjects(_lastHitTestCoords, _hittestStrokeThickness);

                if (_nearestDrawingObjects.Count > 0)
                {
                    var (distance, obj) = _nearestDrawingObjects.First();

                    if (distance <= _hittestStrokeThickness && obj is not DrawingText3D)
                    {
                        _snappedObject = obj;
                        SelectObject(_snappedObject);

                        vertexBufferDirty = true;
                        d3dIsDirty = true;
                    }
                }
            }

            VertexBufferDirty = vertexBufferDirty;
            D3dIsDirty = d3dIsDirty;
        }
        public void CancelHitTesting()
        {
            _hitTestCancellationTokenSource?.Cancel();
        }

        private void SetClip()
        {
            var parent = VisualTreeHelper.GetParent(this);
            while (parent is not null)
            {
                if (parent is Border border)
                {
                    this.Clip = new RectangleGeometry(new Rect(0, 0, border.ActualWidth, border.ActualHeight),
                        border.CornerRadius.TopRight, border.CornerRadius.TopRight);
                    _clipSet = true;
                    break;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
        }

        private void SelectObject(DrawingObject3D obj)
        {
            if (obj is not null)
            {
                if (obj is DrawingGeometry3D geometry)
                {
                    geometry.MouseEnter();

                    int count = 0;
                    for (int i = geometry.StartVertexIndex; i <= geometry.EndVertexIndex; i++)
                    {
                        _lineVertices[i] = geometry.Vertices[count];
                        count++;
                    }
                }
                if (obj is DrawingBlock3D block3D)
                {
                    block3D.MouseEnter();

                    int count = 0;
                    for (int i = block3D.StartLineVertexIndex; i <= block3D.EndLineVertexIndex; i++)
                    {
                        _lineVertices[i] = block3D.GeometryVertices[count];
                        count++;
                    }

                    count = 0;
                    for (int i = block3D.StartTextVertexIndex; i <= block3D.EndTextVertexIndex; i++)
                    {
                        _textVertices[i] = block3D.TextVertices[count];
                        count++;
                    }
                }
                if (obj is DrawingMtext3D drawingMtext)
                {
                    drawingMtext.MouseEnter();

                    int count = 0;
                    for (int i = drawingMtext.StartVertexIndex; i <= drawingMtext.EndVertexIndex; i++)
                    {
                        _textVertices[i] = drawingMtext.TextVertices[count];
                        count++;
                    }
                }
            }
        }
        private void DeselectObject(DrawingObject3D obj)
        {
            if (obj is not null)
            {
                if (obj is DrawingGeometry3D geometry)
                {
                    geometry.MouseLeave();

                    int count = 0;
                    for (int i = geometry.StartVertexIndex; i <= geometry.EndVertexIndex; i++)
                    {
                        _lineVertices[i] = geometry.Vertices[count];
                        count++;
                    }
                }
                if (obj is DrawingBlock3D block3D)
                {
                    block3D.MouseLeave();

                    int count = 0;
                    for (int i = block3D.StartLineVertexIndex; i <= block3D.EndLineVertexIndex; i++)
                    {
                        _lineVertices[i] = block3D.GeometryVertices[count];
                        count++;
                    }

                    count = 0;
                    for (int i = block3D.StartTextVertexIndex; i <= block3D.EndTextVertexIndex; i++)
                    {
                        _textVertices[i] = block3D.TextVertices[count];
                        count++;
                    }
                }
                if (obj is DrawingMtext3D drawingMtext)
                {
                    drawingMtext.MouseLeave();

                    int count = 0;
                    for (int i = drawingMtext.StartVertexIndex; i <= drawingMtext.EndVertexIndex; i++)
                    {
                        _textVertices[i] = drawingMtext.TextVertices[count];
                        count++;
                    }
                }
            }
        }
        private void ResetSnappedObjects()
        {
            if (_snappedObject is not null)
            {
                DeselectObject(_snappedObject);
                _snappedObject = null;
            }
        }

        private void ClearDxf()
        {
            ResetSnappedObjects();
            VertexBufferDirty = true;
            D3dIsDirty = true;
        }

        private static void OnCadManager3DChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not D3dDxfControl control) { return; }

            if (e.OldValue is CadManager3D oldCadManager3D)
            {
                oldCadManager3D.PropertyChanged -= control.CadManager3D_PropertyChanged;
                oldCadManager3D.ZoomToExtentsRequested -= control.ZoomToExtents;
            }

            if (e.NewValue is CadManager3D newCadManager3D)
            {
                newCadManager3D.PropertyChanged += control.CadManager3D_PropertyChanged;
                newCadManager3D.ZoomToExtentsRequested += control.ZoomToExtents;
            }
        }

        private void CadManager3D_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            DxfIsDirty = CadManager3D.DxfDirty;
            DxfNeedsReload = CadManager3D.DxfNeedsReload;

            if (e.PropertyName == nameof(CadManager3D.DxfLoaded) && !CadManager3D.DxfLoaded)
            {
                ClearDxf();
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                    _transformationBuffer?.Dispose();
                    _lineVertexBuffer?.Dispose();
                    _lineVertexShader?.Dispose();
                    _linePixelShader?.Dispose();
                    _lineInputLayout?.Dispose();
                    _textVertexBuffer?.Dispose();
                    _textVertexShader?.Dispose();
                    _textPixelShader?.Dispose();
                    _textInputLayout?.Dispose();
                    _highlightedBrush?.Dispose();
                    _highlightedOuterEdgeBrush?.Dispose();
                    _interactiveObjectStrokeStyle?.Dispose();
                    if (_brushes != null)
                    {
                        foreach (var brush in _brushes.Values)
                        {
                            brush.Dispose();
                        }
                        _brushes.Clear();
                    }
                    _hitTestCancellationTokenSource?.Dispose();
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                // Set large fields to null
                _transformationBuffer = null;
                _lineVertexBuffer = null;
                _lineVertexShader = null;
                _linePixelShader = null;
                _lineInputLayout = null;
                _textVertexBuffer = null;
                _textVertexShader = null;
                _textPixelShader = null;
                _textInputLayout = null;
                _highlightedBrush = null;
                _highlightedOuterEdgeBrush = null;
                _interactiveObjectStrokeStyle = null;
                _brushes = null;
                _hitTestCancellationTokenSource = null;

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
