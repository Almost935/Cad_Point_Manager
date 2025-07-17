using Cad_Point_Manager.Controls.D3DControl.Buffers;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.DrawingObjects3D;
using Cad_Point_Manager.Models.HitTesting;
using Cad_Point_Manager.Models.PointRendering;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Buffer = SharpDX.Direct3D11.Buffer;
using InputElement = SharpDX.Direct3D11.InputElement;
using Matrix = SharpDX.Matrix;
using Point = System.Windows.Point;
using RectangleGeometry = System.Windows.Media.RectangleGeometry;

namespace Cad_Point_Manager.Controls.D3DControl
{
    public class D3dDxfControl : Direct3DControl, INotifyPropertyChanged, IDisposable
    {
        #region Fields
        private Buffer _transformationBuffer;

        private Point _pointerCoords;
        private Vector2 _dxfCoords;
        private string _dxfCoordsString = $"X: {0:F3}   Y: {0:F3}";
        private Matrix _dxfInitialMatrix = Matrix.Identity;
        private bool _clipSet = false;
        private bool _isMouseInside;
        private Window _attachedWindow;

        // Direct3D related fields
        public bool _vertexBuffersInitialized = false;

        // Line shader related fields
        private ResizableBuffer<LineVertex> _lineVertexBuffer;
        private Buffer _lineSettingsBuffer;
        private int _lineVertexCount;
        private VertexShader _lineVertexShader;
        private PixelShader _linePixelShader;
        private InputLayout _lineInputLayout;
        private bool _lineShaderLoaded = false;
        private bool _lineVerticesDirty = false;

        // Line glow shader related fields
        private ResizableBuffer<LineVertex> _lineGlowVertexBuffer;
        private Buffer _lineGlowSettingsBuffer;
        private List<LineVertex> _lineGlowVertices = [];
        private VertexShader _lineGlowVertexShader;
        private PixelShader _lineGlowPixelShader;
        private GeometryShader _lineGlowGeometryShader;
        private bool _lineGlowVerticesDirty = false;

        // Text shader related fields
        private ResizableBuffer<TextVertex> _textVertexBuffer;
        private Buffer _textSettingsBuffer;
        private int _textVertexCount;
        private VertexShader _textVertexShader;
        private PixelShader _textPixelShader;
        private InputLayout _textInputLayout;
        private bool _textShaderLoaded = false;
        private bool _textVerticesDirty = false;

        // Text glow shader related fields
        private ResizableBuffer<TextVertex> _textGlowVertexBuffer;
        private Buffer _textGlowSettingsBuffer;
        private List<TextVertex> _textGlowVertices = [];
        private VertexShader _textGlowVertexShader;
        private PixelShader _textGlowPixelShader;
        private GeometryShader _textGlowGeometryShader;
        private bool _textGlowVerticesDirty = false;

        // Debugging fields
        private CircleVertex _testVertex;

        // Panning and Zooming Fields
        private float _panThreshold = 1.0f;
        private bool _isPanning;

        // Camera based fields
        private bool _isShiftPressed;
        private Vector2 _prevMousePos;

        // Interaction Fields
        private Task _hittestTask;
        private bool _hitTestIsRunning;
        private float _hittestStrokeThickness;
        private Point _lastHitTestCoords;
        private CancellationTokenSource _hitTestCancellationTokenSource;
        private int _currentSnapHitTestIndex = 0; // Represents the current placement in snapped list. Changes on tab press.
        private int _lastSnapHitTestIndex = 0;
        private const int _maxSelectableObjects = 5;
        private List<(double distance, HitTestablePoint hitTestablePoint)> _nearestHitTestablePoints = [];
        private List<(double distance, DrawingGeometry3D geometry)> _nearestHitTestableGeometries = [];
        private List<(double distance, CogoPoint point)> _nearestHitTestableCogoPoints = [];
        private List<(double distance, HitTestableObject hitTestableObject)> _nearestHitTestableObjects = [];
        private HitTestableObject _snappedHitTestableObject = null;
        private List<HitTestableObject> _selectedHitTestableObjects = [];
        #endregion

        #region Properties 
        /// <summary>
        /// Determines if the view matrix needs to be reloaded. Occurs when the Dxf file is changed.
        /// </summary>
        private bool DxfNeedsReload { get; set; }

        /// <summary>
        /// Determines if the Direct3D control needs to be redrawn. Occurs when the camera is panned or zoomed.
        /// </summary>
        public bool D3dIsDirty { get; set; }
        public bool HitTestableObjectTreeDirty { get; set; }
        public bool ConstantBuffersInitialized { get; set; }
        public bool ConstantBuffersDirty { get; set; }
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

        public enum SnapMode { Point, Object }
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

        public Camera Camera
        {
            get { return (Camera)GetValue(CameraProperty); }
            set { SetValue(CameraProperty, value); }
        }
        public static readonly DependencyProperty CameraProperty =
            DependencyProperty.Register(
            nameof(Camera),
            typeof(Camera),
            typeof(D3dDxfControl),
            new PropertyMetadata(null));

        public static readonly DependencyProperty CogoPointsProperty =
            DependencyProperty.Register(
                nameof(CogoPoints),
                typeof(ObservableCollection<CogoPoint>),
                typeof(D3dDxfControl),
                new FrameworkPropertyMetadata(new ObservableCollection<CogoPoint>(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public ObservableCollection<CogoPoint> CogoPoints
        {
            get => (ObservableCollection<CogoPoint>)GetValue(CogoPointsProperty);
            set => SetValue(CogoPointsProperty, value);
        }

        public static readonly DependencyProperty SelectedCogoPointsProperty =
            DependencyProperty.Register(
                nameof(SelectedCogoPoints),
                typeof(ObservableCollection<CogoPoint>),
                typeof(D3dDxfControl),
                new FrameworkPropertyMetadata(new ObservableCollection<CogoPoint>(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public ObservableCollection<CogoPoint> SelectedCogoPoints
        {
            get => (ObservableCollection<CogoPoint>)GetValue(SelectedCogoPointsProperty);
            set => SetValue(SelectedCogoPointsProperty, value);
        }

        public static readonly DependencyProperty SnappedSignificantPointProperty =
        DependencyProperty.Register(
            nameof(SnappedSignificantPoint),
            typeof(HitTestablePoint),
            typeof(D3dDxfControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public HitTestablePoint SnappedSignificantPoint
        {
            get => (HitTestablePoint)GetValue(SnappedSignificantPointProperty);
            set => SetValue(SnappedSignificantPointProperty, value);
        }

        public static readonly DependencyProperty SelectedSignificantPointsProperty =
            DependencyProperty.Register(
                nameof(SelectedSignificantPoints),
                typeof(ObservableCollection<HitTestablePoint>),
                typeof(D3dDxfControl),
                new FrameworkPropertyMetadata(new ObservableCollection<HitTestablePoint>(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public ObservableCollection<HitTestablePoint> SelectedSignificantPoints
        {
            get => (ObservableCollection<HitTestablePoint>)GetValue(SelectedSignificantPointsProperty);
            set => SetValue(SelectedSignificantPointsProperty, value);
        }

        public static readonly DependencyProperty MousePositionProperty =
            DependencyProperty.Register(
                nameof(MousePosition),
                typeof(Point),
                typeof(D3dDxfControl),
                new FrameworkPropertyMetadata(new Point(), null));
        public Point MousePosition
        {
            get => (Point)GetValue(MousePositionProperty);
            set => SetValue(MousePositionProperty, value);
        }
        #endregion

        #region Functions
        public readonly Func<Vector2, string> formatVectorString = (vector) => $"X: {vector.X:F3}   Y: {vector.Y:F3}";
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Constructors
        public D3dDxfControl()
        {
            _attachedWindow = Application.Current.MainWindow;
            if (_attachedWindow != null) 
            { 
                _attachedWindow.KeyUp += Window_KeyUp;
                _attachedWindow.PreviewKeyDown += Window_PreviewKeyDown;
            }
        }
        #endregion

        #region Methods
        public override void Render()
        {
            if (_d3dResCache is null) { return; }

            if (Camera is null)
            {
                GetInitialMatrix();
                Camera = new(Viewport, GlobalHelperProperties.ZoomFactor, CadManager3D.Extents);
            }
            if (DxfNeedsReload)
            {
                GetInitialMatrix();
                Camera.ResetView(_dxfInitialMatrix, CadManager3D.Extents);
                ConstantBuffersDirty = true;
                DxfNeedsReload = false;
                CadManager3D.DxfNeedsReload = false;
            }
            if (!_clipSet) { SetClip(); _clipSet = true; }
            if (!_vertexBuffersInitialized) { InitializeBuffers(); _vertexBuffersInitialized = true; }

            if (_lineGlowVerticesDirty) { UpdateLineGlowVertices(); }
            if (_lineVerticesDirty) { UpdateLineVertices(); }
            if (_textVerticesDirty) { UpdateTextVertices(); }
            if (_textGlowVerticesDirty) { UpdateTextGlowVertices(); }
            if (HitTestableObjectTreeDirty) { LoadHitTestableObjectTree(); }

            if (!_lineShaderLoaded) { InitializeLineShader(); }
            if (!_textShaderLoaded) { InitializeTextShader(); }
            if (!ConstantBuffersInitialized) { InitializeConstantBuffers(); }
            if (ConstantBuffersDirty) { UpdateConstantBuffers(); }
            if (D3dIsDirty) { DrawDxf(); }
            if (!_hitTestIsRunning)
            {
                _hitTestIsRunning = true;
                _hittestTask = Task.Run(() => RunHitTestingAsync());
            }
        }

        private void DrawDxf()
        {
            //Stopwatch stopwatch = Stopwatch.StartNew();

            var context = _d3dResCache.DeviceContext;

            // Set render target and clear it
            context.OutputMerger.SetRenderTargets(_d3dResCache.OffscreenRenderTargetView);
            context.ClearRenderTargetView(_d3dResCache.OffscreenRenderTargetView, new RawColor4(1, 1, 1, 0));

            DrawLineGlowsWithShader();
            DrawTextGlowsWithShader();
            DrawLinesWithShader();
            DrawTextWithShader();

            context.CopyResource(_d3dResCache.OffscreenTexture, _d3dResCache.Texture2D);

            D3dIsDirty = false;

            //stopwatch.Stop();
            //Debug.WriteLine($"D3D Render Time: {stopwatch.ElapsedMilliseconds} ms");
        }

        private void DrawLinesWithShader()
        {
            //Stopwatch stopwatch = Stopwatch.StartNew();

            var context = _d3dResCache.DeviceContext;

            if (_lineVertexBuffer is null) { return; }

            // Set shaders
            context.VertexShader.Set(_lineVertexShader);
            context.PixelShader.Set(_linePixelShader);
            context.InputAssembler.InputLayout = _lineInputLayout;
            context.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            context.VertexShader.SetConstantBuffer(1, _lineSettingsBuffer);
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(
                _lineVertexBuffer.Buffer, _lineVertexBuffer.Stride, 0));
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;

            context.Draw(_lineVertexCount, 0);

            //stopwatch.Stop();
            //Debug.WriteLine($"DrawLinesWithShader Time: {stopwatch.ElapsedMilliseconds} ms");
        }
        private void DrawLineGlowsWithShader()
        {
            //Stopwatch stopwatch = Stopwatch.StartNew();

            var context = _d3dResCache.DeviceContext;

            if (_lineGlowVertexBuffer == null || _lineGlowVertices.Count == 0)
            {
                return;
            }

            // Set shaders
            context.VertexShader.Set(_lineGlowVertexShader);
            context.GeometryShader.Set(_lineGlowGeometryShader);
            context.PixelShader.Set(_lineGlowPixelShader);
            context.InputAssembler.InputLayout = _lineInputLayout;
            context.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            context.GeometryShader.SetConstantBuffer(0, _transformationBuffer);
            context.GeometryShader.SetConstantBuffer(1, _lineGlowSettingsBuffer);
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(
                _lineGlowVertexBuffer.Buffer, _lineGlowVertexBuffer.Stride, 0));

            context.Draw(_lineGlowVertices.Count, 0);

            context.GeometryShader.Set(null);

            //stopwatch.Stop();
            //Debug.WriteLine($"DrawLineGlowsWithShader Time: {stopwatch.ElapsedMilliseconds} ms");
        }
        private void DrawTextWithShader()
        {
            //Stopwatch stopwatch = Stopwatch.StartNew();

            var context = _d3dResCache.DeviceContext;

            if (_textVertexBuffer is null) { return; }

            context.VertexShader.Set(_textVertexShader);
            context.PixelShader.Set(_textPixelShader);
            context.InputAssembler.InputLayout = _textInputLayout;
            context.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            context.PixelShader.SetConstantBuffer(0, _textSettingsBuffer);
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(
                 _textVertexBuffer.Buffer, _textVertexBuffer.Stride, 0));

            context.Draw(_textVertexCount, 0);

            //stopwatch.Stop();
            //Debug.WriteLine($"DrawTextWithShader Time: {stopwatch.ElapsedMilliseconds} ms");
        }
        private void DrawTextGlowsWithShader()
        {
            //Stopwatch stopwatch = Stopwatch.StartNew();

            var context = _d3dResCache.DeviceContext;

            if (_textGlowVertexBuffer == null || _textGlowVertices.Count == 0)
            {
                return;
            }

            // Set shaders
            context.VertexShader.Set(_textGlowVertexShader);
            context.GeometryShader.Set(_textGlowGeometryShader);
            context.PixelShader.Set(_textGlowPixelShader);
            context.InputAssembler.InputLayout = _textInputLayout;
            context.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            context.GeometryShader.SetConstantBuffer(0, _transformationBuffer);
            context.GeometryShader.SetConstantBuffer(1, _textGlowSettingsBuffer);
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(
                _textGlowVertexBuffer.Buffer, _textGlowVertexBuffer.Stride, 0));

            context.Draw(_textGlowVertices.Count, 0);

            context.GeometryShader.Set(null);

            //stopwatch.Stop();
            //Debug.WriteLine($"DrawTextGlowsWithShader Time: {stopwatch.ElapsedMilliseconds} ms");
        }

        private void UpdateLineVertices()
        {
            if (_lineVertexBuffer is null || CadManager3D is null) { return; }

            var context = _d3dResCache.DeviceContext;
            var vertexSpan = CadManager3D.UpdateLineVerticesList();
            _lineVertexBuffer.Update(context, vertexSpan);
            _lineVertexCount = vertexSpan.Length;

            _lineVerticesDirty = false;
            D3dIsDirty = true;
        }
        private void UpdateLineGlowVertices()
        {
            if (_lineGlowVertexBuffer == null)
            {
                _lineGlowVerticesDirty = false;
                return;
            }

            var context = _d3dResCache.DeviceContext;
            _lineGlowVertexBuffer.Update(context, _lineGlowVertices.ToArray());

            _lineGlowVerticesDirty = false;
            D3dIsDirty = true;
        }
        private void UpdateTextVertices()
        {
            if (_textVertexBuffer is null || CadManager3D is null)
            {
                _textVerticesDirty = false;
                return;
            }

            var context = _d3dResCache.DeviceContext;
            var vertexSpan = CadManager3D.UpdateTextVerticesList(_d3dResCache);
            _textVertexBuffer.Update(context, vertexSpan);
            _textVertexCount = vertexSpan.Length;

            _textVerticesDirty = false;
            D3dIsDirty = true;
        }
        private void UpdateTextGlowVertices()
        {
            if (_textGlowVertexBuffer == null)
            {
                _textGlowVerticesDirty = false;
                return;
            }

            var context = _d3dResCache.DeviceContext;
            _textGlowVertexBuffer.Update(context, _textGlowVertices.ToArray());

            _textGlowVerticesDirty = false;
            D3dIsDirty = true;
        }

        private void InitializeLineShader()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            while (Path.GetFileName(path) != "Cad_Point_Manager")
            {
                path = Path.GetDirectoryName(path);
                if (path == null)
                    throw new DirectoryNotFoundException("The 'Cad_Point_Manager' directory could not be found in the path.");
            }

            string shaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\LineShader.hlsl");
            string glowShaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\LineGlowShader.hlsl");

            // Main shaders
            var lineVSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "VSMain", "vs_5_0");
            _lineVertexShader = new VertexShader(_d3dResCache.Device, lineVSBytecode);

            var linePSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "PSMain", "ps_5_0");
            _linePixelShader = new PixelShader(_d3dResCache.Device, linePSBytecode);

            // Glow shaders
            var lineGlowVSBytecode = ShaderBytecode.CompileFromFile(glowShaderPath, "VSMain", "vs_5_0");
            _lineGlowVertexShader = new VertexShader(_d3dResCache.Device, lineGlowVSBytecode);

            var lineGlowGSBytecode = ShaderBytecode.CompileFromFile(glowShaderPath, "GSMain", "gs_5_0");
            _lineGlowGeometryShader = new GeometryShader(_d3dResCache.Device, lineGlowGSBytecode);

            var lineGlowPSBytecode = ShaderBytecode.CompileFromFile(glowShaderPath, "PSMain", "ps_5_0");
            _lineGlowPixelShader = new PixelShader(_d3dResCache.Device, lineGlowPSBytecode);

            _lineInputLayout = new InputLayout(
                _d3dResCache.Device,
                ShaderSignature.GetInputSignature(lineVSBytecode),
                new[]
                {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                    new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 12, 0),
                    new InputElement("ISVISIBLE", 0, Format.R32_Float, 28, 0),
                    new InputElement("ISMOUSEOVER", 0, Format.R32_Float, 32, 0),
                    new InputElement("ISSELECTED", 0, Format.R32_Float, 36, 0),
                });

            _lineShaderLoaded = true;
        }
        private void InitializeTextShader()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            while (Path.GetFileName(path) != "Cad_Point_Manager")
            {
                path = Path.GetDirectoryName(path);
                if (path == null)
                    throw new DirectoryNotFoundException("The 'Cad_Point_Manager' directory could not be found in the path.");
            }

            string shaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\TextShader.hlsl");
            string glowShaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\TextGlowShader.hlsl");

            // Main shaders
            var textVSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "VSMain", "vs_5_0");
            _textVertexShader = new VertexShader(_d3dResCache.Device, textVSBytecode);

            var textPSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "PSMain", "ps_5_0");
            _textPixelShader = new PixelShader(_d3dResCache.Device, textPSBytecode);

            // Glow shaders
            var textGlowVSBytecode = ShaderBytecode.CompileFromFile(glowShaderPath, "VSMain", "vs_5_0");
            _textGlowVertexShader = new VertexShader(_d3dResCache.Device, textGlowVSBytecode);

            var textGlowGSBytecode = ShaderBytecode.CompileFromFile(glowShaderPath, "GSMain", "gs_5_0");
            _textGlowGeometryShader = new GeometryShader(_d3dResCache.Device, textGlowGSBytecode);

            var textGlowPSBytecode = ShaderBytecode.CompileFromFile(glowShaderPath, "PSMain", "ps_5_0");
            _textGlowPixelShader = new PixelShader(_d3dResCache.Device, textGlowPSBytecode);

            // Layout
            _textInputLayout = new InputLayout(
                _d3dResCache.Device,
                ShaderSignature.GetInputSignature(textVSBytecode),
                new[]
                {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                    new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 12, 0),
                    new InputElement("ISVISIBLE", 0, Format.R32_Float, 28, 0),
                    new InputElement("ISMOUSEOVER", 0, Format.R32_Float, 32, 0),
                    new InputElement("ISSELECTED", 0, Format.R32_Float, 36, 0),
                 });

            _textShaderLoaded = true;
        }

        private void InitializeBuffers()
        {
            var device = _d3dResCache.Device;

            _lineVertexBuffer?.Dispose();
            _lineVertexBuffer = new(device, GlobalHelperProperties.InitialLineVertices);

            _lineGlowVertexBuffer?.Dispose();
            _lineGlowVertexBuffer = new(device, GlobalHelperProperties.InitialLineGlowVertices);

            _textVertexBuffer?.Dispose();
            _textVertexBuffer = new(device, GlobalHelperProperties.InitialTextVertices);

            _textGlowVertexBuffer?.Dispose();
            _textGlowVertexBuffer = new(device, GlobalHelperProperties.InitialTextGlowVertices);
        }
        private void InitializeConstantBuffers()
        {
            var transformationBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<TransformationBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _transformationBuffer = new Buffer(_d3dResCache.Device, transformationBufferDesc);

            var lineBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<LineSettingsBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _lineSettingsBuffer = new Buffer(_d3dResCache.Device, lineBufferDesc);

            var lineGlowBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<LineGlowSettingsBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _lineGlowSettingsBuffer = new Buffer(_d3dResCache.Device, lineGlowBufferDesc);

            var textBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<TextSettingsBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _textSettingsBuffer = new Buffer(_d3dResCache.Device, textBufferDesc);

            var textGlowBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<TextGlowSettingsBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _textGlowSettingsBuffer = new Buffer(_d3dResCache.Device, textGlowBufferDesc);

            ConstantBuffersInitialized = true;
            ConstantBuffersDirty = true;
        }
        private void UpdateConstantBuffers()
        {
            var transformation = Camera.ViewProjectionMatrix;
            var transformationBuffer = new TransformationBuffer
            {
                WorldViewProjection = transformation
            };
            _d3dResCache.DeviceContext.UpdateSubresource(ref transformationBuffer, _transformationBuffer);

            var worldUnitsPerPixel = Camera.GetWorldUnitsPerPixel();

            var lineSettings = new LineSettingsBuffer
            {
                SelectedColor = GlobalHelperProperties.SelectedObjectColor,
                SelectedMouseOverColor = GlobalHelperProperties.SelectedMouseOverObjectColor
            };
            _d3dResCache.DeviceContext.UpdateSubresource(ref lineSettings, _lineSettingsBuffer);

            var lineGlowSettings = new LineGlowSettingsBuffer
            {
                GlowOffset = GlobalHelperProperties.LineGlowPixelWidth * worldUnitsPerPixel,
                GlowTransparency = GlobalHelperProperties.LineGlowTransparency,
                SelectedColor = GlobalHelperProperties.SelectedObjectColor,
                SelectedMouseOverColor = GlobalHelperProperties.SelectedMouseOverGlowColor
            };
            _d3dResCache.DeviceContext.UpdateSubresource(ref lineGlowSettings, _lineGlowSettingsBuffer);

            var textSettings = new TextSettingsBuffer
            {
                SelectedColor = GlobalHelperProperties.SelectedObjectColor,
                SelectedMouseOverColor = GlobalHelperProperties.SelectedMouseOverObjectColor
            };
            _d3dResCache.DeviceContext.UpdateSubresource(ref textSettings, _textSettingsBuffer);

            var textGlowSettings = new TextGlowSettingsBuffer
            {
                GlowOffset = GlobalHelperProperties.LineGlowPixelWidth * worldUnitsPerPixel,
                GlowTransparency = GlobalHelperProperties.LineGlowTransparency,
                SelectedColor = GlobalHelperProperties.SelectedObjectColor,
                SelectedMouseOverColor = GlobalHelperProperties.SelectedMouseOverGlowColor
            };
            _d3dResCache.DeviceContext.UpdateSubresource(ref textGlowSettings, _textGlowSettingsBuffer);

            ConstantBuffersDirty = false;
            D3dIsDirty = true;
        }

        private void GetInitialMatrix()
        {
            if (!CadManager3D.DxfLoaded)
            {
                _dxfInitialMatrix = Matrix.Identity;
            }
            else
            {
                double scale = Math.Min(Viewport.Width / CadManager3D.Extents.Width, Viewport.Height / CadManager3D.Extents.Height);

                _dxfInitialMatrix = Matrix.Scaling(scale.ToFloat(), scale.ToFloat(), 1) * Matrix.Translation(-CadManager3D.Extents.Left.ToFloat(), -CadManager3D.Extents.Top.ToFloat(), 0);

                if (Camera is not null)
                {
                    Camera.ResetView(_dxfInitialMatrix, CadManager3D.Extents);
                    CadManager3D.CogoPointManager.UpdateAllVisualTransforms(Camera.D2dMatrix.ToWindowsMatrix());
                    _hittestStrokeThickness = 7.0f / (Camera.InitialViewMatrix.M11 * Camera.CurrentZoom);

                    ConstantBuffersDirty = true;
                }
            }
        }

        private void UpdateDxfCoords(Vector2 mousePos)
        {
            DxfCoords = Camera.ScreenToWorld(mousePos);
            MousePosition = DxfCoords.ToPoint();
            DxfCoordsString = formatVectorString(DxfCoords);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                _isPanning = true;
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
                    UpdateDxfCoords(currentMousePos);
                }

                _isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

                if (e.MiddleButton == MouseButtonState.Pressed)
                {
                    Camera.Pan(currentMousePos, _prevMousePos);
                    CadManager3D.CogoPointManager.UpdateAllVisualTransforms(Camera.D2dMatrix.ToWindowsMatrix());
                    ConstantBuffersDirty = true;
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

            Camera.Zoom(zoomSteps, new Vector2((float)_pointerCoords.X, (float)_pointerCoords.Y));
            CadManager3D.CogoPointManager.UpdateAllVisualTransforms(Camera.D2dMatrix.ToWindowsMatrix());
            _hittestStrokeThickness = 7.0f / (Camera.InitialViewMatrix.M11 * Camera.CurrentZoom);

            ConstantBuffersDirty = true;
            e.Handled = true;
        }
        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);

            _isMouseInside = true;
            _hitTestCancellationTokenSource = new CancellationTokenSource();
            _ = RunHitTestingAsync();
        }
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            var pos = Mouse.GetPosition(this);
            bool isInside =
                pos.X >= 0 && pos.Y >= 0 &&
                pos.X <= this.ActualWidth &&
                pos.Y <= this.ActualHeight;
            if (isInside) { return; } 

            _isMouseInside = false;
            _hitTestCancellationTokenSource.Cancel();
            _isPanning = false;

            (bool linesDirty, bool textsDirty, bool circlesDirty, bool pointTextsDirty, bool sigPointsDirty) =
                GetVerticesDirtyBools([_snappedHitTestableObject]);

            ResetSnappedObjects();

            if (linesDirty) { _lineVerticesDirty = linesDirty; }
            if (linesDirty) { _lineGlowVerticesDirty = linesDirty; }
            if (textsDirty) { _textVerticesDirty = textsDirty; }
            if (textsDirty) { _textGlowVerticesDirty = textsDirty; }
        }
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            List<HitTestableObject> changedObjects = [];

            switch (CadManager3D.SnapSelectionMode)
            {
                case Common.Enums.SelectionMode.Points:
                    if (SnappedSignificantPoint is not null)
                    {
                        if (SnappedSignificantPoint.IsSelected)
                        {
                            SnappedSignificantPoint.IsSelected = false;
                        }
                        else
                        {
                            SnappedSignificantPoint.IsSelected = true;
                        }
                    }
                    break;

                case Common.Enums.SelectionMode.Geometries:
                    if (_snappedHitTestableObject is not null)
                    {
                        if (_snappedHitTestableObject.IsSelected)
                        {
                            DeselectObject(_snappedHitTestableObject);
                            changedObjects.Add(_snappedHitTestableObject);
                        }
                        else
                        {
                            SelectObject(_snappedHitTestableObject);
                            changedObjects.Add(_snappedHitTestableObject);
                        }
                    }
                    break;

                case Common.Enums.SelectionMode.CogoPoints:
                    if (_snappedHitTestableObject is not null && _snappedHitTestableObject is CogoPoint)
                    {
                        if (_snappedHitTestableObject.IsSelected)
                        {
                            DeselectObject(_snappedHitTestableObject);
                            changedObjects.Add(_snappedHitTestableObject);
                        }
                        else
                        {
                            SelectObject(_snappedHitTestableObject);
                            changedObjects.Add(_snappedHitTestableObject);
                        }
                    }
                    break;

                case Common.Enums.SelectionMode.All:
                    if (_snappedHitTestableObject is not null)
                    {
                        if (_snappedHitTestableObject.IsSelected)
                        {
                            DeselectObject(_snappedHitTestableObject);
                            changedObjects.Add(_snappedHitTestableObject);
                        }
                        else
                        {
                            SelectObject(_snappedHitTestableObject);
                            changedObjects.Add(_snappedHitTestableObject);
                        }
                    }
                    break;

                default:
                    break;
            }

            var (linesDirty, textsDirty, circlesDirty, pointTextsDirty, sigPointsDirty) = GetVerticesDirtyBools(changedObjects);
            if (linesDirty) { _lineVerticesDirty = linesDirty; }
            if (linesDirty) { _lineGlowVerticesDirty = linesDirty; }
            if (textsDirty) { _textVerticesDirty = textsDirty; }
            if (textsDirty) { _textGlowVerticesDirty = textsDirty; }
        }
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ResetSelectedObjects();
            }
            if (e.Key == Key.Tab)
            {
                _currentSnapHitTestIndex += 1;

                e.Handled = true;
            }
        }
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                e.Handled = true;
            }
            base.OnPreviewKeyDown(e);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            Viewport = new(0, 0, (float)ActualWidth, (float)ActualHeight);
            CadManager3D.ViewportSize = new((float)ActualWidth, (float)ActualHeight);

            GetInitialMatrix();

            if (Camera is not null)
            {
                Camera.UpdateViewportSize(Viewport);
                ConstantBuffersDirty = true;
            }

            D3dIsDirty = true;
        }

        public void ZoomToExtents()
        {
            if (Camera is null) { return; }

            Camera.ResetView(_dxfInitialMatrix, CadManager3D.Extents);
            CadManager3D.CogoPointManager.UpdateAllVisualTransforms(Camera.D2dMatrix.ToWindowsMatrix());
            ResetSnappedObjects();

            ConstantBuffersDirty = true;
        }

        public async Task RunHitTestingAsync()
        {
            while (_isMouseInside)
            {
                if (_hitTestCancellationTokenSource.Token.IsCancellationRequested) { break; }

                if (CadManager3D.DxfLoaded)
                {
                    switch (CadManager3D.SnapSelectionMode)
                    {
                        case Common.Enums.SelectionMode.Points:
                            RunPointsHitTest(_hitTestCancellationTokenSource.Token);
                            break;

                        case Common.Enums.SelectionMode.Geometries:
                            RunGeometriesHitTest(_hitTestCancellationTokenSource.Token);
                            break;

                        case Common.Enums.SelectionMode.CogoPoints:
                            RunCogoPointsHitTest(_hitTestCancellationTokenSource.Token);
                            break;

                        default:
                            RunObjectHitTest(_hitTestCancellationTokenSource.Token);
                            break;
                    }
                }
                await Task.Delay(50); // Adjust the delay as needed
            }
        }
        private void RunPointsHitTest(CancellationToken token)
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

            Rect rect = new(_lastHitTestCoords.X - _hittestStrokeThickness, _lastHitTestCoords.Y - _hittestStrokeThickness,
                _hittestStrokeThickness * 2, _hittestStrokeThickness * 2);
            var snappedHitTestablePointCopy = SnappedSignificantPoint;

            if (snappedHitTestablePointCopy is not null)
            {
                var testDistance = snappedHitTestablePointCopy.DistanceToPoint(_lastHitTestCoords);

                if (snappedHitTestablePointCopy.DistanceToPoint(_lastHitTestCoords) > _hittestStrokeThickness ||
                    _currentSnapHitTestIndex != _lastSnapHitTestIndex)
                {
                    ResetSnappedObjects();

                    _nearestHitTestablePoints = CadManager3D.HitTestSignficantPoints(_lastHitTestCoords, _hittestStrokeThickness).Take(_maxSelectableObjects).ToList();

                    if (_nearestHitTestablePoints.Count > 0)
                    {
                        bool exists = HitTestingHelpers.TryGetNextHitTestablePoint(_currentSnapHitTestIndex, _nearestHitTestablePoints, out var tup);
                        if (!exists) 
                        { 
                            _currentSnapHitTestIndex = 0;
                            exists = HitTestingHelpers.TryGetNextHitTestablePoint(_currentSnapHitTestIndex, _nearestHitTestablePoints, out tup);
                        }

                        if (exists)
                        {
                            var (distance, point) = tup;

                            if (distance <= _hittestStrokeThickness)
                            {
                                SnappedSignificantPoint = point;
                                _lastSnapHitTestIndex = _currentSnapHitTestIndex;
                            }
                        }
                    }
                }
                else { return; }
            }
            else
            {
                _nearestHitTestablePoints = CadManager3D.HitTestSignficantPoints(_lastHitTestCoords, _hittestStrokeThickness).Take(_maxSelectableObjects).ToList();

                if (_nearestHitTestablePoints.Count < 1) { return; }

                bool exists = HitTestingHelpers.TryGetNextHitTestablePoint(_currentSnapHitTestIndex, _nearestHitTestablePoints, out var tup);
                if (!exists) 
                { 
                    _currentSnapHitTestIndex = 0;
                    exists = HitTestingHelpers.TryGetNextHitTestablePoint(_currentSnapHitTestIndex, _nearestHitTestablePoints, out tup);
                }

                if (exists)
                {
                    var (distance, point) = tup;

                    if (distance <= _hittestStrokeThickness)
                    {
                        SnappedSignificantPoint = point;
                        _lastSnapHitTestIndex = _currentSnapHitTestIndex;
                    }
                }
            }
        }
        private void RunGeometriesHitTest(CancellationToken token)
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

            Rect rect = new(_lastHitTestCoords.X - _hittestStrokeThickness, _lastHitTestCoords.Y - _hittestStrokeThickness,
                _hittestStrokeThickness * 2, _hittestStrokeThickness * 2);
            var snappedGeometryCopy = _snappedHitTestableObject;
            List<HitTestableObject> changedObjects = [];

            if (snappedGeometryCopy is not null)
            {
                if (snappedGeometryCopy.DistanceToPoint(_lastHitTestCoords) > _hittestStrokeThickness)
                {
                    changedObjects.Add(snappedGeometryCopy);
                    ResetSnappedObjects();

                    _nearestHitTestableGeometries = CadManager3D.HitTestGeometries(_lastHitTestCoords, _hittestStrokeThickness).Take(_maxSelectableObjects).ToList();
                    if (_nearestHitTestableGeometries.Count > 0)
                    {
                        bool exists = HitTestingHelpers.TryGetNextDrawingGeometry(_currentSnapHitTestIndex, _nearestHitTestableGeometries, out var tup);
                        if (!exists) { _currentSnapHitTestIndex = 0; }
                        exists = HitTestingHelpers.TryGetNextDrawingGeometry(_currentSnapHitTestIndex, _nearestHitTestableGeometries, out tup);

                        if (exists)
                        {
                            var (distance, geometry) = tup;

                            if (distance <= _hittestStrokeThickness)
                            {
                                changedObjects.Add(geometry);
                                _snappedHitTestableObject = geometry;
                                SnapObject(_snappedHitTestableObject);
                                _lastSnapHitTestIndex = _currentSnapHitTestIndex;
                            }
                        }
                    }
                }
            }
            else
            {
                _nearestHitTestableGeometries = CadManager3D.HitTestGeometries(_lastHitTestCoords, _hittestStrokeThickness).Take(_maxSelectableObjects).ToList();
                if (_nearestHitTestableGeometries.Count > 0)
                {
                    bool exists = HitTestingHelpers.TryGetNextDrawingGeometry(_currentSnapHitTestIndex, _nearestHitTestableGeometries, out var tup);
                    if (!exists) { _currentSnapHitTestIndex = 0; }
                    exists = HitTestingHelpers.TryGetNextDrawingGeometry(_currentSnapHitTestIndex, _nearestHitTestableGeometries, out tup);

                    if (exists)
                    {
                        var (distance, geometry) = tup;

                        if (distance <= _hittestStrokeThickness)
                        {
                            changedObjects.Add(geometry);
                            _snappedHitTestableObject = geometry;
                            SnapObject(_snappedHitTestableObject);
                            _lastSnapHitTestIndex = _currentSnapHitTestIndex;
                        }
                    }
                }
            }
            var (linesDirty, textsDirty, circlesDirty, pointTextsDirty, sigPointsDirty) = GetVerticesDirtyBools(changedObjects);
            if (linesDirty) { _lineVerticesDirty = linesDirty; }
            if (linesDirty) { _lineGlowVerticesDirty = linesDirty; }
            if (textsDirty) { _textVerticesDirty = textsDirty; }
            if (textsDirty) { _textGlowVerticesDirty = textsDirty; }
        }
        private void RunCogoPointsHitTest(CancellationToken token)
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

            Rect rect = new(_lastHitTestCoords.X - _hittestStrokeThickness, _lastHitTestCoords.Y - _hittestStrokeThickness,
                _hittestStrokeThickness * 2, _hittestStrokeThickness * 2);
            var snappedGeometryCopy = _snappedHitTestableObject;
            List<HitTestableObject> changedObjects = [];

            if (snappedGeometryCopy is not null)
            {
                if (snappedGeometryCopy.DistanceToPoint(_lastHitTestCoords) > _hittestStrokeThickness)
                {
                    changedObjects.Add(snappedGeometryCopy);
                    ResetSnappedObjects();

                    _nearestHitTestableCogoPoints = CadManager3D.HitTestCogoPoints(_lastHitTestCoords, _hittestStrokeThickness).Take(_maxSelectableObjects).ToList();
                    if (_nearestHitTestableCogoPoints.Count > 0)
                    {
                        bool exists = HitTestingHelpers.TryGetNextCogoPoint(_currentSnapHitTestIndex, _nearestHitTestableCogoPoints, out var tup);
                        if (!exists) { _currentSnapHitTestIndex = 0; }
                        exists = HitTestingHelpers.TryGetNextCogoPoint(_currentSnapHitTestIndex, _nearestHitTestableCogoPoints, out tup);

                        if (exists)
                        {
                            var (distance, geometry) = tup;

                            if (distance <= _hittestStrokeThickness)
                            {
                                changedObjects.Add(geometry);
                                _snappedHitTestableObject = geometry;
                                SnapObject(_snappedHitTestableObject);
                                _lastSnapHitTestIndex = _currentSnapHitTestIndex;
                            }
                        }
                    }
                }
            }
            else
            {
                _nearestHitTestableCogoPoints = CadManager3D.HitTestCogoPoints(_lastHitTestCoords, _hittestStrokeThickness)
                    .Take(_maxSelectableObjects).ToList();
                if (_nearestHitTestableCogoPoints.Count > 0)
                {
                    bool exists = HitTestingHelpers.TryGetNextCogoPoint(_currentSnapHitTestIndex, _nearestHitTestableCogoPoints, out var tup);
                    if (!exists) { _currentSnapHitTestIndex = 0; }
                    exists = HitTestingHelpers.TryGetNextCogoPoint(_currentSnapHitTestIndex, _nearestHitTestableCogoPoints, out tup);

                    if (exists)
                    {
                        var (distance, geometry) = tup;

                        if (distance <= _hittestStrokeThickness)
                        {
                            changedObjects.Add(geometry);
                            _snappedHitTestableObject = geometry;
                            SnapObject(_snappedHitTestableObject);
                            _lastSnapHitTestIndex = _currentSnapHitTestIndex;
                        }
                    }
                }
            }
            var (linesDirty, textsDirty, circlesDirty, pointTextsDirty, sigPointsDirty) = GetVerticesDirtyBools(changedObjects);
            if (linesDirty) { _lineVerticesDirty = linesDirty; }
            if (linesDirty) { _lineGlowVerticesDirty = linesDirty; }
            if (textsDirty) { _textVerticesDirty = textsDirty; }
            if (textsDirty) { _textGlowVerticesDirty = textsDirty; }
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

            Rect rect = new(_lastHitTestCoords.X - _hittestStrokeThickness, _lastHitTestCoords.Y - _hittestStrokeThickness,
                _hittestStrokeThickness * 2, _hittestStrokeThickness * 2);
            var snappedCopy = _snappedHitTestableObject;
            List<HitTestableObject> changedObjects = [];

            if (snappedCopy is not null)
            {
                if (snappedCopy.DistanceToPoint(_lastHitTestCoords) > _hittestStrokeThickness)
                {
                    changedObjects.Add(snappedCopy);
                    ResetSnappedObjects();

                    _nearestHitTestableObjects = CadManager3D.HitTestAll(_lastHitTestCoords, _hittestStrokeThickness).Take(_maxSelectableObjects).ToList();
                    if (_nearestHitTestableObjects.Count > 0)
                    {
                        bool exists = HitTestingHelpers.TryGetNextHitTestableObject(_currentSnapHitTestIndex, _nearestHitTestableObjects, out var tup);
                        if (!exists) { _currentSnapHitTestIndex = 0; }
                        exists = HitTestingHelpers.TryGetNextHitTestableObject(_currentSnapHitTestIndex, _nearestHitTestableObjects, out tup);

                        if (exists)
                        {
                            var (distance, obj) = tup;

                            if (distance <= _hittestStrokeThickness)
                            {
                                changedObjects.Add(obj);
                                _snappedHitTestableObject = obj;
                                SnapObject(_snappedHitTestableObject);
                            }
                        }
                    }
                }
            }
            else
            {
                _nearestHitTestableObjects = CadManager3D.HitTestAll(_lastHitTestCoords, _hittestStrokeThickness).Take(_maxSelectableObjects).ToList();
                if (_nearestHitTestableObjects.Count > 0)
                {
                    bool exists = HitTestingHelpers.TryGetNextHitTestableObject(_currentSnapHitTestIndex, _nearestHitTestableObjects, out var tup);
                    if (!exists) { _currentSnapHitTestIndex = 0; }
                    exists = HitTestingHelpers.TryGetNextHitTestableObject(_currentSnapHitTestIndex, _nearestHitTestableObjects, out tup);

                    if (exists)
                    {
                        var (distance, obj) = tup;

                        if (distance <= _hittestStrokeThickness)
                        {
                            changedObjects.Add(obj);
                            _snappedHitTestableObject = obj;
                            SnapObject(_snappedHitTestableObject);
                        }
                    }
                }
            }

            var (linesDirty, textsDirty, circlesDirty, pointTextsDirty, sigPointsDirty) = GetVerticesDirtyBools(changedObjects);
            if (linesDirty) { _lineVerticesDirty = linesDirty; }
            if (linesDirty) { _lineGlowVerticesDirty = linesDirty; }
            if (textsDirty) { _textVerticesDirty = textsDirty; }
            if (textsDirty) { _textGlowVerticesDirty = textsDirty; }
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

        private void LoadHitTestableObjectTree()
        {
            if (CadManager3D is null) { return; }

            CadManager3D.UpdateHitTestableObjectTree();
            HitTestableObjectTreeDirty = false;
        }

        private void SnapObject(HitTestableObject hitTestableObject)
        {
            if (hitTestableObject is not null)
            {
                if (hitTestableObject is DrawingObject3D obj)
                {
                    if (obj is DrawingGeometry3D geometry)
                    {
                        geometry.MouseEnter();
                        CadManager3D.UpdateVerticesIsMouseOver(geometry, true);
                        _lineGlowVertices.AddRange(geometry.Vertices);
                    }
                    if (obj is DrawingBlock3D block3D)
                    {
                        block3D.MouseEnter();
                        CadManager3D.UpdateVerticesIsMouseOver(block3D, true);
                        _lineGlowVertices.AddRange(block3D.LineVertices);
                    }
                    if (obj is DrawingText3D drawingMtext3D)
                    {
                        drawingMtext3D.MouseEnter();
                        CadManager3D.UpdateVerticesIsMouseOver(drawingMtext3D, true);
                        _textGlowVertices.AddRange(drawingMtext3D.TextVertices);
                    }
                }
                if (hitTestableObject is CogoPoint dxfPoint)
                {
                    dxfPoint.MouseEnter();
                }
                if (hitTestableObject is HitTestablePoint point)
                {
                    point.MouseEnter();
                }
            }
        }
        private void UnsnapObject(HitTestableObject hitTestableObject)
        {
            if (hitTestableObject is not null)
            {
                if (hitTestableObject is DrawingObject3D obj)
                {
                    if (obj is DrawingGeometry3D geometry)
                    {
                        geometry.MouseLeave();
                        CadManager3D.UpdateVerticesIsMouseOver(geometry, false);
                    }
                    if (obj is DrawingBlock3D block3D)
                    {
                        block3D.MouseLeave();
                        CadManager3D.UpdateVerticesIsMouseOver(block3D, false);
                    }
                    if (obj is DrawingText3D text3D)
                    {
                        text3D.MouseLeave();
                        CadManager3D.UpdateVerticesIsMouseOver(text3D, false);
                    }
                }
                if (hitTestableObject is CogoPoint dxfPoint)
                {
                    dxfPoint.MouseLeave();
                }
                if (hitTestableObject is HitTestablePoint point)
                {
                    point.MouseLeave();
                }
            }
        }
        private void ResetSnappedObjects()
        {
            if (SnappedSignificantPoint is not null)
            {
                SnappedSignificantPoint = null;
            }
            if (_snappedHitTestableObject is not null)
            {
                UnsnapObject(_snappedHitTestableObject);
                _snappedHitTestableObject = null;
            }
            _lineGlowVertices.Clear();
            _textGlowVertices.Clear();
        }

        private void SelectObject(HitTestableObject hitTestableObject)
        {
            if (hitTestableObject is not null)
            {
                if (hitTestableObject is DrawingObject3D obj)
                {
                    if (obj is DrawingGeometry3D geometry)
                    {
                        geometry.Select();
                        CadManager3D.UpdateVerticesIsSelected(geometry, true);
                        _lineGlowVertices.AddRange(geometry.Vertices);
                        _selectedHitTestableObjects.Add(geometry);
                    }
                    if (obj is DrawingBlock3D block3D)
                    {
                        block3D.Select();
                        CadManager3D.UpdateVerticesIsSelected(block3D, true);
                        _lineGlowVertices.AddRange(block3D.LineVertices);
                        _selectedHitTestableObjects.Add(block3D);
                    }
                    if (obj is DrawingText3D drawingText)
                    {
                        drawingText.Select();
                        CadManager3D.UpdateVerticesIsSelected(drawingText, true);
                        _selectedHitTestableObjects.Add(drawingText);
                    }
                }
                if (hitTestableObject is CogoPoint dxfPoint)
                {
                    dxfPoint.Select();
                    SelectedCogoPoints.Add(dxfPoint);
                }
            }
        }
        private void DeselectObject(HitTestableObject hitTestableObject)
        {
            if (hitTestableObject is not null)
            {
                if (hitTestableObject is DrawingObject3D obj)
                {
                    if (obj is DrawingGeometry3D geometry)
                    {
                        geometry.Deselect();
                        CadManager3D.UpdateVerticesIsSelected(geometry, false);
                        _selectedHitTestableObjects.Remove(geometry);
                    }
                    if (obj is DrawingBlock3D block3D)
                    {
                        block3D.Deselect();
                        CadManager3D.UpdateVerticesIsSelected(block3D, false);
                        _selectedHitTestableObjects.Remove(block3D);
                    }
                    if (obj is DrawingMtext3D drawingMtext)
                    {
                        drawingMtext.Deselect();
                        CadManager3D.UpdateVerticesIsSelected(drawingMtext, false);
                        _selectedHitTestableObjects.Remove(drawingMtext);
                    }
                }
                if (hitTestableObject is CogoPoint dxfPoint)
                {
                    dxfPoint.Deselect();
                    SelectedCogoPoints.Remove(dxfPoint);
                }
            }
        }
        private void ResetSelectedObjects()
        {
            var listCopy = _selectedHitTestableObjects.ToList();
            foreach (var obj in listCopy)
            {
                DeselectObject(obj);
            }
            _selectedHitTestableObjects.Clear();

            var sigPointsCopy = SelectedSignificantPoints.ToList();
            foreach (var obj in sigPointsCopy)
            {
                DeselectObject(obj);
            }
            SelectedSignificantPoints.Clear();

            foreach (var point in SelectedCogoPoints)
            {
                point.Deselect();
            }
            SelectedCogoPoints.Clear();

            _lineVerticesDirty = _lineGlowVerticesDirty = _textVerticesDirty = true;
        }

        private (bool linesDirty, bool textsDirty, bool circlesDirty, bool pointTextsDirty, bool sigPointsDirty) GetVerticesDirtyBools
            (List<HitTestableObject> hitTestableObjects)
        {
            (bool linesDirty, bool textsDirty, bool circlesDirty, bool pointTextsDirty, bool sigPointsDirty) = (false, false, false, false, false);

            foreach (var hitTestableObject in hitTestableObjects)
            {
                if (hitTestableObject is not null)
                {
                    if (hitTestableObject is DrawingGeometry3D)
                    {
                        linesDirty = true;
                    }
                    if (hitTestableObject is DrawingBlock3D)
                    {
                        linesDirty = true;
                        textsDirty = true;
                    }
                    if (hitTestableObject is DrawingText3D)
                    {
                        textsDirty = true;
                    }
                    if (hitTestableObject is CogoPoint)
                    {
                        circlesDirty = true;
                        pointTextsDirty = true;
                        textsDirty = true;
                    }
                    if (hitTestableObject is HitTestablePoint)
                    {
                        sigPointsDirty = true;
                    }
                }
            }

            return (linesDirty, textsDirty, circlesDirty, pointTextsDirty, sigPointsDirty);
        }

        private void ClearDxf()
        {
            Camera.ResetView(Matrix.Identity, CadManager3D.Extents);
            ResetSnappedObjects();
            _lineVerticesDirty = _textVerticesDirty = _lineGlowVerticesDirty = true;
        }

        private static void OnCadManager3DChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not D3dDxfControl control) { return; }

            if (e.OldValue is CadManager3D oldCadManager3D)
            {
                oldCadManager3D.PropertyChanged -= control.CadManager3D_PropertyChanged;
                oldCadManager3D.ZoomToExtentsRequested -= control.ZoomToExtents;
                oldCadManager3D.CogoPointManager.CogoPoints.CollectionChanged -= control.CogoPoints_CollectionChanged_Instance;
            }

            if (e.NewValue is CadManager3D newCadManager3D)
            {
                newCadManager3D.PropertyChanged += control.CadManager3D_PropertyChanged;
                newCadManager3D.ZoomToExtentsRequested += control.ZoomToExtents;
                newCadManager3D.CogoPointManager.CogoPoints.CollectionChanged += control.CogoPoints_CollectionChanged_Instance;
            }
        }

        private void CogoPoints_CollectionChanged_Instance(object? sender, NotifyCollectionChangedEventArgs e)
        {
            CogoPoints = CadManager3D?.CogoPointManager?.CogoPoints;
        }

        private void CadManager3D_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CadManager3D.DxfNeedsReload))
            {
                if (CadManager3D.DxfNeedsReload)
                {
                    DxfNeedsReload = true;
                }
            }
            if (e.PropertyName == nameof(CadManager3D.LineVerticesDirty))
            {
                if (CadManager3D.LineVerticesDirty)
                {
                    _lineVerticesDirty = true;
                }
            }
            if (e.PropertyName == nameof(CadManager3D.TextVerticesDirty))
            {
                if (CadManager3D.TextVerticesDirty)
                {
                    _textVerticesDirty = true;
                }
            }
            if (e.PropertyName == nameof(CadManager3D.HitTestableObjectTreeDirty))
            {
                if (CadManager3D.HitTestableObjectTreeDirty)
                {
                    HitTestableObjectTreeDirty = true;
                }
            }
            if (e.PropertyName == nameof(CadManager3D.DxfLoaded) && !CadManager3D.DxfLoaded)
            {
                ClearDxf();
            }
            if (e.PropertyName == nameof(CadManager3D.SnapSelectionMode))
            {
                ResetSelectedObjects();
                ResetSnappedObjects();
                _currentSnapHitTestIndex = 0;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_attachedWindow != null) { _attachedWindow.KeyUp -= Window_KeyUp; }

                    // Dispose text-related resources
                    _textVertexBuffer?.Dispose();
                    _textVertexBuffer = null;

                    _textSettingsBuffer?.Dispose();
                    _textSettingsBuffer = null;

                    _textGlowVertexBuffer?.Dispose();
                    _textGlowVertexBuffer = null;

                    _textGlowSettingsBuffer?.Dispose();
                    _textGlowSettingsBuffer = null;

                    _textVertexShader?.Dispose();
                    _textVertexShader = null;

                    _textPixelShader?.Dispose();
                    _textPixelShader = null;

                    _textGlowVertexShader?.Dispose();
                    _textGlowVertexShader = null;

                    _textGlowPixelShader?.Dispose();
                    _textGlowPixelShader = null;

                    _textInputLayout?.Dispose();
                    _textInputLayout = null;

                    _lineVertexBuffer?.Dispose();
                    _lineVertexBuffer = null;

                    _lineSettingsBuffer?.Dispose();
                    _lineSettingsBuffer = null;

                    _lineGlowVertexBuffer?.Dispose();
                    _lineGlowVertexBuffer = null;

                    _lineGlowSettingsBuffer?.Dispose();
                    _lineGlowSettingsBuffer = null;

                    _lineVertexShader?.Dispose();
                    _lineVertexShader = null;

                    _linePixelShader?.Dispose();
                    _linePixelShader = null;

                    _lineGlowVertexShader?.Dispose();
                    _lineGlowVertexShader = null;

                    _lineGlowPixelShader?.Dispose();
                    _lineGlowPixelShader = null;

                    _lineGlowGeometryShader?.Dispose();
                    _lineGlowGeometryShader = null;

                    _lineInputLayout?.Dispose();
                    _lineInputLayout = null;

                    _transformationBuffer?.Dispose();
                    _transformationBuffer = null;

                    _hitTestCancellationTokenSource?.Dispose();
                    _hitTestCancellationTokenSource = null;
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
