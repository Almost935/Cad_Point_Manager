using Cad_Point_Manager.Common.Collections;
using Cad_Point_Manager.Controls.D3DControl.Buffers;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Helpers.EqualityComparers;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.DrawingObjects;
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
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using Buffer = SharpDX.Direct3D11.Buffer;
using InputElement = SharpDX.Direct3D11.InputElement;
using Matrix = SharpDX.Matrix;
using Point = System.Windows.Point;

namespace Cad_Point_Manager.Controls.D3DControl
{
    public class D3dDxfControl : Direct3DControl, INotifyPropertyChanged, IDisposable
    {
        #region Fields
        private const float HoverTextPadPx = 3f;     // grow the text rect by this many pixels
        private const float HoverEllipsePadPx = 2f;  // extra pixels over the point radius

        // CogoPoint ToggleButton Fields
        private float _desiredHalfWorldForAnchors;
        private float _maxHalfBaseForAnchors;
        private float _featherWorldForAnchors;

        public float AnchorPixelSize = 18f; // UI handle size in pixels
        public float FeatherPx = 1.25f; // anti-aliased edge in px
        public float CornerFracOfHalf = 0.35f; // rounded corner as a fraction of half
        public float MaxCogoToggleToDrawingFraction = 0.02f;// cap relative to drawing extents
        private static readonly Vector4 AnchorBaseColor = new(0.00f, 0.95f, 1.00f, 1.00f);
        private static readonly Vector4 AnchorHoverColor = new(0.67f, 1.00f, 1.00f, 1.00f);
        private static readonly Vector4 AnchorPressedColor = new(0.15f, 0.82f, 0.85f, 1.00f);

        private bool _dxfDirty = true;
        private bool _combinedDirty = true;
        private Buffer _transformationBuffer;
        private Buffer _viewportBuffer;

        private Point _pointerCoords;
        private Vector2 _dxfCoords;
        private string _dxfCoordsString = $"X: {0:F3}   Y: {0:F3}";
        private Matrix _dxfInitialMatrix = Matrix.Identity;
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
        private Buffer _dxfObjectSettingsBuffer;
        private Buffer _lineRenderModeBuffer;
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

        // Solid shader related fields
        private ResizableBuffer<SolidVertex> _solidVertexBuffer;
        private int _solidVertexCount;
        private VertexShader _solidVertexShader;
        private PixelShader _solidPixelShader;
        private InputLayout _solidInputLayout;
        private bool _solidShaderLoaded = false;
        private bool _solidVerticesDirty = false;

        // CogoPoint shader related fields
        private bool _cogoPointShadersLoaded = false;

        // Significant point rendering fields
        private ResizableBuffer<SignificantPointVertex> _sigPointVertexBuffer;
        private Buffer _sigPointSettingsBuffer;
        private InputLayout _sigPointLayout;
        private VertexShader _sigPointVS;
        private PixelShader _sigPointPS;
        private GeometryShader _sigPointGS;
        private bool _sigPointVerticesDirty = false;
        private bool _sigPointShadersLoaded = false;
        private int _sigPointVertexCount;

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
        private Buffer _leaderLineSettings;

        // Cogo point leader line glow rendering fields
        private VertexShader _leaderLineGlowVS;
        private PixelShader _leaderLineGlowPS;
        private GeometryShader _leaderLineGlowGS;
        private Buffer _leaderLineGlowSettings;

        // Cogo point hover rendering
        private bool _cogoHoverShadersLoaded = false;
        private bool _cogoHoverVerticesDirty = false;

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
            SelectedColor = GlobalHelperProperties.SelectedObjectColor,
            HoverColor = GlobalHelperProperties.HoverColor
        };
        private Buffer _cogoPointGlowSettingsBuffer;
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
        private List<(double distance, DrawingGeometry geometry)> _nearestHitTestableGeometries = [];
        private List<(double distance, CogoPoint point)> _nearestHitTestableCogoPoints = [];
        private readonly HashSet<HitTestableObject> _mouseOverHitTestableObjects = [];
        private readonly HashSet<CogoPoint> _mouseOverCogoPoints = new(new CogoPointNumberComparer());

        // CogoPoint Movement Fields
        private CogoPoint _mouseOverToggleButtonPoint = null;
        private CogoPoint _pressedToggleButtonPoint = null;
        private bool _cogoPointTextBeingMoved => _pressedToggleButtonPoint is not null;
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
        public CadManager CadManager
        {
            get { return (CadManager)GetValue(CadManager3DProperty); }
            set { SetValue(CadManager3DProperty, value); }
        }
        public static readonly DependencyProperty CadManager3DProperty =
        DependencyProperty.Register(
            nameof(CadManager),
            typeof(CadManager),
            typeof(D3dDxfControl),
            new PropertyMetadata(null, OnCadManagerChanged));

        public static readonly DependencyProperty LayersProperty =
            DependencyProperty.Register(
                nameof(Layers),
                typeof(BatchableObservableCollection<KeyValuePair<string, ObjectLayer>>),
                typeof(D3dDxfControl),
                new FrameworkPropertyMetadata(new BatchableObservableCollection<KeyValuePair<string, ObjectLayer>>()));
        public BatchableObservableCollection<KeyValuePair<string, ObjectLayer>> Layers
        {
            get => (BatchableObservableCollection<KeyValuePair<string, ObjectLayer>>)GetValue(LayersProperty);
            set => SetValue(LayersProperty, value);
        }

        public static readonly DependencyProperty PointGroupsProperty =
            DependencyProperty.Register(
                nameof(PointGroups),
                typeof(BatchableObservableCollection<PointGroup>),
                typeof(D3dDxfControl),
                new FrameworkPropertyMetadata(new BatchableObservableCollection<PointGroup>()));
        public BatchableObservableCollection<PointGroup> PointGroups
        {
            get => (BatchableObservableCollection<PointGroup>)GetValue(PointGroupsProperty);
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
                typeof(BatchableObservableCollection<DrawingGeometry>),
                typeof(D3dDxfControl),
                new FrameworkPropertyMetadata(new BatchableObservableCollection<DrawingGeometry>(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public BatchableObservableCollection<DrawingGeometry> SelectedGeometries
        {
            get => (BatchableObservableCollection<DrawingGeometry>)GetValue(SelectedGeometriesProperty);
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

        public static readonly DependencyProperty SceneIdMapProperty =
            DependencyProperty.Register(
                nameof(SceneIdMap),
                typeof(SceneIdMap),
                typeof(D3dDxfControl),
                new FrameworkPropertyMetadata(null));
        public SceneIdMap SceneIdMap
        {
            get => (SceneIdMap)GetValue(SceneIdMapProperty);
            set => SetValue(SceneIdMapProperty, value);
        }

        public static readonly DependencyProperty StateControllerProperty =
            DependencyProperty.Register(
                nameof(StateController),
                typeof(D3dStateController),
                typeof(D3dDxfControl),
                new FrameworkPropertyMetadata(null));
        public D3dStateController StateController
        {
            get => (D3dStateController)GetValue(StateControllerProperty);
            set => SetValue(StateControllerProperty, value);
        }

        public static readonly DependencyProperty StateBuffersProperty =
            DependencyProperty.Register(
                nameof(StateBuffers),
                typeof(D3dStateBuffers),
                typeof(D3dDxfControl),
                new FrameworkPropertyMetadata(null));
        public D3dStateBuffers StateBuffers
        {
            get => (D3dStateBuffers)GetValue(StateBuffersProperty);
            set => SetValue(StateBuffersProperty, value);
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
            if (ResCache is null) { return; }

            if (CadManager.Camera is null)
            {
                SetInitialMatrix();
                CadManager.Camera = new(Viewport, GlobalHelperProperties.ZoomFactor, new Rect(0, 0, Viewport.Width, Viewport.Height));
                CadManager.ResetTemplates();
            }
            if (DxfNeedsReload)
            {
                SetInitialMatrix();
                CadManager.Camera.ResetView(_dxfInitialMatrix, CadManager.Extents);
                CadManager.ResetTemplates();

                ConstantBuffersDirty = true;
                DxfNeedsReload = false;
                CadManager.DxfNeedsReload = false;
            }
            if (!_buffersInitialized) { InitializeBuffers(); }

            if (_lineVerticesDirty) { UpdateLineVertices(); }
            if (_textVerticesDirty) { UpdateTextVertices(); }
            if (_solidVerticesDirty) { UpdateSolidVertices(); }
            if (_glyphVerticesDirty) { UpdateGlyphBatches(); }
            if (_pointCircleVerticesDirty) { UpdatePointCircleVertices(); }
            if (_cogoHoverVerticesDirty) { UpdateCogoHoverVertices(); }
            if (HitTestableObjectTreeDirty) { LoadHitTestableObjectTree(); }
            if (_anchorVerticesDirty) { UpdateToggleAnchorVertices(); }
            if (_leaderLineVerticesDirty) { UpdateLeaderLineVertices(); }
            if (_sigPointVerticesDirty) { UpdateSignificantPointVertices(); }

            if (!_lineShadersLoaded) { InitializeLineShaders(); }
            if (!_textShaderLoaded) { InitializeTextShaders(); }
            if (!_solidShaderLoaded) { InitializeSolidShaders(); }
            if (!_overlayShaderLoaded) { InitializeOverlayShaders(); }
            if (!_cogoPointShadersLoaded) { InitializeCogoPointShaders(); }
            if (!_cogoHoverShadersLoaded) { InitializeCogoPointHoverShaders(); }
            if (!_anchorShaderLoaded) { InitializeToggleAnchorShaders(); }
            if (!_leaderLineShadersLoaded) { InitializeLeaderLineShaders(); }
            if (!_sigPointShadersLoaded) { InitializeSignificantPointsShaders(); }

            if (!ConstantBuffersInitialized) { InitializeConstantBuffers(); }
            if (ConstantBuffersDirty || CadManager.Camera.IsDirty) { UpdateConstantBuffers(); }

            if (!_hitTestIsRunning)
            {
                _hitTestIsRunning = true;
                _hittestTask = Task.Run(() => RunHitTestingAsync());
            }

            var ctx = ResCache.DeviceContext;

            if (_dxfDirty)
            {
                DrawDxf(ctx);
                _dxfDirty = false;
                _combinedDirty = true;
            }

            if (_combinedDirty)
            {
                ctx.CopyResource(ResCache.DxfTexture, ResCache.Texture2D);
                ctx.OutputMerger.SetRenderTargets(ResCache.RenderTargetView);

                if (IsDragging && _dragFillVertexCount > 0) { DrawDragOverlay(ctx); }
                if (_hoverRectInstanceCount > 0 || _hoverCircleVertices.Count > 0) { DrawCogoPointHover(ctx); }
                if (_leaderLineInstanceCount > 0) { DrawLeaderLines(ctx); }
                if (_anchorVerticesCount > 0) { DrawCogoPointAnchors(ctx); }
                if (_sigPointVertexCount > 0) { DrawSignificantPoints(ctx); }

                _combinedDirty = false;
            }
        }

        private void DrawDxf(DeviceContext ctx)
        {
            ctx.OutputMerger.SetRenderTargets(ResCache.DxfRenderTargetView);
            ctx.ClearRenderTargetView(ResCache.DxfRenderTargetView, new RawColor4(1, 1, 1, 1));

            DrawLines(ctx);
            DrawText(ctx);
            DrawSolids(ctx);
            DrawPointCircles(ctx);
            DrawGlyphBatches(ctx, ResCache.AsciiGlyphAtlas, _glyphBatches);
        }

        private void DrawLines(DeviceContext ctx)
        {
            if (_lineVertexBuffer is null) { return; }

            ctx.VertexShader.Set(_lineGlowVertexShader);
            ctx.GeometryShader.Set(_lineGlowGeometryShader);
            ctx.PixelShader.Set(_lineGlowPixelShader);
            ctx.InputAssembler.InputLayout = _lineInputLayout;
            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.VertexShader.SetShaderResource(0, StateBuffers.LayerSRV);
            ctx.GeometryShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.GeometryShader.SetConstantBuffer(1, _lineGlowSettingsBuffer);
            ctx.GeometryShader.SetShaderResource(0, StateBuffers.LayerSRV);
            ctx.GeometryShader.SetShaderResource(1, StateBuffers.ObjectSRV);
            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
            ctx.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(
                _lineVertexBuffer.Buffer, _lineVertexBuffer.Stride, 0));
            ctx.Draw(_lineVertexCount, 0);
            ctx.GeometryShader.Set(null);

            // First pass for all non selected lines
            SetLineRenderMode(ctx, false, false);
            ctx.VertexShader.Set(_lineVertexShader);
            ctx.PixelShader.Set(_linePixelShader);
            ctx.InputAssembler.InputLayout = _lineInputLayout;
            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.VertexShader.SetConstantBuffer(1, _dxfObjectSettingsBuffer);
            ctx.VertexShader.SetConstantBuffer(2, _lineRenderModeBuffer);
            ctx.VertexShader.SetShaderResource(0, StateBuffers.LayerSRV);
            ctx.VertexShader.SetShaderResource(1, StateBuffers.ObjectSRV);
            ctx.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(
                _lineVertexBuffer.Buffer, _lineVertexBuffer.Stride, 0));
            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
            ctx.Draw(_lineVertexCount, 0);

            // Second pass for all selected lines
            SetLineRenderMode(ctx, true, false);
            ctx.Draw(_lineVertexCount, 0);
        }
        private void DrawText(DeviceContext ctx)
        {
            if (_textVertexBuffer is null) { return; }

            ctx.VertexShader.Set(_textVertexShader);
            ctx.PixelShader.Set(_textPixelShader);
            ctx.InputAssembler.InputLayout = _textInputLayout;
            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.VertexShader.SetConstantBuffer(2, _viewportBuffer);
            ctx.VertexShader.SetShaderResource(0, StateBuffers.LayerSRV);
            ctx.VertexShader.SetShaderResource(1, StateBuffers.ObjectSRV);
            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            ctx.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(
                 _textVertexBuffer.Buffer, _textVertexBuffer.Stride, 0));

            ctx.Draw(_textVertexCount, 0);
        }
        private void DrawSolids(DeviceContext ctx)
        {
            if (_solidVertexBuffer is null) { return; }

            ctx.VertexShader.Set(_solidVertexShader);
            ctx.PixelShader.Set(_solidPixelShader);
            ctx.InputAssembler.InputLayout = _solidInputLayout;
            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.VertexShader.SetConstantBuffer(1, _dxfObjectSettingsBuffer);
            ctx.VertexShader.SetShaderResource(0, StateBuffers.LayerSRV);
            ctx.VertexShader.SetShaderResource(1, StateBuffers.ObjectSRV);
            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            ctx.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(
                 _solidVertexBuffer.Buffer, _solidVertexBuffer.Stride, 0));

            ctx.Draw(_solidVertexCount, 0);
        }
        private void DrawGlyphBatches(DeviceContext ctx, GlyphAtlas atlas, Dictionary<short, List<GlyphInstance>> batches)
        {
            if (atlas == null || atlas.VertexBuffer == null) { return; }

            // Bind shaders + constant buffers
            ctx.VertexShader.Set(_glyphVS);
            ctx.PixelShader.Set(_glyphPS);
            ctx.InputAssembler.InputLayout = _glyphLayout;
            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.VertexShader.SetConstantBuffer(1, _cogoPointSettingsBuffer);
            ctx.VertexShader.SetConstantBuffer(2, _viewportBuffer);
            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            ctx.VertexShader.SetShaderResource(0, StateBuffers.LabelSRV);
            ctx.VertexShader.SetShaderResource(1, StateBuffers.PointSRV);
            ctx.VertexShader.SetShaderResource(2, StateBuffers.GroupSRV);

            ctx.PixelShader.SetShaderResource(1, StateBuffers.GroupSRV);

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
                _glyphInstanceBuffer.Update(ctx, CollectionsMarshal.AsSpan(instances));
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
        private void DrawPointCircles(DeviceContext ctx)
        {
            ctx.VertexShader.Set(_pointCircleVS);
            ctx.GeometryShader.Set(_pointCircleGS);
            ctx.PixelShader.Set(_pointCirclePS);
            ctx.InputAssembler.InputLayout = _pointCircleInputLayout;
            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.GeometryShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.GeometryShader.SetConstantBuffer(1, _dxfObjectSettingsBuffer);

            ctx.VertexShader.SetShaderResource(0, StateBuffers.PointSRV);
            ctx.VertexShader.SetShaderResource(1, StateBuffers.GroupSRV);

            ctx.GeometryShader.SetShaderResource(0, StateBuffers.PointSRV);
            ctx.GeometryShader.SetShaderResource(1, StateBuffers.GroupSRV);

            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;
            ctx.InputAssembler.SetVertexBuffers(0,
                new VertexBufferBinding(_pointCircleVertexBuffer.Buffer, _pointCircleVertexBuffer.Stride, 0));

            ctx.Draw(_pointCircleVertexCount, 0);
            ctx.GeometryShader.Set(null);
        }
        private void DrawCogoPointAnchors(DeviceContext ctx)
        {
            if (_anchorVerticesCount == 0) { return; }

            ctx.VertexShader.Set(_toggleVS);
            ctx.PixelShader.Set(_togglePS);
            ctx.InputAssembler.InputLayout = _toggleLayout;
            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);

            // ✅ ADD: settings buffer (b1) for both VS & PS
            ctx.VertexShader.SetConstantBuffer(1, _toggleSettingsBuffer);
            ctx.PixelShader.SetConstantBuffer(1, _toggleSettingsBuffer);

            // ✅ ADD: state SRVs (t0/t1) for the VS (shader fetches flags/offset)
            ctx.VertexShader.SetShaderResource(0, StateBuffers.PointSRV); // t0
            ctx.VertexShader.SetShaderResource(1, StateBuffers.GroupSRV); // t1

            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            ctx.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_toggleQuadVB.Buffer, _toggleQuadVB.Stride, 0));
            ctx.InputAssembler.SetVertexBuffers(1, new VertexBufferBinding(_anchorInstanceBuffer.Buffer, _anchorInstanceBuffer.Stride, 0));

            ctx.DrawInstanced(6, _anchorVerticesCount, 0, 0);
        }
        private void DrawLeaderLines(DeviceContext ctx)
        {
            if (_leaderLineInstanceCount <= 0) { return; }

            ctx.InputAssembler.InputLayout = _leaderLineInputLayout;
            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList; // ONE vertex per line-instance

            ctx.VertexShader.Set(_leaderLineGlowVS);
            ctx.GeometryShader.Set(_leaderLineGlowGS);
            ctx.PixelShader.Set(_leaderLineGlowPS);
            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.GeometryShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.GeometryShader.SetConstantBuffer(1, _leaderLineGlowSettings);
            ctx.GeometryShader.SetShaderResource(0, StateBuffers.PointSRV);
            ctx.GeometryShader.SetShaderResource(1, StateBuffers.GroupSRV);
            ctx.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_leaderLineBuffer.Buffer, _leaderLineBuffer.Stride, 0));
            ctx.Draw(_leaderLineInstanceCount, 0);

            ctx.VertexShader.Set(_leaderLineVS);
            ctx.GeometryShader.Set(_leaderLineGS);
            ctx.PixelShader.Set(_leaderLinePS);
            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.GeometryShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.GeometryShader.SetConstantBuffer(1, _leaderLineSettings);
            ctx.GeometryShader.SetShaderResource(0, StateBuffers.PointSRV);
            ctx.GeometryShader.SetShaderResource(1, StateBuffers.GroupSRV);
            ctx.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_leaderLineBuffer.Buffer, _leaderLineBuffer.Stride, 0));
            ctx.Draw(_leaderLineInstanceCount, 0);

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
                ctx.GeometryShader.SetConstantBuffer(1, _cogoPointGlowSettingsBuffer);
                ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;
                ctx.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_hoverCircleBuffer.Buffer, _hoverCircleBuffer.Stride, 0));
                ctx.Draw(_hoverCircleVertices.Count, 0);
                ctx.GeometryShader.Set(null);
            }
        }
        private void DrawSignificantPoints(DeviceContext ctx)
        {
            if (_sigPointVertexCount == 0) { return; }

            ctx.VertexShader.Set(_sigPointVS);
            ctx.PixelShader.Set(_sigPointPS);
            ctx.GeometryShader.Set(_sigPointGS);
            ctx.InputAssembler.InputLayout = _sigPointLayout;
            ctx.GeometryShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.GeometryShader.SetConstantBuffer(1, _sigPointSettingsBuffer);
            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;
            ctx.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_sigPointVertexBuffer.Buffer, _sigPointVertexBuffer.Stride, 0));
            ctx.Draw(_sigPointVertexCount, 0);
        }

        private void UpdateLineVertices()
        {
            if (_lineVertexBuffer is null || CadManager is null) { return; }

            var context = ResCache.DeviceContext;
            var vertexSpan = CadManager.UpdateLineVerticesList(ResCache, SceneIdMap, StateBuffers);
            StateBuffers.EnsureObjectCapacity(SceneIdMap.ObjectCount);
            _lineVertexBuffer.Update(context, vertexSpan);
            _lineVertexCount = vertexSpan.Length;

            StateBuffers.FlushAll();
            _lineVerticesDirty = false;
            _dxfDirty = true;
        }
        private void UpdateTextVertices()
        {
            if (_textVertexBuffer is null || CadManager is null)
            {
                _textVerticesDirty = false;
                return;
            }

            var context = ResCache.DeviceContext;
            var vertexSpan = CadManager.UpdateTextVerticesList(ResCache, SceneIdMap, StateBuffers);
            _textVertexBuffer.Update(context, vertexSpan);
            _textVertexCount = vertexSpan.Length;

            StateBuffers.FlushAll();
            _textVerticesDirty = false;
            _dxfDirty = true;
        }
        private void UpdateSolidVertices()
        {
            if (_solidVertexBuffer is null || CadManager is null)
            {
                _solidVerticesDirty = false;
                return;
            }

            var context = ResCache.DeviceContext;
            var vertexSpan = CadManager.UpdateSolidVerticesList(ResCache, SceneIdMap, StateBuffers);
            _solidVertexBuffer.Update(context, vertexSpan);
            _solidVertexCount = vertexSpan.Length;

            StateBuffers.FlushAll();
            _solidVerticesDirty = false;
            _dxfDirty = true;
        }
        private void UpdateGlyphBatches()
        {
            _glyphBatches.Clear();

            foreach (var pg in PointGroups)
            {
                if (pg is null) { continue; }

                var worldHeight = (float)(pg.FontBaseSize * pg.PointScale);
                var duToWorld = worldHeight / ResCache.CogoPointFontFace.Metrics.DesignUnitsPerEm;

                var duPerEm = ResCache.CogoPointFontFace.Metrics.DesignUnitsPerEm;
                var duToWorldBase = (float)pg.FontBaseSize / duPerEm;

                var color = pg.Color.ToSharpDXVector4();
                var isGroupVisible = pg.IsVisible ? 1f : 0f;

                uint gId = SceneIdMap.GetOrAddGroupId(pg, out var isNewGroup);
                if (isNewGroup) { StateBuffers.EnsureGroupCapacity(SceneIdMap.GroupCount); }

                foreach (var p in pg.Points)
                {
                    if (p == null) { continue; }

                    uint pId = SceneIdMap.GetOrAddPointId(p, out var isNewPoint);
                    if (isNewPoint) { StateBuffers.EnsurePointCapacity(SceneIdMap.PointCount); }

                    var isMO = p.IsMouseOver ? 1f : 0f;
                    var isSel = p.IsSelected ? 1f : 0f;
                    var ySign = -1f;

                    uint idPN = SceneIdMap.GetOrAddLabelId(p, 0, out var isNew);
                    uint idElev = SceneIdMap.GetOrAddLabelId(p, 1, out isNew);
                    uint idDesc = p.HasDescription ? SceneIdMap.GetOrAddLabelId(p, 2, out isNew) : 0xFFFFFFFF;

                    AddCogoTextLabelLine(
                        s: p.PointNumber.ToString(),
                        lineOffset: p.PointNumberOffset,
                        duToWorldBase: duToWorldBase,
                        duToWorld: duToWorld,
                        color: color,
                        isVisible: isGroupVisible, isMouseOver: isMO, isSelected: isSel, ySign: ySign,
                        labelId: idPN, groupId: gId, pointId: pId);
                    AddCogoTextLabelLine(
                        s: p.Elevation.ToString("F3"),
                        lineOffset: p.ElevationOffset,
                        duToWorldBase: duToWorldBase,
                        duToWorld: duToWorld,
                        color: color,
                        isVisible: isGroupVisible, isMouseOver: isMO, isSelected: isSel, ySign: ySign,
                        labelId: idElev, groupId: gId, pointId: pId);
                    if (p.HasDescription)
                    {
                        AddCogoTextLabelLine(
                            s: p.Description,
                            lineOffset: p.DescriptionOffset,
                            duToWorldBase: duToWorldBase,
                            duToWorld: duToWorld,
                            color: color,
                            isVisible: isGroupVisible, isMouseOver: isMO, isSelected: isSel, ySign: ySign,
                            labelId: idDesc, groupId: gId, pointId: pId);
                    }
                    RecomputeCogoPointBoundsFast(p);

                    p.UpdateBounds();
                }
            }

            CadManager.UpdateCogoPointTree();

            StateBuffers.FlushAll();
            _glyphVerticesDirty = false;
            _dxfDirty = true;
        }
        private void UpdatePointCircleVertices()
        {
            if (_pointCircleVertexBuffer is null) { return; }

            var context = ResCache.DeviceContext;
            var vertexSpan = CadManager.UpdatePointCircleVerticesList(SceneIdMap, StateBuffers);
            _pointCircleVertexBuffer.Update(context, vertexSpan);
            _pointCircleVertexCount = vertexSpan.Length;

            StateBuffers.FlushAll();
            _pointCircleVerticesDirty = false;
            _dxfDirty = true;
        }
        private void UpdateDragOverlayVertices(Rect r)
        {
            if (r.IsEmpty || r.Width <= 0 || r.Height <= 0 || !IsDragging)
            {
                _dragFillVertexCount = 0;
                _combinedDirty = true;
                return;
            }

            var settings = new OverlayOutlineSettings
            {
                RectMinWorld = new Vector2((float)r.Left, (float)r.Top),
                RectMaxWorld = new Vector2((float)r.Right, (float)r.Bottom),
                ThicknessPx = 1.0f,     // tweak as desired
                FeatherPx = 1.0f,     // small AA feather
                BorderColor = new Vector4(0f, 0.749f, 1f, 1f) // DeepSkyBlue like your lines
            };
            ResCache.DeviceContext.UpdateSubresource(ref settings, _overlayOutlineSettingsBuffer);

            // world-space coords (z=0)
            var lt = new Vector3((float)r.Left, (float)r.Top, 1);
            var rt = new Vector3((float)r.Right, (float)r.Top, 1);
            var rb = new Vector3((float)r.Right, (float)r.Bottom, 1);
            var lb = new Vector3((float)r.Left, (float)r.Bottom, 1);

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

            _dragFillBuffer.Update(ResCache.DeviceContext, fillVerts);
            _dragFillVertexCount = 6;

            _combinedDirty = true;
        }
        private void UpdateCogoHoverVertices()
        {
            if (ResCache is null || CadManager.Camera is null) { return; }

            var ctx = ResCache.DeviceContext;
            _hoverCircleVertices.Clear();
            var wupp = CadManager.Camera.GetWorldUnitsPerPixel();
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
                    Color = GlobalHelperProperties.HoverColor
                });
                rectInstances.Add(new RoundedHoverRectInstance
                {
                    Center = elevCenter,
                    HalfSize = elevHalfSize,
                    RadiusFeather = radiusFeathering,
                    Color = GlobalHelperProperties.HoverColor
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
                        Color = GlobalHelperProperties.HoverColor
                    });
                }

                // circle
                CircleHoverVertex circleHoverVertex = new(cp.Position.ToSharpDXVector3(), GlobalHelperProperties.CogoPointCircleMouseOverPixelRadius * cp.PointGroup.PointScale.ToFloat());
                _hoverCircleVertices.Add(circleHoverVertex);
            }

            _hoverRectInstanceBuffer.Update(ctx, CollectionsMarshal.AsSpan(rectInstances));
            _hoverRectInstanceCount = rectInstances.Count;

            _hoverCircleBuffer.Update(ctx, _hoverCircleVertices.ToArray());

            _cogoHoverVerticesDirty = false;
            _combinedDirty = true;
        }
        private void UpdateToggleAnchorVertices()
        {
            if (ResCache is null || CadManager.Camera is null || ResCache.DeviceContext is null) { return; }

            var ctx = ResCache.DeviceContext;
            var inst = new List<ToggleAnchorInstance>(SelectedCogoPoints.Count);
            foreach (var pg in PointGroups)
            {
                if (pg is null) { continue; }

                var gid = SceneIdMap.GetOrAddGroupId(pg, out var isNewGroup);
                if (isNewGroup) { StateBuffers.EnsureGroupCapacity(SceneIdMap.GroupCount); }

                foreach (var p in pg.Points)
                {
                    if (p is null) { continue; }
                    var pid = SceneIdMap.GetOrAddPointId(p, out var isNewPoint);
                    if (isNewPoint) { StateBuffers.EnsurePointCapacity(SceneIdMap.PointCount); }

                    var center = Vector2.Zero;

                    inst.Add(new()
                    {
                        Center = center,
                        PointId = pid,
                        GroupId = gid
                    });
                }
            }

            StateBuffers.FlushAll();
            _anchorInstanceBuffer.Update(ctx, CollectionsMarshal.AsSpan(inst));
            _anchorVerticesCount = inst.Count;
            _anchorVerticesDirty = false;
            _combinedDirty = true;
        }
        private void UpdateLeaderLineVertices()
        {
            List<LeaderLineInstance> list = [];
            foreach (var pg in PointGroups)
            {
                if (pg is null) { continue; }

                uint gid = SceneIdMap.GetOrAddGroupId(pg, out var isNewGroup);
                if (isNewGroup) { StateBuffers.EnsureGroupCapacity(SceneIdMap.GroupCount); }

                foreach (var p in pg.Points)
                {
                    if (p is null) { continue; }
                    uint pid = SceneIdMap.GetOrAddPointId(p, out var isNewPoint);
                    if (isNewPoint) { StateBuffers.EnsurePointCapacity(SceneIdMap.PointCount); }

                    var vertex = new LeaderLineInstance
                    {
                        Start = Vector2.Zero,
                        End = Vector2.Zero,
                        PointId = pid,
                        GroupId = gid
                    };
                    list.Add(vertex);
                }
            }
            StateBuffers.FlushAll();
            _leaderLineInstanceCount = list.Count;
            _leaderLineBuffer.Update(ResCache.DeviceContext, CollectionsMarshal.AsSpan(list));
            _leaderLineVerticesDirty = false;
            _combinedDirty = true;
        }
        private void UpdateSignificantPointVertices()
        {
            if (ResCache is null || ResCache.DeviceContext is null) { return; }

            var ctx = ResCache.DeviceContext;
            List<SignificantPointVertex> vertices = [];
            foreach (var sigP in SelectedHitTestablePoints)
            {
                if (sigP is null) { continue; }

                vertices.Add(new SignificantPointVertex
                {
                    Position = sigP.Position.ToSharpDXVector3(),
                });
            }
            _sigPointVertexCount = vertices.Count;
            _sigPointVertexBuffer.Update(ctx, CollectionsMarshal.AsSpan(vertices));

            _sigPointVerticesDirty = false;
            _combinedDirty = true;
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
            _lineVertexShader = new VertexShader(ResCache.Device, lineVSBytecode);

            var linePSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "PSMain", "ps_5_0");
            _linePixelShader = new PixelShader(ResCache.Device, linePSBytecode);

            // Glow shaders
            var lineGlowVSBytecode = ShaderBytecode.CompileFromFile(glowShaderPath, "VSMain", "vs_5_0");
            _lineGlowVertexShader = new VertexShader(ResCache.Device, lineGlowVSBytecode);

            var lineGlowGSBytecode = ShaderBytecode.CompileFromFile(glowShaderPath, "GSMain", "gs_5_0");
            _lineGlowGeometryShader = new GeometryShader(ResCache.Device, lineGlowGSBytecode);

            var lineGlowPSBytecode = ShaderBytecode.CompileFromFile(glowShaderPath, "PSMain", "ps_5_0");
            _lineGlowPixelShader = new PixelShader(ResCache.Device, lineGlowPSBytecode);

            _lineInputLayout = new InputLayout(
                ResCache.Device,
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
                path = Path.GetDirectoryName(path) ?? throw new DirectoryNotFoundException("The 'Cad_Point_Manager' directory could not be found in the path.");
            }

            string shaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\TextShader.hlsl");

            // Main shaders
            var textVSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "VSMain", "vs_5_0");
            _textVertexShader = new VertexShader(ResCache.Device, textVSBytecode);

            var textPSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "PSMain", "ps_5_0");
            _textPixelShader = new PixelShader(ResCache.Device, textPSBytecode);

            // Layout
            _textInputLayout = new InputLayout(
                ResCache.Device,
                ShaderSignature.GetInputSignature(textVSBytecode),
                new[]
                {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                    new InputElement("LAYERID", 0, Format.R32_UInt, 12, 0),
                    new InputElement("OBJECTID", 0, Format.R32_UInt, 16, 0),
                 });

            _textShaderLoaded = true;
        }
        private void InitializeSolidShaders()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            while (Path.GetFileName(path) != "Cad_Point_Manager")
            {
                path = Path.GetDirectoryName(path) ?? throw new DirectoryNotFoundException("The 'Cad_Point_Manager' directory could not be found in the path.");
            }

            string shaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\SolidShader.hlsl");

            // Main shaders
            var textVSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "VSMain", "vs_5_0");
            _solidVertexShader = new VertexShader(ResCache.Device, textVSBytecode);

            var solidPSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "PSMain", "ps_5_0");
            _solidPixelShader = new PixelShader(ResCache.Device, solidPSBytecode);

            // Layout
            _solidInputLayout = new InputLayout(
                ResCache.Device,
                ShaderSignature.GetInputSignature(textVSBytecode),
                new[]
                {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                    new InputElement("LAYERID", 0, Format.R32_UInt, 12, 0),
                    new InputElement("OBJECTID", 0, Format.R32_UInt, 16, 0),
                 });

            _solidShaderLoaded = true;
        }
        private void InitializeCogoPointShaders()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            while (Path.GetFileName(path) != "Cad_Point_Manager")
            {
                path = Path.GetDirectoryName(path);
                if (path == null) { throw new DirectoryNotFoundException("The 'Cad_Point_Manager' directory could not be found in the path."); }
            }

            string pointMarkerShaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\PointMarkerShader.hlsl");
            var pointCircleVsb = ShaderBytecode.CompileFromFile(pointMarkerShaderPath, "VSMain", "vs_5_0");
            var pointCirclePsb = ShaderBytecode.CompileFromFile(pointMarkerShaderPath, "PSMain", "ps_5_0");
            var pointCircleGsb = ShaderBytecode.CompileFromFile(pointMarkerShaderPath, "GSMain", "gs_5_0");
            _pointCircleVS = new VertexShader(ResCache.Device, pointCircleVsb);
            _pointCirclePS = new PixelShader(ResCache.Device, pointCirclePsb);
            _pointCircleGS = new GeometryShader(ResCache.Device, pointCircleGsb);
            _pointCircleInputLayout = new InputLayout(ResCache.Device, ShaderSignature.GetInputSignature(pointCircleVsb),
                new[]
                {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                    new InputElement("RADIUS",   0, Format.R32_Float,       12, 0),
                    new InputElement("LABEL_ID", 0, Format.R32_UInt,        16, 0),
                    new InputElement("POINT_ID", 0, Format.R32_UInt,        20, 0),
                });

            string glyphMeshShaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\GlyphMeshShader.hlsl");
            var glyphMeshVsb = ShaderBytecode.CompileFromFile(glyphMeshShaderPath, "VSMain", "vs_5_0");
            var glyphMeshPsb = ShaderBytecode.CompileFromFile(glyphMeshShaderPath, "PSMain", "ps_5_0");
            _glyphVS = new VertexShader(ResCache.Device, glyphMeshVsb);
            _glyphPS = new PixelShader(ResCache.Device, glyphMeshPsb);
            _glyphLayout = new InputLayout(ResCache.Device, ShaderSignature.GetInputSignature(glyphMeshVsb),
                new[]
                {
                    // Slot 0
                    new InputElement("POSITION",      0, Format.R32G32_Float,       0, 0, InputClassification.PerVertexData,   0),

                    // Slot 1
                    new InputElement("GLYPH_SCALE",   0, Format.R32_Float,          0, 1, InputClassification.PerInstanceData, 1),
                    new InputElement("GLYPH_PEN",     0, Format.R32_Float,          4, 1, InputClassification.PerInstanceData, 1),
                    new InputElement("YSIGN",         0, Format.R32_Float,          8, 1, InputClassification.PerInstanceData, 1),
                    new InputElement("LABEL_ID",      0, Format.R32_UInt,           12, 1, InputClassification.PerInstanceData, 1),
                    new InputElement("POINT_ID",      0, Format.R32_UInt,           16, 1, InputClassification.PerInstanceData, 1),
                });
            _glyphInstanceBuffer = new ResizableBuffer<GlyphInstance>(ResCache.Device, initialCapacity: 256);

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
            _hoverCircleVertexShader = new VertexShader(ResCache.Device, circleVSBytecode);
            var circlePSBytecode = ShaderBytecode.CompileFromFile(circleHoverShaderPath, "PSMain", "ps_5_0");
            _hoverCirclePixelShader = new PixelShader(ResCache.Device, circlePSBytecode);
            var circleGSBytecode = ShaderBytecode.CompileFromFile(circleHoverShaderPath, "GSMain", "gs_5_0");
            _hoverCircleGeometryShader = new GeometryShader(ResCache.Device, circleGSBytecode);

            _hoverCircleLayout = new InputLayout(
                ResCache.Device,
                ShaderSignature.GetInputSignature(circleVSBytecode),
                new[]
                {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                    new InputElement("RADIUS", 0, Format.R32_Float, 12, 0),
                    new InputElement("ISSELECTED", 0, Format.R32_Float, 16, 0),
                });

            var rectVSBytecode = ShaderBytecode.CompileFromFile(rectHoverShaderPath, "VSMain", "vs_5_0");
            _hoverRectVertexShader = new VertexShader(ResCache.Device, rectVSBytecode);
            var rectPSBytecode = ShaderBytecode.CompileFromFile(rectHoverShaderPath, "PSMain", "ps_5_0");
            _hoverRectPixelShader = new PixelShader(ResCache.Device, rectPSBytecode);

            _hoverRectLayout = new InputLayout(
                ResCache.Device,
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

            _hoverRectInstanceBuffer ??= new(ResCache.Device, 6);
            var quad = new[]
            {
                new OverlayQuadVertex{ Local = new(-1,-1) },
                new OverlayQuadVertex{ Local = new(-1, 1) },
                new OverlayQuadVertex{ Local = new( 1, 1) },
                new OverlayQuadVertex{ Local = new(-1,-1) },
                new OverlayQuadVertex{ Local = new( 1, 1) },
                new OverlayQuadVertex{ Local = new( 1,-1) },
            };
            _hoverRectBuffer.Update(ResCache.DeviceContext, quad);

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
            _overlayVS = new VertexShader(ResCache.Device, vs);
            _overlayPS = new PixelShader(ResCache.Device, ps);

            _overlayLayout = new InputLayout(
                ResCache.Device,
                ShaderSignature.GetInputSignature(vs),
                new[] {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                    new InputElement("COLOR",    0, Format.R32G32B32A32_Float, 12, 0),
                });

            // Border
            string outlineFx = Path.Combine(path, @"Controls\D3DControl\Shaders\OverlayOutlineShader.hlsl");
            var ovs = ShaderBytecode.CompileFromFile(outlineFx, "VSMain", "vs_5_0");
            var ops = ShaderBytecode.CompileFromFile(outlineFx, "PSMain", "ps_5_0");
            _overlayOutlineVS = new VertexShader(ResCache.Device, ovs);
            _overlayOutlinePS = new PixelShader(ResCache.Device, ops);

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
            _toggleVS = new VertexShader(ResCache.Device, vs);
            _togglePS = new PixelShader(ResCache.Device, ps);

            _toggleLayout = new InputLayout(
                ResCache.Device,
                ShaderSignature.GetInputSignature(vs),
                new[]
                {
                    // stream 0
                    new InputElement("POSITION", 0, Format.R32G32_Float, 0, 0),
                    
                    // stream 1
                    new InputElement("TEXCOORD", 0, Format.R32G32_Float, 0, 1, InputClassification.PerInstanceData, 1), // Center (float2) @ offset 0
                    new InputElement("POINT_ID", 0, Format.R32_UInt,      8, 1, InputClassification.PerInstanceData, 1), // PointId  @ offset 8
                });

            // Dedicated unit quad for this shader
            _toggleQuadVB ??= new(ResCache.Device, 6);
            var quad = new[]
            {
                new OverlayQuadVertex{ Local = new(-1,-1) },
                new OverlayQuadVertex{ Local = new(-1, 1) },
                new OverlayQuadVertex{ Local = new( 1, 1) },
                new OverlayQuadVertex{ Local = new(-1,-1) },
                new OverlayQuadVertex{ Local = new( 1, 1) },
                new OverlayQuadVertex{ Local = new( 1,-1) },
            };
            _toggleQuadVB.Update(ResCache.DeviceContext, quad);

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

            string lineShaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\LeaderLineShader.hlsl");
            var lineVSBytecode = ShaderBytecode.CompileFromFile(lineShaderPath, "VSMain", "vs_5_0");
            _leaderLineVS = new VertexShader(ResCache.Device, lineVSBytecode);
            var linePSBytecode = ShaderBytecode.CompileFromFile(lineShaderPath, "PSMain", "ps_5_0");
            _leaderLinePS = new PixelShader(ResCache.Device, linePSBytecode);
            var lineGSBytecode = ShaderBytecode.CompileFromFile(lineShaderPath, "GSMain", "gs_5_0");
            _leaderLineGS = new GeometryShader(ResCache.Device, lineGSBytecode);

            _leaderLineInputLayout = new InputLayout(ResCache.Device, ShaderSignature.GetInputSignature(lineVSBytecode), new[]
            {
                new InputElement("POSITION", 0, Format.R32G32_Float,     0, 0), // A
                new InputElement("END", 0, Format.R32G32_Float,          8, 0), // BBase
                new InputElement("POINT_ID", 0, Format.R32_UInt,         16, 0), // PointId
            });

            string lineGlowShaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\LeaderLineGlowShader.hlsl");
            var lineGlowVSBytecode = ShaderBytecode.CompileFromFile(lineGlowShaderPath, "VSMain", "vs_5_0");
            _leaderLineGlowVS = new VertexShader(ResCache.Device, lineGlowVSBytecode);
            var lineGlowPSBytecode = ShaderBytecode.CompileFromFile(lineGlowShaderPath, "PSMain", "ps_5_0");
            _leaderLineGlowPS = new PixelShader(ResCache.Device, lineGlowPSBytecode);
            var lineGlowGSBytecode = ShaderBytecode.CompileFromFile(lineGlowShaderPath, "GSMain", "gs_5_0");
            _leaderLineGlowGS = new GeometryShader(ResCache.Device, lineGlowGSBytecode);

            _leaderLineShadersLoaded = true;
        }
        private void InitializeSignificantPointsShaders()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            while (Path.GetFileName(path) != "Cad_Point_Manager")
            {
                path = Path.GetDirectoryName(path);
                if (path == null) { throw new DirectoryNotFoundException("The 'Cad_Point_Manager' directory could not be found in the path."); }
            }

            string significantPointShaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\SignificantPointShader.hlsl");
            var significantPointVsb = ShaderBytecode.CompileFromFile(significantPointShaderPath, "VSMain", "vs_5_0");
            var significantPointPsb = ShaderBytecode.CompileFromFile(significantPointShaderPath, "PSMain", "ps_5_0");
            var significantPointGsb = ShaderBytecode.CompileFromFile(significantPointShaderPath, "GSMain", "gs_5_0");
            _sigPointVS = new VertexShader(ResCache.Device, significantPointVsb);
            _sigPointPS = new PixelShader(ResCache.Device, significantPointPsb);
            _sigPointGS = new GeometryShader(ResCache.Device, significantPointGsb);
            _sigPointLayout = new InputLayout(ResCache.Device, ShaderSignature.GetInputSignature(significantPointVsb),
                new[]
                {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                });

            _sigPointShadersLoaded = true;
        }

        private void InitializeBuffers()
        {
            var device = ResCache.Device;

            _lineVertexBuffer?.Dispose();
            _lineVertexBuffer = new(device, GlobalHelperProperties.InitialLineVertices);

            _textVertexBuffer?.Dispose();
            _textVertexBuffer = new(device, GlobalHelperProperties.InitialTextVertices);

            _solidVertexBuffer?.Dispose();
            _solidVertexBuffer = new(device, GlobalHelperProperties.InitialLineVertices);

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

            SceneIdMap ??= new();
            StateBuffers?.Dispose();
            StateBuffers = new(device, device.ImmediateContext);
            StateController = new(SceneIdMap, StateBuffers);

            _anchorInstanceBuffer?.Dispose();
            _anchorInstanceBuffer = new(device, 64);

            _leaderLineBuffer?.Dispose();
            _leaderLineBuffer = new(device, 2);

            _sigPointVertexBuffer?.Dispose();
            _sigPointVertexBuffer = new(device, 64);

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
            _transformationBuffer = new Buffer(ResCache.Device, transformationBufferDesc);

            var viewportBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<ViewportBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _viewportBuffer = new Buffer(ResCache.Device, viewportBufferDesc);

            var dxfObjectBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<DxfObjectSettingsBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _dxfObjectSettingsBuffer = new Buffer(ResCache.Device, dxfObjectBufferDesc);

            var lineRenderModeBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Dynamic,
                SizeInBytes = Utilities.SizeOf<LineRenderModeBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None
            };
            _lineRenderModeBuffer = new Buffer(ResCache.Device, lineRenderModeBufferDesc);

            var lineGlowBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<LineGlowSettingsBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _lineGlowSettingsBuffer = new Buffer(ResCache.Device, lineGlowBufferDesc);

            var pointTextBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<GlyphSettingsBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _cogoPointSettingsBuffer = new Buffer(ResCache.Device, pointTextBufferDesc);

            var hoverCircleBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<CogoPointGlowSettingsBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _cogoPointGlowSettingsBuffer = new Buffer(ResCache.Device, hoverCircleBufferDesc);

            var leaderLineBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<LeaderLineSettings>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _leaderLineSettings = new Buffer(ResCache.Device, leaderLineBufferDesc);

            var leaderLineGlowBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<LeaderLineGlowSettings>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _leaderLineGlowSettings = new Buffer(ResCache.Device, leaderLineGlowBufferDesc);

            var toggleAnchorBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<ToggleAnchorSettingsBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _toggleSettingsBuffer = new Buffer(ResCache.Device, toggleAnchorBufferDesc);

            var overlayOutlineBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<OverlayOutlineSettings>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _overlayOutlineSettingsBuffer = new Buffer(ResCache.Device, overlayOutlineBufferDesc);

            var sigPointBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<SignificantPointSettingsBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _sigPointSettingsBuffer = new Buffer(ResCache.Device, sigPointBufferDesc);

            ConstantBuffersInitialized = true;
            ConstantBuffersDirty = true;
        }
        private void UpdateConstantBuffers()
        {
            var transformation = CadManager.Camera.ViewProjectionMatrix;
            var transformationBuffer = new TransformationBuffer
            {
                WorldViewProjection = transformation
            };
            ResCache.DeviceContext.UpdateSubresource(ref transformationBuffer, _transformationBuffer);

            var viewportBuffer = new ViewportBuffer
            {
                ViewportSize = new(Viewport.Width, Viewport.Height)
            };
            ResCache.DeviceContext.UpdateSubresource(ref viewportBuffer, _viewportBuffer);

            var worldUnitsPerPixel = CadManager.Camera.GetWorldUnitsPerPixel();

            var dxfObjectSettings = new DxfObjectSettingsBuffer
            {
                SelectedColor = GlobalHelperProperties.SelectedObjectColor,
                SelectedMouseOverColor = GlobalHelperProperties.SelectedMouseOverObjectColor
            };
            ResCache.DeviceContext.UpdateSubresource(ref dxfObjectSettings, _dxfObjectSettingsBuffer);

            var lineGlowSettings = new LineGlowSettingsBuffer
            {
                GlowOffset = GlobalHelperProperties.LineGlowPixelWidth * worldUnitsPerPixel,
                GlowTransparency = GlobalHelperProperties.HoverTransparency,
                SelectedColor = GlobalHelperProperties.SelectedObjectColor,
                SelectedMouseOverColor = GlobalHelperProperties.SelectedMouseOverGlowColor
            };
            ResCache.DeviceContext.UpdateSubresource(ref lineGlowSettings, _lineGlowSettingsBuffer);

            var cogoPointTextSettings = new GlyphSettingsBuffer
            {
                SelectedColor = GlobalHelperProperties.SelectedObjectColor,
            };
            ResCache.DeviceContext.UpdateSubresource(ref cogoPointTextSettings, _cogoPointSettingsBuffer);

            var cogoPointGlowSettingsBuffer = new CogoPointGlowSettingsBuffer
            {
                GlowOffset = GlobalHelperProperties.LineGlowPixelWidth * worldUnitsPerPixel,
                HoverColor = GlobalHelperProperties.HoverColor,
                SelectedColor = GlobalHelperProperties.SelectedObjectColor,
                SelectedMouseOverColor = GlobalHelperProperties.SelectedMouseOverGlowColor
            };
            ResCache.DeviceContext.UpdateSubresource(ref cogoPointGlowSettingsBuffer, _cogoPointGlowSettingsBuffer);

            var leaderLineSettings = new LeaderLineSettings
            {
                InvViewport = new(1 / Viewport.Width, 1 / Viewport.Height),
                PixelThickness = GlobalHelperProperties.CogoPointLeaderLinePixelWidth,
                SelectedColor = GlobalHelperProperties.SelectedObjectColor,
            };
            ResCache.DeviceContext.UpdateSubresource(ref leaderLineSettings, _leaderLineSettings);

            var leaderLineGlowSettings = new LeaderLineGlowSettings
            {
                InvViewport = new(1 / Viewport.Width, 1 / Viewport.Height),
                PixelThickness = GlobalHelperProperties.CogoPointLeaderLinePixelWidth * 10,
                HoverColor = GlobalHelperProperties.HoverColor,
            };
            ResCache.DeviceContext.UpdateSubresource(ref leaderLineGlowSettings, _leaderLineGlowSettings);

            var toggleSettings = new ToggleAnchorSettingsBuffer
            {
                BaseColor = AnchorBaseColor, // Vector4
                SelectedColor = AnchorPressedColor,// Vector4
                MouseOverColor = AnchorHoverColor, // Vector4
                DesiredHalf = _desiredHalfWorldForAnchors,
                CornerFracOfHalf = CornerFracOfHalf, // 0..1
                Feather = _featherWorldForAnchors,
                MaxHalfBase = _maxHalfBaseForAnchors
            };
            ResCache.DeviceContext.UpdateSubresource(ref toggleSettings, _toggleSettingsBuffer);

            var sigPointSettings = new SignificantPointSettingsBuffer
            {
                Color = GlobalHelperProperties.SelectedSigPointColor,
                RadiusPx = GlobalHelperProperties.SignificantPointPixelRadius,
                ViewPortSize = new Vector2(Viewport.Width, Viewport.Height)
            };
            ResCache.DeviceContext.UpdateSubresource(ref sigPointSettings, _sigPointSettingsBuffer);

            ConstantBuffersDirty = false;
            CadManager.Camera.IsDirty = false;
            _dxfDirty = true;
            _combinedDirty = true;
        }

        private void SetLineRenderMode(DeviceContext ctx,
            bool selectedOnly,
            bool glowPass)
        {
            var data = new LineRenderModeBuffer
            {
                RenderSelectedOnly = selectedOnly ? 1u : 0u,
                RenderGlowPass = glowPass ? 1u : 0u
            };

            DataStream stream;
            ctx.MapSubresource(
                _lineRenderModeBuffer,
                MapMode.WriteDiscard,
                SharpDX.Direct3D11.MapFlags.None,
                out stream);

            stream.Write(data);

            ctx.UnmapSubresource(_lineRenderModeBuffer, 0);

            stream.Dispose();
        }

        private void AddCogoTextLabelLine(string s, float duToWorldBase,
            float duToWorld, Vector4 color, float isVisible, float isMouseOver,
            float isSelected, float ySign, uint labelId, uint groupId, uint pointId,
            Vector2 lineOffset)
        {
            if (string.IsNullOrEmpty(s)) { return; }

            Span<int> cps = stackalloc int[s.Length];
            for (int i = 0; i < s.Length; i++) { cps[i] = s[i]; }
            var gids = ResCache.CogoPointFontFace.GetGlyphIndices(cps.ToArray());
            float penDU = 0f;

            for (int i = 0; i < gids.Length; i++)
            {
                short gid = gids[i];
                if (gid <= 0) { continue; }

                var inst = new GlyphInstance
                {
                    DuToWorld = duToWorldBase,
                    PenDU = penDU,
                    YSign = ySign,
                    LabelId = labelId,
                    PointId = pointId,
                };

                if (!_glyphBatches.TryGetValue(gid, out var list)) { _glyphBatches[gid] = list = new List<GlyphInstance>(32); }

                list.Add(inst);
                penDU += ResCache.AdvanceWidthCache[gid];
            }
            float widthWorld = penDU * duToWorld;
        }

        private void RecomputeCogoPointBoundsFast(CogoPoint p)
        {
            var pg = p.PointGroup;
            var duPerEm = ResCache.CogoPointFontFace.Metrics.DesignUnitsPerEm;
            float duToWorldBase = (float)pg.FontBaseSize / duPerEm;
            float duToWorld = (float)(pg.FontBaseSize * pg.PointScale) / duPerEm; // includes group scale
            float ySign = -1f;

            var baseOrigin = p.Position.ToSharpDXVector2() + p.TextInfoOffset;
            var baseGroupXoffset = p.IsFlippedY ? -pg.PointInfoBaseXoffset : pg.PointInfoBaseXoffset;

            p.PointNumberBounds = MeasureLineRect(
                p.PointNumber.ToString(), baseOrigin, p.PointNumberOffset, baseGroupXoffset,
                duToWorldBase, duToWorld, ySign, p.PointGroup.PointScale.ToFloat());

            p.ElevationBounds = MeasureLineRect(
                p.Elevation.ToString("F3"), baseOrigin, p.ElevationOffset, baseGroupXoffset,
                duToWorldBase, duToWorld, ySign, p.PointGroup.PointScale.ToFloat());

            if (p.HasDescription)
            {
                p.DescriptionBounds = MeasureLineRect(
                    p.Description, baseOrigin, p.DescriptionOffset, baseGroupXoffset,
                    duToWorldBase, duToWorld, ySign, p.PointGroup.PointScale.ToFloat());
            }
            else { p.DescriptionBounds = Rect.Empty; }

            float rW = (float)(GlobalHelperProperties.CogoPointCirclePixelRadius * p.PointGroup.PointScale);
            var c = p.Position;
            p.EllipseBounds = new Rect(c.X - rW, c.Y - rW, 2 * rW, 2 * rW);

            p.UpdateBounds();
        }
        private Rect MeasureLineRect(string s, Vector2 baseOrigin, Vector2 labelOffset, float baseGroupXoffset,
                                        float duToWorldBase, float duToWorld, float ySign, float groupScale)
        {
            if (string.IsNullOrEmpty(s)) return Rect.Empty;

            // Same glyph ID lookup + advances you do in AddLineAndGetRect
            Span<int> cps = stackalloc int[s.Length];
            for (int i = 0; i < s.Length; i++) cps[i] = s[i];
            var gids = ResCache.CogoPointFontFace.GetGlyphIndices(cps.ToArray());

            float widthDU = 0f;
            for (int i = 0; i < gids.Length; i++)
            {
                short gid = gids[i];
                if (gid <= 0) continue;
                widthDU += ResCache.AdvanceWidthCache[gid];
            }

            // Shader applies origin + ls.Offset (+ ps.Offset) and scales DU by group
            float originX = baseOrigin.X + labelOffset.X + baseGroupXoffset;
            float originY = baseOrigin.Y + (labelOffset.Y * groupScale);
            Vector2 originWorld = new(originX, originY);
            float widthWorld = widthDU * duToWorld;     // duToWorld includes group scale

            // Reuse your existing height/top computation (cap-height × duToWorld)
            return ComputeLineRect(originWorld, widthWorld, duToWorld, ySign);
        }
        private Rect ComputeLineRect(Vector2 originWorld, float widthWorld, float duToWorld, float ySign)
        {
            var m = ResCache.CogoPointFontFace.Metrics; // design units (DU)
            float capH = m.CapHeight * duToWorld;

            // baseline is originWorld.Y
            float topY = originWorld.Y - ySign * capH;
            float y = Math.Min(topY, originWorld.Y);
            float height = Math.Abs(capH);

            return new Rect(originWorld.X, y, widthWorld, height);
        }

        private void SetInitialMatrix()
        {
            if (!CadManager.DxfLoaded) { _dxfInitialMatrix = Matrix.Identity; }
            else
            {
                CadManager.UpdateExtents();
                _dxfInitialMatrix = GetExtentsFittingMatrix(Viewport, CadManager.Extents);

                if (CadManager.Camera is not null)
                {
                    CadManager.Camera.ResetView(_dxfInitialMatrix, CadManager.Extents);
                    _hittestStrokeThickness = 7.0f / (CadManager.Camera.InitialViewMatrix.M11 * CadManager.Camera.CurrentZoom);
                    UpdateToggleAnchorDimensions();
                    ConstantBuffersDirty = true;
                }
            }
        }
        private void UpdateInitialMatrix()
        {
            if (CadManager is null || !CadManager.DxfLoaded || CadManager.Camera is null) { return; }

            CadManager.UpdateExtents();
            _dxfInitialMatrix = GetExtentsFittingMatrix(Viewport, CadManager.Extents);
            ConstantBuffersDirty = true;
        }
        private Matrix GetExtentsFittingMatrix(ViewportF viewport, Rect extents)
        {
            double scale = Math.Min(viewport.Width / extents.Width, viewport.Height / extents.Height);
            return Matrix.Scaling(scale.ToFloat(), scale.ToFloat(), 1) * Matrix.Translation(-extents.Left.ToFloat(), -extents.Top.ToFloat(), 0);
        }
        private void UpdateDxfCoords(Vector2 mousePosDip)
        {
            var mousePx = DipToPixel(mousePosDip);

            DxfCoords = CadManager.Camera.ScreenToWorld(mousePx);
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

            if (_cogoPointTextBeingMoved)
            {
                var mousePx = GetMousePx(e);
                var w = CadManager.Camera.ScreenToWorld(mousePx);

                var delta = new Vector2(w.X - _pressedToggleButtonPoint.Position.X.ToFloat(),
                    w.Y - _pressedToggleButtonPoint.Position.Y.ToFloat());

                UpdateCogoPointInfoOffset(_pressedToggleButtonPoint, delta);

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
                        _dragStart = new(_dragStart.X + translate.X, _dragStart.Y + translate.Y);
                    }
                    UpdateDragRect();
                }

                if (e.MiddleButton == MouseButtonState.Pressed)
                {
                    CadManager.Camera.Pan(currentMousePos, _prevMousePos);
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

            var mousePixels = GetMousePx(e);

            var matrix = CurrentlyAppliedDragRectMatrix;
            matrix.ScaleAt(Math.Pow(GlobalHelperProperties.ZoomFactor, zoomStep), Math.Pow(GlobalHelperProperties.ZoomFactor, zoomStep), mousePixels.X, mousePixels.Y);
            CurrentlyAppliedDragRectMatrix = matrix;
            UpdateDragRect();

            CadManager.Camera.Zoom(zoomStep, mousePixels);
            _hittestStrokeThickness = 7.0f / (CadManager.Camera.InitialViewMatrix.M11 * CadManager.Camera.CurrentZoom);

            UpdateToggleAnchorDimensions();

            _cogoHoverVerticesDirty = true;
            ConstantBuffersDirty = true;
            e.Handled = true;
        }
        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);

            if (IsDragging)
            {
                if (Mouse.LeftButton != MouseButtonState.Pressed)
                {
                    EndDrag();
                    UpdateDragRect();
                    _combinedDirty = true;
                }
            }

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
            if (isInside && IsDragging) { return; }

            _isMouseInside = false;
            _hitTestCancellationTokenSource.Cancel();
            _isPanning = false;

            if (_mouseOverCogoPoints.Count > 0 || _mouseOverHitTestableObjects.Count > 0)
            {
                if (!IsDragging)
                {
                    ResetHoverObjects();
                    _lineVerticesDirty = true;
                    _cogoHoverVerticesDirty = true;
                }
            }
        }
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            EndDrag();
            UpdateDragRect();

            if (_cogoPointTextBeingMoved)
            {
                RecomputeCogoPointBoundsFast(_pressedToggleButtonPoint);
                EndCogoToggleButtonPress();
                CadManager.UpdateCogoPointTree();
                UpdateInitialMatrix();

                if (IsMouseCaptured) { ReleaseMouseCapture(); }
                e.Handled = true;

                _combinedDirty = true;

                return;
            }

            _suspendHitTesting = true;
            bool geometryVerticesDirty = false;
            bool cogoHoverVerticesDirty = false;
            bool cogoPointVerticesDirty = false;
            bool sigPointsVerticesDirty = false;

            switch (CadManager.SnapSelectionMode)
            {
                case Common.Enums.SelectionMode.Geometries:
                    {
                        SelectedGeometries.DeferNotifications();
                        var newSel = new HashSet<DrawingGeometry>(_mouseOverHitTestableObjects.OfType<DrawingGeometry>());
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
                            foreach (var g in newSel)
                            {
                                if (IsShiftPressed) { DeselectObject(g); }
                                else { SelectObject(g); }
                            }
                        }
                        SelectedGeometries.EndDefer();

                        geometryVerticesDirty = true;

                        StateController.FlushObjectUpdates();

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
                                    cogoHoverVerticesDirty = true; cogoPointVerticesDirty = true;
                                }
                                else
                                {
                                    if (p.IsSelected) { continue; }
                                    SelectObject(p); SelectedCogoPoints.Add(p);
                                    cogoHoverVerticesDirty = true; cogoPointVerticesDirty = true;
                                }
                            }
                            StateController.FlushPointUpdates();
                        }
                        else
                        {
                            foreach (var p in newSel)
                            {
                                if (IsShiftPressed)
                                {
                                    if (!p.IsSelected) { continue; }
                                    DeselectObject(p); SelectedCogoPoints.Remove(p);
                                    cogoHoverVerticesDirty = true; cogoPointVerticesDirty = true;
                                }
                                else
                                {
                                    if (p.IsSelected) { continue; }
                                    SelectObject(p); SelectedCogoPoints.Add(p);
                                    cogoHoverVerticesDirty = true; cogoPointVerticesDirty = true;
                                }
                            }
                            StateController.FlushPointUpdates();
                        }
                        SelectedCogoPoints.EndDefer();
                        break;
                    }

                case Common.Enums.SelectionMode.Points:
                    {
                        if (SnappedHitTestablePoint is not null)
                        {
                            if (!SnappedHitTestablePoint.IsSelected)
                            {
                                SelectObject(SnappedHitTestablePoint);
                                sigPointsVerticesDirty = true;
                            }
                            else
                            {
                                DeselectObject(SnappedHitTestablePoint);
                                sigPointsVerticesDirty = true;
                            }
                        }
                        break;
                    }
            }

            ResetHoverObjects();

            if (sigPointsVerticesDirty) { _sigPointVerticesDirty = true; }
            if (geometryVerticesDirty) { _dxfDirty = true; }
            if (cogoHoverVerticesDirty) { _cogoHoverVerticesDirty = true; }
            if (cogoPointVerticesDirty) { _combinedDirty = true; _dxfDirty = true; }

            _suspendHitTesting = false;
        }
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            BeginDrag(e.GetPosition(this));
            UpdateDragOverlayVertices(DragRect);

            if (_mouseOverToggleButtonPoint is not null)
            {
                PressCogoToggleButton(_mouseOverToggleButtonPoint);
                ResetHoverObjects();
                ResetCogoToggleButtonMouseOver();

                var mousePx = GetMousePx(e);
                var w = CadManager.Camera.ScreenToWorld(mousePx);

                var delta = new Vector2(w.X - _pressedToggleButtonPoint.Position.X.ToFloat(),
                    w.Y - _pressedToggleButtonPoint.Position.Y.ToFloat());
                UpdateCogoPointInfoOffset(_pressedToggleButtonPoint, delta);
                _pressedToggleButtonPoint.HasLeaderLine = true;

                _cogoHoverVerticesDirty = true;

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
                _combinedDirty = true;
            }
            if (e.Key == Key.Tab)
            {
                _currentSnapHitTestIndex += 1;

                e.Handled = true;
            }
            if (e.Key == Key.Delete)
            {
                if (CadManager.SnapSelectionMode == Common.Enums.SelectionMode.CogoPoints &&
                    SelectedCogoPoints.Count > 0)
                {
                    DeleteCogoPoints(SelectedCogoPoints.ToList());
                    CompactStateBuffersIfUnder25Pct();
                    ResetHoverObjects();

                    _cogoHoverVerticesDirty = true;
                    _glyphVerticesDirty = true;
                    //_dxfDirty = true;
                    //_combinedDirty = true;
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

        protected override void OnTargetsResized(int wPx, int hPx)
        {
            base.OnTargetsResized(wPx, hPx);
            Viewport = new(0, 0, wPx, hPx, 0.0f, 1.0f);
            CadManager.ViewportSize = new Size2F(wPx, hPx);

            SetInitialMatrix();

            if (CadManager.Camera is not null)
            {
                CadManager.Camera.UpdateViewportSize(Viewport);
                CadManager.ResetTemplates();
                ConstantBuffersDirty = true;
            }
            _dxfDirty = true;
            _combinedDirty = true;
        }

        protected override void OnFrontBufferRestored()
        {
            _dxfDirty = true;
            _combinedDirty = true;
            ConstantBuffersDirty = true;
        }

        private void UpdateToggleAnchorDimensions()
        {
            float wupp = CadManager.Camera.GetWorldUnitsPerPixel();
            float desiredHalfWorld = (AnchorPixelSize * 0.5f) * wupp;
            float drawingShort = (float)Math.Min(CadManager.Camera.Extents.Width, CadManager.Camera.Extents.Height);
            float maxHalfBase = (drawingShort * MaxCogoToggleToDrawingFraction) * 0.5f;

            // Cache for settings
            _desiredHalfWorldForAnchors = desiredHalfWorld;
            _maxHalfBaseForAnchors = maxHalfBase;
            _featherWorldForAnchors = FeatherPx * wupp;

            foreach (var pg in CadManager.PointGroups)
            {
                foreach (var p in pg.Points)
                {
                    UpdateToggleAnchorBounds(p);
                }
            }
        }
        private void UpdateToggleAnchorBounds(CogoPoint pt)
        {
            float half = MathF.Min(
            _desiredHalfWorldForAnchors,
            _maxHalfBaseForAnchors * (float)pt.PointGroup.PointScale
            );

            var center = pt.Position.ToSharpDXVector2() + pt.TextInfoOffset; // world center of the toggle
            pt.ToggleBounds = new(center.X - half, center.Y - half, 2f * half, 2f * half);
        }

        private void UpdateCogoPointInfoOffset(CogoPoint point, Vector2 offset)
        {
            if (point is null) { return; }

            point.SetTextInfoOffset(offset);

            bool labelsNeedUpdate = SetCogoPointLabelQuadrant(point, offset);
            if (labelsNeedUpdate) { StateController.FlushLabelUpdates(); }

            StateController.SetPointInfoOffset(point, offset, true, point.IsFlippedY, point.IsFlippedX);
            StateController.FlushPointUpdates();

            UpdateToggleAnchorBounds(point);

            _combinedDirty = true;
            _dxfDirty = true;
        }
        private bool SetCogoPointLabelQuadrant(CogoPoint point, Vector2 offset)
        {
            bool labelsNeedUpdate = false;

            if (offset.X < 0 && !point.IsFlippedY)
            {
                point.IsFlippedY = true;

                point.PointNumberOffset = new(-point.PointNumberBounds.Width.ToFloat(), point.PointNumberOffset.Y);
                point.ElevationOffset = new(-point.ElevationBounds.Width.ToFloat(), point.ElevationOffset.Y);
                point.DescriptionOffset = new(-point.DescriptionBounds.Width.ToFloat(), point.DescriptionOffset.Y);

                StateController.SetLabelOffsets(point, point.PointNumberOffset, point.ElevationOffset, point.DescriptionOffset);
                labelsNeedUpdate = true;
            }
            if (offset.X > 0 && point.IsFlippedY)
            {
                point.IsFlippedY = false;

                point.PointNumberOffset = new(0, point.PointNumberOffset.Y);
                point.ElevationOffset = new(0, point.ElevationOffset.Y);
                point.DescriptionOffset = new(0, point.DescriptionOffset.Y);

                StateController.SetLabelOffsets(point, point.PointNumberOffset, point.ElevationOffset, point.DescriptionOffset);
                labelsNeedUpdate = true;
            }

            if (offset.Y < 0 && !point.IsFlippedX)
            {
                point.IsFlippedX = true;

                var translation = (float)(point.DescriptionBounds.Height / point.PointGroup.PointScale);

                point.PointNumberOffset = new(point.PointNumberOffset.X, -point.BaseDescriptionOffset_Y - translation);
                point.ElevationOffset = new(point.ElevationOffset.X, -point.BaseElevationOffset_Y - translation);
                point.DescriptionOffset = new(point.DescriptionOffset.X, -point.BasePointNumberOffset_Y - translation);

                StateController.SetLabelOffsets(point, point.PointNumberOffset, point.ElevationOffset, point.DescriptionOffset);
                labelsNeedUpdate = true;
            }
            if (offset.Y > 0 && point.IsFlippedX)
            {
                point.IsFlippedX = false;

                point.PointNumberOffset = new(point.PointNumberOffset.X, point.BasePointNumberOffset_Y);
                point.ElevationOffset = new(point.ElevationOffset.X, point.BaseElevationOffset_Y);
                point.DescriptionOffset = new(point.DescriptionOffset.X, point.BaseDescriptionOffset_Y);

                StateController.SetLabelOffsets(point, point.PointNumberOffset, point.ElevationOffset, point.DescriptionOffset);
                labelsNeedUpdate = true;
            }

            return labelsNeedUpdate;
        }

        public void ZoomToExtents()
        {
            if (CadManager.Camera is null) { return; }

            CadManager.Camera.ResetView(_dxfInitialMatrix, CadManager.Extents);
            ResetHoverObjects();
            UpdateToggleAnchorDimensions();
            ConstantBuffersDirty = true;
        }
        public void ZoomToPoint()
        {
            _hittestStrokeThickness = 7.0f / (CadManager.Camera.InitialViewMatrix.M11 * CadManager.Camera.CurrentZoom);
            UpdateToggleAnchorDimensions();
        }
        public void UpdateDragRect()
        {
            if (!IsDragging)
            {
                DragRect = new(0, 0, 0, 0);
                UpdateDragOverlayVertices(DragRect);
                return;
            }
            double width = Math.Abs(_dragStart.X - DxfCoords.X);
            double height = Math.Abs(_dragStart.Y - DxfCoords.Y);
            double left = Math.Min(_dragStart.X, DxfCoords.X);
            double top = Math.Min(_dragStart.Y, DxfCoords.Y);
            DragRect = new(left, top, width, height);

            UpdateDragOverlayVertices(DragRect);
        }
        public void EndDrag()
        {
            IsDragging = false;
            DragRect = new(0, 0, 0, 0);
            _lastQueriedDxfRect = Rect.Empty;
        }
        public void BeginDrag(Point start)
        {
            _dragStartScreen = start;
            _dragStart = DxfCoords.ToPoint();
            DragRect = new(0, 0, 0, 0);
            _dxfDragRectTranslate = new(0, 0);
            CurrentlyAppliedDragRectMatrix = new();
        }

        public async Task RunHitTestingAsync()
        {
            while (_isMouseInside)
            {
                if (_hitTestCancellationTokenSource.Token.IsCancellationRequested) { break; }

                if (_suspendHitTesting) { await Task.Delay(50); continue; }

                if (CadManager.DxfLoaded && CadManager.HitTestingEnabled)
                {
                    switch (CadManager.SnapSelectionMode)
                    {
                        case Common.Enums.SelectionMode.Points:
                            RunPointsHitTest(_hitTestCancellationTokenSource.Token);
                            break;

                        case Common.Enums.SelectionMode.Geometries:
                            if (IsDragging) { RunDragGeometriesHittest(_hitTestCancellationTokenSource.Token); }
                            else { RunGeometriesHitTest(_hitTestCancellationTokenSource.Token); }
                            break;

                        case Common.Enums.SelectionMode.CogoPoints:
                            if (_cogoPointTextBeingMoved) { break; }
                            else
                            {
                                if (IsDragging) { RunDragCogoPointsHittest(_hitTestCancellationTokenSource.Token); }
                                else { RunCogoPointsHitTest(_hitTestCancellationTokenSource.Token); }
                                break;
                            }

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
            if (!CadManager.DxfLoaded) { return; }

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

                    _nearestHitTestablePoints = CadManager.HitTestSignficantPoints(_lastHitTestCoords, _hittestStrokeThickness).Take(_maxSelectableObjects).ToList();

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
                _nearestHitTestablePoints = CadManager.HitTestSignficantPoints(_lastHitTestCoords, _hittestStrokeThickness).Take(_maxSelectableObjects).ToList();

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

            if (!CadManager.DxfLoaded) { return; }

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

                        _nearestHitTestableGeometries = CadManager.HitTestGeometries(_lastHitTestCoords, _hittestStrokeThickness).Take(_maxSelectableObjects).ToList();
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
                _nearestHitTestableGeometries = CadManager.HitTestGeometries(_lastHitTestCoords, _hittestStrokeThickness).Take(_maxSelectableObjects).ToList();

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
                StateController.FlushObjectUpdates();
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
            if (!CadManager.DxfLoaded) { return; }

            _lastHitTestCoords = new(DxfCoords.X, DxfCoords.Y);

            var snappedCogoPointsCopy = _mouseOverCogoPoints.ToList();
            bool hoverVerticesDirty = false;
            bool pointFlushNeeded = false;

            if (_mouseOverToggleButtonPoint is not null)
            {
                if (_mouseOverToggleButtonPoint.IsSelected &&
                    _mouseOverToggleButtonPoint.ToggleBounds.Contains(_lastHitTestCoords))
                {
                    if (!_mouseOverToggleButtonPoint.IsMouseOverToggleButton)
                    {
                        ResetHoverObjects();
                        MouseOverCogoToggleButton(_mouseOverToggleButtonPoint);
                        _cogoHoverVerticesDirty = true;
                        return;
                    }
                }
                else
                {
                    ResetCogoToggleButtonMouseOver();
                    hoverVerticesDirty = true;
                }
            }

            if (snappedCogoPointsCopy is not null && snappedCogoPointsCopy.Count > 0)
            {
                foreach (var snappedCogoPoint in snappedCogoPointsCopy)
                {
                    if (snappedCogoPoint.IsSelected &&
                        snappedCogoPoint.ToggleBounds.Contains(_lastHitTestCoords))
                    {
                        ResetHoverObjects();
                        MouseOverCogoToggleButton(snappedCogoPoint);
                        _cogoHoverVerticesDirty = true;
                        StateController.FlushPointUpdates();
                        return;
                    }

                    if (snappedCogoPoint.DistanceToPoint(_lastHitTestCoords) > _hittestStrokeThickness)
                    {
                        ResetHoverObjects();
                        ResetCogoToggleButtonMouseOver();
                        hoverVerticesDirty = true;
                        pointFlushNeeded = true;

                        _nearestHitTestableCogoPoints = CadManager.HitTestCogoPoints(_lastHitTestCoords, _hittestStrokeThickness).Take(_maxSelectableObjects).ToList();
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
                                    if (point.IsSelected && point.ToggleBounds.Contains(_lastHitTestCoords))
                                    {
                                        MouseOverCogoToggleButton(point);
                                        _cogoHoverVerticesDirty = true;
                                        return;
                                    }
                                    _mouseOverCogoPoints.Add(point);
                                    HoverObject(point);
                                    _lastSnapHitTestIndex = _currentSnapHitTestIndex;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                _nearestHitTestableCogoPoints = CadManager.HitTestCogoPoints(_lastHitTestCoords, _hittestStrokeThickness)
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
                            if (point.IsSelected && point.ToggleBounds.Contains(_lastHitTestCoords))
                            {
                                MouseOverCogoToggleButton(point);
                                ResetHoverObjects();
                                _cogoHoverVerticesDirty = true; StateController.FlushPointUpdates();
                                return;
                            }
                            _mouseOverCogoPoints.Add(point);
                            HoverObject(point);
                            hoverVerticesDirty = true;
                            pointFlushNeeded = true;
                            _lastSnapHitTestIndex = _currentSnapHitTestIndex;
                        }
                    }
                }
            }

            if (hoverVerticesDirty) { _cogoHoverVerticesDirty = true; }
            if (pointFlushNeeded) { StateController.FlushPointUpdates(); }
        }
        private async void RunDragCogoPointsHittest(CancellationToken token)
        {
            if (token.IsCancellationRequested) { return; }
            if (!CadManager.DxfLoaded) { return; }

            // Read DragRect safely from UI thread (cheap, single read)
            Rect currentRect = await Dispatcher.InvokeAsync(() => DragRect, DispatcherPriority.Render);
            if (currentRect.IsEmpty || currentRect.Width <= 0 || currentRect.Height <= 0) { return; }

            var newSet = CadManager
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

            if (adds.Count > 0 || removes.Count > 0) { _cogoHoverVerticesDirty = true; }

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

            if (!CadManager.DxfLoaded) { return; }
            if (_lastQueriedDxfRect == DragRect) { return; }

            var addedRegions = GetDragDelta(_lastQueriedDxfRect, DragRect);
            var removedRegions = GetDragDelta(DragRect, _lastQueriedDxfRect);
            if (_lastQueriedDxfRect.IsEmpty || _lastQueriedDxfRect == new Rect(0, 0, 0, 0))
            {
                removedRegions = [];
            }

            bool flushObjectStates = false;

            foreach (var region in addedRegions)
            {
                var newHits = CadManager.HitTestDragGeometries(region).Distinct();

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
                var possiblyRemoved = CadManager.HitTestDragGeometries(region).Distinct();

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
                StateController.FlushObjectUpdates();
                _dxfDirty = true;
            }
        }
        public void CancelHitTesting()
        {
            _hitTestCancellationTokenSource?.Cancel();
        }

        private void LoadHitTestableObjectTree()
        {
            if (CadManager is null) { return; }

            CadManager.UpdateHitTestableObjectTree();
            HitTestableObjectTreeDirty = false;
        }

        private void HoverObject(HitTestableObject hitTestableObject)
        {
            if (hitTestableObject is not null && !hitTestableObject.IsMouseOver)
            {
                if (hitTestableObject is DrawingObject obj)
                {
                    if (obj is DrawingGeometry geometry)
                    {
                        geometry.MouseEnter();
                        StateController.SetObjectMouseOver(geometry, true);
                    }
                }
                if (hitTestableObject is CogoPoint cogoPoint)
                {
                    if (!cogoPoint.IsMouseOver)
                    {
                        cogoPoint.MouseEnter();
                        StateController.SetPointMouseOver(cogoPoint, true);
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
                if (hitTestableObject is DrawingObject obj)
                {
                    if (obj is DrawingGeometry geometry)
                    {
                        geometry.MouseLeave();
                        StateController.SetObjectMouseOver(geometry, false);
                    }
                }
                if (hitTestableObject is CogoPoint dxfPoint)
                {
                    if (dxfPoint.IsMouseOver)
                    {
                        dxfPoint.MouseLeave();
                        StateController.SetPointMouseOver(dxfPoint, false);
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
            StateController.FlushObjectUpdates();

            foreach (var point in _mouseOverCogoPoints) { DehoverObject(point); }
            StateController.FlushPointUpdates();

            _mouseOverHitTestableObjects.Clear();
            _mouseOverCogoPoints.Clear();
        }
        private void SelectObject(HitTestableObject hitTestableObject)
        {
            if (hitTestableObject is not null)
            {
                if (hitTestableObject is DrawingObject obj)
                {
                    if (obj is DrawingGeometry geometry)
                    {
                        if (geometry.IsSelected) { return; }
                        geometry.Select();
                        StateController.SetObjectSelected(geometry, true);
                        SelectedGeometries.Add(geometry);
                    }
                }
                if (hitTestableObject is CogoPoint dxfPoint)
                {
                    if (!dxfPoint.IsSelected)
                    {
                        dxfPoint.Select();
                        StateController.SetPointSelected(dxfPoint, true);
                    }
                }
                if (hitTestableObject is HitTestablePoint point)
                {
                    if (!point.IsSelected)
                    {
                        point.Select();
                        SelectedHitTestablePoints.Add(point);
                    }
                }
            }
        }
        private void DeselectObject(HitTestableObject hitTestableObject)
        {
            if (hitTestableObject is not null)
            {
                if (hitTestableObject is DrawingObject obj)
                {
                    if (obj is DrawingGeometry geometry)
                    {
                        if (geometry.IsSelected)
                        {
                            geometry.Deselect();
                            StateController.SetObjectSelected(geometry, false);
                            SelectedGeometries.Remove(geometry);
                        }
                    }
                }
                if (hitTestableObject is CogoPoint dxfPoint)
                {
                    if (dxfPoint.IsSelected)
                    {
                        dxfPoint.Deselect();
                        StateController.SetPointSelected(dxfPoint, false);
                    }
                }
                if (hitTestableObject is HitTestablePoint point)
                {
                    if (point.IsSelected)
                    {
                        point.Deselect();
                        SelectedHitTestablePoints.Remove(point);
                    }
                }
            }
        }
        public void ResetSelectedObjects()
        {
            EndDrag();

            var listCopy = SelectedGeometries.ToList();
            foreach (var obj in listCopy) { obj.Deselect(); StateController.SetObjectSelected(obj, false); }
            SelectedGeometries.Clear();

            var sigPointsCopy = SelectedHitTestablePoints.ToList();
            foreach (var obj in sigPointsCopy) { DeselectObject(obj); }
            SelectedHitTestablePoints.Clear();

            var cogoPointsCopy = SelectedCogoPoints.ToList();
            foreach (var point in cogoPointsCopy) { DeselectObject(point); }
            SelectedCogoPoints.Clear();

            StateController.FlushPointUpdates();
            StateController.FlushObjectUpdates();
            _sigPointVerticesDirty = true;
            _dxfDirty = _combinedDirty = true;
        }

        private void MouseOverCogoToggleButton(CogoPoint cogoPoint)
        {
            if (_mouseOverToggleButtonPoint is not null)
            {
                if (_mouseOverToggleButtonPoint == cogoPoint) { return; }
                else
                {
                    _mouseOverToggleButtonPoint.IsMouseOverToggleButton = false;
                    StateController.SetPointAnchorMouseOver(_mouseOverToggleButtonPoint, false);
                    _mouseOverToggleButtonPoint = cogoPoint;
                    _mouseOverToggleButtonPoint.IsMouseOverToggleButton = true;
                    StateController.SetPointAnchorMouseOver(_mouseOverToggleButtonPoint, true);
                    StateController.FlushPointUpdates();
                }
            }
            else
            {
                _mouseOverToggleButtonPoint = cogoPoint;
                _mouseOverToggleButtonPoint.IsMouseOverToggleButton = true;
                StateController.SetPointAnchorMouseOver(_mouseOverToggleButtonPoint, true);
                StateController.FlushPointUpdates();
            }
        }
        private void ResetCogoToggleButtonMouseOver()
        {
            if (_mouseOverToggleButtonPoint is null ||
                !_mouseOverToggleButtonPoint.IsMouseOverToggleButton) { return; }

            _mouseOverToggleButtonPoint.IsMouseOverToggleButton = false;
            StateController.SetPointAnchorMouseOver(_mouseOverToggleButtonPoint, false);
            StateController.FlushPointUpdates();
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
        private void EndCogoToggleButtonPress()
        {
            _pressedToggleButtonPoint?.IsToggleButtonPressed = false;
            _pressedToggleButtonPoint = null;
        }

        public void DeleteCogoPoints(List<CogoPoint> cps)
        {
            foreach (var cp in cps)
            {
                StateController.SetPointVisible(cp, false);
                StateController.SetLabelVisible(cp, 0, false);
                StateController.SetLabelVisible(cp, 1, false);
                StateController.SetLabelVisible(cp, 2, false);

                CadManager.TryDeletePoint(cp);
            }

            StateController.FlushPointUpdates();
            StateController.FlushLabelUpdates();

            CadManager.UpdateCogoPointTree();
            UpdateInitialMatrix();
        }
        private void UnbindAllStateSrvs(DeviceContext ctx)
        {
            ctx.VertexShader.SetShaderResource(0, null);
            ctx.VertexShader.SetShaderResource(1, null);
            ctx.GeometryShader.SetShaderResource(0, null);
            ctx.GeometryShader.SetShaderResource(1, null);
            ctx.PixelShader.SetShaderResource(0, null);
            ctx.PixelShader.SetShaderResource(1, null);
        }
        public void CompactStateBuffersIfUnder25Pct()
        {
            if (StateBuffers is null || CadManager is null) { return; }

            // Current live counts
            int groups = CadManager.PointGroups.Count;
            int points = CadManager.PointGroups.SelectMany(pg => pg.Points).Count();
            int labelsPerPoint = 3;              // adjust if you render fewer/more lines
            int labels = points * labelsPerPoint;
            int layers = SceneIdMap?.LayerCount ?? 0;
            int objects = SceneIdMap?.ObjectCount ?? 0;

            StateBuffers.MaybeShrinkAllTo25PctOrLess(labels, points, groups, layers, objects, UnbindAllStateSrvs);

            // Re-upload CPU shadow arrays (if needed)
            // (Most of your state is already kept in _stateBufs.*Span; call your normal upload)
            // Example: _stateBufs.FlushAll();
        }

        public void InvalidateCogoPointRendering()
        {
            _glyphVerticesDirty = true;
            _pointCircleVerticesDirty = true;
            _leaderLineVerticesDirty = true;
            _anchorVerticesDirty = true;
            _cogoHoverVerticesDirty = true;

            _combinedDirty = true;
            _dxfDirty = true;
        }

        private Vector2 DipToPixel(Vector2 dip)
        {
            var dpi = VisualTreeHelper.GetDpi(this);
            return new Vector2(
                dip.X * (float)dpi.DpiScaleX,
                dip.Y * (float)dpi.DpiScaleY
            );
        }
        private Vector2 GetMousePx(MouseEventArgs e)
        {
            var pDip = e.GetPosition(this);
            var dpi = VisualTreeHelper.GetDpi(this);

            return new Vector2(
                (float)(pDip.X * dpi.DpiScaleX),
                (float)(pDip.Y * dpi.DpiScaleY)
            );
        }

        private void ClearDxf()
        {
            CadManager.Camera.ResetView(Matrix.Identity, CadManager.Extents);
            ResetHoverObjects();
            _lineVerticesDirty = _textVerticesDirty = true;
        }

        private static void OnCadManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not D3dDxfControl control) { return; }

            if (e.OldValue is CadManager oldCadManager3D)
            {
                oldCadManager3D.PropertyChanged -= control.CadManager_PropertyChanged;
                oldCadManager3D.ZoomToExtentsRequested -= control.ZoomToExtents;
                oldCadManager3D.ZoomToPointRequested -= control.ZoomToPoint;
                oldCadManager3D.CogoPoints.CollectionChanged -= control.CogoPoints_CollectionChanged;
                oldCadManager3D.PointGroups.CollectionChanged -= control.PointGroups_CollectionChanged;
                oldCadManager3D.Layers.CollectionChanged -= control.Layers_CollectionChanged;
            }

            if (e.NewValue is CadManager newCadManager3D)
            {
                newCadManager3D.PropertyChanged += control.CadManager_PropertyChanged;
                newCadManager3D.ZoomToExtentsRequested += control.ZoomToExtents;
                newCadManager3D.ZoomToPointRequested += control.ZoomToPoint;
                newCadManager3D.CogoPoints.CollectionChanged += control.CogoPoints_CollectionChanged;
                newCadManager3D.PointGroups.CollectionChanged += control.PointGroups_CollectionChanged;
                newCadManager3D.Layers.CollectionChanged += control.Layers_CollectionChanged;
            }
        }
        private void CadManager_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CadManager.DxfNeedsReload))
            {
                if (CadManager.DxfNeedsReload)
                {
                    DxfNeedsReload = true;
                }
            }
            if (e.PropertyName == nameof(CadManager.LineVerticesDirty))
            {
                if (CadManager.LineVerticesDirty)
                {
                    _lineVerticesDirty = true;
                }
            }
            if (e.PropertyName == nameof(CadManager.TextVerticesDirty))
            {
                if (CadManager.TextVerticesDirty)
                {
                    _textVerticesDirty = true;
                }
            }
            if (e.PropertyName == nameof(CadManager.SolidVerticesDirty))
            {
                if (CadManager.SolidVerticesDirty)
                {
                    _solidVerticesDirty = true;
                }
            }
            if (e.PropertyName == nameof(CadManager.CogoPointTextVerticesDirty))
            {
                if (CadManager.CogoPointTextVerticesDirty)
                {
                    _glyphVerticesDirty = true;
                    _leaderLineVerticesDirty = true;
                    _anchorVerticesDirty = true;
                }
            }
            if (e.PropertyName == nameof(CadManager.CogoPointCircleVerticesDirty))
            {
                if (CadManager.CogoPointCircleVerticesDirty)
                {
                    _pointCircleVerticesDirty = true;
                }
            }
            if (e.PropertyName == nameof(CadManager.HitTestableObjectTreeDirty))
            {
                if (CadManager.HitTestableObjectTreeDirty)
                {
                    HitTestableObjectTreeDirty = true;
                }
            }
            if (e.PropertyName == nameof(CadManager.DxfLoaded) && !CadManager.DxfLoaded)
            {
                ClearDxf();
            }
            if (e.PropertyName == nameof(CadManager.SnapSelectionMode))
            {
                ResetSelectedObjects();
                ResetHoverObjects();
                _currentSnapHitTestIndex = 0;
            }
        }

        private void PointGroups_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            PointGroups = CadManager?.PointGroups;
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (PointGroup pg in e.NewItems)
                {
                    if (pg is null) { continue; }

                    pg.PropertyChanged -= PointGroup_PropertyChanged;
                    pg.PropertyChanged += PointGroup_PropertyChanged;

                    var gId = SceneIdMap.GetOrAddGroupId(pg, out var isNew);
                    if (isNew) { StateBuffers.InitializeGroupState(SceneIdMap.MaxGroupId, pg, gId); }
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (PointGroup pg in e.OldItems)
                {
                    if (pg is null) { continue; }
                    pg.PropertyChanged -= PointGroup_PropertyChanged;
                }
            }
        }
        private void CogoPoints_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            CogoPoints = CadManager?.CogoPoints;
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var obj in e.NewItems)
                {
                    if (obj is not CogoPoint cp) { continue; }

                    cp.PropertyChanged -= CogoPoint_PropertyChanged;
                    cp.PropertyChanged += CogoPoint_PropertyChanged;

                    uint pId = SceneIdMap.GetOrAddPointId(cp, out var isNewPoint);
                    uint gId = SceneIdMap.GetOrAddGroupId(cp.PointGroup, out var isNewGroup);
                    if (isNewGroup) { StateBuffers.InitializeGroupState(SceneIdMap.MaxGroupId, cp.PointGroup, gId); }
                    if (isNewPoint) { StateBuffers.InitializePointState(SceneIdMap.MaxPointId, cp, pId, gId); }

                    uint idPN = SceneIdMap.GetOrAddLabelId(cp, 0, out var isNew);
                    if (isNew) { StateBuffers.InitializeLabelState(SceneIdMap.MaxLabelCount, cp.PointNumberOffset, idPN); }

                    uint idElev = SceneIdMap.GetOrAddLabelId(cp, 1, out isNew);
                    if (isNew) { StateBuffers.InitializeLabelState(SceneIdMap.MaxLabelCount, cp.ElevationOffset, idElev); }

                    uint idDesc = SceneIdMap.GetOrAddLabelId(cp, 2, out isNew);
                    if (isNew) { StateBuffers.InitializeLabelState(SceneIdMap.MaxLabelCount, cp.DescriptionOffset, idDesc); }
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
            Layers = CadManager?.Layers;
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (KeyValuePair<string, ObjectLayer> keyValue in e.NewItems)
                {
                    var layer = keyValue.Value;
                    if (layer is null) { continue; }

                    layer.PropertyChanged -= Layer_PropertyChanged;
                    layer.PropertyChanged += Layer_PropertyChanged;
                    layer.DrawingObjects.CollectionChanged -= DrawingObjects_CollectionChanged;
                    layer.DrawingObjects.CollectionChanged += DrawingObjects_CollectionChanged;

                    var lid = SceneIdMap.GetOrAddLayerId(layer, out bool isNew);
                    layer.Id = lid;
                    if (isNew) { StateBuffers.InitializeLayerState(SceneIdMap.MaxLayerId, layer, lid); }
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (KeyValuePair<string, ObjectLayer> keyValue in e.OldItems)
                {
                    var layer = keyValue.Value;
                    if (layer is null) { continue; }

                    layer.PropertyChanged -= Layer_PropertyChanged;
                    layer.DrawingObjects.CollectionChanged -= DrawingObjects_CollectionChanged;
                }
            }
        }
        private void DrawingObjects_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                bool isNew = true;
                foreach (var obj in e.NewItems)
                {
                    if (obj is not DrawingObject drawingObj) { continue; }

                    if (obj is DrawingMtext drawingMtext)
                    {
                        if (drawingMtext.MtextBlock is null) { drawingMtext.UpdateMtextBlock(ResCache, drawingMtext.Layer.Id, SceneIdMap, StateBuffers); }
                        foreach (var seg in drawingMtext.Segments)
                        {
                            var segId = SceneIdMap.GetOrAddObjectId(seg, out isNew);
                            if (isNew) { StateBuffers.InitializeObjectState(SceneIdMap.MaxObjectId, seg, segId); }
                            continue;
                        }
                    }
                    var oId = SceneIdMap.GetOrAddObjectId(drawingObj, out isNew);
                    if (isNew) { StateBuffers.InitializeObjectState(SceneIdMap.MaxObjectId, drawingObj, oId); }
                }
            }
        }
        private void Layer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ObjectLayer.IsVisible))
            {
                if (sender is ObjectLayer layer)
                {
                    StateController.SetLayerVisibility(layer, layer.IsVisible);
                    StateController.FlushLayerUpdates();
                    _dxfDirty = true;
                }
            }
            if (e.PropertyName == nameof(ObjectLayer.Color))
            {
                if (sender is ObjectLayer layer)
                {
                    StateController.SetLayerColor(layer, layer.Color);
                    StateController.FlushLayerUpdates();
                    _dxfDirty = true;
                }
            }
        }
        private void PointGroup_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is PointGroup pg)
            {
                if (e.PropertyName == nameof(PointGroup.IsVisible))
                {
                    StateController.SetGroupVisibility(pg, pg.IsVisible);
                    StateController.FlushGroupUpdates();
                    _dxfDirty = true;
                    _combinedDirty = true;
                }
                if (e.PropertyName == nameof(PointGroup.Color) || e.PropertyName == nameof(PointGroup.PointScale))
                {
                    pg.UpdatePointInfoBaseXoffset();
                    StateController.SetGroupScaleColorBaseOffset(pg, pg.PointScale.ToFloat(), pg.Color.ToSharpDXVector4(), pg.PointInfoBaseXoffset);
                    StateController.FlushGroupUpdates();

                    if (e.PropertyName == nameof(PointGroup.PointScale))
                    {
                        bool labelsNeedUpdate = false;
                        foreach (var point in pg.Points)
                        {
                            RecomputeCogoPointBoundsFast(point);
                            UpdateToggleAnchorBounds(point);

                            if (point.IsFlippedX || point.IsFlippedY)
                            {
                                point.UpdateOffsetOrientation();
                                StateController.SetLabelOffsets(point, point.PointNumberOffset, point.ElevationOffset, point.DescriptionOffset);
                                labelsNeedUpdate = true;

                                RecomputeCogoPointBoundsFast(point); // Done a second time because the first call is just to get lines width
                            }
                        }
                        if (labelsNeedUpdate) { StateController.FlushLabelUpdates(); }

                        CadManager.UpdateCogoPointTree();
                        UpdateInitialMatrix();
                    }

                    _dxfDirty = true;
                    _combinedDirty = true;
                }
                if (e.PropertyName == nameof(PointGroup.PointInfoBaseXoffset))
                {
                    foreach (var point in pg.Points)
                    {
                        UpdateToggleAnchorBounds(point);
                    }
                    _combinedDirty = true;
                    _dxfDirty = true;
                }
                if (e.PropertyName == nameof(PointGroup.Name))
                {

                }
            }
        }
        private void CogoPoint_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CogoPoint.Easting) || e.PropertyName == nameof(CogoPoint.Northing))
            {
                if (sender is CogoPoint cp)
                {
                    StateController.SetPointOffset(cp, cp.Position.ToSharpDXVector2());
                    StateController.FlushPointUpdates();
                    RecomputeCogoPointBoundsFast(cp);

                    CadManager.UpdateCogoPointTree();
                    UpdateInitialMatrix();

                    _pointCircleVerticesDirty = true; _dxfDirty = true; _combinedDirty = true;
                }
            }
            if (e.PropertyName == nameof(CogoPoint.PointGroup))
            {
                if (sender is CogoPoint cp)
                {
                    var id = SceneIdMap.GetOrAddGroupId(cp.PointGroup, out bool isNew);
                    if (isNew)
                    {
                        StateController.SetPointGroupId(cp, id);
                        StateController.FlushPointUpdates();
                    }
                    RecomputeCogoPointBoundsFast(cp);

                    CadManager.UpdateCogoPointTree();
                    UpdateInitialMatrix();

                    _dxfDirty = true; _combinedDirty = true;
                }
            }
            if (e.PropertyName == nameof(CogoPoint.PointNumber) ||
                e.PropertyName == nameof(CogoPoint.Elevation) ||
                e.PropertyName == nameof(CogoPoint.Description))
            {
                _glyphVerticesDirty = true;
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
                    _attachedWindow?.KeyUp -= Window_KeyUp;

                    _textVertexBuffer?.Dispose(); _textVertexBuffer = null;
                    _textVertexShader?.Dispose(); _textVertexShader = null;
                    _textPixelShader?.Dispose(); _textPixelShader = null;
                    _textInputLayout?.Dispose(); _textInputLayout = null;

                    _lineVertexBuffer?.Dispose(); _lineVertexBuffer = null;
                    _dxfObjectSettingsBuffer?.Dispose(); _dxfObjectSettingsBuffer = null;
                    _lineRenderModeBuffer?.Dispose(); _lineRenderModeBuffer = null;
                    _lineGlowSettingsBuffer?.Dispose(); _lineGlowSettingsBuffer = null;
                    _lineVertexShader?.Dispose(); _lineVertexShader = null;
                    _linePixelShader?.Dispose(); _linePixelShader = null;
                    _lineGlowVertexShader?.Dispose(); _lineGlowVertexShader = null;
                    _lineGlowPixelShader?.Dispose(); _lineGlowPixelShader = null;
                    _lineGlowGeometryShader?.Dispose(); _lineGlowGeometryShader = null;
                    _lineInputLayout?.Dispose(); _lineInputLayout = null;

                    _solidInputLayout?.Dispose(); _solidInputLayout = null;
                    _solidPixelShader?.Dispose(); _solidPixelShader = null;
                    _solidVertexBuffer?.Dispose(); _solidVertexBuffer = null;
                    _solidVertexShader?.Dispose(); _solidVertexShader = null;

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
                    _cogoPointGlowSettingsBuffer?.Dispose(); _cogoPointGlowSettingsBuffer = null;
                    _hoverCircleLayout?.Dispose(); _hoverCircleLayout = null;

                    _leaderLineBuffer?.Dispose(); _leaderLineBuffer = null;
                    _leaderLineGS?.Dispose(); _leaderLineGS = null;
                    _leaderLinePS?.Dispose(); _leaderLinePS = null;
                    _leaderLineVS?.Dispose(); _leaderLineVS = null;
                    _leaderLineInputLayout?.Dispose(); _leaderLineInputLayout = null;
                    _leaderLineSettings?.Dispose(); _leaderLineSettings = null;

                    _leaderLineGlowGS?.Dispose(); _leaderLineGlowGS = null;
                    _leaderLineGlowPS?.Dispose(); _leaderLineGlowPS = null;
                    _leaderLineGlowVS?.Dispose(); _leaderLineGlowVS = null;
                    _leaderLineGlowSettings?.Dispose(); _leaderLineGlowSettings = null;

                    _toggleLayout?.Dispose(); _toggleLayout = null;
                    _toggleQuadVB?.Dispose(); _toggleQuadVB = null;
                    _toggleVS?.Dispose(); _toggleVS = null;
                    _togglePS?.Dispose(); _togglePS = null;

                    StateBuffers.Dispose(); StateBuffers = null;
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
