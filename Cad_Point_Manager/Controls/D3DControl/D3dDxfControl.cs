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
        private const float CogoPointTextHitPaddingPixels = 8.0f;
        private const float CogoQuadrantHysteresisPixels = 4.0f;

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

        // Direct3D related fields
        public bool _buffersInitialized = false;
        private Buffer _drawingSettingsBuffer;

        // Vertex staging fields
        private readonly List<LineInstance> _lineInstanceStaging = [];
        private readonly List<TextVertex> _textVertexStaging = [];
        private readonly List<SolidVertex> _solidVertexStaging = [];
        private readonly List<PointMarkerInstance> _pointMarkerInstanceStaging = [];

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

        // General CogoPoint rendering fields
        private readonly Dictionary<CogoPoint, int> _pointCircleRenderIndices = [];
        private readonly Dictionary<CogoPoint, int> _leaderLineRenderIndices = [];
        private readonly Dictionary<CogoPoint, MsdfRenderRange> _msdfRenderRanges = [];

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

        // Point circle rendering related fields
        private bool _pointMarkerShadersLoaded = false;
        private ResizableBuffer<PointMarkerInstance> _pointCircleVertexBuffer;
        private InputLayout _pointMarkerInputLayout;
        private VertexShader _pointMarkerVS;
        private PixelShader _pointMarkerPS;
        private GeometryShader _pointMarkerGS;
        private int _pointCircleVertexCount;
        private bool _pointCircleVerticesDirty = false;

        // Point circle glow rendering related fields
        private bool _cogoHoverShadersLoaded = false;
        private VertexShader _hoverCircleVertexShader;
        private PixelShader _hoverCirclePixelShader;
        private GeometryShader _hoverCircleGeometryShader;

        // Cogo point leader line rendering fields
        private VertexShader _leaderLineVS;
        private PixelShader _leaderLinePS;
        private GeometryShader _leaderLineGS;
        private InputLayout _leaderLineInputLayout;
        private bool _leaderLineShadersLoaded = false;
        private ResizableBuffer<LeaderLineInstance> _leaderLineBuffer;
        private int _leaderLineInstanceCount = 0;
        private bool _leaderLineVerticesDirty = false;
        private Buffer _leaderLineQuadBuffer;

        // Cogo point leader line glow rendering fields
        private VertexShader _leaderLineGlowVS;
        private PixelShader _leaderLineGlowPS;
        private GeometryShader _leaderLineGlowGS;

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
        private VertexShader _dragOverlayOutlineVS;
        private PixelShader _dragOverlayOutlinePS;
        private InputLayout _dragOverlayLayout;
        private Buffer _dragOverlaySettingsBuffer;
        private Buffer _dragOverlayQuadBuffer;
        private VertexShader _dragOverlayFillVS;
        private PixelShader _dragOverlayFillPS;
        private bool _overlayShaderLoaded;

        // Drag Selection Fields
        private bool _isDragging = false;
        private Point _dragStartScreen;
        private Point _dragStart;
        private Rect _dragRect = new(0, 0, 0, 0);
        private Vector _dxfDragRectTranslate = new();
        private System.Windows.Media.Matrix _currentlyAppliedDragRectMatrix = new();
        private bool _dragOverlayDirty = false;
        private bool _dragHitTestDirty = false;

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

        // Hit Testing Fields
        private double _currentHitTestPadding;

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
            if (_textVerticesDirty) { UpdateTextVertices(); }
            if (_solidVerticesDirty) { UpdateSolidVertices(); }
            if (_cogoTextVerticesDirty) { UpdateMsdfInstances(); }
            if (_pointCircleVerticesDirty) { UpdatePointCircleVertices(); }
            if (HitTestableObjectTreeDirty) { LoadHitTestableObjectTree(); }
            if (_anchorVerticesDirty) { UpdateToggleAnchorVertices(); }
            if (_leaderLineVerticesDirty) { UpdateLeaderLineVertices(); }
            if (_sigPointVerticesDirty) { UpdateSignificantPointVertices(); }
            if (_dragOverlayDirty) { UpdateDragOverlay(DragRect); }

            if (!_lineShadersLoaded) { InitializeLineShaders(); }
            if (!_lineGlowShadersLoaded) { InitializeLineGlowShaders(); }
            if (!_textShaderLoaded) { InitializeTextShaders(); }
            if (!_solidShaderLoaded) { InitializeSolidShaders(); }
            if (!_overlayShaderLoaded) { InitializeOverlayShaders(); }
            if (!_msdfShadersLoaded) { InitializeMsdfShaders(); }
            if (!_pointMarkerShadersLoaded) { InitializePointCircleShaders(); }
            if (!_cogoHoverShadersLoaded) { InitializePointCircleGlowShaders(); }
            if (!_anchorShaderLoaded) { InitializeToggleAnchorShaders(); }
            if (!_leaderLineShadersLoaded) { InitializeLeaderLineShaders(); }
            if (!_sigPointShadersLoaded) { InitializeSignificantPointsShaders(); }
            if (!_panShadersLoaded) { InitializePanShaders(); }

            if (!ConstantBuffersInitialized) { InitializeConstantBuffers(); }
            if (ConstantBuffersDirty) { UpdateConstantBuffers(); }
            if (TransformationBufferDirty || CadManager.Camera.IsDirty) { UpdateTransformationBuffer(); }

            var ctx = ResCache.DeviceContext;

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

                if (_cogoPointTextBeingMoved)
                {
                    DrawMovingPointCircle(ctx, _pressedToggleButtonPoint);
                    DrawMovingMsdfGlyph(ctx, _pressedToggleButtonPoint);
                    DrawMovingLeaderLine(ctx, _pressedToggleButtonPoint);
                }
                else
                {
                    DrawPointCircleGlow(ctx);
                    DrawSignificantPoints(ctx);
                    DrawMsdfGlowGlyphs(ctx);
                    DrawLeaderLinesGlow(ctx);
                    DrawCogoPointAnchors(ctx);
                }

                _interactionDirty = false;
            }

            ctx.CopyResource(ResCache.InteractionTexture, ResCache.FrameTexture);
            ctx.OutputMerger.SetRenderTargets(ResCache.FrameRenderTargetView);

            if (IsDragging)
            {
                DrawDragOverlay(ctx);
            }

            ctx.CopyResource(ResCache.FrameTexture, ResCache.Texture2D);
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

            if (_cogoPointTextBeingMoved)
            {
                DrawPointCirclesExcept(ctx, _pressedToggleButtonPoint);
                //Debug.WriteLine($"Circles {sw.ElapsedMilliseconds} ms");
                //sw.Restart();

                DrawMsdfGlyphsExcept(ctx, _pressedToggleButtonPoint);
                //Debug.WriteLine($"Glyphs {sw.ElapsedMilliseconds} ms");
                //sw.Restart();

                DrawLeaderLinesExcept(ctx, _pressedToggleButtonPoint);
                //Debug.WriteLine($"Glyphs {sw.ElapsedMilliseconds} ms");
            }
            else
            {
                DrawPointCircles(ctx);
                //Debug.WriteLine($"Circles {sw.ElapsedMilliseconds} ms");
                //sw.Restart();

                DrawMsdfGlyphs(ctx);
                //Debug.WriteLine($"Glyphs {sw.ElapsedMilliseconds} ms");
                //sw.Restart();

                DrawLeaderLines(ctx);
                //Debug.WriteLine($"Glyphs {sw.ElapsedMilliseconds} ms");
            }
        }
        private void DrawLines(DeviceContext ctx)
        {
            if (_lineInstanceBuffer is null || _lineInstanceCount == 0) { return; }

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
        }
        private void DrawLineGlows(DeviceContext ctx)
        {
            ctx.OutputMerger.SetRenderTargets(ResCache.GlowRenderTargetView);
            ctx.ClearRenderTargetView(ResCache.GlowRenderTargetView, new RawColor4(0, 0, 0, 0));

            if (_lineInstanceBuffer is null || _lineInstanceCount == 0)
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
            var instanceBinding = new VertexBufferBinding(_lineInstanceBuffer.Buffer, _lineInstanceBuffer.Stride, 0);

            ctx.InputAssembler.SetVertexBuffers(0, quadBinding, instanceBinding);

            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.VertexShader.SetConstantBuffer(1, _drawingSettingsBuffer);

            ctx.PixelShader.SetConstantBuffer(1, _drawingSettingsBuffer);

            ctx.PixelShader.SetShaderResource(0, StateBuffers.LayerSRV);
            ctx.PixelShader.SetShaderResource(1, StateBuffers.ObjectSRV);
            ctx.PixelShader.SetShaderResource(2, StateBuffers.LineTypeSRV);
            ctx.PixelShader.SetShaderResource(3, StateBuffers.PatternSRV);

            ctx.DrawInstanced(6, _lineInstanceCount, 0, 0);
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
        private void SetupMsdfPipeline(DeviceContext ctx)
        {
            ctx.VertexShader.Set(_msdfVS);
            ctx.PixelShader.Set(_msdfPS);

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
        }
        private void DrawMsdfGlyphs(DeviceContext ctx)
        {
            if (_msdfInstanceCount == 0) { return; }

            SetupMsdfPipeline(ctx);

            var quadBinding = new VertexBufferBinding(_msdfQuadBuffer, Utilities.SizeOf<MsdfVertex>(), 0);
            var instanceBinding = new VertexBufferBinding(_msdfInstanceBuffer.Buffer, _msdfInstanceBuffer.Stride, 0);

            ctx.InputAssembler.SetVertexBuffers(0, quadBinding, instanceBinding);

            ctx.DrawInstanced(6, _msdfInstanceCount, 0, 0);
        }
        private void DrawMsdfGlyphsExcept(DeviceContext ctx, CogoPoint excludedPoint)
        {
            if (_msdfInstanceCount == 0)
                return;

            if (!_msdfRenderRanges.TryGetValue(excludedPoint,
                out var excluded))
            {
                DrawMsdfGlyphs(ctx);
                return;
            }

            SetupMsdfPipeline(ctx);

            var quadBinding = new VertexBufferBinding(_msdfQuadBuffer, Utilities.SizeOf<MsdfVertex>(), 0);
            var instanceBinding = new VertexBufferBinding(_msdfInstanceBuffer.Buffer, _msdfInstanceBuffer.Stride, 0);

            ctx.InputAssembler.SetVertexBuffers(0, quadBinding, instanceBinding);

            int beforeCount = excluded.StartInstance;

            if (beforeCount > 0)
            {
                ctx.DrawInstanced(6, beforeCount, 0, 0);
            }

            int afterStart = excluded.StartInstance + excluded.InstanceCount;
            int afterCount = _msdfInstanceCount - afterStart;

            if (afterCount > 0)
            {
                ctx.DrawInstanced(6, afterCount, 0, afterStart);
            }
        }
        private void DrawMovingMsdfGlyph(DeviceContext ctx, CogoPoint point)
        {
            if (!_msdfRenderRanges.TryGetValue(point, out var range))
            {
                return;
            }

            SetupMsdfPipeline(ctx);

            var quadBinding = new VertexBufferBinding(_msdfQuadBuffer, Utilities.SizeOf<MsdfVertex>(), 0);
            var instanceBinding = new VertexBufferBinding(_msdfInstanceBuffer.Buffer, _msdfInstanceBuffer.Stride, 0);

            ctx.InputAssembler.SetVertexBuffers(0, quadBinding, instanceBinding);

            ctx.DrawInstanced(6, range.InstanceCount, 0, range.StartInstance);
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
        private void SetupPointCirclesPipeline(DeviceContext ctx)
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
        }
        private void DrawPointCircles(DeviceContext ctx)
        {
            if (_pointCircleVertexCount == 0) { return; }

            SetupPointCirclesPipeline(ctx);

            ctx.Draw(_pointCircleVertexCount, 0);
            ctx.GeometryShader.Set(null);
        }
        private void DrawPointCirclesExcept(DeviceContext ctx, CogoPoint excludedPoint)
        {
            if (_pointCircleVertexCount == 0)
                return;

            if (!_pointCircleRenderIndices.TryGetValue(excludedPoint, out int excludedIndex))
            {
                DrawPointCircles(ctx);
                return;
            }

            SetupPointCirclesPipeline(ctx);

            // Everything before moving point
            if (excludedIndex > 0)
            {
                ctx.Draw(excludedIndex, 0);
            }

            int afterStart = excludedIndex + 1;
            int afterCount = _pointCircleVertexCount - afterStart;

            if (afterCount > 0)
            {
                ctx.Draw(afterCount, afterStart);
            }

            ctx.GeometryShader.Set(null);
        }
        private void DrawMovingPointCircle(DeviceContext ctx, CogoPoint point)
        {
            if (!_pointCircleRenderIndices.TryGetValue(point, out int index))
            {
                return;
            }

            SetupPointCirclesPipeline(ctx);

            ctx.Draw(1, index);

            ctx.GeometryShader.Set(null);
        }
        private void DrawPointCircleGlow(DeviceContext ctx)
        {
            if (_pointCircleVertexCount == 0) { return; }

            ctx.VertexShader.Set(_hoverCircleVertexShader);
            ctx.GeometryShader.Set(_hoverCircleGeometryShader);
            ctx.PixelShader.Set(_hoverCirclePixelShader);
            ctx.InputAssembler.InputLayout = _pointMarkerInputLayout;

            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);

            ctx.GeometryShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.GeometryShader.SetConstantBuffer(1, _drawingSettingsBuffer);

            ctx.GeometryShader.SetShaderResource(0, StateBuffers.PointSRV);
            ctx.GeometryShader.SetShaderResource(1, StateBuffers.GroupSRV);

            ctx.PixelShader.SetConstantBuffer(1, _drawingSettingsBuffer);

            ctx.PixelShader.SetShaderResource(0, StateBuffers.PointSRV);
            ctx.PixelShader.SetShaderResource(1, StateBuffers.GroupSRV);

            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;

            ctx.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_pointCircleVertexBuffer.Buffer, _pointCircleVertexBuffer.Stride, 0));

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
        private void SetupLeaderLinesPipeline(DeviceContext ctx)
        {
            ctx.GeometryShader.Set(null);
            ctx.InputAssembler.InputLayout = _leaderLineInputLayout;
            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            var quadBinding = new VertexBufferBinding(_leaderLineQuadBuffer, Utilities.SizeOf<LineCornerVertex>(), 0);
            var instanceBinding = new VertexBufferBinding(_leaderLineBuffer.Buffer, _leaderLineBuffer.Stride, 0);
            ctx.InputAssembler.SetVertexBuffers(0, quadBinding, instanceBinding);

            ctx.VertexShader.Set(_leaderLineVS);
            ctx.PixelShader.Set(_leaderLinePS);

            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.VertexShader.SetConstantBuffer(1, _drawingSettingsBuffer);
            ctx.VertexShader.SetShaderResource(0, StateBuffers.PointSRV);
            ctx.VertexShader.SetShaderResource(1, StateBuffers.GroupSRV);

            ctx.PixelShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.PixelShader.SetConstantBuffer(1, _drawingSettingsBuffer);
            ctx.PixelShader.SetShaderResource(0, StateBuffers.PointSRV);
            ctx.PixelShader.SetShaderResource(1, StateBuffers.GroupSRV);
        }
        private void DrawLeaderLines(DeviceContext ctx)
        {
            if (_leaderLineInstanceCount <= 0) { return; }

            SetupLeaderLinesPipeline(ctx);

            ctx.DrawInstanced(6, _leaderLineInstanceCount, 0, 0);
        }
        private void DrawLeaderLinesExcept(DeviceContext ctx, CogoPoint excludedPoint)
        {
            if (_leaderLineInstanceCount == 0)
                return;

            if (!_leaderLineRenderIndices.TryGetValue(excludedPoint, out int excludedIndex))
            {
                DrawLeaderLines(ctx);
                return;
            }

            ctx.GeometryShader.Set(null);
            ctx.InputAssembler.InputLayout = _leaderLineInputLayout;
            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            var quadBinding = new VertexBufferBinding(_leaderLineQuadBuffer, Utilities.SizeOf<LineCornerVertex>(), 0);
            var instanceBinding = new VertexBufferBinding(_leaderLineBuffer.Buffer, _leaderLineBuffer.Stride, 0);
            ctx.InputAssembler.SetVertexBuffers(0, quadBinding, instanceBinding);

            ctx.VertexShader.Set(_leaderLineVS);
            ctx.PixelShader.Set(_leaderLinePS);

            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.VertexShader.SetConstantBuffer(1, _drawingSettingsBuffer);
            ctx.VertexShader.SetShaderResource(0, StateBuffers.PointSRV);
            ctx.VertexShader.SetShaderResource(1, StateBuffers.GroupSRV);

            ctx.PixelShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.PixelShader.SetConstantBuffer(1, _drawingSettingsBuffer);
            ctx.PixelShader.SetShaderResource(0, StateBuffers.PointSRV);
            ctx.PixelShader.SetShaderResource(1, StateBuffers.GroupSRV);

            if (excludedIndex > 0)
            {
                ctx.DrawInstanced(6, excludedIndex, 0, 0);
            }

            int afterStart = excludedIndex + 1;
            int afterCount = _leaderLineInstanceCount - afterStart;

            if (afterCount > 0)
            {
                ctx.DrawInstanced(6, afterCount, 0, afterStart);
            }
        }
        private void DrawMovingLeaderLine(DeviceContext ctx, CogoPoint point)
        {
            if (!_leaderLineRenderIndices.TryGetValue(point, out int index))
            {
                return;
            }

            SetupLeaderLinesPipeline(ctx);

            ctx.DrawInstanced(6, 1, 0, index);
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

            ctx.VertexShader.Set(_leaderLineGlowVS);
            ctx.PixelShader.Set(_leaderLineGlowPS);

            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.VertexShader.SetConstantBuffer(1, _drawingSettingsBuffer);
            ctx.VertexShader.SetShaderResource(0, StateBuffers.PointSRV);
            ctx.VertexShader.SetShaderResource(1, StateBuffers.GroupSRV);

            ctx.PixelShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.PixelShader.SetConstantBuffer(1, _drawingSettingsBuffer);
            ctx.PixelShader.SetShaderResource(0, StateBuffers.PointSRV);
            ctx.PixelShader.SetShaderResource(1, StateBuffers.GroupSRV);

            ctx.DrawInstanced(6, _leaderLineInstanceCount, 0, 0);
        }
        private void DrawDragOverlay(DeviceContext ctx)
        {
            // Drag Rect Fill
            ctx.GeometryShader.Set(null);

            ctx.OutputMerger.SetRenderTargets(ResCache.FrameRenderTargetView);
            ctx.OutputMerger.SetBlendState(ResCache.BaseBlendState);
            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            ctx.InputAssembler.InputLayout = _dragOverlayLayout;
            ctx.InputAssembler.SetVertexBuffers(
                0, new VertexBufferBinding(_dragOverlayQuadBuffer, Utilities.SizeOf<DragOverlayVertex>(), 0));
            ctx.VertexShader.Set(_dragOverlayFillVS);
            ctx.PixelShader.Set(_dragOverlayFillPS);

            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.VertexShader.SetConstantBuffer(1, _dragOverlaySettingsBuffer);

            ctx.PixelShader.SetConstantBuffer(1, _dragOverlaySettingsBuffer);

            ctx.Draw(6, 0);

            // Drag Rect Outline
            ctx.OutputMerger.SetRenderTargets(ResCache.FrameRenderTargetView);
            ctx.OutputMerger.SetBlendState(ResCache.BaseBlendState);
            ctx.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            ctx.InputAssembler.InputLayout = _dragOverlayLayout;
            ctx.InputAssembler.SetVertexBuffers(
                0, new VertexBufferBinding(_dragOverlayQuadBuffer, Utilities.SizeOf<DragOverlayVertex>(), 0));
            ctx.VertexShader.Set(_dragOverlayOutlineVS);
            ctx.PixelShader.Set(_dragOverlayOutlinePS);

            ctx.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            ctx.VertexShader.SetConstantBuffer(1, _dragOverlaySettingsBuffer);

            ctx.PixelShader.SetConstantBuffer(1, _dragOverlaySettingsBuffer);

            ctx.Draw(6, 0);
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
            ctx.GeometryShader.Set(null);
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

            CadManager.BuildLineInstances(_lineInstanceStaging, ResCache, SceneIdMap, StateBuffers);

            StateBuffers.EnsureObjectCapacity(SceneIdMap.ObjectCount);
            _lineInstanceBuffer.Update(context, CollectionsMarshal.AsSpan(_lineInstanceStaging));
            _lineInstanceCount = _lineInstanceStaging.Count;

            StateBuffers.FlushAll();
            _lineVerticesDirty = false;
            _baseSceneDirty = true;
        }
        private void UpdateTextVertices()
        {
            if (_textVertexBuffer is null || CadManager is null)
            {
                _textVerticesDirty = false;
                return;
            }

            var context = ResCache.DeviceContext;

            CadManager.BuildTextVertices(_textVertexStaging, ResCache, SceneIdMap, StateBuffers);

            _textVertexBuffer.Update(context, CollectionsMarshal.AsSpan(_textVertexStaging));
            _textVertexCount = _textVertexStaging.Count;

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
            CadManager.BuildSolidVertices(_solidVertexStaging, ResCache, SceneIdMap, StateBuffers);

            _solidVertexBuffer.Update(context, CollectionsMarshal.AsSpan(_solidVertexStaging));
            _solidVertexCount = _solidVertexStaging.Count;

            StateBuffers.FlushAll();
            _solidVerticesDirty = false;
            _baseSceneDirty = true;
        }
        private void UpdateMsdfInstances()
        {
            _msdfInstances.Clear();
            _msdfRenderRanges.Clear();

            foreach (var pointGroup in CadManager.PointGroups)
            {
                if (pointGroup is null || !pointGroup.IsVisible)
                {
                    continue;
                }

                foreach (var point in CadManager.GetPoints(pointGroup))
                {
                    if (point is null)
                    {
                        continue;
                    }

                    int startInstance = _msdfInstances.Count;
                    AddCogoPoint(point, _msdfInstances);
                    int instanceCount = _msdfInstances.Count - startInstance;

                    _msdfRenderRanges[point] = new MsdfRenderRange(startInstance, instanceCount);
                }
            }

            CadManager.UpdateCogoPointTree();
            StateBuffers.FlushAll();

            _msdfInstanceBuffer.Update(ResCache.DeviceContext, CollectionsMarshal.AsSpan(_msdfInstances));
            _msdfInstanceCount = _msdfInstances.Count;

            _cogoTextVerticesDirty = false;
            _baseSceneDirty = true;
        }
        private void UpdatePointCircleVertices()
        {
            if (_pointCircleVertexBuffer is null)
                return;

            var context = ResCache.DeviceContext;

            _pointCircleRenderIndices.Clear();

            CadManager.BuildPointMarkerInstances(_pointMarkerInstanceStaging, StateController, _pointCircleRenderIndices);

            _pointCircleVertexBuffer.Update(
                context,
                CollectionsMarshal.AsSpan(
                    _pointMarkerInstanceStaging));

            _pointCircleVertexCount =
                _pointMarkerInstanceStaging.Count;

            StateBuffers.FlushAll();

            _pointCircleVerticesDirty = false;
            _baseSceneDirty = true;
        }
        private void UpdateDragOverlay(Rect r)
        {
            if (r.IsEmpty || r.Width <= 0 || r.Height <= 0 || !IsDragging)
            {
                _dragOverlayDirty = false;
                return;
            }

            var settings = new DragOverlaySettings
            {
                RectMinPx = new Vector2((float)r.Left, (float)r.Top),
                RectMaxPx = new Vector2((float)r.Right, (float)r.Bottom),
                ViewportSize = new Vector2(RenderPixelWidth, RenderPixelHeight),
                ThicknessPx = 1.0f,
                FeatherPx = 1.0f,
                FillColor = new Vector4(0f, 0.749f, 1f, 0.3f),
                BorderColor = new Vector4(0f, 0.749f, 1f, 1f)
            };

            var ctx = ResCache.DeviceContext;

            var box = ctx.MapSubresource(
                _dragOverlaySettingsBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None);

            Utilities.Write(box.DataPointer, ref settings);

            ctx.UnmapSubresource(_dragOverlaySettingsBuffer, 0);

            _dragOverlayDirty = false;
        }
        private void UpdateToggleAnchorVertices()
        {
            if (ResCache is null || CadManager.Camera is null || ResCache.DeviceContext is null) { return; }

            var ctx = ResCache.DeviceContext;
            var inst = new List<ToggleAnchorInstance>(SelectedCogoPoints.Count);
            foreach (var pg in CadManager.PointGroups)
            {
                if (pg is null) { continue; }

                var gid = SceneIdMap.GetOrAddGroupId(pg, out var isNewGroup);
                if (isNewGroup) { StateBuffers.EnsureGroupCapacity(SceneIdMap.GroupCount); }

                foreach (var p in CadManager.GetPoints(pg))
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
            _leaderLineRenderIndices.Clear();

            foreach (var pg in CadManager.PointGroups)
            {
                if (pg is null) { continue; }

                uint gid = SceneIdMap.GetOrAddGroupId(pg, out var isNewGroup);

                if (isNewGroup) { StateBuffers.EnsureGroupCapacity(SceneIdMap.GroupCount); }

                foreach (var p in CadManager.GetPoints(pg))
                {
                    if (p is null) { continue; }

                    uint pid = SceneIdMap.GetOrAddPointId(p, out var isNewPoint);

                    if (isNewPoint) { StateBuffers.EnsurePointCapacity(SceneIdMap.PointCount); }

                    list.Add(new LeaderLineInstance
                    {
                        PointId = pid
                    });

                    _leaderLineRenderIndices[p] = list.Count - 1;
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
            using var lineVSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "VSMain", "vs_5_0");
            _lineVertexShader = new VertexShader(device, lineVSBytecode);

            using var linePSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "PSMain", "ps_5_0");
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
            using var vsBytecode = ShaderBytecode.CompileFromFile(shaderPath, "VSMain", "vs_5_0");
            using var psBytecode = ShaderBytecode.CompileFromFile(shaderPath, "PSMain", "ps_5_0");

            _lineGlowVertexShader = new VertexShader(device, vsBytecode);
            _lineGlowPixelShader = new PixelShader(device, psBytecode);

            // Load Composite Shaders
            string compositeShaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\GlowCompositeShader.hlsl");
            using var compositeVsBytecode = ShaderBytecode.CompileFromFile(compositeShaderPath, "VSMain", "vs_5_0");
            using var compositePsBytecode = ShaderBytecode.CompileFromFile(compositeShaderPath, "PSMain", "ps_5_0");

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
            using var textVSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "VSMain", "vs_5_0");
            _textVertexShader = new VertexShader(ResCache.Device, textVSBytecode);

            using var textPSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "PSMain", "ps_5_0");
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
            using var solidVSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "VSMain", "vs_5_0");
            _solidVertexShader = new VertexShader(ResCache.Device, solidVSBytecode);

            using var solidPSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "PSMain", "ps_5_0");
            _solidPixelShader = new PixelShader(ResCache.Device, solidPSBytecode);

            // Layout
            _solidInputLayout = new InputLayout(
                ResCache.Device,
                ShaderSignature.GetInputSignature(solidVSBytecode),
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
            using var vsBytecode = ShaderBytecode.CompileFromFile(shaderPath, "VSMain", "vs_5_0");
            using var psBytecode = ShaderBytecode.CompileFromFile(shaderPath, "PSMain", "ps_5_0");

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
            using var glowVsBytecode = ShaderBytecode.CompileFromFile(glowShaderPath, "VSMain", "vs_5_0");
            using var glowPsBytecode = ShaderBytecode.CompileFromFile(glowShaderPath, "PSMain", "ps_5_0");

            _msdfGlowVS = new VertexShader(ResCache.Device, glowVsBytecode);
            _msdfGlowPS = new PixelShader(ResCache.Device, glowPsBytecode);

            _msdfShadersLoaded = true;
        }
        private void InitializePointCircleShaders()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            while (Path.GetFileName(path) != "Cad_Point_Manager")
            {
                path = Path.GetDirectoryName(path);
                if (path == null) { throw new DirectoryNotFoundException("The 'Cad_Point_Manager' directory could not be found in the path."); }
            }

            string pointMarkerShaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\\PointCircleShader.hlsl");
            using var pointMarkerVsb = ShaderBytecode.CompileFromFile(pointMarkerShaderPath, "VSMain", "vs_5_0");
            using var pointMarkerPsb = ShaderBytecode.CompileFromFile(pointMarkerShaderPath, "PSMain", "ps_5_0");
            using var pointMarkerGsb = ShaderBytecode.CompileFromFile(pointMarkerShaderPath, "GSMain", "gs_5_0");

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
        private void InitializePointCircleGlowShaders()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;

            while (Path.GetFileName(path) != "Cad_Point_Manager")
            {
                path = Path.GetDirectoryName(path);
                if (path == null)
                    throw new DirectoryNotFoundException("The 'Cad_Point_Manager' directory could not be found in the path.");
            }
            string circleHoverShaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\PointCircleGlowShader.hlsl");

            using var circleVSBytecode = ShaderBytecode.CompileFromFile(circleHoverShaderPath, "VSMain", "vs_5_0");
            _hoverCircleVertexShader = new VertexShader(ResCache.Device, circleVSBytecode);
            using var circlePSBytecode = ShaderBytecode.CompileFromFile(circleHoverShaderPath, "PSMain", "ps_5_0");
            _hoverCirclePixelShader = new PixelShader(ResCache.Device, circlePSBytecode);
            using var circleGSBytecode = ShaderBytecode.CompileFromFile(circleHoverShaderPath, "GSMain", "gs_5_0");
            _hoverCircleGeometryShader = new GeometryShader(ResCache.Device, circleGSBytecode);

            _cogoHoverShadersLoaded = true;
        }
        private void InitializeOverlayShaders()
        {
            var device = ResCache.Device;

            var path = AppDomain.CurrentDomain.BaseDirectory;
            while (Path.GetFileName(path) != "Cad_Point_Manager")
            {
                path = Path.GetDirectoryName(path) ?? throw new DirectoryNotFoundException("Cad_Point_Manager not found");
            }

            string fx = Path.Combine(path, @"Controls\D3DControl\Shaders\DragOverlayFillShader.hlsl");
            using var vs = ShaderBytecode.CompileFromFile(fx, "VSMain", "vs_5_0");
            using var ps = ShaderBytecode.CompileFromFile(fx, "PSMain", "ps_5_0");
            _dragOverlayFillVS = new VertexShader(device, vs);
            _dragOverlayFillPS = new PixelShader(device, ps);

            _dragOverlayLayout = new InputLayout(device, ShaderSignature.GetInputSignature(vs),
                new[]
                {
                    new InputElement("LOCAL",0,Format.R32G32_Float,0,0),
                });

            // Border
            string outlineFx = Path.Combine(path, @"Controls\D3DControl\Shaders\DragOverlayOutlineShader.hlsl");
            using var ovs = ShaderBytecode.CompileFromFile(outlineFx, "VSMain", "vs_5_0");
            using var ops = ShaderBytecode.CompileFromFile(outlineFx, "PSMain", "ps_5_0");
            _dragOverlayOutlineVS = new VertexShader(device, ovs);
            _dragOverlayOutlinePS = new PixelShader(device, ops);

            _dragOverlayQuadBuffer?.Dispose();
            var dragQuadVertices = new[]
            {
                new DragOverlayVertex(0f, 0f),
                new DragOverlayVertex(0f, 1f),
                new DragOverlayVertex(1f, 1f),

                new DragOverlayVertex(0f, 0f),
                new DragOverlayVertex(1f, 1f),
                new DragOverlayVertex(1f, 0f),
            };
            _dragOverlayQuadBuffer = Buffer.Create(
                device,
                BindFlags.VertexBuffer,
                dragQuadVertices);

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

            using var vs = ShaderBytecode.CompileFromFile(fx, "VSMain", "vs_5_0");
            using var ps = ShaderBytecode.CompileFromFile(fx, "PSMain", "ps_5_0");
            _toggleVS = new VertexShader(ResCache.Device, vs);
            _togglePS = new PixelShader(ResCache.Device, ps);

            _toggleLayout = new InputLayout(
                ResCache.Device,
                ShaderSignature.GetInputSignature(vs),
                new[]
                {
                    new InputElement("POSITION", 0, Format.R32G32_Float, 0, 0),

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

            string lineShaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\LeaderLineShader.hlsl");
            using var lineVSBytecode = ShaderBytecode.CompileFromFile(lineShaderPath, "VSMain", "vs_5_0");
            using var linePSBytecode = ShaderBytecode.CompileFromFile(lineShaderPath, "PSMain", "ps_5_0");

            _leaderLineVS = new VertexShader(device, lineVSBytecode);
            _leaderLinePS = new PixelShader(device, linePSBytecode);

            _leaderLineInputLayout = new InputLayout(device, ShaderSignature.GetInputSignature(lineVSBytecode), new[]
            {
                new InputElement("LOCAL",0,Format.R32G32_Float,0,0,InputClassification.PerVertexData,0),
                new InputElement("POINT_ID",0,Format.R32_UInt,0,1,InputClassification.PerInstanceData,1)});
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

            string glowShaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\LeaderLineGlowShader.hlsl");
            using var glowVSBytecode = ShaderBytecode.CompileFromFile(glowShaderPath, "VSMain", "vs_5_0");
            using var glowPSBytecode = ShaderBytecode.CompileFromFile(glowShaderPath, "PSMain", "ps_5_0");

            _leaderLineGlowVS = new VertexShader(device, glowVSBytecode);
            _leaderLineGlowPS = new PixelShader(device, glowPSBytecode);

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

            using var significantPointVsb = ShaderBytecode.CompileFromFile(significantPointShaderPath, "VSMain", "vs_5_0");
            using var significantPointPsb = ShaderBytecode.CompileFromFile(significantPointShaderPath, "PSMain", "ps_5_0");
            using var significantPointGsb = ShaderBytecode.CompileFromFile(significantPointShaderPath, "GSMain", "gs_5_0");

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

            using var vsBytecode = ShaderBytecode.CompileFromFile(shaderPath, "VSMain", "vs_5_0");
            using var psBytecode = ShaderBytecode.CompileFromFile(shaderPath, "PSMain", "ps_5_0");

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

            _textVertexBuffer?.Dispose();
            _textVertexBuffer = new(device, GlobalHelperProperties.InitialTextVertices);

            _solidVertexBuffer?.Dispose();
            _solidVertexBuffer = new(device, GlobalHelperProperties.InitialLineVertices);

            _pointCircleVertexBuffer?.Dispose();
            _pointCircleVertexBuffer = new(device, GlobalHelperProperties.InitialCircleVertices);

            _anchorInstanceBuffer?.Dispose();
            _anchorInstanceBuffer = new(device, 64);

            _leaderLineBuffer?.Dispose();
            _leaderLineBuffer = new(device, 2);

            _sigPointVertexBuffer?.Dispose();
            _sigPointVertexBuffer = new(device, 64);

            SceneIdMap ??= new();
            StateBuffers?.Dispose();
            StateBuffers = new(device, device.ImmediateContext);
            StateController = new(SceneIdMap, StateBuffers);

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
                Usage = ResourceUsage.Dynamic,
                SizeInBytes = Utilities.SizeOf<DragOverlaySettings>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None
            };
            _dragOverlaySettingsBuffer = new Buffer(ResCache.Device, overlayOutlineBufferDesc);

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

            // Point number
            var pointNumberLayout = LayoutMsdfString(
                point.PointNumber.ToString(), point, point.PointNumberOffset, emToWorld);
            point.PointNumberBounds = pointNumberLayout.Bounds;
            point.PointNumberGlyphs = CreateGlyphHitRegions(pointNumberLayout);

            AddMsdfString(
                pointNumberLayout, ids.PointNumberLabelId, ids.PointId, emToWorld, destination);

            // Elevation
            var elevationLayout = LayoutMsdfString(
                point.Elevation.ToString("F3"), point, point.ElevationOffset, emToWorld);
            point.ElevationBounds = elevationLayout.Bounds;
            point.ElevationGlyphs = CreateGlyphHitRegions(elevationLayout);

            AddMsdfString(elevationLayout, ids.ElevationLabelId, ids.PointId, emToWorld, destination);

            // Description (if it has one)
            if (point.HasDescription)
            {
                var descriptionLayout = LayoutMsdfString(
                    point.Description.ToString(), point, point.DescriptionOffset, emToWorld);
                point.DescriptionBounds = descriptionLayout.Bounds;
                point.DescriptionGlyphs = CreateGlyphHitRegions(descriptionLayout);

                AddMsdfString(descriptionLayout, ids.DescriptionLabelId, ids.PointId, emToWorld, destination);
            }
            else
            {
                point.DescriptionBounds = Rect.Empty;
                point.DescriptionGlyphs = [];
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
            float emToWorld = p.PointGroup.FontBaseSize.ToFloat();

            // Point Number
            var pointNumberLayout = LayoutMsdfString(
                p.PointNumber.ToString(), p, p.PointNumberOffset, emToWorld);
            p.PointNumberBounds = pointNumberLayout.Bounds;
            p.PointNumberGlyphs = CreateGlyphHitRegions(pointNumberLayout);

            // Elevation
            var elevationLayout = LayoutMsdfString(
                p.Elevation.ToString("F3"), p, p.ElevationOffset, emToWorld);
            p.ElevationBounds = elevationLayout.Bounds;
            p.ElevationGlyphs = CreateGlyphHitRegions(elevationLayout);

            // Description
            if (p.HasDescription)
            {
                var descriptionLayout = LayoutMsdfString(p.Description, p, p.DescriptionOffset, emToWorld);
                p.DescriptionBounds = descriptionLayout.Bounds;
                p.DescriptionGlyphs = CreateGlyphHitRegions(descriptionLayout);
            }
            else
            {
                p.DescriptionBounds = Rect.Empty;
                p.DescriptionGlyphs = [];
            }

            float rW = (float)(GlobalHelperProperties.CogoPointCirclePixelRadius * p.PointGroup.PointScale);
            var c = p.Position;
            p.EllipseBounds = new Rect(c.X - rW, c.Y - rW, 2 * rW, 2 * rW);

            UpdateToggleAnchorBounds(p);

            p.UpdateBounds();
        }
        private MsdfTextLayout LayoutMsdfString(string text, CogoPoint point, Vector2 labelOffset, float emToWorld)
        {
            MsdfTextLayout layout = new();

            float scale = emToWorld * point.PointGroup.PointScale.ToFloat();
            var baseOffset = point.PointGroup.PointInfoBaseXoffset;

            if (point.IsLabelLeft) { baseOffset *= -1; }

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
        private static MsdfGlyphHitRegion[] CreateGlyphHitRegions(MsdfTextLayout layout)
        {
            if (layout.Glyphs.Count == 0) { return []; }

            var regions =
                new MsdfGlyphHitRegion[layout.Glyphs.Count];

            for (int i = 0; i < layout.Glyphs.Count; i++)
            {
                var placement = layout.Glyphs[i];

                regions[i] = new MsdfGlyphHitRegion
                {
                    Bounds = placement.Bounds,
                    UvMin = placement.Glyph.UvMin,
                    UvMax = placement.Glyph.UvMax
                };
            }

            return regions;
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

            if (!_isPanning)
            {
                UpdateDxfCoords(currentMousePos);
            }

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

            var worldUnitsPerPixel = CadManager.Camera.GetWorldUnitsPerPixel();
            _hittestStrokeThickness = 7.0f * worldUnitsPerPixel;
            _currentHitTestPadding = CogoPointTextHitPaddingPixels * worldUnitsPerPixel;

            UpdateToggleAnchorDimensions();

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

            _hitTestCancellationTokenSource?.Cancel();
            _hitTestCancellationTokenSource?.Dispose();

            _hitTestCancellationTokenSource =
                new CancellationTokenSource();

            _ = RunHitTestingAsync(
                _hitTestCancellationTokenSource.Token);
        }
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            var pos = Mouse.GetPosition(this);

            bool isInside =
                pos.X >= 0 && pos.Y >= 0 && pos.X <= this.ActualWidth && pos.Y <= this.ActualHeight;

            if (isInside && IsDragging) { return; }

            _isMouseInside = false;
            _hitTestCancellationTokenSource.Cancel();
            _isPanning = false;

            if (_mouseOverCogoPoints.Count > 0 || _mouseOverHitTestableObjects.Count > 0)
            {
                if (!IsDragging)
                {
                    ResetHoverObjectsWithFlush();
                    _lineVerticesDirty = true;
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
                StateController.FlushPointUpdates();

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

            ResetHoverObjectsWithoutFlush();

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
                _interactionDirty = true;
            }

            _suspendHitTesting = false;
        }
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (_mouseOverToggleButtonPoint is not null)
            {
                PressCogoToggleButton(_mouseOverToggleButtonPoint);
                ResetHoverObjectsWithoutFlush();
                ResetCogoToggleButtonMouseOver();

                var mousePx = GetMousePx(e);
                var w = CadManager.Camera.ScreenToWorld(mousePx);
                var delta = new Vector2(
                    w.X - _pressedToggleButtonPoint.Position.X.ToFloat(),
                    w.Y - _pressedToggleButtonPoint.Position.Y.ToFloat());

                UpdateCogoPointInfoOffset(_pressedToggleButtonPoint, delta);
                _pressedToggleButtonPoint.HasLeaderLine = true;

                CaptureMouse();

                _interactionDirty = true;
                _baseSceneDirty = true;

                e.Handled = true;
                return;
            }

            BeginDrag(e.GetPosition(this));
            UpdateDragRect();
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
                ResetSelectedObjectsWithFlush();
                EndDrag();
                _baseSceneDirty = true;
                _interactionDirty = true;
            }
            if (e.Key == Key.Tab)
            {
                ResetHoverObjectsWithFlush();
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
                    ResetHoverObjectsWithFlush();

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

        //private void UpdateToggleAnchorDimensions()
        //{
        //    float wupp = CadManager.Camera.GetWorldUnitsPerPixel();
        //    float desiredHalfWorld = (AnchorPixelSize * 0.5f) * wupp;
        //    float drawingShort = (float)Math.Min(CadManager.Camera.Extents.Width, CadManager.Camera.Extents.Height);
        //    float maxHalfBase = (drawingShort * MaxCogoToggleToDrawingFraction) * 0.5f;

        //    // Cache for settings
        //    _desiredHalfWorldForAnchors = desiredHalfWorld;
        //    _maxHalfBaseForAnchors = maxHalfBase;
        //    _featherWorldForAnchors = FeatherPx * wupp;

        //    _desiredHalfWorldForAnchors = desiredHalfWorld;
        //    _maxHalfBaseForAnchors = maxHalfBase;
        //    _featherWorldForAnchors = FeatherPx * wupp;

        //    foreach (var pg in CadManager.PointGroups)
        //    {
        //        foreach (var p in CadManager.GetPoints(pg))
        //        {
        //            UpdateToggleAnchorBounds(p);
        //        }
        //    }
        //}
        private void UpdateToggleAnchorDimensions()
        {
            float wupp = CadManager.Camera.GetWorldUnitsPerPixel();

            _desiredHalfWorldForAnchors =
                (AnchorPixelSize * 0.5f) * wupp;

            float drawingShort = (float)Math.Min(
                CadManager.Camera.Extents.Width,
                CadManager.Camera.Extents.Height);

            _maxHalfBaseForAnchors =
                (drawingShort * MaxCogoToggleToDrawingFraction) * 0.5f;

            _featherWorldForAnchors = FeatherPx * wupp;
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
        private bool IsPointInToggleAnchor(CogoPoint point, Point mouse)
        {
            float half = MathF.Min(_desiredHalfWorldForAnchors, _maxHalfBaseForAnchors * (float)point.PointGroup.PointScale);
            var center = point.Position.ToSharpDXVector2() + point.TextInfoOffset;

            return mouse.X >= center.X - half &&
                   mouse.X <= center.X + half &&
                   mouse.Y >= center.Y - half &&
                   mouse.Y <= center.Y + half;
        }
        private void UpdateCogoPointInfoOffset(CogoPoint point, Vector2 offset)
        {
            if (point is null)
                return;

            point.SetTextInfoOffset(offset);

            bool labelsChanged = SetCogoPointLabelQuadrant(point, offset);

            StateController.SetPointInfoOffset(
                point, offset, true);

            if (labelsChanged)
            {
                StateController.FlushLabelUpdates();
            }

            StateController.FlushPointUpdates();

            UpdateToggleAnchorBounds(point);

            _interactionDirty = true;
        }

        private bool SetCogoPointLabelQuadrant(CogoPoint point, Vector2 offset)
        {
            float threshold = CogoQuadrantHysteresisPixels * CadManager.Camera.GetWorldUnitsPerPixel();

            var quadrant = point.LabelQuadrant;

            bool isLeft =
                quadrant == CogoLabelQuadrant.UpperLeft || quadrant == CogoLabelQuadrant.LowerLeft;
            bool isBelow =
                quadrant == CogoLabelQuadrant.LowerLeft || quadrant == CogoLabelQuadrant.LowerRight;

            if (offset.X < -threshold)
                isLeft = true;
            else if (offset.X > threshold)
                isLeft = false;

            if (offset.Y < -threshold)
                isBelow = true;
            else if (offset.Y > threshold)
                isBelow = false;

            var newQuadrant = (isLeft, isBelow) switch
            {
                (false, false) => CogoLabelQuadrant.UpperRight,
                (true, false) => CogoLabelQuadrant.UpperLeft,
                (true, true) => CogoLabelQuadrant.LowerLeft,
                _ => CogoLabelQuadrant.LowerRight
            };

            if (newQuadrant == point.LabelQuadrant)
                return false;

            point.LabelQuadrant = newQuadrant;
            point.UpdateOffsetOrientation();

            StateController.SetLabelOffsets(
                point, point.PointNumberOffset, point.ElevationOffset, point.DescriptionOffset);

            StateController.SetPointLabelQuadrant(point, newQuadrant);

            return true;
        }

        public void ZoomToExtents()
        {
            if (CadManager.Camera is null) { return; }

            CadManager.Camera.ResetView(_dxfInitialMatrix, CadManager.Extents);
            ResetHoverObjectsWithFlush();
            UpdateToggleAnchorDimensions();

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
                _dragHitTestDirty = false;

                return;
            }

            double width = Math.Abs(_dragStart.X - DxfCoords.X);
            double height = Math.Abs(_dragStart.Y - DxfCoords.Y);

            double left = Math.Min(_dragStart.X, DxfCoords.X);
            double top = Math.Min(_dragStart.Y, DxfCoords.Y);

            DragRect = new(left, top, width, height);

            _dragOverlayDirty = true;
            _dragHitTestDirty = true;
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

        public async Task RunHitTestingAsync(CancellationToken token)
        {
            try
            {
                while (_isMouseInside &&
                       !token.IsCancellationRequested)
                {
                    if (_suspendHitTesting)
                    {
                        await Task.Delay(50, token);
                        continue;
                    }

                    if (CadManager.DxfLoaded &&
                        CadManager.HitTestingEnabled)
                    {
                        switch (CadManager.SnapSelectionMode)
                        {
                            case SelectionMode.Points:
                                RunPointsHitTest(token);
                                break;

                            case SelectionMode.Geometries:
                                if (IsDragging)
                                    RunDragGeometriesHittest(token);
                                else
                                    RunGeometriesHitTest(token);
                                break;

                            case SelectionMode.CogoPoints:
                                if (!_cogoPointTextBeingMoved)
                                {
                                    if (IsDragging)
                                        RunDragCogoPointsHittest(token);
                                    else
                                        RunCogoPointsHitTest(token);
                                }
                                break;
                        }
                    }

                    await Task.Delay(50, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
        private void RunPointsHitTest(CancellationToken token)
        {
            if (token.IsCancellationRequested) { token.ThrowIfCancellationRequested(); }

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(() => RunPointsHitTest(token));
                return;
            }

            if (!CadManager.DxfLoaded) { return; }

            _lastHitTestCoords = new(DxfCoords.X, DxfCoords.Y);

            if (SnappedHitTestablePoint is not null &&
                SnappedHitTestablePoint.DistanceToPoint(_lastHitTestCoords) <= _hittestStrokeThickness &&
                _currentSnapHitTestIndex == _lastSnapHitTestIndex)
            {
                return;
            }

            _nearestHitTestablePoints = CadManager
                .HitTestSignficantPoints(_lastHitTestCoords, _hittestStrokeThickness)
                .Take(_maxSelectableObjects)
                .ToList();

            if (_nearestHitTestablePoints.Count == 0)
            {
                SnappedHitTestablePoint = null;
                return;
            }

            var (distance, point) = HitTestingHelpers.GetCycledHit(
                _nearestHitTestablePoints, ref _currentSnapHitTestIndex);

            if (distance > _hittestStrokeThickness)
            {
                SnappedHitTestablePoint = null;
                return;
            }

            SnappedHitTestablePoint = point;
            _lastSnapHitTestIndex = _currentSnapHitTestIndex;
        }
        private void RunGeometriesHitTest(CancellationToken token)
        {
            if (token.IsCancellationRequested) { token.ThrowIfCancellationRequested(); }

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(() => RunGeometriesHitTest(token));
                return;
            }

            if (!CadManager.DxfLoaded) { return; }

            _lastHitTestCoords = new(DxfCoords.X, DxfCoords.Y);

            _nearestHitTestableGeometries = CadManager
                .HitTestGeometries(_lastHitTestCoords, _hittestStrokeThickness)
                .Take(_maxSelectableObjects)
                .ToList();

            ResetHoverObjectsWithoutFlush();

            if (_nearestHitTestableGeometries.Count == 0)
            {
                StateController.FlushObjectUpdates();
                _interactionDirty = true;
                return;
            }

            var (distance, geometry) = HitTestingHelpers.GetCycledHit(
                _nearestHitTestableGeometries, ref _currentSnapHitTestIndex);

            if (distance > _hittestStrokeThickness)
            {
                StateController.FlushObjectUpdates();
                _interactionDirty = true;
                return;
            }

            _mouseOverHitTestableObjects.Add(geometry);
            HoverObject(geometry);

            _lastSnapHitTestIndex = _currentSnapHitTestIndex;

            StateController.FlushObjectUpdates();
            _interactionDirty = true;
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

            _nearestHitTestableCogoPoints = CadManager.HitTestCogoPoints(
                _lastHitTestCoords, _currentHitTestPadding, ResCache.CogoPointMsdfAtlas);

            ResetHoverCogoPointsWithoutFlush();

            PrioritizeCogoToggleHit(_nearestHitTestableCogoPoints, _lastHitTestCoords);

            if (_nearestHitTestableCogoPoints.Count > _maxSelectableObjects)
            {
                _nearestHitTestableCogoPoints = _nearestHitTestableCogoPoints
                    .Take(_maxSelectableObjects)
                    .ToList();
            }

            if (_nearestHitTestableCogoPoints.Count == 0)
            {
                StateController.FlushPointUpdates();
                _interactionDirty = true;

                return;
            }

            var (distance, point) = HitTestingHelpers.GetCycledHit(
                _nearestHitTestableCogoPoints, ref _currentSnapHitTestIndex);

            ResetHoverCogoPointsWithoutFlush();
            ResetCogoToggleButtonMouseOver();

            if (point.IsSelected && IsPointInToggleAnchor(point, _lastHitTestCoords))
            {
                MouseOverCogoToggleButton(point);
                _lastSnapHitTestIndex = _currentSnapHitTestIndex;
                _interactionDirty = true;
                return;
            }

            if (distance <= _currentHitTestPadding)
            {
                _mouseOverCogoPoints.Add(point);
                HoverObject(point);

                _lastSnapHitTestIndex = _currentSnapHitTestIndex;

                StateController.FlushPointUpdates();
                _interactionDirty = true;
            }
        }
        private async void RunDragCogoPointsHittest(CancellationToken token)
        {
            if (token.IsCancellationRequested) { return; }
            if (!CadManager.DxfLoaded) { return; }

            Rect currentRect = await Dispatcher.InvokeAsync(() => DragRect, DispatcherPriority.Render);
            if (currentRect.IsEmpty || currentRect.Width <= 0 || currentRect.Height <= 0) { return; }

            var newSet = CadManager
                .HitTestDragCogoPoints(currentRect, ResCache.CogoPointMsdfAtlas)
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
                _interactionDirty = true;
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
                    if (DragRect.Contains(geometry.Bounds) &&
                        _mouseOverHitTestableObjects.Add(geometry))
                    {
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
                    if (!DragRect.Contains(geometry.Bounds) &&
                        _mouseOverHitTestableObjects.Remove(geometry))
                    {
                        DehoverObject(geometry);
                        lineGlowVerticesDirty = true;
                    }
                }
            }
            _lastQueriedDxfRect = DragRect;

            if (lineGlowVerticesDirty)
            {
                StateController.FlushObjectUpdates();
                _interactionDirty = true;
            }
        }
        private void PrioritizeCogoToggleHit(
            List<(double distance, CogoPoint point)> hits, Point mouse)
        {
            if (hits.Count <= 1)
            {
                return;
            }

            int toggleIndex = hits.FindIndex(
                h => h.point.IsSelected && IsPointInToggleAnchor(h.point, mouse));

            if (toggleIndex <= 0)
            {
                return;
            }

            var toggleHit = hits[toggleIndex];

            hits.RemoveAt(toggleIndex);
            hits.Insert(0, toggleHit);
        }

        private void LoadHitTestableObjectTree()
        {
            if (CadManager is null) { return; }

            CadManager.UpdateHitTestableObjectTree();
            HitTestableObjectTreeDirty = false;
        }

        // Hover Methods
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
        public void ResetHoverObjectsWithFlush()
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
        public void ResetHoverObjectsWithoutFlush()
        {
            ResetHoverHitTestableObjectsWithoutFlush();
            ResetHoverCogoPointsWithoutFlush();
        }
        private void ResetHoverHitTestableObjectsWithoutFlush()
        {
            if (SnappedHitTestablePoint is not null)
            {
                SnappedHitTestablePoint = null;
            }
            if (_mouseOverHitTestableObjects.Count > 0)
            {
                foreach (var obj in _mouseOverHitTestableObjects)
                {
                    DehoverObject(obj);
                }
                _mouseOverHitTestableObjects.Clear();
            }
        }
        public void ResetHoverCogoPointsWithoutFlush()
        {
            if (_mouseOverCogoPoints.Count > 0)
            {
                foreach (var p in _mouseOverCogoPoints)
                {
                    DehoverObject(p);
                }
                _mouseOverCogoPoints.Clear();
            }
        }
        public void ResetHoverCogoPointsWithFlush()
        {
            if (_mouseOverCogoPoints.Count > 0)
            {
                foreach (var p in _mouseOverCogoPoints)
                {
                    DehoverObject(p);
                }

                _mouseOverCogoPoints.Clear();
                StateController.FlushPointUpdates();
            }
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
        public void ResetSelectedObjectsWithFlush()
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
        }
        public void ResetSelectedObjectsWithoutFlush()
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
                    StateController.SetPointAnchorPressed(_pressedToggleButtonPoint, false);
                    _pressedToggleButtonPoint = cogoPoint;
                    _pressedToggleButtonPoint.IsToggleButtonPressed = true;
                    StateController.SetPointAnchorPressed(_pressedToggleButtonPoint, true);
                }
            }
            else
            {
                _pressedToggleButtonPoint = cogoPoint;
                _pressedToggleButtonPoint.IsToggleButtonPressed = true;
                StateController.SetPointAnchorPressed(_pressedToggleButtonPoint, true);
            }
        }
        private void EndCogoToggleButtonPress()
        {
            if (_pressedToggleButtonPoint is null) { return; }

            _pressedToggleButtonPoint.IsToggleButtonPressed = false;
            StateController.SetPointAnchorPressed(_pressedToggleButtonPoint, false);
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

            //StateController.FlushPointUpdates();
            //StateController.FlushLabelUpdates();

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
            int points = CadManager.CogoPoints.Count();
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
            ResetHoverObjectsWithoutFlush();

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
                ResetSelectedObjectsWithoutFlush();
                ResetHoverObjectsWithFlush();
                _currentSnapHitTestIndex = 0;
            }
        }

        private void PointGroups_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
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
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var cp in CadManager.CogoPoints)
                {
                    cp.PropertyChanged -= CogoPoint_PropertyChanged;
                    cp.PropertyChanged += CogoPoint_PropertyChanged;

                    StateController.EnsurePointRegistered(cp);
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
                        foreach (var point in CadManager.GetPoints(pg))
                        {
                            UpdateCogoPointBounds(point);
                            UpdateToggleAnchorBounds(point);

                            if (point.IsLabelBelow || point.IsLabelLeft)
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

                    //// Testing
                    //CadManager.UpdateCogoPointBoundingLines(SceneIdMap);
                    //CadManager.LineVerticesDirty = true;
                    //// End Testing

                    _baseSceneDirty = true;
                    _interactionDirty = true;
                }
                if (e.PropertyName == nameof(PointGroup.PointInfoBaseXoffset))
                {
                    foreach (var point in CadManager.GetPoints(pg))
                    {
                        UpdateToggleAnchorBounds(point);
                    }
                    _interactionDirty = true;
                    _baseSceneDirty = true;
                }
            }
        }
        private void CogoPoint_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CogoPoint.Easting) ||
                e.PropertyName == nameof(CogoPoint.Northing))
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
                    var gId = SceneIdMap.GetOrAddGroupId(cp.PointGroup, out bool isNew);
                    if (isNew)
                    {
                        StateBuffers.InitializeGroupState(SceneIdMap.MaxGroupId, cp.PointGroup, gId);
                    }

                    StateController.SetPointGroupId(cp, gId);
                    StateController.FlushGroupUpdates();

                    _baseSceneDirty = _interactionDirty = true;
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
            if (disposedValue)
                return;

            if (disposing)
            {
                // -------------------------------------------------
                // Stop background work
                // -------------------------------------------------

                _hitTestCancellationTokenSource?.Cancel();

                // -------------------------------------------------
                // Window events
                // -------------------------------------------------

                if (_attachedWindow != null)
                {
                    _attachedWindow.KeyUp -= Window_KeyUp;
                    _attachedWindow.PreviewKeyDown -= Window_PreviewKeyDown;
                    _attachedWindow = null;
                }

                // -------------------------------------------------
                // CadManager / model events
                // -------------------------------------------------

                if (CadManager != null)
                {
                    CadManager.PropertyChanged -= CadManager_PropertyChanged;
                    CadManager.ZoomToExtentsRequested -= ZoomToExtents;
                    CadManager.ZoomToPointRequested -= ZoomToPoint;

                    CadManager.CogoPoints.CollectionChanged -=
                        CogoPoints_CollectionChanged;

                    CadManager.PointGroups.CollectionChanged -=
                        PointGroups_CollectionChanged;

                    CadManager.Layers.CollectionChanged -=
                        Layers_CollectionChanged;

                    foreach (var cp in CadManager.CogoPoints)
                    {
                        cp.PropertyChanged -= CogoPoint_PropertyChanged;
                    }

                    foreach (var pg in CadManager.PointGroups)
                    {
                        pg.PropertyChanged -= PointGroup_PropertyChanged;
                    }

                    foreach (var pair in CadManager.Layers)
                    {
                        var layer = pair.Value;

                        if (layer == null)
                            continue;

                        layer.PropertyChanged -= Layer_PropertyChanged;

                        layer.DrawingObjects.CollectionChanged -=
                            DrawingObjects_CollectionChanged;
                    }
                }

                // -------------------------------------------------
                // Text
                // -------------------------------------------------

                _textVertexBuffer?.Dispose();
                _textVertexBuffer = null;

                _textVertexShader?.Dispose();
                _textVertexShader = null;

                _textPixelShader?.Dispose();
                _textPixelShader = null;

                _textInputLayout?.Dispose();
                _textInputLayout = null;

                // -------------------------------------------------
                // General constant buffers
                // -------------------------------------------------

                _transformationBuffer?.Dispose();
                _transformationBuffer = null;

                _drawingSettingsBuffer?.Dispose();
                _drawingSettingsBuffer = null;

                // -------------------------------------------------
                // Lines
                // -------------------------------------------------

                _lineInstanceBuffer?.Dispose();
                _lineInstanceBuffer = null;

                _lineQuadBuffer?.Dispose();
                _lineQuadBuffer = null;

                _lineRenderModeBuffer?.Dispose();
                _lineRenderModeBuffer = null;

                _lineVertexShader?.Dispose();
                _lineVertexShader = null;

                _linePixelShader?.Dispose();
                _linePixelShader = null;

                _lineInstanceInputLayout?.Dispose();
                _lineInstanceInputLayout = null;

                // -------------------------------------------------
                // Line glow
                // -------------------------------------------------

                _lineGlowVertexShader?.Dispose();
                _lineGlowVertexShader = null;

                _lineGlowPixelShader?.Dispose();
                _lineGlowPixelShader = null;

                _lineGlowCompositeVertexBuffer?.Dispose();
                _lineGlowCompositeVertexBuffer = null;

                _lineGlowCompositeVS?.Dispose();
                _lineGlowCompositeVS = null;

                _lineGlowCompositePS?.Dispose();
                _lineGlowCompositePS = null;

                _lineGlowCompositeLayout?.Dispose();
                _lineGlowCompositeLayout = null;

                _lineGlowCompositeSampler?.Dispose();
                _lineGlowCompositeSampler = null;

                // -------------------------------------------------
                // Solids
                // -------------------------------------------------

                _solidVertexBuffer?.Dispose();
                _solidVertexBuffer = null;

                _solidVertexShader?.Dispose();
                _solidVertexShader = null;

                _solidPixelShader?.Dispose();
                _solidPixelShader = null;

                _solidInputLayout?.Dispose();
                _solidInputLayout = null;

                // -------------------------------------------------
                // MSDF
                // -------------------------------------------------

                _msdfInstanceBuffer?.Dispose();
                _msdfInstanceBuffer = null;

                _msdfVS?.Dispose();
                _msdfVS = null;

                _msdfPS?.Dispose();
                _msdfPS = null;

                _msdfGlowVS?.Dispose();
                _msdfGlowVS = null;

                _msdfGlowPS?.Dispose();
                _msdfGlowPS = null;

                _msdfLayout?.Dispose();
                _msdfLayout = null;

                _msdfQuadBuffer?.Dispose();
                _msdfQuadBuffer = null;

                _msdfSampler?.Dispose();
                _msdfSampler = null;

                _msdfSettingsBuffer?.Dispose();
                _msdfSettingsBuffer = null;

                _cogoPointTextSettingsBuffer?.Dispose();
                _cogoPointTextSettingsBuffer = null;

                // -------------------------------------------------
                // Point circles
                // -------------------------------------------------

                _pointCircleVertexBuffer?.Dispose();
                _pointCircleVertexBuffer = null;

                _pointMarkerInputLayout?.Dispose();
                _pointMarkerInputLayout = null;

                _pointMarkerVS?.Dispose();
                _pointMarkerVS = null;

                _pointMarkerPS?.Dispose();
                _pointMarkerPS = null;

                _pointMarkerGS?.Dispose();
                _pointMarkerGS = null;

                // -------------------------------------------------
                // Cogo hover
                // -------------------------------------------------

                _hoverCircleVertexShader?.Dispose();
                _hoverCircleVertexShader = null;

                _hoverCirclePixelShader?.Dispose();
                _hoverCirclePixelShader = null;

                _hoverCircleGeometryShader?.Dispose();
                _hoverCircleGeometryShader = null;

                // -------------------------------------------------
                // Leader lines
                // -------------------------------------------------

                _leaderLineBuffer?.Dispose();
                _leaderLineBuffer = null;

                _leaderLineVS?.Dispose();
                _leaderLineVS = null;

                _leaderLinePS?.Dispose();
                _leaderLinePS = null;

                _leaderLineGS?.Dispose();
                _leaderLineGS = null;

                _leaderLineInputLayout?.Dispose();
                _leaderLineInputLayout = null;

                _leaderLineQuadBuffer?.Dispose();
                _leaderLineQuadBuffer = null;

                // -------------------------------------------------
                // Leader-line glow
                // -------------------------------------------------

                _leaderLineGlowVS?.Dispose();
                _leaderLineGlowVS = null;

                _leaderLineGlowPS?.Dispose();
                _leaderLineGlowPS = null;

                _leaderLineGlowGS?.Dispose();
                _leaderLineGlowGS = null;

                // -------------------------------------------------
                // Toggle anchors
                // -------------------------------------------------

                _anchorInstanceBuffer?.Dispose();
                _anchorInstanceBuffer = null;

                _toggleQuadVB?.Dispose();
                _toggleQuadVB = null;

                _toggleVS?.Dispose();
                _toggleVS = null;

                _togglePS?.Dispose();
                _togglePS = null;

                _toggleLayout?.Dispose();
                _toggleLayout = null;

                _toggleSettingsBuffer?.Dispose();
                _toggleSettingsBuffer = null;

                // -------------------------------------------------
                // Drag overlay
                // -------------------------------------------------

                _dragOverlayQuadBuffer?.Dispose();
                _dragOverlayQuadBuffer = null;

                _dragOverlayFillVS?.Dispose();
                _dragOverlayFillVS = null;

                _dragOverlayFillPS?.Dispose();
                _dragOverlayFillPS = null;

                _dragOverlayOutlineVS?.Dispose();
                _dragOverlayOutlineVS = null;

                _dragOverlayOutlinePS?.Dispose();
                _dragOverlayOutlinePS = null;

                _dragOverlayLayout?.Dispose();
                _dragOverlayLayout = null;

                _dragOverlaySettingsBuffer?.Dispose();
                _dragOverlaySettingsBuffer = null;

                // -------------------------------------------------
                // Significant points
                // -------------------------------------------------

                _sigPointVertexBuffer?.Dispose();
                _sigPointVertexBuffer = null;

                _sigPointSettingsBuffer?.Dispose();
                _sigPointSettingsBuffer = null;

                _sigPointLayout?.Dispose();
                _sigPointLayout = null;

                _sigPointVS?.Dispose();
                _sigPointVS = null;

                _sigPointPS?.Dispose();
                _sigPointPS = null;

                _sigPointGS?.Dispose();
                _sigPointGS = null;

                // -------------------------------------------------
                // Pan cache
                // -------------------------------------------------

                _panCacheSrv?.Dispose();
                _panCacheSrv = null;

                _panCacheRtv?.Dispose();
                _panCacheRtv = null;

                _panCacheTexture?.Dispose();
                _panCacheTexture = null;

                _panVertexShader?.Dispose();
                _panVertexShader = null;

                _panPixelShader?.Dispose();
                _panPixelShader = null;

                _panInputLayout?.Dispose();
                _panInputLayout = null;

                _panVertexBuffer?.Dispose();
                _panVertexBuffer = null;

                _panSettingsBuffer?.Dispose();
                _panSettingsBuffer = null;

                _panSampler?.Dispose();
                _panSampler = null;

                // -------------------------------------------------
                // Hit testing
                // -------------------------------------------------

                _hitTestCancellationTokenSource?.Dispose();
                _hitTestCancellationTokenSource = null;

                // -------------------------------------------------
                // State buffers
                // -------------------------------------------------

                StateBuffers?.Dispose();
                StateBuffers = null;
            }

            disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
