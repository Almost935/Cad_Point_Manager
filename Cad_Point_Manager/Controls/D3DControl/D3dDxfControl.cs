using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.DrawingObjects3D;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Windows;
using System.Windows.Input;

using Buffer = SharpDX.Direct3D11.Buffer;
using Matrix = SharpDX.Matrix;
using Point = System.Windows.Point;
using PathGeometry = SharpDX.Direct2D1.PathGeometry;

namespace Cad_Point_Manager.Controls.D3DControl
{
    public class D3dDxfControl : Direct3DControl, INotifyPropertyChanged, IDisposable
    {
        #region Fields
        private const float _rotationSpeed = 0.005f;
        private const float _zoomFactor = 1.3f;

        private Buffer _vertexBuffer;
        private Buffer _transformationBuffer;
        private Vertex[] _vertices = [];
        private VertexShader _vertexShader;
        private PixelShader _pixelShader;
        private InputLayout _inputLayout;
        private Point _pointerCoords;
        private bool _dxfInitialized = false;
        private Matrix _dxfInitialMatrix = Matrix.Identity;

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
        private float _hittestStrokeThickness = 2.0f;
        private Rect _hitTestRange = new(0, 0, 10, 10);
        private Point _lastHitTestCoords = new();
        private CancellationTokenSource _cancellationTokenSource;

        private List<(double distance, DrawingObject3D obj)> _nearestDrawingObjects = [];
        private double _hitTestSize = 20;

        // Interactive features fields
        private DrawingObject3D _snappedObject;
        private DrawingObject3D _highlightedObject;

        // Direct2D Fields
        private SharpDX.Direct2D1.Brush _highlightedBrush;
        private SharpDX.Direct2D1.Brush _highlightedOuterEdgeBrush;
        private SharpDX.Direct2D1.StrokeStyle1 _interactiveObjectStrokeStyle;
        private bool _d2dInitialized = false;
        private RawMatrix3x2 _d2dMatrix = new();
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
        public bool ShadersLoaded { get; set; } = false;
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

            if (DxfIsDirty) { UpdateDxfVertices(); }
            if (VertexBufferDirty) { SetVertexBuffer(); }
            if (DxfNeedsReload)
            {
                GetInitialMatrix();
                _camera.ResetView(_dxfInitialMatrix);
                DxfNeedsReload = false;
            }
            if (_camera is null)
            {
                GetInitialMatrix();
                _camera = new(Viewport, _zoomFactor);
            }
            if (!ShadersLoaded) { InitializeShaders(); }
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

            // Set shaders
            context.VertexShader.Set(_vertexShader);
            context.PixelShader.Set(_pixelShader);
            context.InputAssembler.InputLayout = _inputLayout;
            context.VertexShader.SetConstantBuffer(0, _transformationBuffer);

            // Bind vertex buffer and draw
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertexBuffer, Utilities.SizeOf<Vertex>(), 0));
            context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.LineList;

            int invisibleCount = 0;
            foreach (var vertex in _vertices)
            {
                if (vertex.IsVisible < 0.5f)
                {
                    invisibleCount++;
                }
            }
            context.Draw(_vertices.Length, 0);

            D3dIsDirty = false;
        }

        private void DrawInteractiveObjects()
        {
            // test
            if (CadManager3D is null || CadManager3D.DrawingObjectTree3D is null || _camera is null) { return; }

            _d3dResCache.D2DDeviceContext.Transform = _d2dMatrix;

            //SharpDX.Direct2D1.Brush testBrush = new SharpDX.Direct2D1.SolidColorBrush(_d3dResCache.D2DDeviceContext, new RawColor4(0.1f, 1, 0, 1f));
            //foreach (var node in CadManager3D.DrawingObjectTree3D.BaseLevelNodes)
            //{
            //    _d3dResCache.D2DDeviceContext.DrawRectangle(new RawRectangleF((float)node.Extents.TopLeft.X, (float)node.Extents.TopLeft.Y, (float)node.Extents.BottomRight.X, (float)node.Extents.BottomRight.Y), testBrush, 5, _interactiveObjectStrokeStyle);
            //}
            //foreach (var layer in CadManager3D.Layers.Values)
            //{
            //    foreach (var obj in layer.DrawingObject3Ds)
            //    {
            //        var inflatedBounds = Rect.Inflate(obj.Bounds, 1, 1);
            //        //var inflatedBounds = obj.Bounds;

            //        //Debug.WriteLine($"Bounds: {obj.Bounds}");
            //        //Debug.WriteLine($"Inflated Bounds: {inflatedBounds}");

            //        _d3dResCache.D2DDeviceContext.DrawRectangle(new RawRectangleF((float)inflatedBounds.TopLeft.X, (float)inflatedBounds.TopLeft.Y, (float)inflatedBounds.BottomRight.X, (float)inflatedBounds.BottomRight.Y), testBrush, 1, _interactiveObjectStrokeStyle);
            //    }
            //}
            //testBrush.Dispose();

            if (_snappedObject is not null)
            {
                var copy = _snappedObject;
                if (copy is DrawingSegment3D segment)
                {
                    SharpDX.Direct2D1.Brush innerBrush = new SharpDX.Direct2D1.SolidColorBrush(_d3dResCache.D2DDeviceContext, new RawColor4(segment.Color.X, segment.Color.Y, segment.Color.Z, segment.Color.W));
                    SharpDX.Direct2D1.Brush outerBrush = new SharpDX.Direct2D1.SolidColorBrush(_d3dResCache.D2DDeviceContext, new RawColor4(segment.Color.X, segment.Color.Y, segment.Color.Z, 0.3f));

                    segment.DrawToD2D(_d3dResCache.D2DDeviceContext, _d3dResCache.D2dFactory, outerBrush, 4, _interactiveObjectStrokeStyle);
                    segment.DrawToD2D(_d3dResCache.D2DDeviceContext, _d3dResCache.D2dFactory, innerBrush, 1, _interactiveObjectStrokeStyle);

                    //for (int i = 0; i < segment.Vertices.Count / 2; i++)
                    //{
                    //    RawVector2 start = new(segment.Vertices[i * 2].Position.X, segment.Vertices[i * 2].Position.Y);
                    //    RawVector2 end = new(segment.Vertices[i * 2 + 1].Position.X, segment.Vertices[i * 2 + 1].Position.Y);

                    //    _d3dResCache.D2DDeviceContext.DrawLine(start, end, outerBrush, 6, _interactiveObjectStrokeStyle);
                    //    _d3dResCache.D2DDeviceContext.DrawLine(start, end, innerBrush, 1, _interactiveObjectStrokeStyle);
                    //}

                    innerBrush.Dispose();
                    outerBrush.Dispose();
                }

                if (copy is DrawingPolyline3D polyline)
                {
                    SharpDX.Direct2D1.Brush innerBrush = new SharpDX.Direct2D1.SolidColorBrush(_d3dResCache.D2DDeviceContext, new RawColor4(polyline.Color.X, polyline.Color.Y, polyline.Color.Z, polyline.Color.W));
                    SharpDX.Direct2D1.Brush outerBrush = new SharpDX.Direct2D1.SolidColorBrush(_d3dResCache.D2DDeviceContext, new RawColor4(polyline.Color.X, polyline.Color.Y, polyline.Color.Z, 0.3f));

                    polyline.DrawToD2D(_d3dResCache.D2DDeviceContext, _d3dResCache.D2dFactory, outerBrush, 4, _interactiveObjectStrokeStyle);
                    polyline.DrawToD2D(_d3dResCache.D2DDeviceContext, _d3dResCache.D2dFactory, innerBrush, 1, _interactiveObjectStrokeStyle);

                    ////Geometry 
                    //for (int i = 0; i < polyline.Vertices.Count / 2; i++)
                    //{
                    //    RawVector2 start = new(polyline.Vertices[i * 2].Position.X, polyline.Vertices[i * 2].Position.Y);
                    //    RawVector2 end = new(polyline.Vertices[i * 2 + 1].Position.X, polyline.Vertices[i * 2 + 1].Position.Y);

                    //    _d3dResCache.D2DDeviceContext.DrawLine(start, end, outerBrush, 4, _interactiveObjectStrokeStyle);
                    //    _d3dResCache.D2DDeviceContext.DrawLine(start, end, innerBrush, 1, _interactiveObjectStrokeStyle);
                    //}

                    innerBrush.Dispose();
                    outerBrush.Dispose();
                }
            }
        }

        /// <summary>
        /// Resets the _vertices field to the current list of vertices in the CadManager3D.
        /// </summary>
        private void UpdateDxfVertices()
        {
            if (_d3dResCache is null) { return; }

            CadManager3D.UpdateVerticesList();
            _vertices = CadManager3D.Vertices.ToArray();

            SetVertexBuffer();

            DxfIsDirty = false;
            D3dIsDirty = true;
        }

        /// <summary>
        /// Sets the vertex buffer to the field _vertices. Does not update _vertices
        /// </summary>
        private void SetVertexBuffer()
        {
            if (_d3dResCache is null) { return; }

            //_vertices = CadManager3D.Vertices.ToArray();

            if (_vertices is not null && _vertices.Length > 0)
            {
                _vertexBuffer = Buffer.Create(
                _d3dResCache.Device,
                BindFlags.VertexBuffer,
                _vertices);
            }

            VertexBufferDirty = false;
        }

        private void InitializeDirect2D()
        {
            _highlightedBrush?.Dispose();
            _highlightedOuterEdgeBrush?.Dispose();
            _interactiveObjectStrokeStyle?.Dispose();

            _highlightedBrush = new SharpDX.Direct2D1.SolidColorBrush(_d3dResCache.D2DDeviceContext, new RawColor4((97 / 255), 1.0f, 0.0f, 1.0f));
            _highlightedOuterEdgeBrush = new SharpDX.Direct2D1.SolidColorBrush(_d3dResCache.D2DDeviceContext, new RawColor4((97 / 255), 1.0f, 0.0f, 1.0f))
            { Opacity = 0.2f };

            SharpDX.Direct2D1.StrokeStyleProperties1 props = new()
            {
                StartCap = SharpDX.Direct2D1.CapStyle.Square,
                EndCap = SharpDX.Direct2D1.CapStyle.Square,
                DashCap = SharpDX.Direct2D1.CapStyle.Round,
                LineJoin = SharpDX.Direct2D1.LineJoin.Miter,
                MiterLimit = 10,
                DashStyle = SharpDX.Direct2D1.DashStyle.Solid,
                DashOffset = 0,
                TransformType = SharpDX.Direct2D1.StrokeTransformType.Fixed
            };
            _interactiveObjectStrokeStyle = new(_d3dResCache.D2dFactory, props);
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
                    new InputElement("COLOR", 0, SharpDX.DXGI.Format.R32G32B32A32_Float, 12, 0),
                    new InputElement("ISVISIBLE", 0, SharpDX.DXGI.Format.R32_Float, 28, 0)
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
            ResetSnappedObjects();
            VertexBufferDirty = true;
            D3dIsDirty = true;

            base.OnMouseLeave(e);
        }
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            Viewport = new(0, 0, (float)ActualWidth, (float)ActualHeight);

            GetInitialMatrix();

            if (_camera is not null)
            {
                _camera.UpdateViewportSize(Viewport);
            }

            D3dIsDirty = true;
        }


        public async Task RunHitTestingAsync()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await Task.Run(() => RunObjectHitTest(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
                    await Task.Delay(30); // Adjust the delay as needed
                }
            }
            catch (TaskCanceledException)
            {
                // Handle the task cancellation here
                Debug.WriteLine("Task was canceled.");
            }
            catch (Exception ex)
            {
                // Handle other exceptions here
                Debug.WriteLine($"An error occurred: {ex.Message}");
            }
        }
        private void RunObjectHitTest(CancellationToken token)
        {
            // Check for cancellation
            if (token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();
            }
            //Stopwatch stopwatch = Stopwatch.StartNew();

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => RunObjectHitTest(token));

                return;
            }

            if (!CadManager3D.DxfLoaded) { return; }

            _lastHitTestCoords = new(DxfCoords.X, DxfCoords.Y);

            bool vertexBufferDirty = false;
            bool d3dIsDirty = false;
            float tolerance = _hittestStrokeThickness / (_camera.CurrentZoom);
            Rect rect = new(_lastHitTestCoords.X - tolerance, _lastHitTestCoords.Y - tolerance, tolerance * 2, tolerance * 2);
            var snappedCopy = _snappedObject;

            if (snappedCopy is not null)
            {
                if (snappedCopy.DistanceToPoint(_lastHitTestCoords) > tolerance)
                {
                    DeselectObject(snappedCopy);
                    _snappedObject = null;

                    _nearestDrawingObjects = CadManager3D.GetNearestDrawingObjects(_lastHitTestCoords, tolerance);

                    if (_nearestDrawingObjects.Count > 0)
                    {
                        var (distance, obj) = _nearestDrawingObjects.First();

                        if (distance <= tolerance)
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
                _nearestDrawingObjects = CadManager3D.GetNearestDrawingObjects(_lastHitTestCoords, tolerance);

                //Debug.WriteLine($"HitTest Count: {_nearestDrawingObjects.Count}");

                if (_nearestDrawingObjects.Count > 0)
                {
                   var (distance, obj) = _nearestDrawingObjects.First();

                    if (distance <= tolerance)
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

            //stopwatch.Stop();
            //Debug.WriteLine($"RunHitTest Time: {stopwatch.ElapsedMilliseconds}\n\n\n");
        }
        public void CancelHitTesting()
        {
            _cancellationTokenSource?.Cancel();
        }


        private void SelectObject(DrawingObject3D obj)
        {
            if (obj is not null && obj is DrawingGeometry3D geometry)
            {
                geometry.Select();

                int count = 0;
                for (int i = geometry.StartVertexIndex; i <= geometry.EndVertexIndex; i++)
                {
                    _vertices[i] = geometry.Vertices[count];
                    count++;
                }
            }
        }
        private void DeselectObject(DrawingObject3D obj)
        {
            if (obj is not null && obj is DrawingGeometry3D geometry)
            {
                geometry.Deselect();

                int count = 0;
                for (int i = geometry.StartVertexIndex; i <= geometry.EndVertexIndex; i++)
                {
                    _vertices[i] = geometry.Vertices[count];
                    count++;
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
                    // Dispose managed state (managed objects).
                    _vertexBuffer?.Dispose();
                    _transformationBuffer?.Dispose();
                    _vertexShader?.Dispose();
                    _pixelShader?.Dispose();
                    _inputLayout?.Dispose();
                    _highlightedBrush?.Dispose();
                    _highlightedOuterEdgeBrush?.Dispose();
                }

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
