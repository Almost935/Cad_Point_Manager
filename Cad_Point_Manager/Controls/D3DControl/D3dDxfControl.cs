using Cad_Point_Manager.Common.Collections;
using Cad_Point_Manager.Controls.D3DControl.Buffers;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Helpers.EqualityComparers;
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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
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
        private const float HoverTextPadPx = 3f;     // grow the text rect by this many pixels
        private const float HoverEllipsePadPx = 2f;  // extra pixels over the point radius
        private static readonly Vector4 HoverShadowColor = new(0f, 0f, 0f, 0.2f); // translucent black

        // CogoPoint ToggleButton Fields
        private const float AnchorPixelSize = 36f;
        private const float AnchorCornerPixels = 6f;
        private const float AnchorFeatherPixels = 1.5f;
        private const float MaxCogoToggleToDrawingFraction = 0.01f; // max size of toggle button relative to smaller of width/height of drawing area
        private const float CornerFracOfHalf = 0.35f;
        private const float FeatherPx = 1.5f; // keep AA feather in pixels
        private float AnchorWorldHalfSize;
        private float AnchorWorldCornerRadius;
        private float AnchorWorldFeather;
        private Rect CurrentAnchorBounds;
        private static readonly Vector4 AnchorBase = new(0.00f, 0.95f, 1.00f, 1.00f);
        private static readonly Vector4 AnchorHover = new(0.67f, 1.00f, 1.00f, 1.00f);
        private static readonly Vector4 AnchorPressed = new(0.15f, 0.82f, 0.85f, 1.00f);

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
        private HashSet<CogoPoint> _dragCogoCurrent = [];  // last-applied set

        // Drag Selection Fields
        private bool _isDragging = false;
        private Point _dragStartScreen;
        private Point _dragStart;
        private Rect _dragRect = new(0, 0, 0, 0);
        private Vector _dxfDragRectTranslate = new();
        private System.Windows.Media.Matrix _currentlyAppliedDragRectMatrix = new();

        // Direct3D related fields
        public bool _buffersInitialized = false;

        // Line shader related fields
        private ResizableBuffer<LineVertex> _lineVertexBuffer;
        private Buffer _lineSettingsBuffer;
        private int _lineVertexCount;
        private VertexShader _lineVertexShader;
        private PixelShader _linePixelShader;
        private InputLayout _lineInputLayout;
        private bool _lineShadersLoaded = false;
        private bool _lineVerticesDirty = false;

        // Line glow shader related fields
        private Buffer _lineGlowSettingsBuffer;
        private VertexShader _lineGlowVertexShader;
        private PixelShader _lineGlowPixelShader;
        private GeometryShader _lineGlowGeometryShader;

        // Text shader related fields
        private ResizableBuffer<TextVertex> _textVertexBuffer;
        private int _textVertexCount;
        private VertexShader _textVertexShader;
        private PixelShader _textPixelShader;
        private InputLayout _textInputLayout;
        private bool _textShaderLoaded = false;
        private bool _textVerticesDirty = false;


        // CogoPoint shader related fields
        private bool _cogoPointShadersLoaded = false;

        // Point glyph rendering
        private ResizableBuffer<GlyphInstance> _glyphInstanceBuffer;
        private Buffer _cogoPointSettingsBuffer;
        private InputLayout _glyphLayout;
        private VertexShader _glyphVS;
        private PixelShader _glyphPS;
        private readonly Dictionary<short, List<GlyphInstance>> _glyphBatches = [];
        private bool _glyphVerticesDirty = false;

        // Point circle shader related fields
        private ResizableBuffer<PointMarkerInstance> _pointCircleVertexBuffer;
        private Buffer _pointCircleSettingsBuffer;
        private InputLayout _pointCircleInputLayout;
        private VertexShader _pointCircleVS;
        private PixelShader _pointCirclePS;
        private GeometryShader _pointCircleGS;
        private int _pointCircleVertexCount;
        private bool _pointCircleVerticesDirty = false;

        // Cogo point leader line rendering fields
        private VertexShader _leaderLineVS;
        private PixelShader _leaderLinePS;
        private GeometryShader _leaderLineGS;
        private InputLayout _leaderLineInputLayout;
        private bool _leaderLineShadersLoaded = false;
        private ResizableBuffer<LeaderLineInstance> _leaderLineBuffer;
        private int _leaderLineInstanceCount = 0;
        private bool _leaderLineVerticesDirty = false;
        private Buffer _leaderLineSettingsBuffer;

        // --- Per-label & per-group indirection state ---
        private SceneIdMap _ids;
        private D3dStateBuffers _stateBufs;
        private D3dStateController _stateCtl;

        // Cogo point hover rendering
        private bool _cogoHoverShadersLoaded = false;
        private bool _hoverVerticesDirty = false;

        private ResizableBuffer<OverlayQuadVertex> _hoverRectBuffer;
        private ResizableBuffer<RoundedHoverRectInstance> _hoverRectInstanceBuffer;
        private VertexShader _hoverRectVertexShader;
        private PixelShader _hoverRectPixelShader;
        private InputLayout _hoverRectLayout;
        private int _hoverRectInstanceCount = 0;

        private ResizableBuffer<CircleHoverVertex> _hoverCircleBuffer;
        private VertexShader _hoverCircleVertexShader;
        private PixelShader _hoverCirclePixelShader;
        private GeometryShader _hoverCircleGeometryShader;
        private readonly List<CircleHoverVertex> _hoverCircleVertices = [];
        private CircleHoverSettingsBuffer _hoverCircleSettings = new()
        {
            GlowOffset = 1.5f,
            GlowTransparency = 0.6f,
            SelectedColor = GlobalHelperProperties.SelectedObjectColor
        };
        private Buffer _hoverCircleSettingsBuffer;
        private InputLayout _hoverCircleLayout;

        // Cogo point toggle button rendering fields
        private ResizableBuffer<ToggleAnchorInstance> _anchorInstanceBuffer;
        private int _anchorVerticesCount;
        private bool _anchorVerticesDirty = false;
        private VertexShader _toggleVS;
        private PixelShader _togglePS;
        private InputLayout _toggleLayout;
        private ResizableBuffer<OverlayQuadVertex> _toggleQuadVB;
        private Buffer _toggleSettingsBuffer;
        private bool _anchorShaderLoaded = false;


        // Drag rectangle shader
        private VertexShader _overlayOutlineVS;
        private PixelShader _overlayOutlinePS;
        private InputLayout _overlayOutlineLayout;
        private bool _overlayOutlineShadersLoaded = false;
        private Buffer _overlayOutlineSettingsBuffer;

        private ResizableBuffer<OverlayVertex> _dragFillBuffer;
        private int _dragFillVertexCount;

        private VertexShader _overlayVS;
        private PixelShader _overlayPS;
        private InputLayout _overlayLayout;
        private bool _overlayShaderLoaded;

        // Panning and Zooming Fields
        private float _panThreshold = 1.0f;
        private bool _isPanning;

        // Camera based fields
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
        private readonly HashSet<HitTestableObject> _mouseOverHitTestableObjects = [];
        private readonly HashSet<CogoPoint> _mouseOverCogoPoints = new(new CogoPointNumberComparer());

        // CogoPoint Movement Fields
        private CogoPoint _mouseOverToggleButtonPoint = null;
        private CogoPoint _pressedToggleButtonPoint = null;
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

        public static bool IsShiftPressed => (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift));

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

        public static readonly DependencyProperty LayersProperty =
            DependencyProperty.Register(
                nameof(Layers),
                typeof(BatchableObservableCollection<KeyValuePair<string, ObjectLayer3D>>),
                typeof(D3dDxfControl),
                new FrameworkPropertyMetadata(new BatchableObservableCollection<KeyValuePair<string, ObjectLayer3D>>()));
        public BatchableObservableCollection<KeyValuePair<string, ObjectLayer3D>> Layers
        {
            get => (BatchableObservableCollection<KeyValuePair<string, ObjectLayer3D>>)GetValue(LayersProperty);
            set => SetValue(LayersProperty, value);
        }

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
                typeof(BatchableObservableCollection<HitTestablePoint>),
                typeof(D3dDxfControl),
                new FrameworkPropertyMetadata(new BatchableObservableCollection<HitTestablePoint>(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public BatchableObservableCollection<HitTestablePoint> SelectedHitTestablePoints
        {
            get => (BatchableObservableCollection<HitTestablePoint>)GetValue(SelectedHitTestablePointsProperty);
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
            if (_resCache is null) { return; }

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
            if (!_buffersInitialized) { InitializeBuffers(); }

            if (_lineVerticesDirty) { UpdateLineVertices(); }
            if (_textVerticesDirty) { UpdateTextVertices(); }
            if (_glyphVerticesDirty) { UpdateGlyphBatches(); }
            if (_pointCircleVerticesDirty) { UpdatePointCircleVertices(); }
            if (_hoverVerticesDirty) { UpdateCogoHoverVertices(); }
            if (HitTestableObjectTreeDirty) { LoadHitTestableObjectTree(); }
            if (_anchorVerticesDirty) { UpdateToggleAnchorVertices(); }
            if (_leaderLineVerticesDirty) { UpdateLeaderLineVertices(); }

            if (!_lineShadersLoaded) { InitializeLineShaders(); }
            if (!_textShaderLoaded) { InitializeTextShaders(); }
            if (!_overlayShaderLoaded) { InitializeOverlayShaders(); }
            if (!_cogoPointShadersLoaded) { InitializeCogoPointShaders(); }
            if (!_cogoHoverShadersLoaded) { InitializeCogoPointHoverShaders(); }
            if (!_anchorShaderLoaded) { InitializeToggleAnchorShaders(); }
            if (!_leaderLineShadersLoaded) { InitializeLeaderLineShaders(); }

            if (!ConstantBuffersInitialized) { InitializeConstantBuffers(); }
            if (ConstantBuffersDirty) { UpdateConstantBuffers(); }

            if (!_hitTestIsRunning)
            {
                _hitTestIsRunning = true;
                _hittestTask = Task.Run(() => RunHitTestingAsync());
            }

            var ctx = _resCache.DeviceContext;

            if (_dxfDirty)
            {
                DrawDxf(ctx);
                _dxfDirty = false;
                _interactiveDirty = true;
            }

            if (_interactiveDirty)
            {
                ctx.CopyResource(_resCache.DxfTexture, _resCache.InteractionTexture);

                if (IsDragging && _dragFillVertexCount > 0)
                {
                    ctx.OutputMerger.SetRenderTargets(_resCache.InteractiveRenderTargetView);
                    DrawDragOverlay(ctx);
                }

                if (_hoverRectInstanceCount > 0 || _hoverCircleVertices.Count > 0)
                {
                    ctx.OutputMerger.SetRenderTargets(_resCache.InteractiveRenderTargetView);
                    DrawCogoPointHover(ctx);
                }

                if (_anchorVerticesCount > 0)
                {
                    ctx.OutputMerger.SetRenderTargets(_resCache.InteractiveRenderTargetView);
                    DrawCogoPointAnchors(ctx);
                }

                if (_leaderLineInstanceCount > 0)
                {
                    ctx.OutputMerger.SetRenderTargets(_resCache.InteractiveRenderTargetView);
                    DrawLeaderLines(ctx);
                }

                ctx.CopyResource(_resCache.InteractionTexture, _resCache.Texture2D);
                _interactiveDirty = false;
            }
        }

        private void DrawDxf(DeviceContext ctx)
        {
            ctx.OutputMerger.SetRenderTargets(_resCache.DxfRenderTargetView);
            ctx.ClearRenderTargetView(_resCache.DxfRenderTargetView, new RawColor4(1, 1, 1, 1));

            DrawLinesWithShader(ctx);
            DrawTextWithShader(ctx);
            DrawPointCirclesWithShader(ctx);
            DrawGlyphBatches(ctx, _resCache.AsciiGlyphAtlas, _glyphBatches);
        }

        private void DrawLinesWithShader(DeviceContext ctx)
        {
            //Stopwatch stopwatch = Stopwatch.StartNew();
            if (_lineVertexBuffer is null) { return; }

            ctx.VertexShader.Set(_lineGlowVertexShader);
            ctx.GeometryShader.Set(_lineGlowGeometryShader);
            ctx.PixelShader.Set(_lineGlowPixelShader);
            ctx.InputAssembler.InputLayout = _lineInputLayout;
            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.VertexShader.SetShaderResource(0, _stateBufs.LayerSRV);
            ctx.GeometryShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.GeometryShader.SetConstantBuffer(1, _lineGlowSettingsBuffer);
            ctx.GeometryShader.SetShaderResource(0, _stateBufs.LayerSRV);
            ctx.GeometryShader.SetShaderResource(1, _stateBufs.ObjectSRV);
            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
            ctx.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(
                _lineVertexBuffer.Buffer, _lineVertexBuffer.Stride, 0));
            ctx.Draw(_lineVertexCount, 0);
            ctx.GeometryShader.Set(null);

            ctx.VertexShader.Set(_lineVertexShader);
            ctx.PixelShader.Set(_linePixelShader);
            ctx.InputAssembler.InputLayout = _lineInputLayout;
            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.VertexShader.SetConstantBuffer(1, _lineSettingsBuffer);
            ctx.VertexShader.SetShaderResource(0, _stateBufs.LayerSRV);
            ctx.VertexShader.SetShaderResource(1, _stateBufs.ObjectSRV);
            ctx.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(
                _lineVertexBuffer.Buffer, _lineVertexBuffer.Stride, 0));
            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
            ctx.Draw(_lineVertexCount, 0);

            //stopwatch.Stop();
            //Debug.WriteLine($"DrawLinesWithShader Time: {stopwatch.ElapsedMilliseconds} ms");
        }
        private void DrawTextWithShader(DeviceContext ctx)
        {
            //Stopwatch stopwatch = Stopwatch.StartNew();

            if (_textVertexBuffer is null) { return; }

            ctx.VertexShader.Set(_textVertexShader);
            ctx.PixelShader.Set(_textPixelShader);
            ctx.InputAssembler.InputLayout = _textInputLayout;
            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.VertexShader.SetConstantBuffer(2, _viewportBuffer);
            ctx.VertexShader.SetShaderResource(0, _stateBufs.LayerSRV);
            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            ctx.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(
                 _textVertexBuffer.Buffer, _textVertexBuffer.Stride, 0));

            ctx.Draw(_textVertexCount, 0);

            //stopwatch.Stop();
            //Debug.WriteLine($"DrawTextWithShader Time: {stopwatch.ElapsedMilliseconds} ms");
        }
        private void DrawGlyphBatches(DeviceContext ctx, GlyphAtlas atlas, Dictionary<short, List<GlyphInstance>> batches)
        {
            if (atlas == null || atlas.VertexBuffer == null) return;

            // Bind shaders + constant buffers
            ctx.VertexShader.Set(_glyphVS);
            ctx.PixelShader.Set(_glyphPS);
            ctx.InputAssembler.InputLayout = _glyphLayout;
            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.VertexShader.SetConstantBuffer(1, _cogoPointSettingsBuffer); // reuse your TextSettingsBuffer
            ctx.VertexShader.SetConstantBuffer(2, _viewportBuffer);
            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            ctx.VertexShader.SetShaderResource(0, _stateBufs.LabelSRV);
            ctx.VertexShader.SetShaderResource(1, _stateBufs.PointSRV);
            ctx.VertexShader.SetShaderResource(2, _stateBufs.GroupSRV);

            ctx.PixelShader.SetShaderResource(1, _stateBufs.GroupSRV);

            // Bind slot 0 (glyph vertex buffer) once
            var vbGlyph = new VertexBufferBinding(atlas.VertexBuffer, Utilities.SizeOf<GlyphVertexDU>(), 0);

            foreach (var kvp in batches)
            {
                short glyphId = kvp.Key;
                var instances = kvp.Value;
                if (instances == null || instances.Count == 0) { continue; }

                if (!atlas.Ranges.TryGetValue(glyphId, out var range)) { continue; }
                if (range.VertexCount <= 0) { continue; }

                // Update instance buffer (slot 1)
                _glyphInstanceBuffer.Update(ctx, CollectionsMarshal.AsSpan(instances)); // .NET 7; or instances.ToArray()
                var vbInst = new VertexBufferBinding(_glyphInstanceBuffer.Buffer, _glyphInstanceBuffer.Stride, 0);

                ctx.InputAssembler.SetVertexBuffers(0, vbGlyph, vbInst);

                // One draw per glyph id
                ctx.DrawInstanced(
                    vertexCountPerInstance: range.VertexCount,
                    instanceCount: instances.Count,
                    startVertexLocation: range.StartVertex,
                    startInstanceLocation: 0);
            }
        }
        private void DrawPointCirclesWithShader(DeviceContext ctx)
        {
            //Stopwatch stopwatch = Stopwatch.StartNew();

            ctx.VertexShader.Set(_pointCircleVS);
            ctx.GeometryShader.Set(_pointCircleGS);
            ctx.PixelShader.Set(_pointCirclePS);
            ctx.InputAssembler.InputLayout = _pointCircleInputLayout;
            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.GeometryShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.GeometryShader.SetConstantBuffer(1, _pointCircleSettingsBuffer);

            // NEW: bind state buffers
            ctx.VertexShader.SetShaderResource(0, _stateBufs.PointSRV);
            ctx.VertexShader.SetShaderResource(1, _stateBufs.GroupSRV);

            ctx.GeometryShader.SetShaderResource(0, _stateBufs.PointSRV);
            ctx.GeometryShader.SetShaderResource(1, _stateBufs.GroupSRV);

            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;
            ctx.InputAssembler.SetVertexBuffers(0,
                new VertexBufferBinding(_pointCircleVertexBuffer.Buffer, _pointCircleVertexBuffer.Stride, 0));

            ctx.Draw(_pointCircleVertexCount, 0);
            ctx.GeometryShader.Set(null);

            //stopwatch.Stop();
            //Debug.WriteLine($"DrawCirclesWithShader Time: {stopwatch.ElapsedMilliseconds} ms");
        }
        private void DrawCogoPointAnchors(DeviceContext ctx)
        {
            if (_anchorVerticesCount == 0) return;

            ctx.VertexShader.Set(_toggleVS);
            ctx.PixelShader.Set(_togglePS);
            ctx.InputAssembler.InputLayout = _toggleLayout;
            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);

            // ✅ ADD: settings buffer (b1) for both VS & PS
            ctx.VertexShader.SetConstantBuffer(1, _toggleSettingsBuffer);
            ctx.PixelShader.SetConstantBuffer(1, _toggleSettingsBuffer);

            // ✅ ADD: state SRVs (t0/t1) for the VS (shader fetches flags/offset)
            ctx.VertexShader.SetShaderResource(0, _stateBufs.PointSRV); // t0
            ctx.VertexShader.SetShaderResource(1, _stateBufs.GroupSRV); // t1

            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            ctx.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_toggleQuadVB.Buffer, _toggleQuadVB.Stride, 0));
            ctx.InputAssembler.SetVertexBuffers(1, new VertexBufferBinding(_anchorInstanceBuffer.Buffer, _anchorInstanceBuffer.Stride, 0));

            ctx.DrawInstanced(6, _anchorVerticesCount, 0, 0);
        }
        private void DrawLeaderLines(DeviceContext ctx)
        {
            if (_leaderLineInstanceCount <= 0) { return; }

            // Interactive RT already bound in your interactive block
            ctx.InputAssembler.InputLayout = _leaderLineInputLayout;
            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList; // ONE vertex per line-instance

            ctx.VertexShader.Set(_leaderLineVS);
            ctx.GeometryShader.Set(_leaderLineGS);
            ctx.PixelShader.Set(_leaderLinePS);

            // cb0: ViewProj on VS & GS (PS not needed here)
            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.GeometryShader.SetConstantBuffer(0, _transformationBuffer);

            // cb1: settings on GS
            ctx.GeometryShader.SetConstantBuffer(1, _leaderLineSettingsBuffer);

            // SRVs (label/group) 
            ctx.GeometryShader.SetShaderResource(0, _stateBufs.PointSRV);
            ctx.GeometryShader.SetShaderResource(1, _stateBufs.GroupSRV);

            // VB
            ctx.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_leaderLineBuffer.Buffer, _leaderLineBuffer.Stride, 0));

            ctx.Draw(_leaderLineInstanceCount, 0);

            // Clean up GS SRVs (optional, prevents hazards on some drivers)
            ctx.GeometryShader.SetShaderResource(0, null);
            ctx.GeometryShader.SetShaderResource(1, null);
            ctx.GeometryShader.Set(null);
        }
        private void DrawDragOverlay(DeviceContext ctx)
        {
            // --- fill (triangles) ---
            ctx.VertexShader.Set(_overlayVS);
            ctx.PixelShader.Set(_overlayPS);
            ctx.InputAssembler.InputLayout = _overlayLayout;
            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            ctx.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_dragFillBuffer.Buffer, _dragFillBuffer.Stride, 0));
            ctx.Draw(_dragFillVertexCount, 0);

            // --- border (same VB, triangle list) ---
            ctx.VertexShader.Set(_overlayOutlineVS);
            ctx.PixelShader.Set(_overlayOutlinePS);
            ctx.InputAssembler.InputLayout = _overlayOutlineLayout;
            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);      // b0
            ctx.PixelShader.SetConstantBuffer(0, null);                        // not used
            ctx.VertexShader.SetConstantBuffer(1, _overlayOutlineSettingsBuffer); // b1
            ctx.PixelShader.SetConstantBuffer(1, _overlayOutlineSettingsBuffer);  // b1
            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            ctx.InputAssembler.SetVertexBuffers(0,
               new VertexBufferBinding(_dragFillBuffer.Buffer, _dragFillBuffer.Stride, 0));
            ctx.Draw(_dragFillVertexCount, 0);
        }
        private void DrawCogoPointHover(DeviceContext ctx)
        {
            if (_hoverRectInstanceCount > 0)
            {
                ctx.VertexShader.Set(_hoverRectVertexShader);
                ctx.PixelShader.Set(_hoverRectPixelShader);
                ctx.InputAssembler.InputLayout = _hoverRectLayout;
                ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
                ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
                ctx.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_hoverRectBuffer.Buffer, _hoverRectBuffer.Stride, 0));
                ctx.InputAssembler.SetVertexBuffers(1, new VertexBufferBinding(_hoverRectInstanceBuffer.Buffer, _hoverRectInstanceBuffer.Stride, 0));
                ctx.DrawInstanced(6, _hoverRectInstanceCount, 0, 0);
            }
            if (_hoverCircleVertices.Count > 0)
            {
                ctx.VertexShader.Set(_hoverCircleVertexShader);
                ctx.GeometryShader.Set(_hoverCircleGeometryShader);
                ctx.PixelShader.Set(_hoverCirclePixelShader);
                ctx.InputAssembler.InputLayout = _hoverCircleLayout;
                ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
                ctx.GeometryShader.SetConstantBuffer(0, _transformationBuffer);
                ctx.GeometryShader.SetConstantBuffer(1, _hoverCircleSettingsBuffer);
                ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;
                ctx.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_hoverCircleBuffer.Buffer, _hoverCircleBuffer.Stride, 0));
                ctx.Draw(_hoverCircleVertices.Count, 0);
                ctx.GeometryShader.Set(null);
            }
        }

        private void UpdateLineVertices()
        {
            if (_lineVertexBuffer is null || CadManager3D is null) { return; }

            var context = _resCache.DeviceContext;
            var vertexSpan = CadManager3D.UpdateLineVerticesList(_ids);
            _stateBufs.EnsureObjectCapacity(_ids.ObjectCount);
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

            var context = _resCache.DeviceContext;
            var vertexSpan = CadManager3D.UpdateTextVerticesList(_resCache, _ids);
            _textVertexBuffer.Update(context, vertexSpan);
            _textVertexCount = vertexSpan.Length;

            _textVerticesDirty = false;
            _dxfDirty = true;
        }
        private void UpdateGlyphBatches()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            _glyphBatches.Clear();

            foreach (var kv in PointGroups)
            {
                var pg = kv.Value;
                if (pg is null) { continue; }

                var worldHeight = (float)(pg.FontBaseSize * pg.PointScale);
                var duToWorld = worldHeight / _resCache.CogoPointFontFace.Metrics.DesignUnitsPerEm;

                var duPerEm = _resCache.CogoPointFontFace.Metrics.DesignUnitsPerEm;
                var duToWorldBase = (float)pg.FontBaseSize / duPerEm;

                var color = pg.Color.ToSharpDXVector4();
                var isGroupVisible = pg.IsVisible ? 1f : 0f;

                uint gId = _ids.GetOrAddGroupId(pg);
                _stateBufs.EnsureGroupCapacity(_ids.GroupCount);

                foreach (var p in pg.Points)
                {
                    if (p == null) { continue; }

                    uint pId = _ids.GetOrAddPointId(p);
                    _stateBufs.EnsurePointCapacity(_ids.PointCount);

                    var isMO = p.IsMouseOver ? 1f : 0f;
                    var isSel = p.IsSelected ? 1f : 0f;
                    var ySign = -1f;

                    uint idPN = _ids.GetOrAddLabelId(p, 0);
                    uint idElev = _ids.GetOrAddLabelId(p, 1);
                    uint idDesc = p.HasDescription ? _ids.GetOrAddLabelId(p, 2) : 0xFFFFFFFF;

                    p.PointNumberBounds = AddLineAndGetRect(
                        s: p.PointNumber.ToString(),
                        originWorld: p.TextInfoBasePosition,
                        lineOffset: p.PointNumberOffset,
                        duToWorldBase: duToWorldBase,
                        duToWorld: duToWorld,
                        color: color,
                        isVisible: isGroupVisible, isMouseOver: isMO, isSelected: isSel, ySign: ySign,
                        labelId: idPN, groupId: gId, pointId: pId);

                    p.ElevationBounds = AddLineAndGetRect(
                        s: p.Elevation.ToString("F3"),
                        originWorld: p.TextInfoBasePosition,
                        lineOffset: p.ElevationOffset,
                        duToWorldBase: duToWorldBase,
                        duToWorld: duToWorld,
                        color: color,
                        isVisible: isGroupVisible, isMouseOver: isMO, isSelected: isSel, ySign: ySign,
                        labelId: idElev, groupId: gId, pointId: pId);

                    if (p.HasDescription)
                    {
                        p.DescriptionBounds = AddLineAndGetRect(
                            s: p.Description,
                            originWorld: p.TextInfoBasePosition,
                            lineOffset: p.DescriptionOffset,
                            duToWorldBase: duToWorldBase,
                            duToWorld: duToWorld,
                            color: color,
                            isVisible: isGroupVisible, isMouseOver: isMO, isSelected: isSel, ySign: ySign,
                            labelId: idDesc, groupId: gId, pointId: pId);

                        RecomputeCogoPointBoundsFast(p);
                    }

                    float wupp = Camera.GetWorldUnitsPerPixel();
                    float rW = (float)(GlobalHelperProperties.CogoPointCirclePixelRadius * wupp * p.PointGroup.PointScale);
                    var c = p.Position;
                    p.EllipseBounds = new Rect(c.X - rW, c.Y - rW, 2 * rW, 2 * rW);

                    p.UpdateBounds();
                }
            }

            _stateBufs.FlushAll();

            HitTestableObjectTreeDirty = true;
            _glyphVerticesDirty = false;
            _dxfDirty = true;

            stopwatch.Stop();
            Debug.WriteLine($"UpdateGlyphBatches Time: {stopwatch.ElapsedMilliseconds} ms");
        }
        private void UpdatePointCircleVertices()
        {
            if (_pointCircleVertexBuffer is null) { return; }

            var context = _resCache.DeviceContext;
            var vertexSpan = CadManager3D.UpdatePointCircleVerticesList(_ids);
            _pointCircleVertexBuffer.Update(context, vertexSpan);
            _pointCircleVertexCount = vertexSpan.Length;

            _pointCircleVerticesDirty = false;
            _dxfDirty = true;
        }
        private void UpdateDragOverlayVertices(Rect r)
        {
            if (r.IsEmpty || r.Width <= 0 || r.Height <= 0)
            {
                _dragFillVertexCount = 0;
                return;
            }

            var settings = new OverlayOutlineSettings
            {
                RectMinWorld = new Vector2((float)r.Left, (float)r.Top),
                RectMaxWorld = new Vector2((float)r.Right, (float)r.Bottom),
                ThicknessPx = 1.5f,     // tweak as desired
                FeatherPx = 1.0f,     // small AA feather
                BorderColor = new Vector4(0f, 0.749f, 1f, 1f) // DeepSkyBlue like your lines
            };
            _resCache.DeviceContext.UpdateSubresource(ref settings, _overlayOutlineSettingsBuffer);

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

            _dragFillBuffer.Update(_resCache.DeviceContext, fillVerts);
            _dragFillVertexCount = 6;

            _interactiveDirty = true;
        }
        private void UpdateCogoHoverVertices()
        {
            if (_resCache is null || Camera is null) { return; }

            var ctx = _resCache.DeviceContext;
            _hoverCircleVertices.Clear();
            var wupp = Camera.GetWorldUnitsPerPixel();
            var rectInstances = new List<RoundedHoverRectInstance>(16);

            foreach (CogoPoint cp in _mouseOverCogoPoints)
            {
                float padW = HoverTextPadPx * wupp;
                var pointNumBounds = cp.PointNumberBounds;
                var elevationBounds = cp.ElevationBounds;
                pointNumBounds.Inflate(padW, padW);
                elevationBounds.Inflate(padW, padW);

                var pointNumCenter = new Vector2((float)(pointNumBounds.X + pointNumBounds.Width * 0.5), (float)(pointNumBounds.Y + pointNumBounds.Height * 0.5));
                var pointNumHalfSize = new Vector2((float)(pointNumBounds.Width * 0.5), (float)(pointNumBounds.Height * 0.5));
                var elevCenter = new Vector2((float)(elevationBounds.X + elevationBounds.Width * 0.5), (float)(elevationBounds.Y + elevationBounds.Height * 0.5));
                var elevHalfSize = new Vector2((float)(elevationBounds.Width * 0.5), (float)(elevationBounds.Height * 0.5));

                var radiusFeathering = new Vector2(5f * wupp, 1.5f * wupp);
                rectInstances.Add(new RoundedHoverRectInstance
                {
                    Center = pointNumCenter,
                    HalfSize = pointNumHalfSize,
                    RadiusFeather = radiusFeathering,
                    Color = HoverShadowColor
                });
                rectInstances.Add(new RoundedHoverRectInstance
                {
                    Center = elevCenter,
                    HalfSize = elevHalfSize,
                    RadiusFeather = radiusFeathering,
                    Color = HoverShadowColor
                });

                if (!string.IsNullOrEmpty(cp.Description) && !cp.DescriptionBounds.IsEmpty)
                {
                    var descBounds = cp.DescriptionBounds;
                    var descCenter = new Vector2((float)(descBounds.X + descBounds.Width * 0.5), (float)(descBounds.Y + descBounds.Height * 0.5));
                    var descHalfSize = new Vector2((float)(descBounds.Width * 0.5), (float)(descBounds.Height * 0.5));
                    rectInstances.Add(new RoundedHoverRectInstance
                    {
                        Center = descCenter,
                        HalfSize = descHalfSize,
                        RadiusFeather = radiusFeathering,
                        Color = HoverShadowColor
                    });
                }

                // circle
                CircleHoverVertex circleHoverVertex = new(cp.Position.ToSharpDXVector3(), GlobalHelperProperties.CogoPointCircleMouseOverPixelRadius * cp.PointGroup.PointScale.ToFloat());
                _hoverCircleVertices.Add(circleHoverVertex);
            }

            _hoverRectInstanceBuffer.Update(ctx, CollectionsMarshal.AsSpan(rectInstances));
            _hoverRectInstanceCount = rectInstances.Count;

            _hoverCircleBuffer.Update(ctx, _hoverCircleVertices.ToArray());

            _hoverVerticesDirty = false;
            _interactiveDirty = true;
        }
        private void UpdateToggleAnchorVertices()
        {
            if (_resCache is null || Camera is null) return;

            var ctx = _resCache.DeviceContext;
            float wupp = Camera.GetWorldUnitsPerPixel();

            // Desired size from pixels (keeps shrinking as you zoom in)
            float desiredHalfWorld = (AnchorPixelSize * 0.5f) * wupp;

            // Cap from DXF extents: a fraction of the drawing short side
            float drawingShort = Math.Min(Camera.Extents.Width, Camera.Extents.Height).ToFloat();
            float maxHalfWorld = (drawingShort * MaxCogoToggleToDrawingFraction) * 0.5f;

            var inst = new List<ToggleAnchorInstance>(SelectedCogoPoints.Count);
            foreach (var keyValue in PointGroups)
            {
                var pg = keyValue.Value;
                var gid = _ids.GetOrAddGroupId(pg);

                if (pg is null) { continue; }

                foreach (var p in pg.Points)
                {
                    if (p is null) { continue; }
                    var pid = _ids.GetOrAddPointId(p);
                    var center = p.TextInfoBasePosition;

                    inst.Add(new()
                    {
                        Center = center,
                        PointId = pid,
                        GroupId = gid
                    });
                }
            }

            _anchorInstanceBuffer.Update(ctx, CollectionsMarshal.AsSpan(inst));
            _anchorVerticesCount = inst.Count;
            _anchorVerticesDirty = false;
            _interactiveDirty = true;
        }
        private void UpdateLeaderLineVertices()
        {
            List<LeaderLineInstance> list = [];
            foreach (var keyValue in PointGroups)
            {
                var pg = keyValue.Value;
                if (pg is null) { continue; }

                uint gid = _ids.GetOrAddGroupId(pg);
                foreach (var p in pg.Points)
                {
                    if (p is null) { continue; }
                    uint pid = _ids.GetOrAddPointId(p);

                    var vertex = new LeaderLineInstance
                    {
                        Start = p.Position.ToSharpDXVector2(),
                        End = p.TextInfoBasePosition,
                        PointId = pid,
                        GroupId = gid
                    };
                    list.Add(vertex);
                }
            }
            _leaderLineInstanceCount = list.Count;
            _leaderLineBuffer.Update(_resCache.DeviceContext, CollectionsMarshal.AsSpan(list));
            _leaderLineVerticesDirty = false;
            _interactiveDirty = true;
        }

        private void InitializeLineShaders()
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
            _lineVertexShader = new VertexShader(_resCache.Device, lineVSBytecode);

            var linePSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "PSMain", "ps_5_0");
            _linePixelShader = new PixelShader(_resCache.Device, linePSBytecode);

            // Glow shaders
            var lineGlowVSBytecode = ShaderBytecode.CompileFromFile(glowShaderPath, "VSMain", "vs_5_0");
            _lineGlowVertexShader = new VertexShader(_resCache.Device, lineGlowVSBytecode);

            var lineGlowGSBytecode = ShaderBytecode.CompileFromFile(glowShaderPath, "GSMain", "gs_5_0");
            _lineGlowGeometryShader = new GeometryShader(_resCache.Device, lineGlowGSBytecode);

            var lineGlowPSBytecode = ShaderBytecode.CompileFromFile(glowShaderPath, "PSMain", "ps_5_0");
            _lineGlowPixelShader = new PixelShader(_resCache.Device, lineGlowPSBytecode);

            _lineInputLayout = new InputLayout(
                _resCache.Device,
                ShaderSignature.GetInputSignature(lineVSBytecode),
                new[]
                {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                    new InputElement("LAYERID", 0, Format.R32_UInt, 12, 0),
                    new InputElement("OBJECTID", 0, Format.R32_UInt, 16, 0)
                });

            _lineShadersLoaded = true;
        }
        private void InitializeTextShaders()
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
            _textVertexShader = new VertexShader(_resCache.Device, textVSBytecode);

            var textPSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "PSMain", "ps_5_0");
            _textPixelShader = new PixelShader(_resCache.Device, textPSBytecode);

            // Layout
            _textInputLayout = new InputLayout(
                _resCache.Device,
                ShaderSignature.GetInputSignature(textVSBytecode),
                new[]
                {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                    new InputElement("LAYERID", 0, Format.R32_UInt, 12, 0),
                    new InputElement("OBJECTID", 0, Format.R32_UInt, 16, 0),
                    new InputElement("ISMOUSEOVER", 0, Format.R32_Float, 20, 0),
                    new InputElement("ISSELECTED", 0, Format.R32_Float, 24, 0),
                 });

            _textShaderLoaded = true;
        }
        private void InitializeCogoPointShaders()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            while (Path.GetFileName(path) != "Cad_Point_Manager")
            {
                path = Path.GetDirectoryName(path);
                if (path == null)
                    throw new DirectoryNotFoundException("The 'Cad_Point_Manager' directory could not be found in the path.");
            }

            string pointCircleShaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\PointMarkerShader.hlsl");
            var pointCircleVsb = ShaderBytecode.CompileFromFile(pointCircleShaderPath, "VSMain", "vs_5_0");
            var pointCirclePsb = ShaderBytecode.CompileFromFile(pointCircleShaderPath, "PSMain", "ps_5_0");
            var pointCircleGsb = ShaderBytecode.CompileFromFile(pointCircleShaderPath, "GSMain", "gs_5_0");
            _pointCircleVS = new VertexShader(_resCache.Device, pointCircleVsb);
            _pointCirclePS = new PixelShader(_resCache.Device, pointCirclePsb);
            _pointCircleGS = new GeometryShader(_resCache.Device, pointCircleGsb);
            _pointCircleInputLayout = new InputLayout(_resCache.Device, ShaderSignature.GetInputSignature(pointCircleVsb),
                new[]
                {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                    new InputElement("RADIUS",   0, Format.R32_Float,       12, 0),
                    new InputElement("LABEL_ID", 0, Format.R32_UInt,        16, 0),
                    new InputElement("POINT_ID", 0, Format.R32_UInt,        20, 0),
                    new InputElement("GROUP_ID", 0, Format.R32_UInt,        24, 0)
                });

            string glyphMeshShaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\GlyphMeshShader.hlsl");
            var glyphMeshVsb = ShaderBytecode.CompileFromFile(glyphMeshShaderPath, "VSMain", "vs_5_0");
            var glyphMeshPsb = ShaderBytecode.CompileFromFile(glyphMeshShaderPath, "PSMain", "ps_5_0");
            _glyphVS = new VertexShader(_resCache.Device, glyphMeshVsb);
            _glyphPS = new PixelShader(_resCache.Device, glyphMeshPsb);
            _glyphLayout = new InputLayout(_resCache.Device, ShaderSignature.GetInputSignature(glyphMeshVsb),
                new[]
                {
                    // Slot 0
                    new InputElement("POSITION",      0, Format.R32G32_Float,       0, 0, InputClassification.PerVertexData,   0),

                    // Slot 1
                    new InputElement("GLYPH_ORIGIN",  0, Format.R32G32_Float,       0, 1, InputClassification.PerInstanceData, 1),
                    new InputElement("GLYPH_SCALE",   0, Format.R32_Float,          8, 1, InputClassification.PerInstanceData, 1),
                    new InputElement("GLYPH_PEN",     0, Format.R32_Float,          12, 1, InputClassification.PerInstanceData, 1),
                    new InputElement("YSIGN",         0, Format.R32_Float,          16, 1, InputClassification.PerInstanceData, 1),
                    new InputElement("LABEL_ID",      0, Format.R32_UInt,           20, 1, InputClassification.PerInstanceData, 1),
                    new InputElement("POINT_ID",      0, Format.R32_UInt,           24, 1, InputClassification.PerInstanceData, 1),
                    new InputElement("GROUP_ID",      0, Format.R32_UInt,           28, 1, InputClassification.PerInstanceData, 1)
                });
            _glyphInstanceBuffer = new ResizableBuffer<GlyphInstance>(_resCache.Device, initialCapacity: 256);

            _cogoPointShadersLoaded = true;
        }
        private void InitializeCogoPointHoverShaders()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            while (Path.GetFileName(path) != "Cad_Point_Manager")
            {
                path = Path.GetDirectoryName(path);
                if (path == null)
                    throw new DirectoryNotFoundException("The 'Cad_Point_Manager' directory could not be found in the path.");
            }
            string circleHoverShaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\HoverCircleShader.hlsl");
            string rectHoverShaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\HoverRoundedRectShader.hlsl");

            var circleVSBytecode = ShaderBytecode.CompileFromFile(circleHoverShaderPath, "VSMain", "vs_5_0");
            _hoverCircleVertexShader = new VertexShader(_resCache.Device, circleVSBytecode);
            var circlePSBytecode = ShaderBytecode.CompileFromFile(circleHoverShaderPath, "PSMain", "ps_5_0");
            _hoverCirclePixelShader = new PixelShader(_resCache.Device, circlePSBytecode);
            var circleGSBytecode = ShaderBytecode.CompileFromFile(circleHoverShaderPath, "GSMain", "gs_5_0");
            _hoverCircleGeometryShader = new GeometryShader(_resCache.Device, circleGSBytecode);

            _hoverCircleLayout = new InputLayout(
                _resCache.Device,
                ShaderSignature.GetInputSignature(circleVSBytecode),
                new[]
                {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                    new InputElement("RADIUS", 0, Format.R32_Float, 12, 0),
                    new InputElement("ISSELECTED", 0, Format.R32_Float, 16, 0),
                });

            var rectVSBytecode = ShaderBytecode.CompileFromFile(rectHoverShaderPath, "VSMain", "vs_5_0");
            _hoverRectVertexShader = new VertexShader(_resCache.Device, rectVSBytecode);
            var rectPSBytecode = ShaderBytecode.CompileFromFile(rectHoverShaderPath, "PSMain", "ps_5_0");
            _hoverRectPixelShader = new PixelShader(_resCache.Device, rectPSBytecode);

            _hoverRectLayout = new InputLayout(
                _resCache.Device,
                ShaderSignature.GetInputSignature(rectVSBytecode),
                new[]
                {
                    // STREAM 0
                    new InputElement("POSITION", 0, Format.R32G32_Float,      0, 0, InputClassification.PerVertexData,   0),

                    // STREAM 1
                    new InputElement("CENTER",   0, Format.R32G32_Float,       0, 1, InputClassification.PerInstanceData, 1),
                    new InputElement("HALFSIZE", 0, Format.R32G32_Float,       8, 1, InputClassification.PerInstanceData, 1),
                    new InputElement("RF",       0, Format.R32G32_Float,      16, 1, InputClassification.PerInstanceData, 1),
                    new InputElement("COLOR",    0, Format.R32G32B32A32_Float,24, 1, InputClassification.PerInstanceData, 1),
                });

            _hoverRectInstanceBuffer ??= new(_resCache.Device, 6);
            var quad = new[]
            {
                new OverlayQuadVertex{ Local = new(-1,-1) },
                new OverlayQuadVertex{ Local = new(-1, 1) },
                new OverlayQuadVertex{ Local = new( 1, 1) },
                new OverlayQuadVertex{ Local = new(-1,-1) },
                new OverlayQuadVertex{ Local = new( 1, 1) },
                new OverlayQuadVertex{ Local = new( 1,-1) },
            };
            _hoverRectBuffer.Update(_resCache.DeviceContext, quad);

            _cogoHoverShadersLoaded = true;
        }
        private void InitializeOverlayShaders()
        {
            // Fill
            var path = AppDomain.CurrentDomain.BaseDirectory;
            while (Path.GetFileName(path) != "Cad_Point_Manager")
            {
                path = Path.GetDirectoryName(path);
                if (path == null) throw new DirectoryNotFoundException("Cad_Point_Manager not found");
            }

            string fx = Path.Combine(path, @"Controls\D3DControl\Shaders\OverlaySolidShader.hlsl");
            var vs = ShaderBytecode.CompileFromFile(fx, "VSMain", "vs_5_0");
            var ps = ShaderBytecode.CompileFromFile(fx, "PSMain", "ps_5_0");
            _overlayVS = new VertexShader(_resCache.Device, vs);
            _overlayPS = new PixelShader(_resCache.Device, ps);

            _overlayLayout = new InputLayout(
                _resCache.Device,
                ShaderSignature.GetInputSignature(vs),
                new[] {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                    new InputElement("COLOR",    0, Format.R32G32B32A32_Float, 12, 0),
                });

            // Border
            string outlineFx = Path.Combine(path, @"Controls\D3DControl\Shaders\OverlayOutlineShader.hlsl");
            var ovs = ShaderBytecode.CompileFromFile(outlineFx, "VSMain", "vs_5_0");
            var ops = ShaderBytecode.CompileFromFile(outlineFx, "PSMain", "ps_5_0");
            _overlayOutlineVS = new VertexShader(_resCache.Device, ovs);
            _overlayOutlinePS = new PixelShader(_resCache.Device, ops);

            // Reuse the SAME input layout as OverlaySolid (POSITION, COLOR)
            _overlayOutlineLayout = _overlayLayout;

            _overlayShaderLoaded = true;
        }
        private void InitializeToggleAnchorShaders()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            while (Path.GetFileName(path) != "Cad_Point_Manager")
            {
                path = Path.GetDirectoryName(path);
                if (path == null) throw new DirectoryNotFoundException("Cad_Point_Manager not found");
            }

            string fx = Path.Combine(path, @"Controls\D3DControl\Shaders\ToggleAnchorShader.hlsl");

            var vs = ShaderBytecode.CompileFromFile(fx, "VSMain", "vs_5_0");
            var ps = ShaderBytecode.CompileFromFile(fx, "PSMain", "ps_5_0");
            _toggleVS = new VertexShader(_resCache.Device, vs);
            _togglePS = new PixelShader(_resCache.Device, ps);

            _toggleLayout = new InputLayout(
                _resCache.Device,
                ShaderSignature.GetInputSignature(vs),
                new[]
                {
                    // stream 0
                    new InputElement("POSITION", 0, Format.R32G32_Float, 0, 0),
                    
                    // stream 1
                    new InputElement("TEXCOORD", 0, Format.R32G32_Float, 0, 1, InputClassification.PerInstanceData, 1), // Center (float2) @ offset 0
                    new InputElement("POINT_ID", 0, Format.R32_UInt,      8, 1, InputClassification.PerInstanceData, 1), // PointId  @ offset 8
                    new InputElement("GROUP_ID", 0, Format.R32_UInt,     12, 1, InputClassification.PerInstanceData, 1), // GroupId  @ offset 12
                });

            // Dedicated unit quad for this shader
            _toggleQuadVB ??= new(_resCache.Device, 6);
            var quad = new[]
            {
                new OverlayQuadVertex{ Local = new(-1,-1) },
                new OverlayQuadVertex{ Local = new(-1, 1) },
                new OverlayQuadVertex{ Local = new( 1, 1) },
                new OverlayQuadVertex{ Local = new(-1,-1) },
                new OverlayQuadVertex{ Local = new( 1, 1) },
                new OverlayQuadVertex{ Local = new( 1,-1) },
            };
            _toggleQuadVB.Update(_resCache.DeviceContext, quad);

            _anchorShaderLoaded = true;
        }
        private void InitializeLeaderLineShaders()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            while (Path.GetFileName(path) != "Cad_Point_Manager")
            {
                path = Path.GetDirectoryName(path);
                if (path == null)
                    throw new DirectoryNotFoundException("The 'Cad_Point_Manager' directory could not be found in the path.");
            }

            string shaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\LeaderLineShader.hlsl");

            // Main shaders
            var lineVSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "VSMain", "vs_5_0");
            _leaderLineVS = new VertexShader(_resCache.Device, lineVSBytecode);

            var linePSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "PSMain", "ps_5_0");
            _leaderLinePS = new PixelShader(_resCache.Device, linePSBytecode);

            var lineGSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "GSMain", "gs_5_0");
            _leaderLineGS = new GeometryShader(_resCache.Device, lineGSBytecode);

            _leaderLineInputLayout = new InputLayout(_resCache.Device, ShaderSignature.GetInputSignature(lineVSBytecode), new[]
            {
                new InputElement("POSITION", 0, Format.R32G32_Float,     0, 0), // A
                new InputElement("END", 0, Format.R32G32_Float,          8, 0), // BBase
                new InputElement("POINT_ID", 0, Format.R32_UInt,         16, 0), // PointId
                new InputElement("GROUP_ID", 0, Format.R32_UInt,         20, 0), // GroupId
            });

            _leaderLineShadersLoaded = true;
        }

        private void InitializeBuffers()
        {
            var device = _resCache.Device;

            _lineVertexBuffer?.Dispose();
            _lineVertexBuffer = new(device, GlobalHelperProperties.InitialLineVertices);

            _textVertexBuffer?.Dispose();
            _textVertexBuffer = new(device, GlobalHelperProperties.InitialTextVertices);

            _pointCircleVertexBuffer?.Dispose();
            _pointCircleVertexBuffer = new(device, GlobalHelperProperties.InitialCircleVertices);

            _hoverRectBuffer?.Dispose();
            _hoverRectBuffer = new(device, 64);

            _hoverRectInstanceBuffer?.Dispose();
            _hoverRectInstanceBuffer = new(device, 64);

            _hoverCircleBuffer?.Dispose();
            _hoverCircleBuffer = new(device, 16);

            _dragFillBuffer?.Dispose();
            _dragFillBuffer = new(device, 6);

            _ids ??= new();
            _stateBufs?.Dispose();
            _stateBufs = new(device, device.ImmediateContext);
            _stateCtl = new(_ids, _stateBufs);

            _anchorInstanceBuffer?.Dispose();
            _anchorInstanceBuffer = new(device, 64);

            _leaderLineBuffer?.Dispose();
            _leaderLineBuffer = new(device, 2);

            _buffersInitialized = true;
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
            _transformationBuffer = new Buffer(_resCache.Device, transformationBufferDesc);

            var viewportBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<ViewportBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _viewportBuffer = new Buffer(_resCache.Device, viewportBufferDesc);

            var lineBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<LineSettingsBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _lineSettingsBuffer = new Buffer(_resCache.Device, lineBufferDesc);

            var lineGlowBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<LineGlowSettingsBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _lineGlowSettingsBuffer = new Buffer(_resCache.Device, lineGlowBufferDesc);

            var pointTextBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<GlyphSettingsBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _cogoPointSettingsBuffer = new Buffer(_resCache.Device, pointTextBufferDesc);

            var circleBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<CircleSettingsBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _pointCircleSettingsBuffer = new Buffer(_resCache.Device, circleBufferDesc);

            var hoverCircleBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<CircleGlowSettingsBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _hoverCircleSettingsBuffer = new Buffer(_resCache.Device, hoverCircleBufferDesc);

            var leaderLineBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<LeaderLineSettings>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _leaderLineSettingsBuffer = new Buffer(_resCache.Device, leaderLineBufferDesc);

            var toggleAnchorBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<ToggleAnchorSettingsBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _toggleSettingsBuffer = new Buffer(_resCache.Device, toggleAnchorBufferDesc);

            var overlayOutlineBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<OverlayOutlineSettings>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _overlayOutlineSettingsBuffer = new Buffer(_resCache.Device, overlayOutlineBufferDesc);

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
            _resCache.DeviceContext.UpdateSubresource(ref transformationBuffer, _transformationBuffer);

            var viewportBuffer = new ViewportBuffer
            {
                ViewportSize = new(Viewport.Width, Viewport.Height)
            };
            _resCache.DeviceContext.UpdateSubresource(ref viewportBuffer, _viewportBuffer);

            var worldUnitsPerPixel = Camera.GetWorldUnitsPerPixel();

            var lineSettings = new LineSettingsBuffer
            {
                SelectedColor = GlobalHelperProperties.SelectedObjectColor,
                SelectedMouseOverColor = GlobalHelperProperties.SelectedMouseOverObjectColor
            };
            _resCache.DeviceContext.UpdateSubresource(ref lineSettings, _lineSettingsBuffer);

            var lineGlowSettings = new LineGlowSettingsBuffer
            {
                GlowOffset = GlobalHelperProperties.LineGlowPixelWidth * worldUnitsPerPixel,
                GlowTransparency = GlobalHelperProperties.LineGlowTransparency,
                SelectedColor = GlobalHelperProperties.SelectedObjectColor,
                SelectedMouseOverColor = GlobalHelperProperties.SelectedMouseOverGlowColor
            };
            _resCache.DeviceContext.UpdateSubresource(ref lineGlowSettings, _lineGlowSettingsBuffer);

            var cogoPointTextSettings = new GlyphSettingsBuffer
            {
                SelectedColor = GlobalHelperProperties.SelectedObjectColor,
            };
            _resCache.DeviceContext.UpdateSubresource(ref cogoPointTextSettings, _cogoPointSettingsBuffer);

            var circleSettings = new CircleSettingsBuffer
            {
                SelectedColor = GlobalHelperProperties.SelectedObjectColor,
                SelectedMouseOverColor = GlobalHelperProperties.SelectedMouseOverGlowColor
            };
            _resCache.DeviceContext.UpdateSubresource(ref circleSettings, _pointCircleSettingsBuffer);

            var hoverCircleSettings = new CircleGlowSettingsBuffer
            {
                GlowOffset = GlobalHelperProperties.LineGlowPixelWidth * worldUnitsPerPixel,
                GlowTransparency = GlobalHelperProperties.LineGlowTransparency,
                SelectedColor = GlobalHelperProperties.SelectedObjectColor,
                SelectedMouseOverColor = GlobalHelperProperties.SelectedMouseOverGlowColor
            };
            _resCache.DeviceContext.UpdateSubresource(ref hoverCircleSettings, _hoverCircleSettingsBuffer);

            var leaderLineSettings = new LeaderLineSettings
            {
                InvViewport = new(1 / Viewport.Width, 1 / Viewport.Height),
                PixelThickness = 1,
                SelectedColor = GlobalHelperProperties.SelectedObjectColor,
            };
            _resCache.DeviceContext.UpdateSubresource(ref leaderLineSettings, _leaderLineSettingsBuffer);

            var toggleSettings = new ToggleAnchorSettingsBuffer
            {
                BaseColor = AnchorBase,
                SelectedColor = AnchorPressed,
                MouseOverColor = AnchorHover,
                Size = AnchorWorldHalfSize,
                CornerRadius = AnchorWorldCornerRadius,
                Feather = AnchorWorldFeather
            };
            _resCache.DeviceContext.UpdateSubresource(ref toggleSettings, _toggleSettingsBuffer);

            ConstantBuffersDirty = false;
            _dxfDirty = true;
        }

        private Rect AddLineAndGetRect(string s, Vector2 originWorld, float duToWorldBase,
            float duToWorld, Vector4 color, float isVisible, float isMouseOver,
            float isSelected, float ySign, uint labelId, uint groupId, uint pointId,
            Vector2 lineOffset)
        {
            if (string.IsNullOrEmpty(s)) { return Rect.Empty; }

            Span<int> cps = stackalloc int[s.Length];
            for (int i = 0; i < s.Length; i++) { cps[i] = s[i]; }
            var gids = _resCache.CogoPointFontFace.GetGlyphIndices(cps.ToArray()); // or cachedzzza 
            float penDU = 0f;

            for (int i = 0; i < gids.Length; i++)
            {
                short gid = gids[i];
                if (gid <= 0) { continue; }

                var inst = new GlyphInstance
                {
                    Origin = originWorld,
                    DuToWorld = duToWorldBase,
                    PenDU = penDU,
                    YSign = ySign,
                    LabelId = labelId,
                    GroupId = groupId,
                    PointId = pointId,
                };

                if (!_glyphBatches.TryGetValue(gid, out var list)) { _glyphBatches[gid] = list = new List<GlyphInstance>(32); }

                list.Add(inst);
                penDU += _resCache.AdvanceWidthCache[gid];
            }

            float widthWorld = penDU * duToWorld;
            return ComputeLineRect(originWorld + lineOffset, widthWorld, duToWorld, ySign);
        }

        private void RecomputeCogoPointBoundsFast(CogoPoint p)
        {
            var pg = p.PointGroup;
            var duPerEm = _resCache.CogoPointFontFace.Metrics.DesignUnitsPerEm;
            float duToWorldBase = (float)pg.FontBaseSize / duPerEm;
            float duToWorld = (float)(pg.FontBaseSize * pg.PointScale) / duPerEm; // includes group scale
            float ySign = -1f;

            var baseOrigin = p.TextInfoBasePosition + p.TextInfoOffset;

            p.PointNumberBounds = MeasureLineRectFast(
                p.PointNumber.ToString(), baseOrigin, p.PointNumberOffset, duToWorldBase, duToWorld, ySign, p.PointGroup.PointScale.ToFloat());

            p.ElevationBounds = MeasureLineRectFast(
                p.Elevation.ToString("F3"), baseOrigin, p.ElevationOffset, duToWorldBase, duToWorld, ySign, p.PointGroup.PointScale.ToFloat());

            if (p.HasDescription)
                p.DescriptionBounds = MeasureLineRectFast(
                    p.Description, baseOrigin, p.DescriptionOffset, duToWorldBase, duToWorld, ySign, p.PointGroup.PointScale.ToFloat());
            else
                p.DescriptionBounds = Rect.Empty;

            // Circle stays pixel-fixed → recompute from WUPP and group scale
            float wupp = Camera.GetWorldUnitsPerPixel();
            float rW = (float)(GlobalHelperProperties.CogoPointCirclePixelRadius * wupp * pg.PointScale);
            var c = p.Position;
            p.EllipseBounds = new Rect(c.X - rW, c.Y - rW, 2 * rW, 2 * rW);

            p.UpdateBounds();

            // Make hover/interaction + hit tree pick up the new rects (no glyph rebuild)
            _interactiveDirty = true;
            HitTestableObjectTreeDirty = true;
        }
        private Rect MeasureLineRectFast(string s, Vector2 baseOrigin, Vector2 labelOffset,
                                        float duToWorldBase, float duToWorld, float ySign, float groupScale)
        {
            if (string.IsNullOrEmpty(s)) return Rect.Empty;

            // Same glyph ID lookup + advances you do in AddLineAndGetRect
            Span<int> cps = stackalloc int[s.Length];
            for (int i = 0; i < s.Length; i++) cps[i] = s[i];
            var gids = _resCache.CogoPointFontFace.GetGlyphIndices(cps.ToArray());

            float widthDU = 0f;
            for (int i = 0; i < gids.Length; i++)
            {
                short gid = gids[i];
                if (gid <= 0) continue;
                widthDU += _resCache.AdvanceWidthCache[gid];
            }

            // Shader applies origin + ls.Offset (+ ps.Offset) and scales DU by group
            //var originWorld = baseOrigin + labelOffset; // + pointOffset if you start using PointState.Offset
            var originWorld = baseOrigin + (labelOffset * groupScale);
            float widthWorld = widthDU * duToWorld;     // duToWorld includes group scale

            // Reuse your existing height/top computation (cap-height × duToWorld)
            return ComputeLineRect(originWorld, widthWorld, duToWorld, ySign);
        }
        private Rect ComputeLineRect(Vector2 originWorld, float widthWorld, float duToWorld, float ySign)
        {
            var m = _resCache.CogoPointFontFace.Metrics; // design units (DU)
            float capH = m.CapHeight * duToWorld;

            // baseline is originWorld.Y
            float topY = originWorld.Y - ySign * capH;
            float y = Math.Min(topY, originWorld.Y);
            float height = Math.Abs(capH);

            return new Rect(originWorld.X, y, widthWorld, height);
        }

        private void GetInitialMatrix()
        {
            if (!CadManager3D.DxfLoaded) { _dxfInitialMatrix = Matrix.Identity; }
            else
            {
                double scale = Math.Min(Viewport.Width / CadManager3D.Extents.Width, Viewport.Height / CadManager3D.Extents.Height);

                _dxfInitialMatrix = Matrix.Scaling(scale.ToFloat(), scale.ToFloat(), 1) * Matrix.Translation(-CadManager3D.Extents.Left.ToFloat(), -CadManager3D.Extents.Top.ToFloat(), 0);

                if (Camera is not null)
                {
                    Camera.ResetView(_dxfInitialMatrix, CadManager3D.Extents);
                    _hittestStrokeThickness = 7.0f / (Camera.InitialViewMatrix.M11 * Camera.CurrentZoom);
                    UpdateToggleAnchorDimensions();
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

            if (_pressedToggleButtonPoint != null)
            {
                var s = e.GetPosition(this);
                var w = Camera.ScreenToWorld(new Vector2((float)s.X, (float)s.Y));

                var delta = new Vector2(w.X - _pressedToggleButtonPoint.TextInfoBasePosition.X, w.Y - _pressedToggleButtonPoint.TextInfoBasePosition.Y);
                UpdateCogoPointOffset(_pressedToggleButtonPoint, delta);

                e.Handled = true;
                return;
            }

            if (Vector2.Distance(currentMousePos, _prevMousePos) > _panThreshold)
            {
                if (!_isPanning)
                {
                    UpdateDxfCoords(currentMousePos);
                }
                // Begin drag when crossing system threshold
                if (e.LeftButton == MouseButtonState.Pressed && !IsDragging)
                {
                    if (Math.Abs(_pointerCoords.X - _dragStartScreen.X) >= SystemParameters.MinimumHorizontalDragDistance ||
                        Math.Abs(_pointerCoords.Y - _dragStartScreen.Y) >= SystemParameters.MinimumVerticalDragDistance)
                    {
                        IsDragging = true;
                        UpdateDragRect();
                    }
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

                if (e.MiddleButton == MouseButtonState.Pressed)
                {
                    Camera.Pan(currentMousePos, _prevMousePos);
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
            _hittestStrokeThickness = 7.0f / (Camera.InitialViewMatrix.M11 * Camera.CurrentZoom);

            UpdateToggleAnchorDimensions();

            _hoverVerticesDirty = true;
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

            if (_mouseOverCogoPoints.Count > 0 || _mouseOverHitTestableObjects.Count > 0)
            {
                ResetHoverObjects();
                _lineVerticesDirty = true;
                _hoverVerticesDirty = true;
            }
        }
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (_pressedToggleButtonPoint != null)
            {
                RecomputeCogoPointBoundsFast(_pressedToggleButtonPoint);
                ResetCogoToggleButtonPress();

                if (IsMouseCaptured) { ReleaseMouseCapture(); }
                e.Handled = true;
                return;
            }

            _suspendHitTesting = true;
            bool geometryVerticesDirty = false;
            bool hoverVerticesDirty = false;
            bool cogoPointVerticesDirty = false;

            switch (CadManager3D.SnapSelectionMode)
            {
                case Common.Enums.SelectionMode.Geometries:
                    {
                        var newSel = new HashSet<DrawingGeometry3D>(_mouseOverHitTestableObjects.OfType<DrawingGeometry3D>());
                        if (IsDragging)
                        {
                            foreach (var g in newSel)
                            {
                                if (IsShiftPressed) { DeselectObject(g); }
                                else { SelectObject(g); }
                            }
                        }
                        else
                        {
                            var oldSel = new HashSet<DrawingGeometry3D>(SelectedGeometries);
                            foreach (var g in newSel.Except(oldSel))
                            {
                                if (IsShiftPressed || g.IsSelected) { DeselectObject(g); }
                                else { SelectObject(g); }
                            }
                        }
                        SelectedGeometries.AddRange(newSel);
                        geometryVerticesDirty = true;
                        break;
                    }

                case Common.Enums.SelectionMode.CogoPoints:
                    {
                        SelectedCogoPoints.DeferNotifications();

                        var newSel = new HashSet<CogoPoint>(_mouseOverCogoPoints);
                        if (IsDragging)
                        {
                            foreach (var p in newSel)
                            {
                                if (IsShiftPressed)
                                {
                                    if (!p.IsSelected) { continue; }
                                    DeselectObject(p); SelectedCogoPoints.Remove(p);
                                    hoverVerticesDirty = true; cogoPointVerticesDirty = true;
                                }
                                else
                                {
                                    if (p.IsSelected) { continue; }
                                    SelectObject(p); SelectedCogoPoints.Add(p);
                                    hoverVerticesDirty = true; cogoPointVerticesDirty = true;
                                }
                            }
                            _stateCtl.FlushPointUpdates();
                        }
                        else
                        {
                            foreach (var p in newSel)
                            {
                                if (IsShiftPressed)
                                {
                                    if (!p.IsSelected) { continue; }
                                    DeselectObject(p); SelectedCogoPoints.Remove(p);
                                    hoverVerticesDirty = true; cogoPointVerticesDirty = true;
                                }
                                else
                                {
                                    if (p.IsSelected) { continue; }
                                    SelectObject(p); SelectedCogoPoints.Add(p);
                                    hoverVerticesDirty = true; cogoPointVerticesDirty = true;
                                }
                            }
                            _stateCtl.FlushPointUpdates();
                        }
                        SelectedCogoPoints.EndDefer();
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

            if (geometryVerticesDirty) { _dxfDirty = true; }
            if (hoverVerticesDirty) { _hoverVerticesDirty = true; }
            if (cogoPointVerticesDirty) { _interactiveDirty = true; _dxfDirty = true; }

            EndDrag();

            _suspendHitTesting = false;
            UpdateDragRect();
        }
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            StartDrag(e.GetPosition(this));

            if (_mouseOverToggleButtonPoint != null)
            {
                PressCogoToggleButton(_mouseOverToggleButtonPoint);
                ResetHoverObjects();
                ResetCogoToggleButtonMouseOver();

                var s = e.GetPosition(this);
                var w = Camera.ScreenToWorld(new Vector2((float)s.X, (float)s.Y));
                var delta = new Vector2(w.X - _pressedToggleButtonPoint.TextInfoBasePosition.X, w.Y - _pressedToggleButtonPoint.TextInfoBasePosition.Y);
                UpdateCogoPointOffset(_pressedToggleButtonPoint, delta);
                _pressedToggleButtonPoint.HasLeaderLine = true;

                CaptureMouse();
                e.Handled = true;
                return;
            }
        }
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ResetSelectedObjects();
                EndDrag();
                _dxfDirty = true;
                _interactiveDirty = true;
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

        private void UpdateToggleAnchorDimensions()
        {
            float wupp = Camera.GetWorldUnitsPerPixel();

            float desiredHalfWorld = (AnchorPixelSize * 0.5f) * wupp;
            float drawingShort = Math.Min(Camera.Extents.Width, Camera.Extents.Height).ToFloat();
            float maxHalfWorld = (drawingShort * MaxCogoToggleToDrawingFraction) * 0.5f;

            AnchorWorldHalfSize = Math.Min(desiredHalfWorld, maxHalfWorld) / 2;
            AnchorWorldCornerRadius = Math.Min(AnchorWorldHalfSize * CornerFracOfHalf, AnchorWorldHalfSize - 1e-5f);
            AnchorWorldFeather = FeatherPx * wupp;
            CurrentAnchorBounds = new(0, 0, 2 * AnchorWorldHalfSize, 2 * AnchorWorldHalfSize);

            foreach (var pg in CadManager3D.CogoPointManager.PointGroups)
            {
                foreach (var p in pg.Value.Points)
                {
                    p.ToggleBounds = new(p.TextInfoBasePosition.X + p.TextInfoOffset.X - AnchorWorldHalfSize,
                                              p.TextInfoBasePosition.Y + p.TextInfoOffset.Y - AnchorWorldHalfSize,
                                              2 * AnchorWorldHalfSize,
                                              2 * AnchorWorldHalfSize);
                }
            }
        }
        private void UpdateCogoPointOffset(CogoPoint point, Vector2 offset)
        {
            if (point is null) { return; }

            bool labelsNeedUpdate = false;
            if (offset.X < 0 && !point.IsFlipped_Y)
            {
                point.IsFlipped_Y = true;

                point.PointNumberOffset = new(-point.PointNumberBounds.Width.ToFloat() - point.TextInfoBaseOffset_X / 2, point.PointNumberOffset.Y);
                point.ElevationOffset = new(-point.ElevationBounds.Width.ToFloat() - point.TextInfoBaseOffset_X / 2, point.ElevationOffset.Y);
                point.DescriptionOffset = new(-point.DescriptionBounds.Width.ToFloat() - point.TextInfoBaseOffset_X / 2, point.DescriptionOffset.Y);

                _stateCtl.SetLabelOffsets(point, point.PointNumberOffset, point.ElevationOffset, point.DescriptionOffset);
                labelsNeedUpdate = true;
            }
            if (offset.X > 0 && point.IsFlipped_Y)
            {
                point.IsFlipped_Y = false;

                point.PointNumberOffset = new(0, point.PointNumberOffset.Y);
                point.ElevationOffset = new(0, point.ElevationOffset.Y);
                point.DescriptionOffset = new(0, point.DescriptionOffset.Y);

                _stateCtl.SetLabelOffsets(point, point.PointNumberOffset, point.ElevationOffset, point.DescriptionOffset);
                labelsNeedUpdate = true;
            }

            if (offset.Y < 0 && !point.IsFlipped_X)
            {
                point.IsFlipped_X = true;

                point.PointNumberOffset = new(point.PointNumberOffset.X, -point.PointNumberBounds.Height.ToFloat() - point.BaseDescriptionOffset_Y);
                point.ElevationOffset = new(point.ElevationOffset.X, -point.ElevationBounds.Height.ToFloat() - point.BaseElevationOffset_Y);
                point.DescriptionOffset = new(point.DescriptionOffset.X, -point.DescriptionBounds.Height.ToFloat() - point.BasePointNumberOffset_Y);
                _stateCtl.SetLabelOffsets(point, point.PointNumberOffset, point.ElevationOffset, point.DescriptionOffset);
                labelsNeedUpdate = true;
            }
            if (offset.Y > 0 && point.IsFlipped_X)
            {
                point.IsFlipped_X = false;

                point.PointNumberOffset = new(point.PointNumberOffset.X, point.BasePointNumberOffset_Y);
                point.ElevationOffset = new(point.ElevationOffset.X, point.BaseElevationOffset_Y);
                point.DescriptionOffset = new(point.DescriptionOffset.X, point.BaseDescriptionOffset_Y);

                _stateCtl.SetLabelOffsets(point, point.PointNumberOffset, point.ElevationOffset, point.DescriptionOffset);
                labelsNeedUpdate = true;
            }

            if (labelsNeedUpdate) { _stateCtl.FlushLabelUpdates(); }

            point.SetTextInfoOffset(offset);
            _stateCtl.SetPointOffset(point, offset, true);
            _stateCtl.FlushPointUpdates();

            point.ToggleBounds = new(point.TextInfoBasePosition.X + point.TextInfoOffset.X - AnchorWorldHalfSize,
                                      point.TextInfoBasePosition.Y + point.TextInfoOffset.Y - AnchorWorldHalfSize,
                                      2 * AnchorWorldHalfSize,
                                      2 * AnchorWorldHalfSize);

            _interactiveDirty = true;
            _dxfDirty = true;
        }

        public void ZoomToExtents()
        {
            if (Camera is null) { return; }

            Camera.ResetView(_dxfInitialMatrix, CadManager3D.Extents);
            ResetHoverObjects();
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
                    ResetHoverObjects();

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
            var snappedObjectsCopy = _mouseOverHitTestableObjects.ToList();

            bool flushObjectStates = false;

            if (_mouseOverHitTestableObjects is not null && _mouseOverHitTestableObjects.Count > 0)
            {
                foreach (var snappedObj in snappedObjectsCopy)
                {
                    if (snappedObj.DistanceToPoint(_lastHitTestCoords) > _hittestStrokeThickness)
                    {
                        ResetHoverObjects();
                        flushObjectStates = true;

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
                                    _mouseOverHitTestableObjects.Add(geometry);
                                    HoverObject(geometry);
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
                            _mouseOverHitTestableObjects.Add(geometry);
                            HoverObject(geometry);
                            flushObjectStates = true;
                            _lastSnapHitTestIndex = _currentSnapHitTestIndex;
                        }
                    }
                }
            }

            if (flushObjectStates)
            {
                _stateCtl.FlushObjectUpdates();
                _dxfDirty = true;
            }
        }
        private void RunCogoPointsHitTest(CancellationToken token)
        {
            // Check for cancellation
            if (token.IsCancellationRequested) { token.ThrowIfCancellationRequested(); }
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(() => RunCogoPointsHitTest(token));
                return;
            }
            if (!CadManager3D.DxfLoaded) { return; }

            _lastHitTestCoords = new(DxfCoords.X, DxfCoords.Y);

            Rect rect = new(_lastHitTestCoords.X - _hittestStrokeThickness, _lastHitTestCoords.Y - _hittestStrokeThickness,
                _hittestStrokeThickness * 2, _hittestStrokeThickness * 2);
            var snappedCogoPointsCopy = _mouseOverCogoPoints.ToList();
            List<HitTestableObject> changedObjects = [];
            bool hoverVerticesDirty = false;

            if (snappedCogoPointsCopy is not null && snappedCogoPointsCopy.Count > 0)
            {
                foreach (var snappedCogoPoint in snappedCogoPointsCopy)
                {
                    if (snappedCogoPoint.DistanceToPoint(_lastHitTestCoords) > _hittestStrokeThickness)
                    {
                        changedObjects.Add(snappedCogoPoint);
                        ResetHoverObjects();
                        ResetCogoToggleButtonMouseOver();
                        hoverVerticesDirty = true;

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
                                    _mouseOverCogoPoints.Add(point);
                                    point.MouseEnter();
                                    _lastSnapHitTestIndex = _currentSnapHitTestIndex;

                                    if (point.IsSelected && point.ToggleBounds.Contains(_lastHitTestCoords)) { MouseOverCogoToggleButton(point); }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (snappedCogoPoint.IsSelected && !snappedCogoPoint.IsMouseOverToggleButton && snappedCogoPoint.ToggleBounds.Contains(_lastHitTestCoords))
                        {
                            MouseOverCogoToggleButton(snappedCogoPoint);
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
                            _mouseOverCogoPoints.Add(point);
                            point.MouseEnter();
                            hoverVerticesDirty = true;
                            _lastSnapHitTestIndex = _currentSnapHitTestIndex;

                            if (point.IsSelected && point.ToggleBounds.Contains(_lastHitTestCoords))
                            {
                                MouseOverCogoToggleButton(point);
                            }
                        }
                    }
                }
            }
            if (hoverVerticesDirty) { _hoverVerticesDirty = true; }
        }
        private async void RunDragCogoPointsHittest(CancellationToken token)
        {
            if (token.IsCancellationRequested) { return; }
            if (!CadManager3D.DxfLoaded) { return; }

            // Read DragRect safely from UI thread (cheap, single read)
            Rect currentRect = await Dispatcher.InvokeAsync(() => DragRect, DispatcherPriority.Render);
            if (currentRect.IsEmpty || currentRect.Width <= 0 || currentRect.Height <= 0) { return; }

            var newSet = CadManager3D
                .HitTestDragCogoPoints(currentRect)
                .Where(p => currentRect.Contains(p.Bounds))
                .ToHashSet();

            // 2) Compute diffs off-thread
            List<CogoPoint> adds, removes;
            lock (_dragCogoLock)
            {
                adds = newSet.Except(_dragCogoCurrent).ToList();
                removes = _dragCogoCurrent.Except(newSet).ToList();
                _dragCogoCurrent = newSet; // update snapshot
            }

            foreach (var p in adds)
            {
                p.MouseEnter();
                _mouseOverCogoPoints.Add(p);
            }
            foreach (var p in removes)
            {
                p.MouseLeave();
                _mouseOverCogoPoints.Remove(p);
            }

            if (adds.Count > 0 || removes.Count > 0) { _hoverVerticesDirty = true; }

            //Something is causing the cogo text hover vertices to not reset and the the drag Rect
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

            bool flushObjectStates = false;

            foreach (var region in addedRegions)
            {
                var newHits = CadManager3D.HitTestDragGeometries(region).Distinct();

                foreach (var geometry in newHits)
                {
                    if (DragRect.Contains(geometry.Bounds))
                    {
                        _mouseOverHitTestableObjects.Add(geometry);
                        HoverObject(geometry);
                        flushObjectStates = true;
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
                        _mouseOverHitTestableObjects.Remove(geometry);
                        DehoverObject(geometry);
                        flushObjectStates = true;
                    }
                }
            }
            _lastQueriedDxfRect = DragRect;

            if (flushObjectStates)
            {
                _stateCtl.FlushObjectUpdates();
                _dxfDirty = true;
            }

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

        private void HoverObject(HitTestableObject hitTestableObject)
        {
            if (hitTestableObject is not null && !hitTestableObject.IsMouseOver)
            {
                if (hitTestableObject is DrawingObject3D obj)
                {
                    if (obj is DrawingGeometry3D geometry)
                    {
                        geometry.MouseEnter();
                        _stateCtl.SetObjectMouseOver(geometry, true);
                    }
                }
                if (hitTestableObject is CogoPoint dxfPoint)
                {
                    if (!dxfPoint.IsSelected)
                    {
                        dxfPoint.MouseEnter();
                    }
                }
                if (hitTestableObject is HitTestablePoint point)
                {
                    point.MouseEnter();
                }
            }
        }
        private void DehoverObject(HitTestableObject hitTestableObject)
        {
            if (hitTestableObject is not null && hitTestableObject.IsMouseOver)
            {
                if (hitTestableObject is DrawingObject3D obj)
                {
                    if (obj is DrawingGeometry3D geometry)
                    {
                        geometry.MouseLeave();
                        _stateCtl.SetObjectMouseOver(geometry, false);
                    }
                }
                if (hitTestableObject is CogoPoint dxfPoint)
                {
                    if (dxfPoint.IsSelected)
                    {
                        dxfPoint.MouseLeave();
                    }
                }
                if (hitTestableObject is HitTestablePoint point)
                {
                    point.MouseLeave();
                }
            }
        }
        public void ResetHoverObjects()
        {
            if (SnappedHitTestablePoint is not null)
            {
                SnappedHitTestablePoint = null;
            }
            foreach (var obj in _mouseOverHitTestableObjects)
            {
                DehoverObject(obj);
            }
            _stateCtl.FlushObjectUpdates();

            foreach (var point in _mouseOverCogoPoints) { point.MouseLeave(); }

            _mouseOverHitTestableObjects.Clear();
            _mouseOverCogoPoints.Clear();

            if (_mouseOverToggleButtonPoint is not null)
            {
                _mouseOverToggleButtonPoint.IsMouseOverToggleButton = false;
                _stateCtl.SetPointAnchorMouseOver(_mouseOverToggleButtonPoint, false);
                _mouseOverToggleButtonPoint = null;
                _stateCtl.FlushPointUpdates();
            }
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
                        _stateCtl.SetObjectSelected(geometry, true);
                    }
                }
                if (hitTestableObject is CogoPoint dxfPoint)
                {
                    if (!dxfPoint.IsSelected)
                    {
                        dxfPoint.Select();
                        _stateCtl.SetPointSelected(dxfPoint, true);
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
                        _stateCtl.SetObjectSelected(geometry, false);
                    }
                }
                if (hitTestableObject is CogoPoint dxfPoint)
                {
                    if (dxfPoint.IsSelected)
                    {
                        dxfPoint.Deselect();
                        _stateCtl.SetPointSelected(dxfPoint, false);
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

            var cogoPointsCopy = SelectedCogoPoints.ToList();
            foreach (var point in cogoPointsCopy) { DeselectObject(point); }
            SelectedCogoPoints.Clear();

            _stateCtl.FlushPointUpdates();
            _stateCtl.FlushObjectUpdates();
            _lineVerticesDirty = _textVerticesDirty = _dxfDirty = _interactiveDirty = true;
        }
        public void EndDrag()
        {
            IsDragging = false;
            DragRect = new(0, 0, 0, 0);
            _lastQueriedDxfRect = Rect.Empty;
        }
        public void StartDrag(Point start)
        {
            _dragStartScreen = start;
            _dragStart = DxfCoords.ToPoint();
            DragRect = new(0, 0, 0, 0);
            _dxfDragRectTranslate = new(0, 0);
            CurrentlyAppliedDragRectMatrix = new();
        }

        private void MouseOverCogoToggleButton(CogoPoint cogoPoint)
        {
            if (_mouseOverToggleButtonPoint is not null)
            {
                if (_mouseOverToggleButtonPoint == cogoPoint) { return; }
                else
                {
                    _mouseOverToggleButtonPoint.IsMouseOverToggleButton = false;
                    _stateCtl.SetPointAnchorMouseOver(_mouseOverToggleButtonPoint, false);
                    _mouseOverToggleButtonPoint = cogoPoint;
                    _mouseOverToggleButtonPoint.IsMouseOverToggleButton = true;
                    _stateCtl.SetPointAnchorMouseOver(_mouseOverToggleButtonPoint, true);
                    _stateCtl.FlushPointUpdates();
                }
            }
            else
            {
                _mouseOverToggleButtonPoint = cogoPoint;
                _mouseOverToggleButtonPoint.IsMouseOverToggleButton = true;
                _stateCtl.SetPointAnchorMouseOver(_mouseOverToggleButtonPoint, true);
                _stateCtl.FlushPointUpdates();
            }
        }
        private void ResetCogoToggleButtonMouseOver()
        {
            _mouseOverToggleButtonPoint?.IsMouseOverToggleButton = false;
            _mouseOverToggleButtonPoint = null;
        }
        private void PressCogoToggleButton(CogoPoint cogoPoint)
        {
            if (_pressedToggleButtonPoint is not null)
            {
                if (_pressedToggleButtonPoint == cogoPoint) { return; }
                else
                {
                    _pressedToggleButtonPoint.IsToggleButtonPressed = false;
                    _pressedToggleButtonPoint = cogoPoint;
                    _pressedToggleButtonPoint.IsToggleButtonPressed = true;
                }
            }
            else
            {
                _pressedToggleButtonPoint = cogoPoint;
                _pressedToggleButtonPoint.IsToggleButtonPressed = true;
            }
        }
        private void ResetCogoToggleButtonPress()
        {
            _pressedToggleButtonPoint?.IsToggleButtonPressed = false;
            _pressedToggleButtonPoint = null;
        }

        private void ClearDxf()
        {
            Camera.ResetView(Matrix.Identity, CadManager3D.Extents);
            ResetHoverObjects();
            _lineVerticesDirty = _textVerticesDirty = true;
        }

        private static void OnCadManager3DChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not D3dDxfControl control) { return; }

            if (e.OldValue is CadManager3D oldCadManager3D)
            {
                oldCadManager3D.PropertyChanged -= control.CadManager3D_PropertyChanged;
                oldCadManager3D.ZoomToExtentsRequested -= control.ZoomToExtents;
                oldCadManager3D.CogoPointManager.CogoPoints.CollectionChanged -= control.CogoPoints_CollectionChanged;
                oldCadManager3D.CogoPointManager.PointGroups.CollectionChanged -= control.PointGroups_CollectionChanged;
                oldCadManager3D.Layers.CollectionChanged -= control.Layers_CollectionChanged;
            }

            if (e.NewValue is CadManager3D newCadManager3D)
            {
                newCadManager3D.PropertyChanged += control.CadManager3D_PropertyChanged;
                newCadManager3D.ZoomToExtentsRequested += control.ZoomToExtents;
                newCadManager3D.CogoPointManager.CogoPoints.CollectionChanged += control.CogoPoints_CollectionChanged;
                newCadManager3D.CogoPointManager.PointGroups.CollectionChanged += control.PointGroups_CollectionChanged;
                newCadManager3D.Layers.CollectionChanged += control.Layers_CollectionChanged;
            }
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
                    _glyphVerticesDirty = true;
                    _leaderLineVerticesDirty = true;
                    _anchorVerticesDirty = true;
                }
            }
            if (e.PropertyName == nameof(CadManager3D.PointCircleVerticesDirty))
            {
                if (CadManager3D.PointCircleVerticesDirty)
                {
                    _pointCircleVerticesDirty = true;
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
                ResetHoverObjects();
                _currentSnapHitTestIndex = 0;
            }
        }

        private void CogoPoint_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CogoPoint.Easting) || e.PropertyName == nameof(CogoPoint.Northing) ||
                e.PropertyName == nameof(CogoPoint.Elevation) || e.PropertyName == nameof(CogoPoint.Description))
            {
                _glyphVerticesDirty = true;
                _pointCircleVerticesDirty = true;
                _anchorVerticesDirty = true;
                _leaderLineVerticesDirty = true;
            }
        }

        private void PointGroups_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            PointGroups = CadManager3D?.CogoPointManager?.PointGroups;
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (KeyValuePair<string, PointGroup> keyValue in e.NewItems)
                {
                    var pg = keyValue.Value;
                    pg.PropertyChanged -= PointGroup_PropertyChanged;
                    pg.PropertyChanged += PointGroup_PropertyChanged;

                    var gId = _ids.GetOrAddGroupId(pg);
                    _stateBufs.InitializeGroupState(_ids.GroupCount, pg, gId);

                    _stateBufs.EnsureGroupCapacity(_ids.GroupCount);

                    ref var gs = ref _stateBufs.GroupSpan[(int)gId];
                    gs.Color = pg.Color.ToSharpDXVector4();
                    gs.Scale = (float)pg.PointScale;
                    gs.Flags = pg.IsVisible ? 1u : 0u;
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (KeyValuePair<string, PointGroup> keyValue in e.OldItems)
                {
                    var pg = keyValue.Value;
                    if (pg is null) { continue; }
                    pg.PropertyChanged -= PointGroup_PropertyChanged;
                }
            }
        }
        private void CogoPoints_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            CogoPoints = CadManager3D?.CogoPointManager?.CogoPoints;
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var obj in e.NewItems)
                {
                    if (obj is not CogoPoint cp) { continue; }
                    cp.PropertyChanged -= CogoPoint_PropertyChanged;
                    cp.PropertyChanged += CogoPoint_PropertyChanged;

                    uint pId = _ids.GetOrAddPointId(cp);
                    _stateBufs.InitializePointState(_ids.PointCount, cp, pId);

                    uint idPN = _ids.GetOrAddLabelId(cp, 0);
                    _stateBufs.InitializeLabelState(_ids.MaxLabelCount, cp.PointNumberOffset, idPN);
                    uint idElev = _ids.GetOrAddLabelId(cp, 1);
                    _stateBufs.InitializeLabelState(_ids.MaxLabelCount, cp.ElevationOffset, idElev);
                    uint idDesc = _ids.GetOrAddLabelId(cp, 2);
                    _stateBufs.InitializeLabelState(_ids.MaxLabelCount, cp.DescriptionOffset, idDesc);
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var obj in e.OldItems)
                {
                    if (obj is not CogoPoint cogoPoint) { continue; }
                    cogoPoint.PropertyChanged -= CogoPoint_PropertyChanged;
                }
            }
        }
        private void Layers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Layers = CadManager3D?.Layers;
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (KeyValuePair<string, ObjectLayer3D> keyValue in e.NewItems)
                {
                    var layer = keyValue.Value;
                    if (layer is null) { continue; }

                    layer.PropertyChanged -= Layer_PropertyChanged;
                    layer.PropertyChanged += Layer_PropertyChanged;
                    layer.DrawingObject3Ds.CollectionChanged -= DrawingObject3Ds_CollectionChanged;
                    layer.DrawingObject3Ds.CollectionChanged += DrawingObject3Ds_CollectionChanged;

                    var lid = _ids.GetOrAddLayerId(layer);
                    _stateBufs.InitializeLayerState(_ids.LayerCount, layer, lid);
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (KeyValuePair<string, ObjectLayer3D> keyValue in e.OldItems)
                {
                    var layer = keyValue.Value;
                    if (layer is null) { continue; }

                    layer.PropertyChanged -= Layer_PropertyChanged;
                    layer.DrawingObject3Ds.CollectionChanged -= DrawingObject3Ds_CollectionChanged;
                }
            }
        }
        private void DrawingObject3Ds_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var obj in e.NewItems)
                {
                    if (obj is not DrawingObject3D drawingObj) { continue; }

                    var oId = _ids.GetOrAddObjectId(drawingObj);
                    _stateBufs.InitializeObjectState(_ids.ObjectCount, drawingObj, oId);
                }
            }
            //if (e.Action == NotifyCollectionChangedAction.Remove)
            //{

            //}
        }

        private void Layer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ObjectLayer3D.IsVisible))
            {
                if (sender is ObjectLayer3D layer)
                {
                    _stateCtl.SetLayerVisibility(layer, layer.IsVisible);
                    _stateCtl.FlushLayerUpdates();
                    _dxfDirty = true;
                }
            }
            if (e.PropertyName == nameof(ObjectLayer3D.Color))
            {
                if (sender is ObjectLayer3D layer)
                {
                    _stateCtl.SetLayerColor(layer, layer.Color);
                    _stateCtl.FlushLayerUpdates();
                    _dxfDirty = true;
                }
            }
        }
        private void PointGroup_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PointGroup.IsVisible))
            {
                if (sender is PointGroup pg)
                {
                    _stateCtl.SetGroupVisibility(pg, pg.IsVisible);
                    _stateCtl.FlushGroupUpdates();
                    _dxfDirty = true;
                }
            }
            if (e.PropertyName == nameof(PointGroup.Color) || e.PropertyName == nameof(PointGroup.PointScale))
            {
                if (sender is PointGroup pg)
                {
                    _stateCtl.SetGroupScaleColor(pg, pg.PointScale.ToFloat(), pg.Color.ToSharpDXVector4());
                    _stateCtl.FlushGroupUpdates();
                    _dxfDirty = true;

                    foreach (var point in pg.Points)
                    {
                        RecomputeCogoPointBoundsFast(point);
                    }
                }
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

                    _textVertexBuffer?.Dispose(); _textVertexBuffer = null;
                    _textVertexShader?.Dispose(); _textVertexShader = null;
                    _textPixelShader?.Dispose(); _textPixelShader = null;
                    _textInputLayout?.Dispose(); _textInputLayout = null;

                    _lineVertexBuffer?.Dispose(); _lineVertexBuffer = null;
                    _lineSettingsBuffer?.Dispose(); _lineSettingsBuffer = null;
                    _lineGlowSettingsBuffer?.Dispose(); _lineGlowSettingsBuffer = null;
                    _lineVertexShader?.Dispose(); _lineVertexShader = null;
                    _linePixelShader?.Dispose(); _linePixelShader = null;
                    _lineGlowVertexShader?.Dispose(); _lineGlowVertexShader = null;
                    _lineGlowPixelShader?.Dispose(); _lineGlowPixelShader = null;
                    _lineGlowGeometryShader?.Dispose(); _lineGlowGeometryShader = null;
                    _lineInputLayout?.Dispose(); _lineInputLayout = null;

                    _transformationBuffer?.Dispose(); _transformationBuffer = null;

                    _hitTestCancellationTokenSource?.Dispose(); _hitTestCancellationTokenSource = null;

                    _hoverRectBuffer?.Dispose(); _hoverRectBuffer = null;
                    _hoverRectInstanceBuffer?.Dispose(); _hoverRectInstanceBuffer = null;
                    _hoverRectLayout?.Dispose(); _hoverRectLayout = null;
                    _hoverRectVertexShader?.Dispose(); _hoverRectVertexShader = null;
                    _hoverRectPixelShader?.Dispose(); _hoverRectPixelShader = null;
                    _hoverCircleBuffer?.Dispose(); _hoverCircleBuffer = null;
                    _hoverCircleVertexShader?.Dispose(); _hoverCircleVertexShader = null;
                    _hoverCirclePixelShader?.Dispose(); _hoverCirclePixelShader = null;
                    _hoverCircleGeometryShader?.Dispose(); _hoverCircleGeometryShader = null;
                    _hoverCircleSettingsBuffer?.Dispose(); _hoverCircleSettingsBuffer = null;
                    _hoverCircleLayout?.Dispose(); _hoverCircleLayout = null;

                    _leaderLineBuffer?.Dispose(); _leaderLineBuffer = null;
                    _leaderLineGS?.Dispose(); _leaderLineGS = null;
                    _leaderLinePS?.Dispose(); _leaderLinePS = null;
                    _leaderLineVS?.Dispose(); _leaderLineVS = null;
                    _leaderLineInputLayout?.Dispose(); _leaderLineInputLayout = null;

                    _toggleLayout?.Dispose(); _toggleLayout = null;
                    _toggleQuadVB?.Dispose(); _toggleQuadVB = null;
                    _toggleVS?.Dispose(); _toggleVS = null;
                    _togglePS?.Dispose(); _togglePS = null;

                    _stateBufs.Dispose(); _stateBufs = null;
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
