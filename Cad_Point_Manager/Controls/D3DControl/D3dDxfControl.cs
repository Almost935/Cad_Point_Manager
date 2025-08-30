using Cad_Point_Manager.Common.Collections;
using Cad_Point_Manager.Controls.D3DControl.Buffers;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.DrawingObjects3D;
using Cad_Point_Manager.Models.HitTesting;
using Cad_Point_Manager.Models.PointRendering;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using System.Collections.Generic;
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
using System.Windows.Threading;

using BlendOperation = SharpDX.Direct3D11.BlendOperation;
using Buffer = SharpDX.Direct3D11.Buffer;
using FillMode = SharpDX.Direct3D11.FillMode;
using InputElement = SharpDX.Direct3D11.InputElement;
using Matrix = SharpDX.Matrix;
using Point = System.Windows.Point;
using RectangleGeometry = System.Windows.Media.RectangleGeometry;

namespace Cad_Point_Manager.Controls.D3DControl
{
    public class D3dDxfControl : Direct3DControl, INotifyPropertyChanged, IDisposable
    {
        #region Fields
        private bool _dxfDirty = true;
        private bool _interactiveDirty = true;
        private Buffer _transformationBuffer;
        private Buffer _viewportBuffer;

        private Point _pointerCoords;
        private Vector2 _dxfCoords;
        private string _dxfCoordsString = $"X: {0:F3}   Y: {0:F3}";
        private Matrix _dxfInitialMatrix = Matrix.Identity;
        private bool _clipSet = false;
        private bool _isMouseInside;
        private Window _attachedWindow;
        private volatile bool _suspendHitTesting;

        // Drag selection state for cogo points
        private readonly object _dragCogoLock = new();
        private HashSet<CogoPoint> _dragCogoCurrent = new();  // last-applied set

        // Drag Selection Fields
        private bool _isDragging = false;
        private Point _dragStart;
        private Rect _dragRect = new(0, 0, 0, 0);
        private Vector _dxfDragRectTranslate = new();
        private System.Windows.Media.Matrix _currentlyAppliedDragRectMatrix = new();

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
        private Buffer _lineGlowSettingsBuffer;
        private VertexShader _lineGlowVertexShader;
        private PixelShader _lineGlowPixelShader;
        private GeometryShader _lineGlowGeometryShader;

        // Text shader related fields
        private ResizableBuffer<TextVertex> _textVertexBuffer;
        private Buffer _textSettingsBuffer;
        private int _textVertexCount;
        private VertexShader _textVertexShader;
        private PixelShader _textPixelShader;
        private InputLayout _textInputLayout;
        private bool _textShaderLoaded = false;
        private bool _textVerticesDirty = false;

        // DxfPoint text shader related fields
        private ResizableBuffer<TextVertex> _pointTextVertexBuffer;
        private ResizableBuffer<GlyphInstance> _glyphInstanceVB;
        private Buffer _pointTextSettingsBuffer;
        private VertexShader _pointTextVertexShader;
        private PixelShader _pointTextPixelShader;
        private InputLayout _pointTextInputLayout;
        private int _pointTextVertexCount;
        private bool _pointTextShadersLoaded = false;
        private bool _pointTextVerticesDirty = false;

        // Point glyph rendering
        private ResizableBuffer<GlyphInstance> _glyphInstanceBuffer;
        private InputLayout _glyphLayout;
        private VertexShader _glyphVS;
        private PixelShader _glyphPS;
        private bool _glyphShadersLoaded;

        // Text glow shader related fields
        private ResizableBuffer<TextVertex> _textGlowVertexBuffer;
        private Buffer _textGlowSettingsBuffer;
        private List<TextVertex> _textGlowVertices = [];
        private VertexShader _textGlowVertexShader;
        private PixelShader _textGlowPixelShader;
        private GeometryShader _textGlowGeometryShader;
        private bool _textGlowVerticesDirty = false;

        // Circle shader related fields
        private ResizableBuffer<CircleVertex> _circleVertexBuffer;
        private Buffer _circleSettingsBuffer;
        private InputLayout _circleInputLayout;
        private VertexShader _circleVertexShader;
        private PixelShader _circlePixelShader;
        private GeometryShader _circleGeometryShader;
        private int _circleVertexCount;
        private bool _circleShadersLoaded = false;
        private bool _circleVerticesDirty = false;

        // Circle glow shader related fields
        private ResizableBuffer<CircleVertex> _circleGlowVertexBuffer;
        private Buffer _circleGlowSettingsBuffer;
        private List<CircleVertex> _circleGlowVertices = [];
        private VertexShader _circleGlowVertexShader;
        private PixelShader _circleGlowPixelShader;
        private GeometryShader _circleGlowGeometryShader;
        private bool _circleGlowShadersLoaded = false;
        private bool _circleGlowVerticesDirty = false;

        // Drag rectangle shader
        private ResizableBuffer<OverlayVertex> _dragFillBuffer;
        private int _dragFillVertexCount;
        private ResizableBuffer<LineVertex> _dragOutlineBuffer;
        private int _dragOutlineVertexCount;

        private VertexShader _overlayVS;
        private PixelShader _overlayPS;
        private InputLayout _overlayLayout;
        private bool _overlayShaderLoaded;

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
        private Rect _lastQueriedDxfRect = Rect.Empty;
        private CancellationTokenSource _hitTestCancellationTokenSource;
        private int _currentSnapHitTestIndex = 0;
        private int _lastSnapHitTestIndex = 0;
        private const int _maxSelectableObjects = 5;
        private List<(double distance, HitTestablePoint hitTestablePoint)> _nearestHitTestablePoints = [];
        private List<(double distance, DrawingGeometry3D geometry)> _nearestHitTestableGeometries = [];
        private List<(double distance, CogoPoint point)> _nearestHitTestableCogoPoints = [];
        private readonly HashSet<HitTestableObject> _snappedHitTestableObjects = [];
        #endregion

        #region Properties 
        /// <summary>
        /// Determines if the view matrix needs to be reloaded. Occurs when the Dxf file is changed.
        /// </summary>
        private bool DxfNeedsReload { get; set; }

        /// <summary>
        /// Determines if the Direct3D control needs to be redrawn. Occurs when the camera is panned or zoomed.
        /// </summary>
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

        // Drag Properties
        public bool IsDragging
        {
            get => _isDragging;
            set
            {
                _isDragging = value;
                OnPropertyChanged(nameof(IsDragging));
            }
        }
        public Point DragStart
        {
            get => _dragStart;
            set
            {
                _dragStart = value;
                OnPropertyChanged(nameof(DragStart));
            }
        }
        public Rect DragRect
        {

            get => _dragRect;
            set
            {
                _dragRect = value;
                OnPropertyChanged();
            }
        }
        public Vector DxfDragRectTranslate
        {
            get => _dxfDragRectTranslate;
            set
            {
                _dxfDragRectTranslate = value;
                OnPropertyChanged(nameof(DxfDragRectTranslate));
            }
        }
        public System.Windows.Media.Matrix CurrentlyAppliedDragRectMatrix
        {
            get => _currentlyAppliedDragRectMatrix;
            set
            {
                _currentlyAppliedDragRectMatrix = value;
                OnPropertyChanged(nameof(CurrentlyAppliedDragRectMatrix));
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
            new PropertyMetadata(new Camera(new ViewportF(), 1.15f, new Rect(0, 0, 0, 0))));

        public static readonly DependencyProperty PointGroupsProperty =
            DependencyProperty.Register(
                nameof(PointGroups),
                typeof(BatchableObservableCollection<KeyValuePair<string, PointGroup>>),
                typeof(D3dDxfControl),
                new FrameworkPropertyMetadata(new BatchableObservableCollection<KeyValuePair<string, PointGroup>>()));
        public BatchableObservableCollection<KeyValuePair<string, PointGroup>> PointGroups
        {
            get => (BatchableObservableCollection<KeyValuePair<string, PointGroup>>)GetValue(PointGroupsProperty);
            set => SetValue(PointGroupsProperty, value);
        }

        public static readonly DependencyProperty CogoPointsProperty =
            DependencyProperty.Register(
                nameof(CogoPoints),
                typeof(BatchableObservableCollection<CogoPoint>),
                typeof(D3dDxfControl),
                new FrameworkPropertyMetadata(new BatchableObservableCollection<CogoPoint>(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public BatchableObservableCollection<CogoPoint> CogoPoints
        {
            get => (BatchableObservableCollection<CogoPoint>)GetValue(CogoPointsProperty);
            set => SetValue(CogoPointsProperty, value);
        }

        public static readonly DependencyProperty SelectedCogoPointsProperty =
            DependencyProperty.Register(
                nameof(SelectedCogoPoints),
                typeof(BatchableObservableCollection<CogoPoint>),
                typeof(D3dDxfControl),
                new FrameworkPropertyMetadata(new BatchableObservableCollection<CogoPoint>(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public BatchableObservableCollection<CogoPoint> SelectedCogoPoints
        {
            get => (BatchableObservableCollection<CogoPoint>)GetValue(SelectedCogoPointsProperty);
            set => SetValue(SelectedCogoPointsProperty, value);
        }

        public static readonly DependencyProperty SnappedHitTestablePointProperty =
        DependencyProperty.Register(
            nameof(SnappedHitTestablePoint),
            typeof(HitTestablePoint),
            typeof(D3dDxfControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public HitTestablePoint SnappedHitTestablePoint
        {
            get => (HitTestablePoint)GetValue(SnappedHitTestablePointProperty);
            set => SetValue(SnappedHitTestablePointProperty, value);
        }

        public static readonly DependencyProperty SelectedHitTestablePointsProperty =
            DependencyProperty.Register(
                nameof(SelectedHitTestablePoints),
                typeof(ObservableCollection<HitTestablePoint>),
                typeof(D3dDxfControl),
                new FrameworkPropertyMetadata(new ObservableCollection<HitTestablePoint>(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public ObservableCollection<HitTestablePoint> SelectedHitTestablePoints
        {
            get => (ObservableCollection<HitTestablePoint>)GetValue(SelectedHitTestablePointsProperty);
            set => SetValue(SelectedHitTestablePointsProperty, value);
        }

        public static readonly DependencyProperty SelectedGeometriesProperty =
            DependencyProperty.Register(
                nameof(SelectedGeometries),
                typeof(BatchableObservableCollection<DrawingGeometry3D>),
                typeof(D3dDxfControl),
                new FrameworkPropertyMetadata(new BatchableObservableCollection<DrawingGeometry3D>(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public BatchableObservableCollection<DrawingGeometry3D> SelectedGeometries
        {
            get => (BatchableObservableCollection<DrawingGeometry3D>)GetValue(SelectedGeometriesProperty);
            set => SetValue(SelectedGeometriesProperty, value);
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

            if (_lineVerticesDirty) { UpdateLineVertices(); }
            if (_textVerticesDirty) { UpdateTextVertices(); }
            if (_textGlowVerticesDirty) { UpdateTextGlowVertices(); }
            if (_pointTextVerticesDirty) { UpdatePointTextVertices(); }
            if (_circleVerticesDirty) { UpdateCircleVertices(); }
            if (_circleGlowVerticesDirty) { UpdateCircleGlowVertices(); }
            if (HitTestableObjectTreeDirty) { LoadHitTestableObjectTree(); }

            if (!_lineShaderLoaded) { InitializeLineShader(); }
            if (!_textShaderLoaded) { InitializeTextShader(); }
            if (!_overlayShaderLoaded) { InitializeOverlayShader(); }
            if (!_pointTextShadersLoaded) { InitializePointTextShader(); }
            if (!_glyphShadersLoaded) { InitializeGlyphShader(); }
            if (!_circleShadersLoaded) { InitializeCircleShader(); }
            if (!_circleGlowShadersLoaded) { InitializeCircleGlowShader(); }
            if (_dragFillBuffer == null) _dragFillBuffer = new(_d3dResCache.Device, 6);
            if (_dragOutlineBuffer == null) _dragOutlineBuffer = new(_d3dResCache.Device, 8);

            if (!ConstantBuffersInitialized) { InitializeConstantBuffers(); }
            if (ConstantBuffersDirty) { UpdateConstantBuffers(); }

            if (!_hitTestIsRunning)
            {
                _hitTestIsRunning = true;
                _hittestTask = Task.Run(() => RunHitTestingAsync());
            }


            var ctx = _d3dResCache.DeviceContext;

            // 1) Rebuild scene only when needed
            if (_dxfDirty)
            {
                DrawDxf(ctx);
                _dxfDirty = false;
                _interactiveDirty = true; // force one compose after scene refresh
            }

            // 2) Compose: copy scene -> interactive, draw overlay on top, present
            if (_interactiveDirty)
            {
                // Copy scene into interactive texture
                ctx.CopyResource(_d3dResCache.DxfTexture, _d3dResCache.InteractionTexture);

                // Draw overlay on the interactive texture (no clear!)
                if (IsDragging && _dragFillVertexCount > 0)
                {
                    ctx.OutputMerger.SetRenderTargets(_d3dResCache.InteractiveRenderTargetView);
                    DrawDragOverlay(ctx);
                }

                // Present the composed image to the shared WPF texture
                ctx.CopyResource(_d3dResCache.InteractionTexture, _d3dResCache.Texture2D);
                _interactiveDirty = false;
            }
        }

        private void DrawDxf(SharpDX.Direct3D11.DeviceContext context)
        {
            context.OutputMerger.SetRenderTargets(_d3dResCache.DxfRenderTargetView);
            context.ClearRenderTargetView(_d3dResCache.DxfRenderTargetView, new RawColor4(1, 1, 1, 1));

            DrawTextGlowsWithShader(context);
            DrawLinesWithShader(context);
            DrawTextWithShader(context);
            DrawPointTextWithShader(context);
            DrawCirclesWithShader(context);
            DrawCircleGlowsWithShader(context);
        }

        private void DrawLinesWithShader(SharpDX.Direct3D11.DeviceContext context)
        {
            //Stopwatch stopwatch = Stopwatch.StartNew();
            if (_lineVertexBuffer is null) { return; }

            context.VertexShader.Set(_lineGlowVertexShader);
            context.GeometryShader.Set(_lineGlowGeometryShader);
            context.PixelShader.Set(_lineGlowPixelShader);
            context.InputAssembler.InputLayout = _lineInputLayout;
            context.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            context.GeometryShader.SetConstantBuffer(0, _transformationBuffer);
            context.GeometryShader.SetConstantBuffer(1, _lineGlowSettingsBuffer);
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(
                _lineVertexBuffer.Buffer, _lineVertexBuffer.Stride, 0));
            context.Draw(_lineVertexCount, 0);
            context.GeometryShader.Set(null);

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
        private void DrawTextWithShader(SharpDX.Direct3D11.DeviceContext context)
        {
            //Stopwatch stopwatch = Stopwatch.StartNew();

            if (_textVertexBuffer is null) { return; }

            context.VertexShader.Set(_textVertexShader);
            context.PixelShader.Set(_textPixelShader);
            context.InputAssembler.InputLayout = _textInputLayout;
            context.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            context.VertexShader.SetConstantBuffer(1, _pointTextSettingsBuffer);
            context.VertexShader.SetConstantBuffer(2, _viewportBuffer);
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(
                 _textVertexBuffer.Buffer, _textVertexBuffer.Stride, 0));

            context.Draw(_textVertexCount, 0);

            //stopwatch.Stop();
            //Debug.WriteLine($"DrawTextWithShader Time: {stopwatch.ElapsedMilliseconds} ms");
        }
        private void DrawPointTextWithShader(SharpDX.Direct3D11.DeviceContext context)
        {
            //Stopwatch stopwatch = Stopwatch.StartNew();

            if (_pointTextVertexBuffer is null || _pointTextVertexCount == 0) { return; }

            context.VertexShader.Set(_pointTextVertexShader);
            context.PixelShader.Set(_pointTextPixelShader);
            context.InputAssembler.InputLayout = _pointTextInputLayout;
            context.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            context.VertexShader.SetConstantBuffer(1, _pointTextSettingsBuffer);
            context.VertexShader.SetConstantBuffer(2, _viewportBuffer);
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(
                 _pointTextVertexBuffer.Buffer, _pointTextVertexBuffer.Stride, 0));

            context.Draw(_pointTextVertexCount, 0);

            //stopwatch.Stop();
            //Debug.WriteLine($"DrawTextWithShader Time: {stopwatch.ElapsedMilliseconds} ms");
        }
        private void DrawTextGlowsWithShader(SharpDX.Direct3D11.DeviceContext context)
        {
            //Stopwatch stopwatch = Stopwatch.StartNew();

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
        private void DrawCirclesWithShader(SharpDX.Direct3D11.DeviceContext context)
        {
            //Stopwatch stopwatch = Stopwatch.StartNew();

            if (_circleVertexBuffer == null || _circleVertexCount == 0)
            {
                return;
            }

            // Set shaders
            context.VertexShader.Set(_circleVertexShader);
            context.GeometryShader.Set(_circleGeometryShader);
            context.PixelShader.Set(_circlePixelShader);
            context.InputAssembler.InputLayout = _circleInputLayout;
            context.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            context.GeometryShader.SetConstantBuffer(0, _transformationBuffer);
            context.GeometryShader.SetConstantBuffer(1, _circleSettingsBuffer);
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(
                _circleVertexBuffer.Buffer, _circleVertexBuffer.Stride, 0));

            context.Draw(_circleVertexCount, 0);

            context.GeometryShader.Set(null);

            //stopwatch.Stop();
            //Debug.WriteLine($"DrawCirclesWithShader Time: {stopwatch.ElapsedMilliseconds} ms");
        }
        private void DrawCircleGlowsWithShader(SharpDX.Direct3D11.DeviceContext context)
        {
            //Stopwatch stopwatch = Stopwatch.StartNew();

            if (_circleGlowVertexBuffer == null || _circleGlowVertices.Count == 0)
            {
                return;
            }

            // Set shaders
            context.VertexShader.Set(_circleGlowVertexShader);
            context.GeometryShader.Set(_circleGlowGeometryShader);
            context.PixelShader.Set(_circleGlowPixelShader);
            context.InputAssembler.InputLayout = _circleInputLayout;
            context.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            context.GeometryShader.SetConstantBuffer(0, _transformationBuffer);
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(
                _circleGlowVertexBuffer.Buffer, _circleGlowVertexBuffer.Stride, 0));

            context.Draw(_circleGlowVertices.Count, 0);

            context.GeometryShader.Set(null);

            //stopwatch.Stop();
            //Debug.WriteLine($"DrawCirclesWithShader Time: {stopwatch.ElapsedMilliseconds} ms");
        }
        private void DrawDragOverlay(SharpDX.Direct3D11.DeviceContext context)
        {
            // --- fill (triangles) ---
            context.VertexShader.Set(_overlayVS);
            context.PixelShader.Set(_overlayPS);
            context.InputAssembler.InputLayout = _overlayLayout;
            context.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_dragFillBuffer.Buffer, _dragFillBuffer.Stride, 0));
            context.Draw(_dragFillVertexCount, 0);

            // --- outline (lines) ---
            if (_dragOutlineVertexCount > 0)
            {
                context.VertexShader.Set(_lineVertexShader);
                context.PixelShader.Set(_linePixelShader);
                context.InputAssembler.InputLayout = _lineInputLayout;
                context.VertexShader.SetConstantBuffer(0, _transformationBuffer);
                context.VertexShader.SetConstantBuffer(1, _lineSettingsBuffer);
                context.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
                context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_dragOutlineBuffer.Buffer, _dragOutlineBuffer.Stride, 0));
                context.Draw(_dragOutlineVertexCount, 0);
            }
        }

        private void UpdateLineVertices()
        {
            if (_lineVertexBuffer is null || CadManager3D is null) { return; }

            var context = _d3dResCache.DeviceContext;
            var vertexSpan = CadManager3D.UpdateLineVerticesList();
            _lineVertexBuffer.Update(context, vertexSpan);
            _lineVertexCount = vertexSpan.Length;

            _lineVerticesDirty = false;
            _dxfDirty = true;
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
            _dxfDirty = true;
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
            _dxfDirty = true;
        }
        private void UpdatePointTextVertices()
        {
            if (_pointTextVertexBuffer is null || CadManager3D is null)
            {
                _pointTextVerticesDirty = false;
                return;
            }

            var context = _d3dResCache.DeviceContext;
            var vertexSpan = CadManager3D.UpdatePointTextVertices(_d3dResCache);
            _pointTextVertexBuffer.Update(context, vertexSpan);
            _pointTextVertexCount = vertexSpan.Length;

            _pointTextVerticesDirty = false;
            _dxfDirty = true;
        }
        private void UpdateCircleVertices()
        {
            if (_circleVertexBuffer is null) { return; }

            var context = _d3dResCache.DeviceContext;
            var vertexSpan = CadManager3D.UpdateCircleVerticesList();
            _circleVertexBuffer.Update(context, vertexSpan);
            _circleVertexCount = vertexSpan.Length;

            _circleVerticesDirty = false;
            _dxfDirty = true;
        }
        private void UpdateCircleGlowVertices()
        {
            if (_circleGlowVertexBuffer is null) { return; }

            var context = _d3dResCache.DeviceContext;
            _circleGlowVertexBuffer.Update(context, _circleGlowVertices.ToArray());

            _circleGlowVerticesDirty = false;
            _dxfDirty = true;
        }
        private void UpdateDragOverlayVertices(Rect r)
        {
            if (r.IsEmpty || r.Width <= 0 || r.Height <= 0)
            {
                _dragFillVertexCount = 0;
                _dragOutlineVertexCount = 0;

                return;
            }

            // world-space coords (z=0)
            var lt = new Vector3((float)r.Left, (float)r.Top, 1);
            var rt = new Vector3((float)r.Right, (float)r.Top, 1);
            var rb = new Vector3((float)r.Right, (float)r.Bottom, 1);
            var lb = new Vector3((float)r.Left, (float)r.Bottom, 1);

            //Debug.WriteLine($"{lt} {rt} {rb} {lb}");

            // fill color (ARGB #3300FFFF like your XAML)
            var fill = new Vector4(0f, 1f, 1f, 0.2f); // DeepSkyBlue-ish with alpha

            var fillVerts = new OverlayVertex[6]
            {
                new() { Position = lt, Color = fill },
                new() { Position = lb, Color = fill },
                new() { Position = rb, Color = fill },
                new() { Position = lt, Color = fill },
                new() { Position = rb, Color = fill },
                new() { Position = rt, Color = fill },
            };

            _dragFillBuffer.Update(_d3dResCache.DeviceContext, fillVerts);
            _dragFillVertexCount = 6;

            // outline as 4 segments (1px—no GS "glow" pass)
            var c = new Vector4(0f, 0.749f, 1f, 1f); // DeepSkyBlue
            float one = 1f, zero = 0f;

            var outline = new LineVertex[8]
            {
                new() { Position = lt, Color = c, IsVisible = one, IsMouseOver = zero, IsSelected = zero },
                new() { Position = rt, Color = c, IsVisible = one, IsMouseOver = zero, IsSelected = zero },

                new() { Position = rt, Color = c, IsVisible = one, IsMouseOver = zero, IsSelected = zero },
                new() { Position = rb, Color = c, IsVisible = one, IsMouseOver = zero, IsSelected = zero },

                new() { Position = rb, Color = c, IsVisible = one, IsMouseOver = zero, IsSelected = zero },
                new() { Position = lb, Color = c, IsVisible = one, IsMouseOver = zero, IsSelected = zero },

                new() { Position = lb, Color = c, IsVisible = one, IsMouseOver = zero, IsSelected = zero },
                new() { Position = lt, Color = c, IsVisible = one, IsMouseOver = zero, IsSelected = zero },
            };

            _dragOutlineBuffer.Update(_d3dResCache.DeviceContext, outline);
            _dragOutlineVertexCount = 8;

            _interactiveDirty = true;
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
        private void InitializePointTextShader()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            while (Path.GetFileName(path) != "Cad_Point_Manager")
            {
                path = Path.GetDirectoryName(path);
                if (path == null)
                    throw new DirectoryNotFoundException("The 'Cad_Point_Manager' directory could not be found in the path.");
            }

            string shaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\TextShader.hlsl");

            // Main shaders
            var textVSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "VSMain", "vs_5_0");
            _pointTextVertexShader = new VertexShader(_d3dResCache.Device, textVSBytecode);

            var textPSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "PSMain", "ps_5_0");
            _pointTextPixelShader = new PixelShader(_d3dResCache.Device, textPSBytecode);

            // Layout
            _pointTextInputLayout = new InputLayout(
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

            _textInputLayout = new InputLayout(
                _d3dResCache.Device,
                ShaderSignature.GetInputSignature(textVSBytecode),
                new[] {
                    // slot 0: glyph base vertex (DU)
                    new InputElement("POSITION", 0, Format.R32G32_Float, 0, 0, InputClassification.PerVertexData, 0),

                    // slot 1: instance data
                    new InputElement("ORIGIN",       0, Format.R32G32_Float,   0, 1, InputClassification.PerInstanceData, 1),
                    new InputElement("DUTOWORLD",    0, Format.R32_Float,      8, 1, InputClassification.PerInstanceData, 1),
                    new InputElement("YSIGN",        0, Format.R32_Float,     12, 1, InputClassification.PerInstanceData, 1),
                    new InputElement("PENDU",        0, Format.R32_Float,     16, 1, InputClassification.PerInstanceData, 1),
                    new InputElement("COLOR",        0, Format.R32G32B32A32_Float, 20,1, InputClassification.PerInstanceData, 1),
                    new InputElement("ISVISIBLE",    0, Format.R32_Float,     36, 1, InputClassification.PerInstanceData, 1),
                    new InputElement("ISMOUSEOVER",  0, Format.R32_Float,     40, 1, InputClassification.PerInstanceData, 1),
                    new InputElement("ISSELECTED",   0, Format.R32_Float,     44, 1, InputClassification.PerInstanceData, 1),
                });

            _pointTextShadersLoaded = true;
        }
        private void InitializeGlyphShader()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            while (Path.GetFileName(path) != "Cad_Point_Manager")
            {
                path = Path.GetDirectoryName(path);
                if (path == null)
                    throw new DirectoryNotFoundException("The 'Cad_Point_Manager' directory could not be found in the path.");
            }

            string shaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\GlyphMeshShader.hlsl");
            var vsb = ShaderBytecode.CompileFromFile(shaderPath, "VSMain", "vs_5_0");
            var psb = ShaderBytecode.CompileFromFile(shaderPath, "PSMain", "ps_5_0");
            _glyphVS = new VertexShader(_d3dResCache.Device, vsb);
            _glyphPS = new PixelShader(_d3dResCache.Device, psb);

            _glyphLayout = new InputLayout(_d3dResCache.Device, ShaderSignature.GetInputSignature(vsb),
                new[]
                {
                    new InputElement("POSITION",      0, Format.R32G32_Float,          0, 0, InputClassification.PerVertexData,   0),
                    new InputElement("GLYPH_ORIGIN",  0, Format.R32G32_Float,          0, 1, InputClassification.PerInstanceData, 1),
                    new InputElement("GLYPH_SCALE",   0, Format.R32_Float,              8, 1, InputClassification.PerInstanceData, 1),
                    new InputElement("GLYPH_PEN",     0, Format.R32_Float,             12, 1, InputClassification.PerInstanceData, 1),
                    new InputElement("COLOR",         0, Format.R32G32B32A32_Float,    16, 1, InputClassification.PerInstanceData, 1),
                    new InputElement("ISVISIBLE",     0, Format.R32_Float,             32, 1, InputClassification.PerInstanceData, 1),
                    new InputElement("ISMOUSEOVER",   0, Format.R32_Float,             36, 1, InputClassification.PerInstanceData, 1),
                    new InputElement("ISSELECTED",    0, Format.R32_Float,             40, 1, InputClassification.PerInstanceData, 1),
                    new InputElement("YSIGN",         0, Format.R32_Float,             44, 1, InputClassification.PerInstanceData, 1),
                });

            _glyphInstanceBuffer = new ResizableBuffer<GlyphInstance>(_d3dResCache.Device, initialCapacity: 256);
            _glyphShadersLoaded = true;
        }
        private void InitializeCircleShader()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            while (Path.GetFileName(path) != "Cad_Point_Manager")
            {
                path = Path.GetDirectoryName(path);
                if (path == null)
                    throw new DirectoryNotFoundException("The 'Cad_Point_Manager' directory could not be found in the path.");
            }

            string shaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\CircleShader.hlsl");

            // Main shaders
            var circleVSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "VSMain", "vs_5_0");
            _circleVertexShader = new VertexShader(_d3dResCache.Device, circleVSBytecode);

            var circlePSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "PSMain", "ps_5_0");
            _circlePixelShader = new PixelShader(_d3dResCache.Device, circlePSBytecode);

            var circleGSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "GSMain", "gs_5_0");
            _circleGeometryShader = new GeometryShader(_d3dResCache.Device, circleGSBytecode);

            _circleInputLayout = new(
                _d3dResCache.Device,
                ShaderSignature.GetInputSignature(circleVSBytecode),
                new[]
                {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                    new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 12, 0),
                    new InputElement("RADIUS", 0, Format.R32_Float, 28, 0),
                    new InputElement("ISVISIBLE", 0, Format.R32_Float, 32, 0),
                    new InputElement("ISMOUSEOVER", 0, Format.R32_Float, 36, 0),
                    new InputElement("ISSELECTED", 0, Format.R32_Float, 40, 0),
                 });

            _circleShadersLoaded = true;
        }
        private void InitializeCircleGlowShader()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            while (Path.GetFileName(path) != "Cad_Point_Manager")
            {
                path = Path.GetDirectoryName(path);
                if (path == null)
                    throw new DirectoryNotFoundException("The 'Cad_Point_Manager' directory could not be found in the path.");
            }

            string shaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\CircleGlowShader.hlsl");

            // Main shaders
            var circleVSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "VSMain", "vs_5_0");
            _circleGlowVertexShader = new VertexShader(_d3dResCache.Device, circleVSBytecode);

            var circlePSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "PSMain", "ps_5_0");
            _circleGlowPixelShader = new PixelShader(_d3dResCache.Device, circlePSBytecode);

            var circleGSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "GSMain", "gs_5_0");
            _circleGlowGeometryShader = new GeometryShader(_d3dResCache.Device, circleGSBytecode);

            _circleInputLayout = new(
                _d3dResCache.Device,
                ShaderSignature.GetInputSignature(circleVSBytecode),
                new[]
                {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                    new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 12, 0),
                    new InputElement("RADIUS", 0, Format.R32_Float, 28, 0),
                    new InputElement("ISVISIBLE", 0, Format.R32_Float, 32, 0),
                    new InputElement("ISMOUSEOVER", 0, Format.R32_Float, 36, 0),
                    new InputElement("ISSELECTED", 0, Format.R32_Float, 40, 0),
                 });

            _circleGlowShadersLoaded = true;
        }
        private void InitializeOverlayShader()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            while (Path.GetFileName(path) != "Cad_Point_Manager")
            {
                path = Path.GetDirectoryName(path);
                if (path == null) throw new DirectoryNotFoundException("Cad_Point_Manager not found");
            }

            string fx = Path.Combine(path, @"Controls\D3DControl\Shaders\OverlaySolid.hlsl");
            var vs = ShaderBytecode.CompileFromFile(fx, "VSMain", "vs_5_0");
            var ps = ShaderBytecode.CompileFromFile(fx, "PSMain", "ps_5_0");
            _overlayVS = new VertexShader(_d3dResCache.Device, vs);
            _overlayPS = new PixelShader(_d3dResCache.Device, ps);

            _overlayLayout = new InputLayout(
                _d3dResCache.Device,
                ShaderSignature.GetInputSignature(vs),
                new[] {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            new InputElement("COLOR",    0, Format.R32G32B32A32_Float, 12, 0),
                });

            _overlayShaderLoaded = true;
        }

        private void InitializeBuffers()
        {
            var device = _d3dResCache.Device;

            _lineVertexBuffer?.Dispose();
            _lineVertexBuffer = new(device, GlobalHelperProperties.InitialLineVertices);

            _textVertexBuffer?.Dispose();
            _textVertexBuffer = new(device, GlobalHelperProperties.InitialTextVertices);

            _textGlowVertexBuffer?.Dispose();
            _textGlowVertexBuffer = new(device, GlobalHelperProperties.InitialTextGlowVertices);

            _pointTextVertexBuffer?.Dispose();
            _pointTextVertexBuffer = new(device, GlobalHelperProperties.InitialTextVertices);

            _circleVertexBuffer?.Dispose();
            _circleVertexBuffer = new(device, GlobalHelperProperties.InitialCircleVertices);

            _circleGlowVertexBuffer?.Dispose();
            _circleGlowVertexBuffer = new(device, GlobalHelperProperties.InitialCircleGlowVertices);
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

            var viewportBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<ViewportBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _viewportBuffer = new Buffer(_d3dResCache.Device, viewportBufferDesc);

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

            var pointTextBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<TextSettingsBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _pointTextSettingsBuffer = new Buffer(_d3dResCache.Device, pointTextBufferDesc);

            var circleBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<CircleSettingsBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _circleSettingsBuffer = new Buffer(_d3dResCache.Device, circleBufferDesc);

            var circleGlowBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<CircleGlowSettingsBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _circleGlowSettingsBuffer = new Buffer(_d3dResCache.Device, circleGlowBufferDesc);

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

            var viewportBuffer = new ViewportBuffer
            {
                ViewportSize = new(Viewport.Width, Viewport.Height)
            };
            _d3dResCache.DeviceContext.UpdateSubresource(ref viewportBuffer, _viewportBuffer);

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

            var pointTextSettings = new TextSettingsBuffer
            {
                SelectedColor = GlobalHelperProperties.SelectedObjectColor,
                SelectedMouseOverColor = GlobalHelperProperties.SelectedMouseOverGlowColor
            };
            _d3dResCache.DeviceContext.UpdateSubresource(ref pointTextSettings, _pointTextSettingsBuffer);

            var circleSettings = new CircleSettingsBuffer
            {
                SelectedColor = GlobalHelperProperties.SelectedObjectColor,
                SelectedMouseOverColor = GlobalHelperProperties.SelectedMouseOverGlowColor
            };
            _d3dResCache.DeviceContext.UpdateSubresource(ref circleSettings, _circleSettingsBuffer);

            var circleGlowSettings = new CircleGlowSettingsBuffer
            {
                GlowOffset = GlobalHelperProperties.LineGlowPixelWidth * worldUnitsPerPixel,
                GlowTransparency = GlobalHelperProperties.LineGlowTransparency,
                SelectedColor = GlobalHelperProperties.SelectedObjectColor,
                SelectedMouseOverColor = GlobalHelperProperties.SelectedMouseOverGlowColor
            };
            _d3dResCache.DeviceContext.UpdateSubresource(ref circleGlowSettings, _circleGlowSettingsBuffer);


            ConstantBuffersDirty = false;
            _dxfDirty = true;
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
                    //CadManager3D.CogoPointManager.UpdateAllVisualTransforms(Camera.D2dMatrix.ToWindowsMatrix());
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
                if (IsDragging)
                {
                    if (_isPanning)
                    {
                        var translate = currentMousePos - _prevMousePos;
                        DragStart = new(DragStart.X + translate.X, DragStart.Y + translate.Y);
                    }
                    UpdateDragRect();
                }

                _isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

                if (e.MiddleButton == MouseButtonState.Pressed)
                {
                    Camera.Pan(currentMousePos, _prevMousePos);
                    //CadManager3D.CogoPointManager.UpdateAllVisualTransforms(Camera.D2dMatrix.ToWindowsMatrix());
                    ConstantBuffersDirty = true;
                    e.Handled = true;
                }
                _prevMousePos = currentMousePos;
            }
        }
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            int zoomStep;
            if (e.Delta > 0) { zoomStep = 1; }
            else { zoomStep = -1; }

            var matrix = CurrentlyAppliedDragRectMatrix;
            matrix.ScaleAt(Math.Pow(GlobalHelperProperties.ZoomFactor, zoomStep), Math.Pow(GlobalHelperProperties.ZoomFactor, zoomStep), _pointerCoords.X, _pointerCoords.Y);
            CurrentlyAppliedDragRectMatrix = matrix;
            UpdateDragRect();

            Camera.Zoom(zoomStep, new Vector2((float)_pointerCoords.X, (float)_pointerCoords.Y));
            //CadManager3D.CogoPointManager.UpdateAllVisualTransforms(Camera.D2dMatrix.ToWindowsMatrix());
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
                GetVerticesDirtyBools(_snappedHitTestableObjects.ToList());

            ResetSnappedObjects();

            if (linesDirty) { _lineVerticesDirty = linesDirty; }
            if (textsDirty) { _textVerticesDirty = textsDirty; }
            if (textsDirty) { _textGlowVerticesDirty = textsDirty; }
        }
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            _suspendHitTesting = true;
            IsDragging = false;
            DragRect = new(0, 0, 0, 0);
            _lastQueriedDxfRect = Rect.Empty;

            switch (CadManager3D.SnapSelectionMode)
            {
                case Common.Enums.SelectionMode.Geometries:
                    {
                        var newSel = new HashSet<DrawingGeometry3D>(_snappedHitTestableObjects.OfType<DrawingGeometry3D>());
                        var oldSel = new HashSet<DrawingGeometry3D>(SelectedGeometries);

                        foreach (var g in newSel.Except(oldSel))
                        {
                            g.MouseLeave();
                            g.Select();
                            CadManager3D.UpdateVerticesIsSelectedAndIsMouseOver(g, true, false);
                        }
                        SelectedGeometries.AddRange(newSel);

                        _lineVerticesDirty = true;
                        break;
                    }

                case Common.Enums.SelectionMode.CogoPoints:
                    {
                        var newSel = new HashSet<CogoPoint>(_snappedHitTestableObjects.OfType<CogoPoint>());
                        var oldSel = new HashSet<CogoPoint>(SelectedCogoPoints);

                        foreach (var p in oldSel.Except(newSel))
                        {
                            p.MouseLeave();
                            p.Deselect();
                        }
                        foreach (var p in newSel.Except(oldSel))
                        {
                            p.MouseLeave();
                            p.Select();
                        }
                        break;
                    }

                case Common.Enums.SelectionMode.Points:
                    {
                        if (SnappedHitTestablePoint is not null)
                        {
                            if (!SnappedHitTestablePoint.IsSelected) { SnappedHitTestablePoint.Select(); }
                            else { SnappedHitTestablePoint.Deselect(); }
                        }
                        break;
                    }
            }

            _snappedHitTestableObjects.Clear();
            _textGlowVertices.Clear();
            _textGlowVerticesDirty = true;

            _suspendHitTesting = false;
        }
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            _dragStart = DxfCoords.ToPoint();
            DragRect = new(0, 0, 0, 0);
            _dxfDragRectTranslate = new(0, 0);
            CurrentlyAppliedDragRectMatrix = new();
            IsDragging = true;
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
            if (e.Key == Key.Delete)
            {
                if (CadManager3D.SnapSelectionMode == Common.Enums.SelectionMode.CogoPoints &&
                    SelectedCogoPoints.Count > 0)
                {
                    foreach (var cogoPoint in SelectedCogoPoints.ToList())
                    {
                        CadManager3D.CogoPointManager.DeletePoint(cogoPoint);
                        SelectedCogoPoints.Remove(cogoPoint);
                    }
                    e.Handled = true;
                }
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

            _dxfDirty = true;
        }

        public void ZoomToExtents()
        {
            if (Camera is null) { return; }

            Camera.ResetView(_dxfInitialMatrix, CadManager3D.Extents);
            //CadManager3D.CogoPointManager.UpdateAllVisualTransforms(Camera.D2dMatrix.ToWindowsMatrix());
            ResetSnappedObjects();

            ConstantBuffersDirty = true;
        }
        public void UpdateDragRect()
        {
            double width = Math.Abs(_dragStart.X - DxfCoords.X);
            double height = Math.Abs(_dragStart.Y - DxfCoords.Y);
            double left = Math.Min(_dragStart.X, DxfCoords.X);
            double top = Math.Min(_dragStart.Y, DxfCoords.Y);
            DragRect = new(left, top, width, height);

            UpdateDragOverlayVertices(DragRect);
        }

        public async Task RunHitTestingAsync()
        {
            while (_isMouseInside)
            {
                if (_hitTestCancellationTokenSource.Token.IsCancellationRequested) { break; }
                if (_suspendHitTesting) { await Task.Delay(50); continue; }

                if (CadManager3D.DxfLoaded && CadManager3D.HitTestingEnabled)
                {
                    switch (CadManager3D.SnapSelectionMode)
                    {
                        case Common.Enums.SelectionMode.Points:
                            RunPointsHitTest(_hitTestCancellationTokenSource.Token);
                            break;

                        case Common.Enums.SelectionMode.Geometries:
                            if (IsDragging) { RunDragGeometriesHittest(_hitTestCancellationTokenSource.Token); }
                            else { RunGeometriesHitTest(_hitTestCancellationTokenSource.Token); }
                            break;

                        case Common.Enums.SelectionMode.CogoPoints:
                            if (IsDragging) { RunDragCogoPointsHittest(_hitTestCancellationTokenSource.Token); }
                            else { RunCogoPointsHitTest(_hitTestCancellationTokenSource.Token); }
                            break;

                        default:
                            break;
                    }
                }
                await Task.Delay(50);
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
                Dispatcher.BeginInvoke(() => RunPointsHitTest(token));
                return;
            }
            if (!CadManager3D.DxfLoaded) { return; }

            _lastHitTestCoords = new(DxfCoords.X, DxfCoords.Y);

            Rect rect = new(_lastHitTestCoords.X - _hittestStrokeThickness, _lastHitTestCoords.Y - _hittestStrokeThickness,
                _hittestStrokeThickness * 2, _hittestStrokeThickness * 2);
            var snappedHitTestablePointCopy = SnappedHitTestablePoint;

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
                                SnappedHitTestablePoint = point;
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
                        SnappedHitTestablePoint = point;
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
                Dispatcher.BeginInvoke(() => RunGeometriesHitTest(token));
                return;
            }

            if (!CadManager3D.DxfLoaded) { return; }

            _lastHitTestCoords = new(DxfCoords.X, DxfCoords.Y);

            Rect rect = new(_lastHitTestCoords.X - _hittestStrokeThickness, _lastHitTestCoords.Y - _hittestStrokeThickness,
                _hittestStrokeThickness * 2, _hittestStrokeThickness * 2);
            var snappedObjectsCopy = _snappedHitTestableObjects.ToList();
            List<HitTestableObject> changedObjects = [];

            if (_snappedHitTestableObjects is not null && _snappedHitTestableObjects.Count > 0)
            {
                foreach (var snappedObj in snappedObjectsCopy)
                {
                    if (snappedObj.DistanceToPoint(_lastHitTestCoords) > _hittestStrokeThickness)
                    {
                        changedObjects.Add(snappedObj);
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
                                    _snappedHitTestableObjects.Add(geometry);
                                    SnapObject(geometry);
                                    _lastSnapHitTestIndex = _currentSnapHitTestIndex;
                                }
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
                            _snappedHitTestableObjects.Add(geometry);
                            SnapObject(geometry);
                            _lastSnapHitTestIndex = _currentSnapHitTestIndex;
                        }
                    }
                }
            }
            var (linesDirty, textsDirty, circlesDirty, pointTextsDirty, sigPointsDirty) = GetVerticesDirtyBools(changedObjects);
            if (linesDirty) { _lineVerticesDirty = linesDirty; }
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
                Dispatcher.BeginInvoke(() => RunCogoPointsHitTest(token));
                return;
            }
            if (!CadManager3D.DxfLoaded) { return; }

            _lastHitTestCoords = new(DxfCoords.X, DxfCoords.Y);

            Rect rect = new(_lastHitTestCoords.X - _hittestStrokeThickness, _lastHitTestCoords.Y - _hittestStrokeThickness,
                _hittestStrokeThickness * 2, _hittestStrokeThickness * 2);
            var snappedObjectsCopy = _snappedHitTestableObjects.ToList();
            List<HitTestableObject> changedObjects = [];

            if (snappedObjectsCopy is not null && snappedObjectsCopy.Count > 0)
            {
                foreach (var snappedObj in snappedObjectsCopy)
                {
                    if (snappedObj.DistanceToPoint(_lastHitTestCoords) > _hittestStrokeThickness)
                    {
                        changedObjects.Add(snappedObj);
                        ResetSnappedObjects();

                        _nearestHitTestableCogoPoints = CadManager3D.HitTestCogoPoints(_lastHitTestCoords, _hittestStrokeThickness).Take(_maxSelectableObjects).ToList();
                        if (_nearestHitTestableCogoPoints.Count > 0)
                        {
                            bool exists = HitTestingHelpers.TryGetNextCogoPoint(_currentSnapHitTestIndex, _nearestHitTestableCogoPoints, out var tup);
                            if (!exists) { _currentSnapHitTestIndex = 0; }
                            exists = HitTestingHelpers.TryGetNextCogoPoint(_currentSnapHitTestIndex, _nearestHitTestableCogoPoints, out tup);

                            if (exists)
                            {
                                var (distance, point) = tup;

                                if (distance <= _hittestStrokeThickness)
                                {
                                    changedObjects.Add(point);
                                    _snappedHitTestableObjects.Add(point);
                                    SnapObject(point);
                                    _lastSnapHitTestIndex = _currentSnapHitTestIndex;
                                }
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
                        var (distance, point) = tup;

                        if (distance <= _hittestStrokeThickness)
                        {
                            changedObjects.Add(point);
                            SnapObject(point);
                            _lastSnapHitTestIndex = _currentSnapHitTestIndex;
                        }
                    }
                }
            }
            var (linesDirty, textsDirty, circlesDirty, pointTextsDirty, sigPointsDirty) = GetVerticesDirtyBools(changedObjects);
            if (linesDirty) { _lineVerticesDirty = linesDirty; }
            if (textsDirty) { _textVerticesDirty = textsDirty; }
            if (textsDirty) { _textGlowVerticesDirty = textsDirty; }
        }

        private async void RunDragCogoPointsHittest(CancellationToken token)
        {
            if (token.IsCancellationRequested) return;
            if (!CadManager3D.DxfLoaded) return;

            // Read DragRect safely from UI thread (cheap, single read)
            Rect currentRect = await Dispatcher.InvokeAsync(() => DragRect, DispatcherPriority.Render);
            if (currentRect.IsEmpty || currentRect.Width <= 0 || currentRect.Height <= 0)
                return;

            // 1) Heavy work OFF the UI thread
            //    Query full set of points inside the rect; *do not* touch WPF objects here.
            //    Make sure CadManager3D.HitTestDragCogoPoints is read-only & thread-safe.
            var newSet = CadManager3D
                .HitTestDragCogoPoints(currentRect)
                .Where(p => currentRect.Contains(p.Bounds))   // keep if you can’t get “fully-inside” from the index
                .ToHashSet();

            // 2) Compute diffs off-thread
            List<CogoPoint> adds, removes;
            lock (_dragCogoLock)
            {
                adds = newSet.Except(_dragCogoCurrent).ToList();
                removes = _dragCogoCurrent.Except(newSet).ToList();
                _dragCogoCurrent = newSet; // update snapshot
            }

            if (adds.Count == 0 && removes.Count == 0) return;
            if (token.IsCancellationRequested) return;

            // 3) Apply the minimal UI changes ON the UI thread in one shot
            await Dispatcher.InvokeAsync(() =>
            {
                foreach (var p in adds)
                    TrySnapCogoPoint(p);

                foreach (var p in removes)
                    TryUnsnapCogoPoint(p);
            }, DispatcherPriority.Background);
        }

        private void RunDragGeometriesHittest(CancellationToken token)
        {
            if (token.IsCancellationRequested) { return; }

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(() => RunDragGeometriesHittest(token));
                return;
            }

            if (!CadManager3D.DxfLoaded) { return; }
            if (_lastQueriedDxfRect == DragRect) { return; }

            //Stopwatch stopwatch = Stopwatch.StartNew();

            var addedRegions = GetDragDelta(_lastQueriedDxfRect, DragRect);
            var removedRegions = GetDragDelta(DragRect, _lastQueriedDxfRect);
            if (_lastQueriedDxfRect.IsEmpty || _lastQueriedDxfRect == new Rect(0, 0, 0, 0))
            {
                removedRegions = [];
            }

            bool linesDirty = false;

            foreach (var region in addedRegions)
            {
                var newHits = CadManager3D.HitTestDragGeometries(region).Distinct();

                foreach (var geometry in newHits)
                {
                    if (DragRect.Contains(geometry.Bounds))
                    {
                        _snappedHitTestableObjects.Add(geometry);
                        SnapObject(geometry);
                        linesDirty = true;
                    }
                }
            }
            foreach (var region in removedRegions)
            {
                var possiblyRemoved = CadManager3D.HitTestDragGeometries(region).Distinct();

                foreach (var geometry in possiblyRemoved)
                {
                    if (!DragRect.Contains(geometry.Bounds))
                    {
                        _snappedHitTestableObjects.Remove(geometry);

                        UnsnapObject(geometry);
                        linesDirty = true;
                    }
                }
            }
            _lastQueriedDxfRect = DragRect;
            _lineVerticesDirty = linesDirty;

            //stopwatch.Stop();
            //Debug.WriteLine($"stopwatch.ElapsedMilliseconds: {stopwatch.ElapsedMilliseconds}");
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

        private void TrySnapCogoPoint(CogoPoint cogoPoint)
        {
            _snappedHitTestableObjects.Add(cogoPoint);
            if (!cogoPoint.IsMouseOver) { SnapObject(cogoPoint); }
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
                    }
                    if (obj is DrawingBlock3D block3D)
                    {
                        block3D.MouseEnter();
                        CadManager3D.UpdateVerticesIsMouseOver(block3D, true);
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
        private void TryUnsnapCogoPoint(CogoPoint cogoPoint)
        {
            _snappedHitTestableObjects.Remove(cogoPoint);
            if (cogoPoint.IsMouseOver) { UnsnapObject(cogoPoint); }
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
        public void ResetSnappedObjects()
        {
            if (SnappedHitTestablePoint is not null)
            {
                SnappedHitTestablePoint = null;
            }
            foreach (var obj in _snappedHitTestableObjects) { UnsnapObject(obj); }

            _snappedHitTestableObjects.Clear();
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
                    }
                }
                if (hitTestableObject is CogoPoint dxfPoint)
                {
                    if (!dxfPoint.IsSelected)
                    {
                        dxfPoint.Select();
                        SelectedCogoPoints.Add(dxfPoint);
                    }
                }
                if (hitTestableObject is HitTestablePoint point)
                {
                    point.Select();
                    SelectedHitTestablePoints.Add(point);
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
                    }
                }
                if (hitTestableObject is CogoPoint dxfPoint)
                {
                    if (dxfPoint.IsSelected)
                    {
                        dxfPoint.Deselect();
                        SelectedCogoPoints.Remove(dxfPoint);
                    }
                }
                if (hitTestableObject is HitTestablePoint point)
                {
                    point.Deselect();
                    SelectedHitTestablePoints.Remove(point);
                }
            }
        }
        public void ResetSelectedObjects()
        {
            var listCopy = SelectedGeometries.ToList();
            foreach (var obj in listCopy) { DeselectObject(obj); }
            SelectedGeometries.Clear();

            var sigPointsCopy = SelectedHitTestablePoints.ToList();
            foreach (var obj in sigPointsCopy) { DeselectObject(obj); }
            SelectedHitTestablePoints.Clear();

            foreach (var point in SelectedCogoPoints) { point.Deselect(); }

            SelectedCogoPoints.Clear();

            _lineVerticesDirty = _textVerticesDirty = true;
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
            _lineVerticesDirty = _textVerticesDirty = true;
        }

        private static void OnCadManager3DChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not D3dDxfControl control) { return; }

            if (e.OldValue is CadManager3D oldCadManager3D)
            {
                oldCadManager3D.PropertyChanged -= control.CadManager3D_PropertyChanged;
                oldCadManager3D.ZoomToExtentsRequested -= control.ZoomToExtents;
                oldCadManager3D.CogoPointManager.CogoPoints.CollectionChanged -= control.CogoPoints_CollectionChanged_Instance;
                oldCadManager3D.CogoPointManager.PointGroups.CollectionChanged -= control.PointGroups_CollectionChanged_Instance;
            }

            if (e.NewValue is CadManager3D newCadManager3D)
            {
                newCadManager3D.PropertyChanged += control.CadManager3D_PropertyChanged;
                newCadManager3D.ZoomToExtentsRequested += control.ZoomToExtents;
                newCadManager3D.CogoPointManager.CogoPoints.CollectionChanged += control.CogoPoints_CollectionChanged_Instance;
                newCadManager3D.CogoPointManager.PointGroups.CollectionChanged += control.PointGroups_CollectionChanged_Instance;
            }
        }

        private void CogoPoints_CollectionChanged_Instance(object? sender, NotifyCollectionChangedEventArgs e)
        {
            CogoPoints = CadManager3D?.CogoPointManager?.CogoPoints;
        }
        private void PointGroups_CollectionChanged_Instance(object? sender, NotifyCollectionChangedEventArgs e)
        {
            PointGroups = CadManager3D?.CogoPointManager?.PointGroups;
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
            if (e.PropertyName == nameof(CadManager3D.PointTextVerticesDirty))
            {
                if (CadManager3D.PointTextVerticesDirty)
                {
                    _pointTextVerticesDirty = true;
                }
            }
            if (e.PropertyName == nameof(CadManager3D.PointCircleVerticesDirty))
            {
                if (CadManager3D.PointCircleVerticesDirty)
                {
                    _circleVerticesDirty = true;
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

        #region Static Methods
        public static List<Rect> GetDragDelta(Rect previous, Rect current)
        {
            var deltaRects = new List<Rect>();

            // First, find the union and intersection
            Rect intersection = Rect.Intersect(previous, current);
            if (intersection.IsEmpty)
            {
                deltaRects.Add(current); // No overlap, full rect is new
                return deltaRects;
            }

            // Top band
            if (current.Top < previous.Top)
            {
                double height = previous.Top - current.Top;
                deltaRects.Add(new Rect(current.Left, current.Top, current.Width, height));
            }

            // Bottom band
            if (current.Bottom > previous.Bottom)
            {
                double height = current.Bottom - previous.Bottom;
                deltaRects.Add(new Rect(current.Left, previous.Bottom, current.Width, height));
            }

            // Left band
            if (current.Left < previous.Left)
            {
                double width = previous.Left - current.Left;
                double top = Math.Max(current.Top, previous.Top);
                double height = Math.Min(current.Bottom, previous.Bottom) - top;
                deltaRects.Add(new Rect(current.Left, top, width, height));
            }

            // Right band
            if (current.Right > previous.Right)
            {
                double width = current.Right - previous.Right;
                double top = Math.Max(current.Top, previous.Top);
                double height = Math.Min(current.Bottom, previous.Bottom) - top;
                deltaRects.Add(new Rect(previous.Right, top, width, height));
            }

            return deltaRects;
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
