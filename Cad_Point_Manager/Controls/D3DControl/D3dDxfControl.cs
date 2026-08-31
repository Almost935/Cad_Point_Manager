using Cad_Point_Manager.Common;
using Cad_Point_Manager.Common.Collections;
using Cad_Point_Manager.Controls.D3DControl.Buffers;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Msdf;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Helpers;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Helpers.EqualityComparers;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.DrawingObjects;
using Cad_Point_Manager.Models.HitTesting;
using Cad_Point_Manager.Models.PointRendering;
using PdfSharpCore.Pdf.Advanced;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct2D1.Effects;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SixLabors.Fonts;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
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

        private bool _baseSceneDirty = true;
        private bool _interactionDirty = true;

        private Buffer _transformationBuffer;

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
        private bool _dragOverlayDirty = false;

        // Direct3D related fields
        public bool _buffersInitialized = false;
        private Buffer _drawingSettingsBuffer;

        // Line shader related fields
        private ResizableBuffer<LineInstance> _lineInstanceBuffer;
        private Buffer _lineQuadBuffer;
        private Buffer _lineRenderModeBuffer;
        private int _lineInstanceCount;
        private VertexShader _lineVertexShader;
        private PixelShader _linePixelShader;
        private InputLayout _lineInstanceInputLayout;
        private bool _lineShadersLoaded = false;
        private bool _lineVerticesDirty = false;

        // Line glow shader related fields
        private VertexShader _lineGlowVertexShader;
        private PixelShader _lineGlowPixelShader;
        private Buffer _lineGlowCompositeVertexBuffer;
        private VertexShader _lineGlowCompositeVS;
        private PixelShader _lineGlowCompositePS;
        private InputLayout _lineGlowCompositeLayout;
        private SamplerState _lineGlowCompositeSampler;
        private bool _lineGlowShadersLoaded = false;
        private ResizableBuffer<LineInstance> _lineGlowInstanceBuffer;
        private int _lineGlowInstanceCount = 0;
        private bool _lineGlowVerticesDirty = false;

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

        // CogoPoint shader related fields
        private bool _pointMarkerShadersLoaded = false;

        // MSDF rendering
        private VertexShader _msdfVS;
        private PixelShader _msdfPS;
        private InputLayout _msdfLayout;
        private Buffer _msdfQuadBuffer;
        private ResizableBuffer<MsdfGlyphInstance> _msdfInstanceBuffer;
        private bool _msdfShadersLoaded;
        private int _msdfInstanceCount;
        private SamplerState _msdfSampler;
        private readonly List<MsdfGlyphInstance> _msdfInstances = [];
        private Buffer _msdfSettingsBuffer;
        private bool _cogoTextVerticesDirty = false;
        private Buffer _cogoPointTextSettingsBuffer;

        // MSDF glow rendering
        private VertexShader _msdfGlowVS;
        private PixelShader _msdfGlowPS;

        // Point circle shader related fields
        private ResizableBuffer<PointMarkerInstance> _pointCircleVertexBuffer;
        private InputLayout _pointMarkerInputLayout;
        private VertexShader _pointMarkerVS;
        private PixelShader _pointMarkerPS;
        private GeometryShader _pointMarkerGS;
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
        private Buffer _leaderLineQuadBuffer;

        // Cogo point leader line glow rendering fields
        private VertexShader _leaderLineGlowVS;
        private PixelShader _leaderLineGlowPS;
        private GeometryShader _leaderLineGlowGS;
        private Buffer _leaderLineGlowSettings;

        // Cogo point hover rendering
        private bool _cogoHoverShadersLoaded = false;
        private bool _cogoHoverVerticesDirty = false;

        private ResizableBuffer<CircleHoverVertex> _hoverCircleBuffer;
        private VertexShader _hoverCircleVertexShader;
        private PixelShader _hoverCirclePixelShader;
        private GeometryShader _hoverCircleGeometryShader;
        private readonly List<CircleHoverVertex> _hoverCircleVertices = [];
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
        private Buffer _overlayOutlineSettingsBuffer;
        private ResizableBuffer<OverlayVertex> _dragFillBuffer;
        private int _dragFillVertexCount;
        private VertexShader _overlayVS;
        private PixelShader _overlayPS;
        private InputLayout _overlayLayout;
        private bool _overlayShaderLoaded;

        // Cached pan rendering
        private VertexShader _panVertexShader;
        private PixelShader _panPixelShader;
        private InputLayout _panInputLayout;
        private Buffer _panVertexBuffer;
        private Buffer _panSettingsBuffer;
        private SamplerState _panSampler;
        private bool _panShadersLoaded;
        private Vector2 _panCurrentMousePos;
        private Texture2D _panCacheTexture;
        private RenderTargetView _panCacheRtv;
        private ShaderResourceView _panCacheSrv;
        private int _panCacheWidth;
        private int _panCacheHeight;
        private bool _panCacheValid;

        // Panning and Zooming Fields
        private bool _isPanning;
        private Vector2 _panStartMousePos;
        private Vector2 _panStartCameraTranslate;
        private Vector2 _prevMousePos;
        private float _panWorldUnitsPerPixel;

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
        public bool TransformationBufferDirty { get; set; } = false;
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
                TransformationBufferDirty = true;
                DxfNeedsReload = false;
                CadManager.DxfNeedsReload = false;
            }
            if (!_buffersInitialized) { InitializeBuffers(); }

            if (_lineVerticesDirty) { UpdateLineVertices(); }
            if (_lineGlowVerticesDirty) { UpdateLineGlowInstances(); }
            if (_textVerticesDirty) { UpdateTextVertices(); }
            if (_solidVerticesDirty) { UpdateSolidVertices(); }
            if (_cogoTextVerticesDirty) { UpdateMsdfInstances(); }
            if (_pointCircleVerticesDirty) { UpdatePointCircleVertices(); }
            if (_cogoHoverVerticesDirty) { UpdateCogoHoverVertices(); }
            if (HitTestableObjectTreeDirty) { LoadHitTestableObjectTree(); }
            if (_anchorVerticesDirty) { UpdateToggleAnchorVertices(); }
            if (_leaderLineVerticesDirty) { UpdateLeaderLineVertices(); }
            if (_sigPointVerticesDirty) { UpdateSignificantPointVertices(); }
            if (_dragOverlayDirty) { UpdateDragOverlayVertices(DragRect); }

            if (!_lineShadersLoaded) { InitializeLineShaders(); }
            if (!_lineGlowShadersLoaded) { InitializeLineGlowShaders(); }
            if (!_textShaderLoaded) { InitializeTextShaders(); }
            if (!_solidShaderLoaded) { InitializeSolidShaders(); }
            if (!_overlayShaderLoaded) { InitializeOverlayShaders(); }
            if (!_msdfShadersLoaded) { InitializeMsdfShaders(); }
            if (!_pointMarkerShadersLoaded) { InitializePointMarkerShaders(); }
            if (!_cogoHoverShadersLoaded) { InitializeCogoPointHoverShaders(); }
            if (!_anchorShaderLoaded) { InitializeToggleAnchorShaders(); }
            if (!_leaderLineShadersLoaded) { InitializeLeaderLineShaders(); }
            if (!_sigPointShadersLoaded) { InitializeSignificantPointsShaders(); }
            if (!_panShadersLoaded) { InitializePanShaders(); }

            if (!ConstantBuffersInitialized) { InitializeConstantBuffers(); }
            if (ConstantBuffersDirty) { UpdateConstantBuffers(); }
            if (TransformationBufferDirty || CadManager.Camera.IsDirty) { UpdateTransformationBuffer(); }

            if (!_hitTestIsRunning)
            {
                _hitTestIsRunning = true;
                _hittestTask = Task.Run(() => RunHitTestingAsync());
            }

            var ctx = ResCache.DeviceContext;

            // Only Draw the cached pan if the user is actively panning, otherwise draw the full scene
            if (_isPanning)
            {
                DrawCachedPan(ctx);
                return;
            }

            if (_baseSceneDirty)
            {
                DrawDxf(ctx);

                _baseSceneDirty = false;
                _interactionDirty = true;
            }

            if (_interactionDirty)
            {
                ctx.CopyResource(ResCache.DxfTexture, ResCache.InteractionTexture);
                ctx.OutputMerger.SetRenderTargets(ResCache.InteractionRenderTargetView);

                DrawLineGlows(ctx);
                CompositeGlowTexture(ctx, ResCache.InteractionRenderTargetView);

                if (_hoverCircleVertices.Count > 0) { DrawCogoPointHover(ctx); }
                if (_anchorVerticesCount > 0) { DrawCogoPointAnchors(ctx); }
                if (_sigPointVertexCount > 0) { DrawSignificantPoints(ctx); }
                if (_msdfInstanceCount > 0) { DrawMsdfGlowGlyphs(ctx); }

                _interactionDirty = false;
            }

            ctx.CopyResource(ResCache.InteractionTexture, ResCache.Texture2D);
            ctx.OutputMerger.SetRenderTargets(ResCache.RenderTargetView);

            if (IsDragging && _dragFillVertexCount > 0)
            {
                DrawDragOverlay(ctx);
            }
        }

        private void DrawDxf(DeviceContext ctx)
        {
            ctx.OutputMerger.SetRenderTargets(ResCache.DxfRenderTargetView);
            ctx.ClearRenderTargetView(ResCache.DxfRenderTargetView, new RawColor4(1, 1, 1, 1));

            //Stopwatch sw = Stopwatch.StartNew();
            //Debug.WriteLine($"\n");

            DrawLines(ctx);
            //Debug.WriteLine($"Lines {sw.ElapsedMilliseconds} ms");
            //sw.Restart();

            DrawText(ctx);
            //Debug.WriteLine($"Text {sw.ElapsedMilliseconds} ms");
            //sw.Restart();

            DrawSolids(ctx);
            //Debug.WriteLine($"Solids {sw.ElapsedMilliseconds} ms");
            //sw.Restart();

            DrawPointCircles(ctx);
            //Debug.WriteLine($"Circles {sw.ElapsedMilliseconds} ms");
            //sw.Restart();

            DrawMsdfGlyphs(ctx);
            //Debug.WriteLine($"Glyphs {sw.ElapsedMilliseconds} ms");
            //sw.Restart();

            DrawLeaderLines(ctx);
            //Debug.WriteLine($"Glyphs {sw.ElapsedMilliseconds} ms");
        }

        private void DrawLines(DeviceContext ctx)
        {
            if (_lineInstanceBuffer is null || _lineInstanceCount == 0) { return; }

            // First pass for all non selected lines
            SetLineRenderMode(ctx, false, false);
            ctx.VertexShader.Set(_lineVertexShader);
            ctx.PixelShader.Set(_linePixelShader);
            ctx.InputAssembler.InputLayout = _lineInstanceInputLayout;

            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.VertexShader.SetConstantBuffer(1, _drawingSettingsBuffer);
            ctx.VertexShader.SetConstantBuffer(2, _lineRenderModeBuffer);

            ctx.VertexShader.SetShaderResource(0, StateBuffers.LayerSRV);
            ctx.VertexShader.SetShaderResource(1, StateBuffers.ObjectSRV);
            ctx.VertexShader.SetShaderResource(2, StateBuffers.LineTypeSRV);
            ctx.VertexShader.SetShaderResource(3, StateBuffers.PatternSRV);

            ctx.PixelShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.PixelShader.SetConstantBuffer(1, _drawingSettingsBuffer);
            ctx.PixelShader.SetConstantBuffer(2, _lineRenderModeBuffer);

            ctx.PixelShader.SetShaderResource(0, StateBuffers.LayerSRV);
            ctx.PixelShader.SetShaderResource(1, StateBuffers.ObjectSRV);
            ctx.PixelShader.SetShaderResource(2, StateBuffers.LineTypeSRV);
            ctx.PixelShader.SetShaderResource(3, StateBuffers.PatternSRV);

            ctx.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(
                _lineInstanceBuffer.Buffer, _lineInstanceBuffer.Stride, 0));
            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            var quadBinding = new VertexBufferBinding(
                _lineQuadBuffer, Utilities.SizeOf<LineCornerVertex>(), 0);

            var instanceBinding = new VertexBufferBinding(
                _lineInstanceBuffer.Buffer, _lineInstanceBuffer.Stride, 0);

            ctx.InputAssembler.SetVertexBuffers(0, quadBinding, instanceBinding);

            ctx.DrawInstanced(6, _lineInstanceCount, 0, 0);

            // Second pass for all selected lines
            SetLineRenderMode(ctx, true, false);
            ctx.DrawInstanced(6, _lineInstanceCount, 0, 0);
        }
        private void DrawLineGlows(DeviceContext ctx)
        {
            ctx.OutputMerger.SetRenderTargets(ResCache.GlowRenderTargetView);
            ctx.ClearRenderTargetView(ResCache.GlowRenderTargetView, new RawColor4(0, 0, 0, 0));

            if (_lineGlowInstanceBuffer is null || _lineGlowInstanceCount == 0)
            {
                return;
            }

            ctx.OutputMerger.SetBlendState(ResCache.MaxBlendState);

            ctx.VertexShader.Set(_lineGlowVertexShader);
            ctx.PixelShader.Set(_lineGlowPixelShader);
            ctx.GeometryShader.Set(null);

            ctx.InputAssembler.InputLayout = _lineInstanceInputLayout;
            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            var quadBinding = new VertexBufferBinding(_lineQuadBuffer, Utilities.SizeOf<LineCornerVertex>(), 0);
            var instanceBinding = new VertexBufferBinding(_lineGlowInstanceBuffer.Buffer, _lineGlowInstanceBuffer.Stride, 0);

            ctx.InputAssembler.SetVertexBuffers(0, quadBinding, instanceBinding);

            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.VertexShader.SetConstantBuffer(1, _drawingSettingsBuffer);

            ctx.PixelShader.SetConstantBuffer(1, _drawingSettingsBuffer);

            ctx.PixelShader.SetShaderResource(0, StateBuffers.LayerSRV);
            ctx.PixelShader.SetShaderResource(1, StateBuffers.ObjectSRV);
            ctx.PixelShader.SetShaderResource(2, StateBuffers.LineTypeSRV);
            ctx.PixelShader.SetShaderResource(3, StateBuffers.PatternSRV);

            ctx.DrawInstanced(6, _lineGlowInstanceCount, 0, 0);
        }
        private void CompositeGlowTexture(DeviceContext ctx, RenderTargetView rtv)
        {
            ctx.OutputMerger.SetRenderTargets(rtv);
            ctx.OutputMerger.SetBlendState(ResCache.BaseBlendState);

            ctx.VertexShader.Set(_lineGlowCompositeVS);
            ctx.PixelShader.Set(_lineGlowCompositePS);
            ctx.GeometryShader.Set(null);

            ctx.InputAssembler.InputLayout = _lineGlowCompositeLayout;
            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            ctx.InputAssembler.SetVertexBuffers(
                0, new VertexBufferBinding(_lineGlowCompositeVertexBuffer, Utilities.SizeOf<GlowCompositeVertex>(), 0));

            ctx.PixelShader.SetShaderResource(0, ResCache.GlowShaderResourceView);

            ctx.PixelShader.SetSampler(0, _lineGlowCompositeSampler);

            ctx.Draw(6, 0);
            ctx.PixelShader.SetShaderResource(0, null);
        }
        private void DrawText(DeviceContext ctx)
        {
            if (_textVertexBuffer is null) { return; }

            ctx.VertexShader.Set(_textVertexShader);
            ctx.PixelShader.Set(_textPixelShader);
            ctx.InputAssembler.InputLayout = _textInputLayout;

            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.VertexShader.SetConstantBuffer(1, _drawingSettingsBuffer);

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
            ctx.VertexShader.SetConstantBuffer(1, _drawingSettingsBuffer);

            ctx.VertexShader.SetShaderResource(0, StateBuffers.LayerSRV);
            ctx.VertexShader.SetShaderResource(1, StateBuffers.ObjectSRV);

            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            ctx.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(
                 _solidVertexBuffer.Buffer, _solidVertexBuffer.Stride, 0));

            ctx.Draw(_solidVertexCount, 0);
        }
        private void DrawMsdfGlyphs(DeviceContext ctx)
        {
            if (_msdfInstanceCount == 0) { return; }

            ctx.VertexShader.Set(_msdfVS);
            ctx.PixelShader.Set(_msdfPS);

            ctx.PixelShader.SetSampler(0, _msdfSampler);

            ctx.InputAssembler.InputLayout = _msdfLayout;
            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.VertexShader.SetConstantBuffer(1, _drawingSettingsBuffer);
            ctx.VertexShader.SetConstantBuffer(2, _msdfSettingsBuffer);

            ctx.PixelShader.SetConstantBuffer(2, _msdfSettingsBuffer);

            ctx.VertexShader.SetShaderResource(0, StateBuffers.LabelSRV);
            ctx.VertexShader.SetShaderResource(1, StateBuffers.PointSRV);
            ctx.VertexShader.SetShaderResource(2, StateBuffers.GroupSRV);

            ctx.PixelShader.SetShaderResource(2, StateBuffers.GroupSRV);
            ctx.PixelShader.SetShaderResource(3, ResCache.CogoPointMsdfAtlas.ShaderResourceView);

            var quadBinding = new VertexBufferBinding(_msdfQuadBuffer, Utilities.SizeOf<MsdfVertex>(), 0);
            var instanceBinding = new VertexBufferBinding(_msdfInstanceBuffer.Buffer, _msdfInstanceBuffer.Stride, 0);

            ctx.InputAssembler.SetVertexBuffers(0, quadBinding, instanceBinding);

            ctx.DrawInstanced(6, _msdfInstanceCount, 0, 0);
        }
        private void DrawMsdfGlowGlyphs(DeviceContext ctx)
        {
            if (_msdfInstanceCount == 0) { return; }

            ctx.VertexShader.Set(_msdfGlowVS);
            ctx.PixelShader.Set(_msdfGlowPS);

            ctx.PixelShader.SetSampler(0, _msdfSampler);

            ctx.InputAssembler.InputLayout = _msdfLayout;
            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.VertexShader.SetConstantBuffer(1, _drawingSettingsBuffer);
            ctx.VertexShader.SetConstantBuffer(2, _msdfSettingsBuffer);

            ctx.PixelShader.SetConstantBuffer(1, _drawingSettingsBuffer);
            ctx.PixelShader.SetConstantBuffer(2, _msdfSettingsBuffer);

            ctx.VertexShader.SetShaderResource(0, StateBuffers.LabelSRV);
            ctx.VertexShader.SetShaderResource(1, StateBuffers.PointSRV);
            ctx.VertexShader.SetShaderResource(2, StateBuffers.GroupSRV);

            ctx.PixelShader.SetShaderResource(2, StateBuffers.GroupSRV);
            ctx.PixelShader.SetShaderResource(3, ResCache.CogoPointMsdfAtlas.ShaderResourceView);

            var quadBinding = new VertexBufferBinding(_msdfQuadBuffer, Utilities.SizeOf<MsdfVertex>(), 0);
            var instanceBinding = new VertexBufferBinding(_msdfInstanceBuffer.Buffer, _msdfInstanceBuffer.Stride, 0);

            ctx.InputAssembler.SetVertexBuffers(0, quadBinding, instanceBinding);

            ctx.DrawInstanced(6, _msdfInstanceCount, 0, 0);
        }
        private void DrawPointCircles(DeviceContext ctx)
        {
            ctx.VertexShader.Set(_pointMarkerVS);
            ctx.GeometryShader.Set(_pointMarkerGS);
            ctx.PixelShader.Set(_pointMarkerPS);
            ctx.InputAssembler.InputLayout = _pointMarkerInputLayout;

            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.GeometryShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.GeometryShader.SetConstantBuffer(1, _drawingSettingsBuffer);

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

            ctx.VertexShader.SetConstantBuffer(1, _toggleSettingsBuffer);
            ctx.PixelShader.SetConstantBuffer(1, _toggleSettingsBuffer);

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

            ctx.GeometryShader.Set(null);
            ctx.InputAssembler.InputLayout = _leaderLineInputLayout;
            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            var quadBinding = new VertexBufferBinding(_leaderLineQuadBuffer, Utilities.SizeOf<LineCornerVertex>(), 0);
            var instanceBinding = new VertexBufferBinding(_leaderLineBuffer.Buffer, _leaderLineBuffer.Stride, 0);
            ctx.InputAssembler.SetVertexBuffers(0, quadBinding, instanceBinding);

            //--------------------------------------------
            // Glow
            //--------------------------------------------
            ctx.VertexShader.Set(_leaderLineGlowVS);
            ctx.PixelShader.Set(_leaderLineGlowPS);

            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.VertexShader.SetConstantBuffer(1, _leaderLineGlowSettings);
            ctx.VertexShader.SetShaderResource(0, StateBuffers.PointSRV);
            ctx.VertexShader.SetShaderResource(1, StateBuffers.GroupSRV);

            ctx.PixelShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.PixelShader.SetConstantBuffer(1, _leaderLineGlowSettings);
            ctx.PixelShader.SetShaderResource(0, StateBuffers.PointSRV);
            ctx.PixelShader.SetShaderResource(1, StateBuffers.GroupSRV);

            ctx.DrawInstanced(6, _leaderLineInstanceCount, 0, 0);

            //--------------------------------------------
            // Normal leader
            //--------------------------------------------
            ctx.VertexShader.Set(_leaderLineVS);
            ctx.PixelShader.Set(_leaderLinePS);

            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.VertexShader.SetConstantBuffer(1, _leaderLineSettings);
            ctx.VertexShader.SetShaderResource(0, StateBuffers.PointSRV);
            ctx.VertexShader.SetShaderResource(1, StateBuffers.GroupSRV);

            ctx.PixelShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.PixelShader.SetConstantBuffer(1, _leaderLineSettings);
            ctx.PixelShader.SetShaderResource(0, StateBuffers.PointSRV);
            ctx.PixelShader.SetShaderResource(1, StateBuffers.GroupSRV);

            ctx.DrawInstanced(6, _leaderLineInstanceCount, 0, 0);
        }
        private void DrawLeaderLinesGlow(DeviceContext ctx)
        {
            if (_leaderLineInstanceCount <= 0) { return; }

            ctx.GeometryShader.Set(null);
            ctx.InputAssembler.InputLayout = _leaderLineInputLayout;
            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            var quadBinding = new VertexBufferBinding(_leaderLineQuadBuffer, Utilities.SizeOf<LineCornerVertex>(), 0);
            var instanceBinding = new VertexBufferBinding(_leaderLineBuffer.Buffer, _leaderLineBuffer.Stride, 0);
            ctx.InputAssembler.SetVertexBuffers(0, quadBinding, instanceBinding);

            //--------------------------------------------
            // Glow
            //--------------------------------------------
            ctx.VertexShader.Set(_leaderLineGlowVS);
            ctx.PixelShader.Set(_leaderLineGlowPS);

            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.VertexShader.SetConstantBuffer(1, _leaderLineGlowSettings);
            ctx.VertexShader.SetShaderResource(0, StateBuffers.PointSRV);
            ctx.VertexShader.SetShaderResource(1, StateBuffers.GroupSRV);

            ctx.PixelShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.PixelShader.SetConstantBuffer(1, _leaderLineGlowSettings);
            ctx.PixelShader.SetShaderResource(0, StateBuffers.PointSRV);
            ctx.PixelShader.SetShaderResource(1, StateBuffers.GroupSRV);

            ctx.DrawInstanced(6, _leaderLineInstanceCount, 0, 0);
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
            if (_hoverCircleVertices.Count > 0)
            {
                ctx.VertexShader.Set(_hoverCircleVertexShader);
                ctx.GeometryShader.Set(_hoverCircleGeometryShader);
                ctx.PixelShader.Set(_hoverCirclePixelShader);
                ctx.InputAssembler.InputLayout = _hoverCircleLayout;
                ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
                ctx.GeometryShader.SetConstantBuffer(0, _transformationBuffer);
                ctx.GeometryShader.SetConstantBuffer(1, _drawingSettingsBuffer);
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
        private void DrawCachedPan(DeviceContext ctx)
        {
            if (!_panCacheValid || _panCacheSrv is null || _panCacheSrv.IsDisposed)
            {
                return;
            }

            Vector2 deltaPixels = _panCurrentMousePos - _panStartMousePos;
            float offsetU = -deltaPixels.X / _panCacheWidth;
            float offsetV = -deltaPixels.Y / _panCacheHeight;

            var settings = new PanSettings
            {
                OffsetUv = new Vector2(offsetU, offsetV),
                Padding = Vector2.Zero
            };

            ctx.UpdateSubresource(ref settings, _panSettingsBuffer);
            ctx.OutputMerger.SetRenderTargets(ResCache.RenderTargetView);
            ctx.ClearRenderTargetView(ResCache.RenderTargetView, new RawColor4(1, 1, 1, 1));

            ctx.VertexShader.Set(_panVertexShader);
            ctx.GeometryShader.Set(null);
            ctx.PixelShader.Set(_panPixelShader);
            ctx.InputAssembler.InputLayout = _panInputLayout;
            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
            ctx.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_panVertexBuffer, Utilities.SizeOf<PanVertex>(), 0));

            ctx.VertexShader.SetConstantBuffer(0, _panSettingsBuffer);

            ctx.PixelShader.SetShaderResource(0, _panCacheSrv);
            ctx.PixelShader.SetSampler(0, _panSampler);

            ctx.Draw(4, 0);

            ctx.PixelShader.SetShaderResource(0, null);
        }

        private void UpdateLineVertices()
        {
            if (_lineInstanceBuffer is null || CadManager is null) { return; }

            var context = ResCache.DeviceContext;
            var vertexSpan = CadManager.UpdateLineVerticesList(ResCache, SceneIdMap, StateBuffers);

            StateBuffers.EnsureObjectCapacity(SceneIdMap.ObjectCount);
            _lineInstanceBuffer.Update(context, vertexSpan);
            _lineInstanceCount = vertexSpan.Length;

            StateBuffers.FlushAll();
            _lineVerticesDirty = false;
            _baseSceneDirty = true;
        }
        private void UpdateLineGlowInstances()
        {
            if (_lineGlowInstanceBuffer is null)
            {
                _lineGlowVerticesDirty = false;
                return;
            }

            int estimatedCount = 0;

            foreach (var obj in _mouseOverHitTestableObjects)
            {
                if (obj is DrawingGeometry geometry)
                {
                    estimatedCount += geometry.LineInstances.Count();
                }
            }

            if (estimatedCount == 0)
            {
                _lineGlowInstanceCount = 0;
                _lineGlowVerticesDirty = false;
                _interactionDirty = true;
                return;
            }

            var instances = new List<LineInstance>(estimatedCount);

            foreach (var obj in _mouseOverHitTestableObjects)
            {
                if (obj is not DrawingGeometry geometry) { continue; }

                instances.AddRange(geometry.LineInstances);
            }

            _lineGlowInstanceBuffer.Update(ResCache.DeviceContext, CollectionsMarshal.AsSpan(instances));

            _lineGlowInstanceCount = instances.Count;

            _lineGlowVerticesDirty = false;
            _interactionDirty = true;
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
            _baseSceneDirty = true;
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
            _baseSceneDirty = true;
        }
        private void UpdateMsdfInstances()
        {
            _msdfInstances.Clear();

            foreach (var pointGroup in PointGroups)
            {
                if (pointGroup is null || !pointGroup.IsVisible)
                {
                    continue;
                }

                foreach (var point in pointGroup.Points)
                {
                    if (point is null)
                    {
                        continue;
                    }

                    AddCogoPoint(point, _msdfInstances);
                }
            }

            CadManager.UpdateCogoPointTree();
            StateBuffers.FlushAll();

            _msdfInstanceBuffer.Update(ResCache.DeviceContext, CollectionsMarshal.AsSpan(_msdfInstances));
            _msdfInstanceCount = _msdfInstances.Count;

            _cogoTextVerticesDirty = false;
        }
        private void UpdateMsdfGlowInstances()
        {
            //_msdfGlowInstances.Clear();

            //foreach (var point in _mouseOverCogoPoints)
            //{
            //    if (point == null) { continue; }

            //    if (!point.PointGroup.IsVisible) { continue; }

            //    AddCogoPoint(point, _msdfGlowInstances);
            //}

            //_msdfGlowInstanceBuffer.Update(ResCache.DeviceContext, CollectionsMarshal.AsSpan(_msdfGlowInstances));

            //_msdfGlowInstanceCount = _msdfGlowInstances.Count;

            //_msdfGlowVerticesDirty = false;
            //_interactionDirty = true;
        }
        private void UpdatePointCircleVertices()
        {
            if (_pointCircleVertexBuffer is null) { return; }

            var context = ResCache.DeviceContext;
            var vertexSpan = CadManager.UpdatePointCircleVerticesList(StateController);
            _pointCircleVertexBuffer.Update(context, vertexSpan);
            _pointCircleVertexCount = vertexSpan.Length;

            StateBuffers.FlushAll();
            _pointCircleVerticesDirty = false;
            _baseSceneDirty = true;
        }
        private void UpdateDragOverlayVertices(Rect r)
        {
            if (r.IsEmpty || r.Width <= 0 || r.Height <= 0 || !IsDragging)
            {
                _dragFillVertexCount = 0;
                _dragOverlayDirty = false;
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

            _dragOverlayDirty = false;
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

                var radiusFeathering = new Vector2(1f * wupp, 1f * wupp);
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
                CircleHoverVertex circleHoverVertex = new(
                    cp.Position.ToSharpDXVector3(),
                    GlobalHelperProperties.CogoPointCirclePixelRadius * cp.PointGroup.PointScale.ToFloat());
                _hoverCircleVertices.Add(circleHoverVertex);
            }

            _hoverCircleBuffer.Update(ctx, _hoverCircleVertices.ToArray());

            _cogoHoverVerticesDirty = false;
            _interactionDirty = true;
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
            _interactionDirty = true;
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

                    list.Add(new LeaderLineInstance
                    {
                        PointId = pid
                    });
                }
            }

            StateBuffers.FlushAll();

            _leaderLineInstanceCount = list.Count;
            _leaderLineBuffer.Update(ResCache.DeviceContext, CollectionsMarshal.AsSpan(list));

            _leaderLineVerticesDirty = false;
            _interactionDirty = true;
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
            _interactionDirty = true;
        }

        private void InitializeLineShaders()
        {
            var device = ResCache.Device;

            var path = AppDomain.CurrentDomain.BaseDirectory;
            while (Path.GetFileName(path) != "Cad_Point_Manager")
            {
                path = Path.GetDirectoryName(path) ?? throw new DirectoryNotFoundException("The 'Cad_Point_Manager' directory could not be found in the path.");
            }

            string shaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\LineShader.hlsl");
            string glowShaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\LineGlowShader.hlsl");

            // Main shaders
            var lineVSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "VSMain", "vs_5_0");
            _lineVertexShader = new VertexShader(device, lineVSBytecode);

            var linePSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "PSMain", "ps_5_0");
            _linePixelShader = new PixelShader(device, linePSBytecode);

            _lineInstanceInputLayout = new InputLayout(device, ShaderSignature.GetInputSignature(lineVSBytecode),
                new[]
                {
                    new InputElement("LOCAL", 0, Format.R32G32_Float, 0, 0, InputClassification.PerVertexData, 0),

                    new InputElement("START", 0, Format.R32G32_Float, 0, 1,InputClassification.PerInstanceData, 1),
                    new InputElement("END", 0, Format.R32G32_Float, 8, 1,InputClassification.PerInstanceData, 1),
                    new InputElement("LAYERID", 0, Format.R32_UInt, 16, 1,InputClassification.PerInstanceData, 1),
                    new InputElement("OBJECTID", 0, Format.R32_UInt, 20, 1,InputClassification.PerInstanceData, 1),
                    new InputElement("STARTDISTANCE",0,Format.R32_Float,24,1,InputClassification.PerInstanceData,1),
                    new InputElement("FLAGS",0,Format.R32_UInt,28,1,InputClassification.PerInstanceData,1),
                    new InputElement("PARENTSEGMENTLENGTH",0,Format.R32_Float,32,1,InputClassification.PerInstanceData,1)
                });

            LineCornerVertex[] quad = { new(-1, 0), new(1, 0), new(1, 1), new(-1, 0), new(1, 1), new(-1, 1) };

            _lineQuadBuffer = Buffer.Create(device, BindFlags.VertexBuffer, quad);

            _lineShadersLoaded = true;
        }
        private void InitializeLineGlowShaders()
        {
            var device = ResCache.Device;
            var path = AppDomain.CurrentDomain.BaseDirectory;

            while (Path.GetFileName(path) != "Cad_Point_Manager")
            {
                path = Path.GetDirectoryName(path) ?? throw new DirectoryNotFoundException(
                        "The 'Cad_Point_Manager' directory could not be found.");
            }

            // Load Line Glow Shaders
            string shaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\LineGlowShader.hlsl");
            var vsBytecode = ShaderBytecode.CompileFromFile(shaderPath, "VSMain", "vs_5_0");
            var psBytecode = ShaderBytecode.CompileFromFile(shaderPath, "PSMain", "ps_5_0");

            _lineGlowVertexShader = new VertexShader(device, vsBytecode);
            _lineGlowPixelShader = new PixelShader(device, psBytecode);

            // Load Composite Shaders
            string compositeShaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\GlowCompositeShader.hlsl");
            var compositeVsBytecode = ShaderBytecode.CompileFromFile(compositeShaderPath, "VSMain", "vs_5_0");
            var compositePsBytecode = ShaderBytecode.CompileFromFile(compositeShaderPath, "PSMain", "ps_5_0");

            _lineGlowCompositeVS = new VertexShader(device, compositeVsBytecode);
            _lineGlowCompositePS = new PixelShader(device, compositePsBytecode);

            _lineGlowCompositeLayout = new InputLayout(device, ShaderSignature.GetInputSignature(compositeVsBytecode), new[]
            {
                new InputElement("POSITION",0,Format.R32G32_Float,0,0),
                new InputElement("TEXCOORD",0,Format.R32G32_Float,8,0)});

            GlowCompositeVertex[] compositeVertices =
            {
                    new(new Vector2(-1, -1), new Vector2(0, 1)),
                    new(new Vector2(-1,  1), new Vector2(0, 0)),
                    new(new Vector2( 1,  1), new Vector2(1, 0)),

                    new(new Vector2(-1, -1), new Vector2(0, 1)),
                    new(new Vector2( 1,  1), new Vector2(1, 0)),
                    new(new Vector2( 1, -1), new Vector2(1, 1))
                };

            _lineGlowCompositeVertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, compositeVertices);

            _lineGlowCompositeSampler = new SamplerState(device, new SamplerStateDescription
            {
                Filter = Filter.MinMagMipPoint,
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                ComparisonFunction = Comparison.Never,
                MinimumLod = 0,
                MaximumLod = float.MaxValue
            });

            _lineGlowShadersLoaded = true;
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
        private void InitializeMsdfShaders()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;

            while (Path.GetFileName(path) != "Cad_Point_Manager")
            {
                path = Path.GetDirectoryName(path) ?? throw new DirectoryNotFoundException("The 'Cad_Point_Manager' directory could not be found.");
            }

            string shaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\MsdfShader.hlsl");
            var vsBytecode = ShaderBytecode.CompileFromFile(shaderPath, "VSMain", "vs_5_0");
            var psBytecode = ShaderBytecode.CompileFromFile(shaderPath, "PSMain", "ps_5_0");

            _msdfVS = new VertexShader(ResCache.Device, vsBytecode);
            _msdfPS = new PixelShader(ResCache.Device, psBytecode);

            _msdfLayout = new InputLayout(ResCache.Device, ShaderSignature.GetInputSignature(vsBytecode),
                new[]
                {
                    new InputElement("POSITION",0,Format.R32G32_Float,0,0,InputClassification.PerVertexData,0),
                    new InputElement("EM_TO_WORLD", 0, Format.R32_Float,0,1,InputClassification.PerInstanceData,1),
                    new InputElement("PEN_X",0,Format.R32_Float,4,1,InputClassification.PerInstanceData,1),
                    new InputElement("YSIGN",0,Format.R32_Float,8,1,InputClassification.PerInstanceData,1),
                    new InputElement("LABEL_ID",0,Format.R32_UInt,12,1,InputClassification.PerInstanceData,1),
                    new InputElement("POINT_ID",0,Format.R32_UInt,16,1,InputClassification.PerInstanceData,1),
                    new InputElement("PLANE_ORIGIN",0,Format.R32G32_Float,20,1,InputClassification.PerInstanceData,1),
                    new InputElement("PLANE_SIZE",0,Format.R32G32_Float,28,1,InputClassification.PerInstanceData,1),
                    new InputElement("UV_ORIGIN",0,Format.R32G32_Float,36,1,InputClassification.PerInstanceData,1),
                    new InputElement("UV_SIZE",0,Format.R32G32_Float,44,1,InputClassification.PerInstanceData,1),
                });

            MsdfVertex[] quad =
            {
                new(-0.5f,-0.5f),
                new( 0.5f,-0.5f),
                new( 0.5f, 0.5f),

                new(-0.5f,-0.5f),
                new( 0.5f, 0.5f),
                new(-0.5f, 0.5f)
            };

            _msdfQuadBuffer = Buffer.Create(ResCache.Device, BindFlags.VertexBuffer, quad);
            _msdfInstanceBuffer = new ResizableBuffer<MsdfGlyphInstance>(ResCache.Device, 1024);

            _msdfSampler = new SamplerState(ResCache.Device, new SamplerStateDescription
            {
                Filter = Filter.MinMagLinearMipPoint,
                AddressU = TextureAddressMode.Border,
                AddressV = TextureAddressMode.Border,
                AddressW = TextureAddressMode.Border,
                ComparisonFunction = Comparison.Never,
                MinimumLod = 0,
                MaximumLod = float.MaxValue,
                BorderColor = new RawColor4(0.5f, 0.5f, 0.5f, 0.5f)
            });

            // Glow msdf shaders
            string glowShaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\MsdfGlowShader.hlsl");
            var glowVsBytecode = ShaderBytecode.CompileFromFile(glowShaderPath, "VSMain", "vs_5_0");
            var glowPsBytecode = ShaderBytecode.CompileFromFile(glowShaderPath, "PSMain", "ps_5_0");

            var glowReflection = new ShaderReflection(glowVsBytecode);

            _msdfGlowVS = new VertexShader(ResCache.Device, glowVsBytecode);
            _msdfGlowPS = new PixelShader(ResCache.Device, glowPsBytecode);

            _msdfShadersLoaded = true;
        }
        private void InitializePointMarkerShaders()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            while (Path.GetFileName(path) != "Cad_Point_Manager")
            {
                path = Path.GetDirectoryName(path);
                if (path == null) { throw new DirectoryNotFoundException("The 'Cad_Point_Manager' directory could not be found in the path."); }
            }

            string pointMarkerShaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\PointMarkerShader.hlsl");
            var pointMarkerVsb = ShaderBytecode.CompileFromFile(pointMarkerShaderPath, "VSMain", "vs_5_0");
            var pointMarkerPsb = ShaderBytecode.CompileFromFile(pointMarkerShaderPath, "PSMain", "ps_5_0");
            var pointMarkerGsb = ShaderBytecode.CompileFromFile(pointMarkerShaderPath, "GSMain", "gs_5_0");
            _pointMarkerVS = new VertexShader(ResCache.Device, pointMarkerVsb);
            _pointMarkerPS = new PixelShader(ResCache.Device, pointMarkerPsb);
            _pointMarkerGS = new GeometryShader(ResCache.Device, pointMarkerGsb);
            _pointMarkerInputLayout = new InputLayout(ResCache.Device, ShaderSignature.GetInputSignature(pointMarkerVsb),
                new[]
                {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                    new InputElement("RADIUS",   0, Format.R32_Float,       12, 0),
                    new InputElement("LABEL_ID", 0, Format.R32_UInt,        16, 0),
                    new InputElement("POINT_ID", 0, Format.R32_UInt,        20, 0),
                });

            _pointMarkerShadersLoaded = true;
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
                    new InputElement("TEXCOORD", 0, Format.R32_Float, 12, 0),
                    new InputElement("TEXCOORD", 1, Format.R32_Float, 16, 0),
                });

            _cogoHoverShadersLoaded = true;
        }
        private void InitializeOverlayShaders()
        {
            // Fill
            var path = AppDomain.CurrentDomain.BaseDirectory;
            while (Path.GetFileName(path) != "Cad_Point_Manager")
            {
                path = Path.GetDirectoryName(path) ?? throw new DirectoryNotFoundException("Cad_Point_Manager not found");
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
            var device = ResCache.Device;
            var path = AppDomain.CurrentDomain.BaseDirectory;

            while (Path.GetFileName(path) != "Cad_Point_Manager")
            {
                path = Path.GetDirectoryName(path);

                if (path == null)
                {
                    throw new DirectoryNotFoundException("The 'Cad_Point_Manager' directory could not be found in the path.");
                }
            }

            //--------------------------------------------
            // Normal leader shader
            //--------------------------------------------

            string lineShaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\LeaderLineShader.hlsl");
            var lineVSBytecode = ShaderBytecode.CompileFromFile(lineShaderPath, "VSMain", "vs_5_0");
            var linePSBytecode = ShaderBytecode.CompileFromFile(lineShaderPath, "PSMain", "ps_5_0");

            _leaderLineVS = new VertexShader(device, lineVSBytecode);
            _leaderLinePS = new PixelShader(device, linePSBytecode);

            //--------------------------------------------
            // Input layout
            //--------------------------------------------

            _leaderLineInputLayout = new InputLayout(device, ShaderSignature.GetInputSignature(lineVSBytecode), new[]
            {
                // Stream 0 - static quad
                new InputElement("LOCAL",0,Format.R32G32_Float,0,0,InputClassification.PerVertexData,0),

                // Stream 1 - leader instance
                new InputElement("POINT_ID",0,Format.R32_UInt,0,1,InputClassification.PerInstanceData,1)});

            //--------------------------------------------
            // Static line quad
            //--------------------------------------------

            LineCornerVertex[] quad =
            {
                new(-1, 0),
                new( 1, 0),
                new( 1, 1),

                new(-1, 0),
                new( 1, 1),
                new(-1, 1)
            };

            _leaderLineQuadBuffer?.Dispose();
            _leaderLineQuadBuffer = Buffer.Create(device, BindFlags.VertexBuffer, quad);

            //--------------------------------------------
            // Glow shader
            //--------------------------------------------

            string glowShaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\LeaderLineGlowShader.hlsl");
            var glowVSBytecode = ShaderBytecode.CompileFromFile(glowShaderPath, "VSMain", "vs_5_0");

            var glowPSBytecode = ShaderBytecode.CompileFromFile(glowShaderPath, "PSMain", "ps_5_0");

            _leaderLineGlowVS = new VertexShader(device, glowVSBytecode);
            _leaderLineGlowPS = new PixelShader(device, glowPSBytecode);

            //--------------------------------------------

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
        private void InitializePanShaders()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;

            while (Path.GetFileName(path) != "Cad_Point_Manager")
            {
                path =
                    Path.GetDirectoryName(path) ?? throw new DirectoryNotFoundException("The 'Cad_Point_Manager' directory could not be found.");
            }

            string shaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\PanShader.hlsl");

            var vsBytecode = ShaderBytecode.CompileFromFile(shaderPath, "VSMain", "vs_5_0");
            var psBytecode = ShaderBytecode.CompileFromFile(shaderPath, "PSMain", "ps_5_0");

            _panVertexShader = new VertexShader(ResCache.Device, vsBytecode);
            _panPixelShader = new PixelShader(ResCache.Device, psBytecode);

            _panInputLayout = new InputLayout(ResCache.Device, ShaderSignature.GetInputSignature(vsBytecode),
                new[]
                {
                    new InputElement("POSITION",0,Format.R32G32_Float,0,0),
                    new InputElement("TEXCOORD",0,Format.R32G32_Float,8,0)
                });

            var vertices = new[]
            {
                new PanVertex(new Vector2(-1f,  1f),new Vector2(0f, 0f)),
                new PanVertex(new Vector2( 1f,  1f),new Vector2(1f, 0f)),
                new PanVertex(new Vector2(-1f, -1f),new Vector2(0f, 1f)),
                new PanVertex(new Vector2( 1f, -1f),new Vector2(1f, 1f))
            };

            _panVertexBuffer = Buffer.Create(ResCache.Device, BindFlags.VertexBuffer, vertices);
            _panSettingsBuffer = new Buffer(
                ResCache.Device, Utilities.SizeOf<PanSettings>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _panSampler = new SamplerState(ResCache.Device, new SamplerStateDescription
            {
                Filter = Filter.MinMagMipPoint,
                AddressU = TextureAddressMode.Border,
                AddressV = TextureAddressMode.Border,
                AddressW = TextureAddressMode.Border,
                BorderColor = new RawColor4(1, 1, 1, 1),
                ComparisonFunction = Comparison.Never,
                MinimumLod = 0,
                MaximumLod = float.MaxValue
            });

            _panShadersLoaded = true;
        }

        private void InitializeBuffers()
        {
            var device = ResCache.Device;

            _lineInstanceBuffer?.Dispose();
            _lineInstanceBuffer = new ResizableBuffer<LineInstance>(device, GlobalHelperProperties.InitialLineVertices / 2);

            _lineGlowInstanceBuffer?.Dispose();
            _lineGlowInstanceBuffer = new ResizableBuffer<LineInstance>(device, 256);

            _textVertexBuffer?.Dispose();
            _textVertexBuffer = new(device, GlobalHelperProperties.InitialTextVertices);

            _solidVertexBuffer?.Dispose();
            _solidVertexBuffer = new(device, GlobalHelperProperties.InitialLineVertices);

            _hoverCircleBuffer?.Dispose();
            _hoverCircleBuffer = new(device, 16);

            _dragFillBuffer?.Dispose();
            _dragFillBuffer = new(device, 6);

            SceneIdMap ??= new();
            StateBuffers?.Dispose();
            StateBuffers = new(device, device.ImmediateContext);
            StateController = new(SceneIdMap, StateBuffers);

            _pointCircleVertexBuffer?.Dispose();
            _pointCircleVertexBuffer = new(device, GlobalHelperProperties.InitialCircleVertices);

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

            var drawingSettingsBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<DrawingSettingsBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _drawingSettingsBuffer = new Buffer(ResCache.Device, drawingSettingsBufferDesc);

            var lineRenderModeBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Dynamic,
                SizeInBytes = Utilities.SizeOf<LineRenderModeBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None
            };
            _lineRenderModeBuffer = new Buffer(ResCache.Device, lineRenderModeBufferDesc);

            var msdfBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<MsdfSettingsBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _msdfSettingsBuffer = new Buffer(ResCache.Device, msdfBufferDesc);

            var pointTextBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<GlyphSettingsBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _cogoPointTextSettingsBuffer = new Buffer(ResCache.Device, pointTextBufferDesc);

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
            //UpdateDrawingSettingsBuffer(Viewport.Width, Viewport.Height);
            UpdateDrawingSettingsBuffer(RenderPixelWidth, RenderPixelHeight);

            var msdfSettings = new MsdfSettingsBuffer
            {
                AtlasHeight = ResCache.CogoPointMsdfAtlas.Height,
                AtlasWidth = ResCache.CogoPointMsdfAtlas.Width,
                DistanceRange = ResCache.CogoPointMsdfAtlas.DistanceRange,
                CameraZoom = CadManager.Camera.CurrentZoom
            };
            ResCache.DeviceContext.UpdateSubresource(ref msdfSettings, _msdfSettingsBuffer);

            var cogoPointTextSettings = new GlyphSettingsBuffer
            {
                SelectedColor = GlobalHelperProperties.SelectedObjectColor,
            };
            ResCache.DeviceContext.UpdateSubresource(ref cogoPointTextSettings, _cogoPointTextSettingsBuffer);

            var leaderLineSettings = new LeaderLineSettings
            {
                ViewportSize = new Vector2(RenderPixelWidth, RenderPixelHeight),
                PixelThickness = GlobalHelperProperties.CogoPointLeaderLinePixelWidth,
                SelectedColor = GlobalHelperProperties.SelectedObjectColor
            };
            ResCache.DeviceContext.UpdateSubresource(ref leaderLineSettings, _leaderLineSettings);

            var leaderLineGlowSettings = new LeaderLineGlowSettings
            {
                ViewportSize = new Vector2(RenderPixelWidth, RenderPixelHeight),
                PixelThickness = GlobalHelperProperties.CogoPointLeaderLinePixelWidth,
                GlowPixelOffset = GlobalHelperProperties.GlowPixelOffset,
                HoverColor = GlobalHelperProperties.HoverColor
            };
            ResCache.DeviceContext.UpdateSubresource(ref leaderLineGlowSettings, _leaderLineGlowSettings);

            var sigPointSettings = new SignificantPointSettingsBuffer
            {
                Color = GlobalHelperProperties.SelectedSigPointColor,
                RadiusPx = GlobalHelperProperties.SignificantPointPixelRadius,
                ViewPortSize = new Vector2(Viewport.Width, Viewport.Height)
            };
            ResCache.DeviceContext.UpdateSubresource(ref sigPointSettings, _sigPointSettingsBuffer);

            ConstantBuffersDirty = false;
            CadManager.Camera.IsDirty = false;
            _baseSceneDirty = true;
            _interactionDirty = true;
        }
        private void UpdateTransformationBuffer()
        {
            var transformation = CadManager.Camera.ViewProjectionMatrix;
            var transformationBuffer = new TransformationBuffer
            {
                WorldViewProjection = transformation
            };
            ResCache.DeviceContext.UpdateSubresource(ref transformationBuffer, _transformationBuffer);

            // CogoPoint toggle button settings must also be updated when the transformation buffer is updated,
            // because the toggle button size is in world units and depends on the current zoom level.
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

            TransformationBufferDirty = false;
            CadManager.Camera.IsDirty = false;

            _baseSceneDirty = true;
        }
        private void UpdateDrawingSettingsBuffer(float viewportWidth, float viewportHeight)
        {
            var drawingSettings = new DrawingSettingsBuffer
            {
                ViewportSize = new Vector2(viewportWidth, viewportHeight),

                LineHalfWidthPixels = GlobalHelperProperties.CogoPointLeaderLinePixelWidth,
                GlobalLineTypeScale = CadManager.OverallDrawingLineTypeScale,
                AnnotationScale = 1,
                GlowPixelOffset = GlobalHelperProperties.GlowPixelOffset,
                SelectedColor = GlobalHelperProperties.SelectedObjectColor,
                SelectedMouseOverColor = GlobalHelperProperties.SelectedMouseOverObjectColor
            };

            ResCache.DeviceContext.UpdateSubresource(ref drawingSettings, _drawingSettingsBuffer);
        }

        private void EnsurePanCache()
        {
            int width = RenderPixelWidth * 2;
            int height = RenderPixelHeight * 2;

            if (_panCacheTexture is not null && !_panCacheTexture.IsDisposed &&
                _panCacheWidth == width && _panCacheHeight == height)
            {
                return;
            }

            _panCacheSrv?.Dispose();
            _panCacheRtv?.Dispose();
            _panCacheTexture?.Dispose();

            _panCacheWidth = width;
            _panCacheHeight = height;

            var description = new Texture2DDescription
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            _panCacheTexture = new Texture2D(ResCache.Device, description);
            _panCacheRtv = new RenderTargetView(ResCache.Device, _panCacheTexture);
            _panCacheSrv = new ShaderResourceView(ResCache.Device, _panCacheTexture);

            _panCacheValid = false;
        }
        private void BuildPanCache()
        {
            EnsurePanCache();

            if (_panCacheTexture is null ||
                _panCacheRtv is null)
            {
                return;
            }

            var ctx = ResCache.DeviceContext;
            var normalTransformation = CadManager.Camera.ViewProjectionMatrix;
            var panCacheTransformation = normalTransformation * Matrix.Scaling(0.5f, 0.5f, 1.0f);

            var transformationBuffer = new TransformationBuffer
            {
                WorldViewProjection = panCacheTransformation
            };

            ctx.UpdateSubresource(ref transformationBuffer, _transformationBuffer);
            ctx.Rasterizer.SetViewport(0, 0, _panCacheWidth, _panCacheHeight);

            UpdateDrawingSettingsBuffer(_panCacheWidth, _panCacheHeight);

            ctx.OutputMerger.SetRenderTargets(_panCacheRtv);
            ctx.ClearRenderTargetView(_panCacheRtv, new RawColor4(1, 1, 1, 1));

            ctx.OutputMerger.SetBlendState(ResCache.BaseBlendState);

            DrawLines(ctx);
            DrawText(ctx);
            DrawSolids(ctx);
            DrawPointCircles(ctx);
            DrawMsdfGlyphs(ctx);
            DrawLeaderLines(ctx);

            ctx.Rasterizer.SetViewport(0, 0, RenderPixelWidth, RenderPixelHeight);
            transformationBuffer = new TransformationBuffer { WorldViewProjection = normalTransformation };
            ctx.UpdateSubresource(ref transformationBuffer, _transformationBuffer);
            UpdateDrawingSettingsBuffer(RenderPixelWidth, RenderPixelHeight);

            _panCacheValid = true;
        }

        // Msdf
        private void AddCogoPoint(CogoPoint point, List<MsdfGlyphInstance> destination)
        {
            float emToWorld = (float)point.PointGroup.FontBaseSize;

            var ids = StateController.EnsurePointRegistered(point);

            var pointNumberLayout = LayoutMsdfString(
                point.PointNumber.ToString(),
                point,
                point.PointNumberOffset,
                emToWorld);

            point.PointNumberBounds = pointNumberLayout.Bounds;

            AddMsdfString(
                pointNumberLayout,
                ids.PointNumberLabelId,
                ids.PointId,
                emToWorld,
                destination);

            var elevationLayout = LayoutMsdfString(
                    point.Elevation.ToString("F3"),
                    point,
                    point.ElevationOffset,
                    emToWorld);

            point.ElevationBounds = elevationLayout.Bounds;

            AddMsdfString(
                elevationLayout,
                ids.ElevationLabelId,
                ids.PointId,
                emToWorld,
                destination);

            if (point.HasDescription)
            {
                var descriptionLayout = LayoutMsdfString(
                    point.Description.ToString(),
                    point,
                    point.DescriptionOffset,
                    emToWorld);

                point.DescriptionBounds = descriptionLayout.Bounds;

                AddMsdfString(
                    descriptionLayout,
                    ids.DescriptionLabelId,
                    ids.PointId,
                    emToWorld,
                    destination);
            }

            float rW = (float)(GlobalHelperProperties.CogoPointCirclePixelRadius * point.PointGroup.PointScale);
            var c = point.Position;
            point.EllipseBounds = new Rect(c.X - rW, c.Y - rW, 2 * rW, 2 * rW);

            UpdateToggleAnchorBounds(point);

            point.UpdateBounds();
        }
        private void AddMsdfString(MsdfTextLayout layout, uint labelId, uint pointId, float emToWorld, List<MsdfGlyphInstance> destination)
        {
            foreach (var placement in layout.Glyphs)
            {
                MsdfGlyphInstance instance = new()
                {
                    EmToWorld = emToWorld,
                    PenX = placement.PenX,
                    YSign = -1,
                    LabelId = labelId,
                    PointId = pointId,
                    PlaneOrigin = new Vector2(
                        placement.Glyph.PlaneMin.X,
                        placement.Glyph.PlaneMax.Y),
                    PlaneSize = placement.Glyph.PlaneSize,
                    UvOrigin = placement.Glyph.UvMin,
                    UvSize = placement.Glyph.UvMax - placement.Glyph.UvMin
                };

                destination.Add(instance);
            }
        }
        private void UpdateCogoPointBounds(CogoPoint p)
        {
            p.PointNumberBounds =
                 LayoutMsdfString(
                     p.PointNumber.ToString(),
                     p,
                     p.PointNumberOffset,
                     p.PointGroup.FontBaseSize.ToFloat()).Bounds;
            p.ElevationBounds =
                 LayoutMsdfString(
                     p.Elevation.ToString("F3"),
                     p,
                     p.ElevationOffset,
                     p.PointGroup.FontBaseSize.ToFloat()).Bounds;
            if (p.HasDescription)
            {
                p.DescriptionBounds =
                     LayoutMsdfString(
                         p.Description,
                         p,
                         p.DescriptionOffset,
                         p.PointGroup.FontBaseSize.ToFloat()).Bounds;
            }
            else { p.DescriptionBounds = Rect.Empty; }

            UpdateToggleAnchorBounds(p);

            p.UpdateBounds();
        }
        private MsdfTextLayout LayoutMsdfString(string text, CogoPoint point, Vector2 labelOffset, float emToWorld)
        {
            MsdfTextLayout layout = new();

            float scale = emToWorld * point.PointGroup.PointScale.ToFloat();

            var baseOffset = point.PointGroup.PointInfoBaseXoffset;
            if (point.IsFlippedY) { baseOffset *= -1; }
            Vector2 origin = new(
                point.Position.X.ToFloat() + labelOffset.X + baseOffset + point.TextInfoOffset.X,
                point.Position.Y.ToFloat() + (labelOffset.Y * point.PointGroup.PointScale.ToFloat() + point.TextInfoOffset.Y));

            if (string.IsNullOrEmpty(text)) { return layout; }

            var atlas = ResCache.CogoPointMsdfAtlas;
            float penX = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (!atlas.Glyphs.TryGetValue(c, out var glyph)) { continue; }

                float left = penX + glyph.PlaneMin.X;
                float right = penX + glyph.PlaneMax.X;

                var planeOrigin = new Vector2(
                        glyph.PlaneMin.X,
                        glyph.PlaneMax.Y);
                float y0 = planeOrigin.Y;
                float y1 = planeOrigin.Y + glyph.PlaneSize.Y;
                y0 *= -1;
                y1 *= -1;
                float top = Math.Min(y0, y1);
                float bottom = Math.Max(y0, y1);

                Rect glyphBounds = new(
                    origin.X + left * scale,
                    origin.Y + top * scale,
                    (right - left) * scale,
                    (bottom - top) * scale);

                layout.Glyphs.Add(new MsdfGlyphPlacement
                {
                    Glyph = glyph,
                    PenX = penX,
                    Bounds = glyphBounds
                });

                if (layout.Bounds.IsEmpty) { layout.Bounds = glyphBounds; }
                else { layout.Bounds = Rect.Union(layout.Bounds, glyphBounds); }

                penX += glyph.Advance;

                if (i + 1 < text.Length)
                {
                    uint key = ((uint)c << 16) | text[i + 1];

                    if (atlas.Kernings.TryGetValue(key, out float kern)) { penX += kern; }
                }
            }
            return layout;
        }

        private void SetLineRenderMode(DeviceContext ctx, bool selectedOnly, bool glowPass)
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

                    //ConstantBuffersDirty = true;
                    TransformationBufferDirty = true;
                }
            }
        }
        private void UpdateInitialMatrix()
        {
            if (CadManager is null || !CadManager.DxfLoaded || CadManager.Camera is null) { return; }

            CadManager.UpdateExtents();
            _dxfInitialMatrix = GetExtentsFittingMatrix(Viewport, CadManager.Extents);

            //ConstantBuffersDirty = true;
            TransformationBufferDirty = true;
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

        protected override void OnMouseMove(MouseEventArgs e)
        {
            _pointerCoords = e.GetPosition(this);
            var currentMousePos = new Vector2((float)_pointerCoords.X, (float)_pointerCoords.Y);

            if (_cogoPointTextBeingMoved)
            {
                var mousePx = GetMousePx(e);
                var w = CadManager.Camera.ScreenToWorld(mousePx);

                var delta = new Vector2(
                    w.X - _pressedToggleButtonPoint.Position.X.ToFloat(), w.Y - _pressedToggleButtonPoint.Position.Y.ToFloat());

                UpdateCogoPointInfoOffset(_pressedToggleButtonPoint, delta);

                e.Handled = true;
                return;
            }

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

            if (_isPanning && e.MiddleButton == MouseButtonState.Pressed)
            {
                _panCurrentMousePos = GetMousePx(e);
                CadManager.Camera.PanFromStart(
                    _panStartCameraTranslate, _panStartMousePos, _panCurrentMousePos, _panWorldUnitsPerPixel);

                e.Handled = true;
            }

            _prevMousePos = currentMousePos;
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
            //ConstantBuffersDirty = true;
            TransformationBufferDirty = true;

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
                    _interactionDirty = true;
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
                UpdateCogoPointBounds(_pressedToggleButtonPoint);
                EndCogoToggleButtonPress();
                CadManager.UpdateCogoPointTree();
                UpdateInitialMatrix();

                if (IsMouseCaptured) { ReleaseMouseCapture(); }
                e.Handled = true;

                _interactionDirty = true;

                return;
            }

            _suspendHitTesting = true;
            bool geometrySelectionChanged = false;
            bool cogoPointSelectionChanged = false;
            bool sigPointsSelectionChanged = false;

            switch (CadManager.SnapSelectionMode)
            {
                case SelectionMode.Geometries:
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
                        geometrySelectionChanged = true;

                        break;
                    }

                case SelectionMode.CogoPoints:
                    {
                        using (SelectedCogoPoints.DeferNotifications())
                        {
                            var newSel = new HashSet<CogoPoint>(_mouseOverCogoPoints);

                            foreach (var p in newSel)
                            {
                                if (IsShiftPressed)
                                {
                                    if (!p.IsSelected) { continue; }
                                    DeselectObject(p);
                                    SelectedCogoPoints.Remove(p);
                                    cogoPointSelectionChanged = true;
                                }
                                else
                                {
                                    if (p.IsSelected) { continue; }
                                    SelectObject(p);
                                    SelectedCogoPoints.Add(p);
                                    cogoPointSelectionChanged = true;
                                }
                            }

                            StateController.FlushPointUpdates();
                        }
                        break;
                    }

                case SelectionMode.Points:
                    {
                        if (SnappedHitTestablePoint is not null)
                        {
                            if (!SnappedHitTestablePoint.IsSelected)
                            {
                                SelectObject(SnappedHitTestablePoint);
                                sigPointsSelectionChanged = true;
                            }
                            else
                            {
                                DeselectObject(SnappedHitTestablePoint);
                                sigPointsSelectionChanged = true;
                            }
                        }
                        break;
                    }
            }

            ResetHoverObjects();

            if (sigPointsSelectionChanged)
            {
                _sigPointVerticesDirty = true;
            }
            if (geometrySelectionChanged)
            {
                StateController.FlushObjectUpdates();
                _lineVerticesDirty = true;
            }
            if (cogoPointSelectionChanged)
            {
                StateController.FlushPointUpdates();
                _cogoHoverVerticesDirty = _leaderLineVerticesDirty = true;
            }

            _suspendHitTesting = false;
        }
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            BeginDrag(e.GetPosition(this));
            UpdateDragRect();

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
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                BuildPanCache();

                _isPanning = true;

                _panStartMousePos = GetMousePx(e);
                _panCurrentMousePos = _panStartMousePos;
                _panStartCameraTranslate = CadManager.Camera.Translate;
                _panWorldUnitsPerPixel = CadManager.Camera.GetWorldUnitsPerPixel();
                _prevMousePos = _panStartMousePos;

                CaptureMouse();

                e.Handled = true;
                return;
            }

            base.OnMouseDown(e);
        }
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Released && e.ChangedButton == MouseButton.Middle)
            {
                _isPanning = false;

                if (IsMouseCaptured)
                {
                    ReleaseMouseCapture();
                }

                TransformationBufferDirty = true;

                e.Handled = true;
            }
        }
        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);

            _isPanning = false;
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ResetSelectedObjects();
                EndDrag();
                _baseSceneDirty = true;
                _interactionDirty = true;
            }
            if (e.Key == Key.Tab)
            {
                _currentSnapHitTestIndex += 1;

                e.Handled = true;
            }
            if (e.Key == Key.Delete)
            {
                if (CadManager.SnapSelectionMode == SelectionMode.CogoPoints &&
                    SelectedCogoPoints.Count > 0)
                {
                    DeleteCogoPoints(SelectedCogoPoints.ToList());
                    CompactStateBuffersIfUnder25Pct();
                    ResetHoverObjects();

                    _cogoHoverVerticesDirty = true;
                    _cogoTextVerticesDirty = true;

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
                //ConstantBuffersDirty = true;
                TransformationBufferDirty = true;
            }
            //_dxfDirty = true;
            //_combinedDirty = true;
        }

        protected override void OnFrontBufferRestored()
        {
            _baseSceneDirty = true;
            _interactionDirty = true;
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

            _interactionDirty = true;
            _baseSceneDirty = true;
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
            //ConstantBuffersDirty = true;
            TransformationBufferDirty = true;
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

                _dragOverlayDirty = true;
                _interactionDirty = true;

                return;
            }

            double width = Math.Abs(_dragStart.X - DxfCoords.X);
            double height = Math.Abs(_dragStart.Y - DxfCoords.Y);

            double left = Math.Min(_dragStart.X, DxfCoords.X);
            double top = Math.Min(_dragStart.Y, DxfCoords.Y);

            DragRect = new(left, top, width, height);

            _dragOverlayDirty = true;
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

                if (_suspendHitTesting) { await Task.Delay(100); continue; }

                if (CadManager.DxfLoaded && CadManager.HitTestingEnabled)
                {
                    switch (CadManager.SnapSelectionMode)
                    {
                        case SelectionMode.Points:
                            RunPointsHitTest(_hitTestCancellationTokenSource.Token);
                            break;

                        case SelectionMode.Geometries:
                            if (IsDragging)
                            {
                                RunDragGeometriesHittest(_hitTestCancellationTokenSource.Token);
                            }
                            else { RunGeometriesHitTest(_hitTestCancellationTokenSource.Token); }
                            break;

                        case SelectionMode.CogoPoints:
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

            bool lineGlowVerticesDirty = false;

            if (_mouseOverHitTestableObjects is not null && _mouseOverHitTestableObjects.Count > 0)
            {
                foreach (var snappedObj in snappedObjectsCopy)
                {
                    if (snappedObj.DistanceToPoint(_lastHitTestCoords) > _hittestStrokeThickness)
                    {
                        ResetHoverObjects();
                        lineGlowVerticesDirty = true;

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
                            lineGlowVerticesDirty = true;
                            _lastSnapHitTestIndex = _currentSnapHitTestIndex;
                        }
                    }
                }
            }

            if (lineGlowVerticesDirty)
            {
                StateController.FlushObjectUpdates();
                _lineGlowVerticesDirty = true;
            }
        }
        private void RunCogoPointsHitTest(CancellationToken token)
        {
            if (token.IsCancellationRequested) { token.ThrowIfCancellationRequested(); }
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(() => RunCogoPointsHitTest(token));
                return;
            }
            if (!CadManager.DxfLoaded) { return; }

            _lastHitTestCoords = new(DxfCoords.X, DxfCoords.Y);

            var snappedCogoPointsCopy = _mouseOverCogoPoints.ToList();
            bool cogoMouseOverChanged = false;

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
                    cogoMouseOverChanged = true;
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

                        return;
                    }

                    if (snappedCogoPoint.DistanceToPoint(_lastHitTestCoords) > _hittestStrokeThickness)
                    {
                        ResetHoverObjects();
                        ResetCogoToggleButtonMouseOver();
                        cogoMouseOverChanged = true;

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
                                    cogoMouseOverChanged = true;
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
                                _cogoHoverVerticesDirty = true;

                                return;
                            }

                            _mouseOverCogoPoints.Add(point);
                            HoverObject(point);
                            cogoMouseOverChanged = true;
                            _lastSnapHitTestIndex = _currentSnapHitTestIndex;
                        }
                    }
                }
            }

            if (cogoMouseOverChanged)
            {
                StateController.FlushPointUpdates();
                _cogoHoverVerticesDirty = _leader = true;
            }
        }
        private async void RunDragCogoPointsHittest(CancellationToken token)
        {
            if (token.IsCancellationRequested) { return; }
            if (!CadManager.DxfLoaded) { return; }

            Rect currentRect = await Dispatcher.InvokeAsync(() => DragRect, DispatcherPriority.Render);
            if (currentRect.IsEmpty || currentRect.Width <= 0 || currentRect.Height <= 0) { return; }

            var newSet = CadManager
                .HitTestDragCogoPoints(currentRect)
                .Where(p => currentRect.Contains(p.Bounds))
                .ToHashSet();

            List<CogoPoint> adds, removes;

            lock (_dragCogoLock)
            {
                adds = newSet.Except(_dragCogoCurrent).ToList();
                removes = _dragCogoCurrent.Except(newSet).ToList();
                _dragCogoCurrent = newSet; // update snapshot
            }

            foreach (var p in adds)
            {
                HoverObject(p);
                _mouseOverCogoPoints.Add(p);
            }
            foreach (var p in removes)
            {
                DehoverObject(p);
                _mouseOverCogoPoints.Remove(p);
            }

            if (adds.Count > 0 || removes.Count > 0)
            {
                StateController.FlushPointUpdates();
                _cogoHoverVerticesDirty = true;
            }
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

            bool lineGlowVerticesDirty = false;

            foreach (var region in addedRegions)
            {
                var newHits = CadManager.HitTestDragGeometries(region).Distinct();

                foreach (var geometry in newHits)
                {
                    if (DragRect.Contains(geometry.Bounds))
                    {
                        _mouseOverHitTestableObjects.Add(geometry);
                        HoverObject(geometry);
                        lineGlowVerticesDirty = true;
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
                        lineGlowVerticesDirty = true;
                    }
                }
            }
            _lastQueriedDxfRect = DragRect;

            if (lineGlowVerticesDirty)
            {
                StateController.FlushObjectUpdates();
                _lineGlowVerticesDirty = true;
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
            _baseSceneDirty = _interactionDirty = true;
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
        }

        public void InvalidateCogoPointRendering()
        {
            _cogoTextVerticesDirty = true;
            _pointCircleVerticesDirty = true;
            _leaderLineVerticesDirty = true;
            _anchorVerticesDirty = true;
            _cogoHoverVerticesDirty = true;
            _interactionDirty = true;
            _baseSceneDirty = true;
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

            StateController.FlushObjectUpdates();

            _lineVerticesDirty = _textVerticesDirty = _interactionDirty = true;
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
                    _cogoTextVerticesDirty = true;
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

                    StateController.EnsurePointRegistered(cp);
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
            //if (e.Action == NotifyCollectionChangedAction.Reset)
            //{
            //    foreach (var cp in CadManager.CogoPoints)
            //    {
            //        cp.PropertyChanged -= CogoPoint_PropertyChanged;
            //        cp.PropertyChanged += CogoPoint_PropertyChanged;

            //        uint pId = SceneIdMap.GetOrAddPointId(cp, out var isNewPoint);
            //        uint gId = SceneIdMap.GetOrAddGroupId(cp.PointGroup, out var isNewGroup);

            //        if (isNewGroup)
            //        { StateBuffers.InitializeGroupState(SceneIdMap.MaxGroupId, cp.PointGroup, gId); }

            //        if (isNewPoint)
            //        { StateBuffers.InitializePointState(SceneIdMap.MaxPointId, cp, pId, gId); }

            //        uint idPN = SceneIdMap.GetOrAddLabelId(cp, 0, out var isNew);
            //        if (isNew)
            //        { StateBuffers.InitializeLabelState(SceneIdMap.MaxLabelCount, cp.PointNumberOffset, idPN); }

            //        uint idElev = SceneIdMap.GetOrAddLabelId(cp, 1, out isNew);
            //        if (isNew)
            //        { StateBuffers.InitializeLabelState(SceneIdMap.MaxLabelCount, cp.ElevationOffset, idElev); }

            //        uint idDesc = SceneIdMap.GetOrAddLabelId(cp, 2, out isNew);
            //        if (isNew)
            //        { StateBuffers.InitializeLabelState(SceneIdMap.MaxLabelCount, cp.DescriptionOffset, idDesc); }
            //    }
            //}
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
                foreach (var obj in e.NewItems)
                {
                    if (obj is not DrawingObject) { continue; }

                    if (obj is DrawingMtext drawingMtext)
                    {
                        if (drawingMtext.MtextBlock is null)
                        {
                            uint ltId = SceneIdMap.GetOrAddLineTypeId(drawingMtext.LineType, out var isNewLtype);
                            if (isNewLtype) { StateBuffers.InitializeLineTypeState(SceneIdMap.MaxLineTypeId, drawingMtext.LineType, ltId); }

                            drawingMtext.UpdateMtextBlock(ResCache, drawingMtext.Layer.Id, ltId, SceneIdMap, StateBuffers);
                        }
                    }
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
                    _baseSceneDirty = true;
                }
            }
            if (e.PropertyName == nameof(ObjectLayer.Color))
            {
                if (sender is ObjectLayer layer)
                {
                    StateController.SetLayerColor(layer, layer.Color);
                    StateController.FlushLayerUpdates();
                    _baseSceneDirty = true;
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
                    _baseSceneDirty = true;
                    _interactionDirty = true;
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
                            UpdateCogoPointBounds(point);
                            UpdateToggleAnchorBounds(point);

                            if (point.IsFlippedX || point.IsFlippedY)
                            {
                                point.UpdateOffsetOrientation();
                                StateController.SetLabelOffsets(point, point.PointNumberOffset, point.ElevationOffset, point.DescriptionOffset);
                                labelsNeedUpdate = true;

                                UpdateCogoPointBounds(point); // Done a second time because the first call is just to get lines width
                            }
                        }
                        if (labelsNeedUpdate) { StateController.FlushLabelUpdates(); }

                        CadManager.UpdateCogoPointTree();
                        UpdateInitialMatrix();
                    }

                    _baseSceneDirty = true;
                    _interactionDirty = true;
                }
                if (e.PropertyName == nameof(PointGroup.PointInfoBaseXoffset))
                {
                    foreach (var point in pg.Points)
                    {
                        UpdateToggleAnchorBounds(point);
                    }
                    _interactionDirty = true;
                    _baseSceneDirty = true;
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
                    UpdateCogoPointBounds(cp);

                    CadManager.UpdateCogoPointTree();
                    UpdateInitialMatrix();

                    _pointCircleVerticesDirty = true; _baseSceneDirty = true; _interactionDirty = true;
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
                    UpdateCogoPointBounds(cp);

                    CadManager.UpdateCogoPointTree();
                    UpdateInitialMatrix();

                    _baseSceneDirty = true; _interactionDirty = true;
                }
            }
            if (e.PropertyName == nameof(CogoPoint.PointNumber) ||
                e.PropertyName == nameof(CogoPoint.Elevation) ||
                e.PropertyName == nameof(CogoPoint.Description))
            {
                _cogoTextVerticesDirty = true;
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

                    _drawingSettingsBuffer?.Dispose(); _drawingSettingsBuffer = null;

                    _lineInstanceBuffer?.Dispose(); _lineInstanceBuffer = null;
                    _lineRenderModeBuffer?.Dispose(); _lineRenderModeBuffer = null;
                    _lineVertexShader?.Dispose(); _lineVertexShader = null;
                    _linePixelShader?.Dispose(); _linePixelShader = null;
                    _lineInstanceInputLayout?.Dispose(); _lineInstanceInputLayout = null;

                    _lineGlowVertexShader?.Dispose(); _lineGlowVertexShader = null;
                    _lineGlowPixelShader?.Dispose(); _lineGlowPixelShader = null;
                    _lineGlowInstanceBuffer?.Dispose();
                    _lineGlowInstanceBuffer = null;

                    _solidInputLayout?.Dispose(); _solidInputLayout = null;
                    _solidPixelShader?.Dispose(); _solidPixelShader = null;
                    _solidVertexBuffer?.Dispose(); _solidVertexBuffer = null;
                    _solidVertexShader?.Dispose(); _solidVertexShader = null;

                    _transformationBuffer?.Dispose(); _transformationBuffer = null;

                    _hitTestCancellationTokenSource?.Dispose(); _hitTestCancellationTokenSource = null;

                    _hoverCircleBuffer?.Dispose(); _hoverCircleBuffer = null;
                    _hoverCircleVertexShader?.Dispose(); _hoverCircleVertexShader = null;
                    _hoverCirclePixelShader?.Dispose(); _hoverCirclePixelShader = null;
                    _hoverCircleGeometryShader?.Dispose(); _hoverCircleGeometryShader = null;
                    _hoverCircleLayout?.Dispose(); _hoverCircleLayout = null;

                    _cogoPointTextSettingsBuffer?.Dispose(); _cogoPointTextSettingsBuffer = null;
                    _msdfSampler.Dispose(); _msdfSampler = null;

                    _msdfInstanceBuffer?.Dispose(); _msdfInstanceBuffer = null;
                    _msdfSettingsBuffer?.Dispose(); _msdfSettingsBuffer = null;

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

                    _panCacheSrv?.Dispose(); _panCacheSrv = null;
                    _panCacheRtv?.Dispose(); _panCacheRtv = null;
                    _panCacheTexture?.Dispose(); _panCacheTexture = null;
                    _panVertexShader?.Dispose(); _panVertexShader = null;
                    _panPixelShader?.Dispose(); _panPixelShader = null;
                    _panInputLayout?.Dispose(); _panInputLayout = null;
                    _panVertexBuffer?.Dispose(); _panVertexBuffer = null;
                    _panSettingsBuffer?.Dispose(); _panSettingsBuffer = null;
                    _panSampler?.Dispose(); _panSampler = null;

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
