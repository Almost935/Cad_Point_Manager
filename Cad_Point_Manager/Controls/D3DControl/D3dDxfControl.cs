using Cad_Point_Manager.Controls.D3DControl.Buffers;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.DrawingObjects3D;
using Cad_Point_Manager.Models.HitTesting;
using Cad_Point_Manager.Models.PointRendering;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DirectWrite;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Transactions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

using Buffer = SharpDX.Direct3D11.Buffer;
using InputElement = SharpDX.Direct3D11.InputElement;
using MapFlags = SharpDX.Direct3D11.MapFlags;
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

        // DxfPoint text shader related fields
        private ResizableBuffer<TextVertex> _pointTextVertexBuffer;
        private Buffer _pointTextSettingsBuffer;
        private VertexShader _pointTextVertexShader;
        private PixelShader _pointTextPixelShader;
        private GeometryShader _pointTextGeometryShader;
        private InputLayout _pointTextInputLayout;
        private int _pointTextVertexCount;
        private bool _pointTextShadersLoaded = false;
        private bool _pointTextVerticesDirty = false;

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

        // Signficant points shader related fields
        private ResizableBuffer<SigPointVertex> _sigPointVertexBuffer;
        private Buffer _sigPointSettingsBuffer;
        private SigPointVertex[] _combinedSigPointVertices = [];
        //private List<SigPointVertex> _snappedSigPointVertices = []; // Removing these in favor of two separate fields to just hold all snapped or selected sig points
        //private List<SigPointVertex> _selectedSigPointVertices = [];
        private InputLayout _sigPointInputLayout;
        private VertexShader _sigPointVertexShader;
        private PixelShader _sigPointPixelShader;
        private GeometryShader _sigPointGeometryShader;
        private bool _sigPointShadersLoaded = false;
        private bool _sigPointVerticesDirty = false;

        // Debugging fields
        private CircleVertex _testVertex;

        // Panning and Zooming Fields
        private float _panThreshold = 1.0f;
        private bool _isPanning;

        // Camera based fields
        private Camera _camera;
        private bool _isShiftPressed;
        private Vector2 _prevMousePos;

        // Interaction Fields
        private Task _hittestTask;
        private bool _hitTestIsRunning;
        private float _hittestStrokeThickness;
        private Point _lastHitTestCoords;
        private CancellationTokenSource _hitTestCancellationTokenSource;
        private List<(double distance, HitTestableObject hitTestableObject)> _nearestHitTestableObjects = [];
        private HitTestableObject _snappedHitTestableObject;
        private List<HitTestableObject> _selectedHitTestableObjects = [];
        private List<HitTestablePoint> _selectedPoints = [];
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
            if (_attachedWindow != null) { _attachedWindow.KeyUp += Window_KeyUp; }
        }
        #endregion

        #region Methods
        public override void Render()
        {
            if (_d3dResCache is null) { return; }

            if (_camera is null)
            {
                GetInitialMatrix();
                _camera = new(Viewport, GlobalHelperProperties._zoomFactor);
            }
            if (DxfNeedsReload)
            {
                GetInitialMatrix();
                _camera.ResetView(_dxfInitialMatrix);
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
            if (_pointTextVerticesDirty) { UpdatePointTextVertices(); }
            if (_circleVerticesDirty) { UpdateCircleVertices(); }
            if (_circleGlowVerticesDirty) { UpdateCircleGlowVertices(); }
            if (_sigPointVerticesDirty) { UpdateSigPointVertices(); }
            if (HitTestableObjectTreeDirty) { LoadHitTestableObjectTree(); }

            if (!_lineShaderLoaded) { InitializeLineShader(); }
            if (!_textShaderLoaded) { InitializeTextShader(); }
            if (!_pointTextShadersLoaded) { InitializePointTextShader(); }
            if (!_circleShadersLoaded) { InitializeCircleShader(); }
            if (!_circleGlowShadersLoaded) { InitializeCircleGlowShader(); }
            if (!_sigPointShadersLoaded) { InitializeSigPointShader(); }
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
            DrawPointTextWithShader();
            DrawCirclesWithShader();
            DrawCircleGlowsWithShader();
            DrawSigPointsWithShader();

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
        private void DrawPointTextWithShader()
        {
            //Stopwatch stopwatch = Stopwatch.StartNew();

            var context = _d3dResCache.DeviceContext;

            if (_pointTextVertexBuffer is null || _pointTextVertexCount == 0) { return; }

            context.VertexShader.Set(_pointTextVertexShader);
            context.PixelShader.Set(_pointTextPixelShader);
            context.InputAssembler.InputLayout = _pointTextInputLayout;
            context.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            context.PixelShader.SetConstantBuffer(0, _pointTextSettingsBuffer);
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(
                 _pointTextVertexBuffer.Buffer, _pointTextVertexBuffer.Stride, 0));

            context.Draw(_pointTextVertexCount, 0);

            //stopwatch.Stop();
            //Debug.WriteLine($"DrawTextWithShader Time: {stopwatch.ElapsedMilliseconds} ms");
        }
        private void DrawCirclesWithShader()
        {
            //Stopwatch stopwatch = Stopwatch.StartNew();

            var context = _d3dResCache.DeviceContext;

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
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(
                _circleVertexBuffer.Buffer, _circleVertexBuffer.Stride, 0));

            context.Draw(_circleVertexCount, 0);

            context.GeometryShader.Set(null);

            //stopwatch.Stop();
            //Debug.WriteLine($"DrawCirclesWithShader Time: {stopwatch.ElapsedMilliseconds} ms");
        }
        private void DrawCircleGlowsWithShader()
        {
            //Stopwatch stopwatch = Stopwatch.StartNew();

            var context = _d3dResCache.DeviceContext;

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
        private void DrawSigPointsWithShader()
        {
            //Stopwatch stopwatch = Stopwatch.StartNew();

            var context = _d3dResCache.DeviceContext;

            if (_sigPointVertexBuffer == null || _combinedSigPointVertices.Length == 0) { return; }

            // Set shaders
            context.VertexShader.Set(_sigPointVertexShader);
            context.GeometryShader.Set(_sigPointGeometryShader);
            context.PixelShader.Set(_sigPointPixelShader);
            context.InputAssembler.InputLayout = _sigPointInputLayout;
            context.VertexShader.SetConstantBuffer(0, _transformationBuffer);
            context.GeometryShader.SetConstantBuffer(0, _transformationBuffer);
            context.GeometryShader.SetConstantBuffer(1, _sigPointSettingsBuffer);
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(
                _sigPointVertexBuffer.Buffer, _sigPointVertexBuffer.Stride, 0));

            context.Draw(_combinedSigPointVertices.Length, 0);
            context.GeometryShader.Set(null);

            //stopwatch.Stop();
            //Debug.WriteLine($"DrawCirclesWithShader Time: {stopwatch.ElapsedMilliseconds} ms");
        }

        private void TestCircleShader()
        {
            // GSMain
            var transformationMatrix = _camera.ViewProjectionMatrix;
            Vector4 center4d = Vector4.Transform(new Vector4(_testVertex.Position, 1), transformationMatrix);
            Vector2 center = new Vector2(center4d.X, center4d.Y);
            float radiusX = _testVertex.Radius * transformationMatrix.M11;
            float radiusY = _testVertex.Radius * transformationMatrix.M22;

            float left = center.X - radiusX;
            float right = center.X + radiusX;
            float top = center.Y + radiusY;
            float bottom = center.Y - radiusY;

            Vector2 topLeft = new Vector2(left, top);
            Vector2 bottomLeft = new Vector2(left, bottom);
            Vector2 topRight = new Vector2(right, top);
            Vector2 bottomRight = new Vector2(right, bottom);

            // PSMain
            //float2 dist = length(input.position.xy - input.center);
            //if (dist.x > input.radiusX || dist.y > input.radiusY)
            //{
            //    discard;
            //}

            Vector2 testDist = topLeft - center;

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
            if (_lineGlowVertexBuffer == null || _lineGlowVertices == null || _lineGlowVertices.Count == 0)
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
            if (_textGlowVertexBuffer == null || _textGlowVertices.Count == 0)
            {
                _textGlowVerticesDirty = false;
                return;
            }

            var context = _d3dResCache.DeviceContext;
            _textGlowVertexBuffer.Update(context, _textGlowVertices.ToArray());

            _textGlowVerticesDirty = false;
            D3dIsDirty = true;
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
            D3dIsDirty = true;
        }
        private void UpdateCircleVertices()
        {
            if (_circleVertexBuffer is null) { return; }

            var context = _d3dResCache.DeviceContext;
            var vertexSpan = CadManager3D.UpdateCircleVerticesList();
            _circleVertexBuffer.Update(context, vertexSpan);
            _circleVertexCount = vertexSpan.Length;

            _circleVerticesDirty = false;
            D3dIsDirty = true;
        }
        private void UpdateCircleGlowVertices()
        {
            if (_circleGlowVertexBuffer is null || _circleGlowVertices.Count == 0) { return; }

            var context = _d3dResCache.DeviceContext;
            _circleGlowVertexBuffer.Update(context, _circleGlowVertices.ToArray());

            _circleGlowVerticesDirty = false;
            D3dIsDirty = true;
        }
        private void UpdateSigPointVertices()
        {
            _combinedSigPointVertices = _selectedSigPointVertices.Concat(_snappedSigPointVertices).ToArray();

            if (_sigPointVertexBuffer is null || _combinedSigPointVertices.Length == 0) { return; }

            Debug.WriteLine($"\n_snappedSigPointVertices: _snappedSigPointVertices.Count: {_snappedSigPointVertices.Count}");
            foreach (var sigPointVertex in _snappedSigPointVertices)
            {
                Debug.WriteLine($"IsSelected: {sigPointVertex.IsSelected}");
            }
            Debug.WriteLine($"_selectedSigPointVertices: _selectedSigPointVertices.Count: {_selectedSigPointVertices.Count}");
            foreach (var sigPointVertex in _selectedSigPointVertices)
            {
                Debug.WriteLine($"IsSelected: {sigPointVertex.IsSelected}");
            }

            var context = _d3dResCache.DeviceContext;
            _sigPointVertexBuffer.Update(context, _combinedSigPointVertices);

            _sigPointVerticesDirty = false;
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

            _pointTextShadersLoaded = true;
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
        private void InitializeSigPointShader()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            while (Path.GetFileName(path) != "Cad_Point_Manager")
            {
                path = Path.GetDirectoryName(path);
                if (path == null)
                    throw new DirectoryNotFoundException("The 'Cad_Point_Manager' directory could not be found in the path.");
            }

            string shaderPath = Path.Combine(path, @"Controls\D3DControl\Shaders\SigPointShader.hlsl");

            // Main shaders
            var sigPointVSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "VSMain", "vs_5_0");
            _sigPointVertexShader = new VertexShader(_d3dResCache.Device, sigPointVSBytecode);

            var sigPointPSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "PSMain", "ps_5_0");
            _sigPointPixelShader = new PixelShader(_d3dResCache.Device, sigPointPSBytecode);

            var sigPointGSBytecode = ShaderBytecode.CompileFromFile(shaderPath, "GSMain", "gs_5_0");
            _sigPointGeometryShader = new GeometryShader(_d3dResCache.Device, sigPointGSBytecode);

            _sigPointInputLayout = new(
                _d3dResCache.Device,
                ShaderSignature.GetInputSignature(sigPointVSBytecode),
                new[]
                {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                    new InputElement("ISMOUSEOVER", 0, Format.R32G32B32A32_Float, 12, 0),
                    new InputElement("ISSELECTED", 0, Format.R32_Float, 16, 0),
                 });

            _sigPointShadersLoaded = true;
        }
        private void InitializeBuffers()
        {
            var device = _d3dResCache.Device;

            _lineVertexBuffer?.Dispose();
            _lineVertexBuffer = new(device, GlobalHelperProperties._initialLineVertices);

            _lineGlowVertexBuffer?.Dispose();
            _lineGlowVertexBuffer = new(device, GlobalHelperProperties._initialLineGlowVertices);

            _textVertexBuffer?.Dispose();
            _textVertexBuffer = new(device, GlobalHelperProperties._initialTextVertices);

            _textGlowVertexBuffer?.Dispose();
            _textGlowVertexBuffer = new(device, GlobalHelperProperties._initialTextGlowVertices);

            _pointTextVertexBuffer?.Dispose();
            _pointTextVertexBuffer = new(device, GlobalHelperProperties._initialTextVertices);

            _circleVertexBuffer?.Dispose();
            _circleVertexBuffer = new(device, GlobalHelperProperties._initialCircleVertices);

            _circleGlowVertexBuffer?.Dispose();
            _circleGlowVertexBuffer = new(device, GlobalHelperProperties._initialCircleGlowVertices);

            _sigPointVertexBuffer?.Dispose();
            _sigPointVertexBuffer = new(device, GlobalHelperProperties._initialCircleGlowVertices);
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

            var sigPointBufferDesc = new BufferDescription
            {
                Usage = ResourceUsage.Default,
                SizeInBytes = Utilities.SizeOf<SigPointSettingsBuffer>(),
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            _sigPointSettingsBuffer = new Buffer(_d3dResCache.Device, sigPointBufferDesc);

            ConstantBuffersInitialized = true;
            ConstantBuffersDirty = true;
        }
        private void UpdateConstantBuffers()
        {
            var transformation = _camera.ViewProjectionMatrix;
            var transformationBuffer = new TransformationBuffer
            {
                WorldViewProjection = transformation
            };
            _d3dResCache.DeviceContext.UpdateSubresource(ref transformationBuffer, _transformationBuffer);

            var worldUnitsPerPixel = _camera.GetWorldUnitsPerPixel();

            var lineSettings = new LineSettingsBuffer
            {
                SelectedColor = GlobalHelperProperties._selectedObjectColor,
                SelectedMouseOverColor = GlobalHelperProperties._selectedMouseOverObjectColor
            };
            _d3dResCache.DeviceContext.UpdateSubresource(ref lineSettings, _lineSettingsBuffer);

            var lineGlowSettings = new LineGlowSettingsBuffer
            {
                GlowOffset = GlobalHelperProperties._lineGlowPixelWidth * worldUnitsPerPixel,
                GlowTransparency = GlobalHelperProperties._lineGlowTransparency,
                SelectedColor = GlobalHelperProperties._selectedObjectColor,
                SelectedMouseOverColor = GlobalHelperProperties._selectedMouseOverGlowColor
            };
            _d3dResCache.DeviceContext.UpdateSubresource(ref lineGlowSettings, _lineGlowSettingsBuffer);

            var textSettings = new TextSettingsBuffer
            {
                SelectedColor = GlobalHelperProperties._selectedObjectColor,
                SelectedMouseOverColor = GlobalHelperProperties._selectedMouseOverObjectColor
            };
            _d3dResCache.DeviceContext.UpdateSubresource(ref textSettings, _textSettingsBuffer);

            var textGlowSettings = new TextGlowSettingsBuffer
            {
                GlowOffset = GlobalHelperProperties._lineGlowPixelWidth * worldUnitsPerPixel,
                GlowTransparency = GlobalHelperProperties._lineGlowTransparency,
                SelectedColor = GlobalHelperProperties._selectedObjectColor,
                SelectedMouseOverColor = GlobalHelperProperties._selectedMouseOverGlowColor
            };
            _d3dResCache.DeviceContext.UpdateSubresource(ref textGlowSettings, _textGlowSettingsBuffer);

            var pointTextSettings = new TextSettingsBuffer
            {
                SelectedColor = GlobalHelperProperties._selectedObjectColor,
                SelectedMouseOverColor = GlobalHelperProperties._selectedMouseOverObjectColor
            };
            _d3dResCache.DeviceContext.UpdateSubresource(ref pointTextSettings, _pointTextSettingsBuffer);

            var circleSettings = new CircleSettingsBuffer
            {
                SelectedColor = GlobalHelperProperties._selectedObjectColor,
                SelectedMouseOverColor = GlobalHelperProperties._selectedMouseOverObjectColor
            };
            _d3dResCache.DeviceContext.UpdateSubresource(ref circleSettings, _circleSettingsBuffer);

            var circleGlowSettings = new CircleGlowSettingsBuffer
            {
                GlowOffset = GlobalHelperProperties._lineGlowPixelWidth * worldUnitsPerPixel,
                GlowTransparency = GlobalHelperProperties._lineGlowTransparency,
                SelectedColor = GlobalHelperProperties._selectedObjectColor,
                SelectedMouseOverColor = GlobalHelperProperties._selectedMouseOverGlowColor
            };
            _d3dResCache.DeviceContext.UpdateSubresource(ref circleGlowSettings, _circleGlowSettingsBuffer);

            var sigPointsSettings = new SigPointSettingsBuffer
            {
                BaseColor = GlobalHelperProperties._sigPointColor,
                SelectedColor = GlobalHelperProperties._selectedObjectColor,
                SelectedMouseOverColor = GlobalHelperProperties._selectedMouseOverObjectColor,
                Radius = GlobalHelperProperties._sigPointRadiusInPixels,
                ViewportSize = new Vector2(Viewport.Width, Viewport.Height),
            };
            _d3dResCache.DeviceContext.UpdateSubresource(ref sigPointsSettings, _sigPointSettingsBuffer);

            ConstantBuffersDirty = false;
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
                    _hittestStrokeThickness = 7.0f / (_camera.InitialViewMatrix.M11 * _camera.CurrentZoom);

                    ConstantBuffersDirty = true;
                }
            }
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

                _lineGlowVerticesDirty = true;
                D3dIsDirty = true;
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
                    _camera.Pan(currentMousePos, _prevMousePos);
                    ConstantBuffersDirty = true;
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
            _hittestStrokeThickness = 7.0f / (_camera.InitialViewMatrix.M11 * _camera.CurrentZoom);

            ConstantBuffersDirty = true;
            D3dIsDirty = true;
            //e.Handled = true;
        }
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            _isMouseInside = false;
            _hitTestCancellationTokenSource.Cancel();
            _isPanning = false;

            ResetSnappedObjects();

            _lineGlowVerticesDirty = true;
            D3dIsDirty = true;
        }
        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);

            _isMouseInside = true;
            _hitTestCancellationTokenSource = new CancellationTokenSource();
            _ = RunHitTestingAsync();
        }
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (_snappedHitTestableObject is not null)
            {
                if (_snappedHitTestableObject.IsSelected)
                {
                    DeselectObject(_snappedHitTestableObject);

                    if (_snappedHitTestableObject is DrawingGeometry3D) { _lineVerticesDirty = true; _lineGlowVerticesDirty = true; }
                    else if (_snappedHitTestableObject is DrawingMtext3D) { _textVerticesDirty = true; }
                    else if (_snappedHitTestableObject is HitTestablePoint point) { _sigPointVerticesDirty = true; }
                    else { _lineVerticesDirty = true; _textVerticesDirty = true; }

                    D3dIsDirty = true;
                }
                else
                {
                    SelectObject(_snappedHitTestableObject);

                    if (_snappedHitTestableObject is DrawingGeometry3D) { _lineVerticesDirty = true; _lineGlowVerticesDirty = true; }
                    else if (_snappedHitTestableObject is DrawingMtext3D) { _textVerticesDirty = true; }
                    else if (_snappedHitTestableObject is HitTestablePoint point) { _sigPointVerticesDirty = true; }
                    else { _lineVerticesDirty = true; _textVerticesDirty = true; }

                    D3dIsDirty = true;
                }
            }
        }
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ResetSelectedObjects();
            }
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
                ConstantBuffersDirty = true;
            }

            D3dIsDirty = true;
        }

        public void ZoomToExtents()
        {
            if (_camera is null) { return; }

            _camera.ResetView(_dxfInitialMatrix);
            ResetSnappedObjects();

            ConstantBuffersDirty = true;
            _lineGlowVerticesDirty = true;
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

            bool d3dIsDirty = false;
            Rect rect = new(_lastHitTestCoords.X - _hittestStrokeThickness, _lastHitTestCoords.Y - _hittestStrokeThickness, 
                _hittestStrokeThickness * 2, _hittestStrokeThickness * 2);
            var snappedCopy = _snappedHitTestableObject;
            var oldSnappedObject = _snappedHitTestableObject;

            if (snappedCopy is not null)
            {
                if (snappedCopy.DistanceToPoint(_lastHitTestCoords) > _hittestStrokeThickness)
                {
                    ResetSnappedObjects();

                    _nearestHitTestableObjects = CadManager3D.GetNearestHitTestableObjects(_lastHitTestCoords, _hittestStrokeThickness);

                    if (_nearestHitTestableObjects.Count > 0)
                    {
                        var (distance, obj) = _nearestHitTestableObjects.First();

                        if (distance <= _hittestStrokeThickness)
                        {
                            _snappedHitTestableObject = obj;
                            SnapObject(_snappedHitTestableObject);
                        }
                    }

                    d3dIsDirty = true;
                }
                else
                {
                    return;
                }
            }
            else
            {
                _nearestHitTestableObjects = CadManager3D.GetNearestHitTestableObjects(_lastHitTestCoords, _hittestStrokeThickness);

                if (_nearestHitTestableObjects.Count > 0)
                {
                    var (distance, obj) = _nearestHitTestableObjects.First();

                    if (distance <= _hittestStrokeThickness)
                    {
                        _snappedHitTestableObject = obj;
                        SnapObject(_snappedHitTestableObject);

                        d3dIsDirty = true;
                    }
                }
            }

            SetVerticesDirty(_snappedHitTestableObject);
            SetVerticesDirty(oldSnappedObject);
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

                    if (obj is DrawingText3D drawingText3D)
                    {
                        drawingText3D.MouseEnter();
                        CadManager3D.UpdateVerticesIsMouseOver(drawingText3D, true);
                        _textGlowVertices.AddRange(drawingText3D.TextVertices);
                    }
                }
                if (hitTestableObject is DxfPoint dxfPoint)
                {
                    dxfPoint.MouseEnter();
                    CadManager3D.UpdateVerticesIsMouseOver(dxfPoint, true);
                    _textGlowVertices.AddRange(dxfPoint.TextVertices);
                    _circleGlowVertices.AddRange(dxfPoint.MarkerVertices);
                }
                if (hitTestableObject is HitTestablePoint point)
                {
                    point.MouseEnter();
                    _snappedSigPointVertices.Add(point.SigPointVertex);
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
                if (hitTestableObject is DxfPoint dxfPoint)
                {
                    dxfPoint.MouseEnter();
                    CadManager3D.UpdateVerticesIsMouseOver(dxfPoint, false);
                }
                if (hitTestableObject is HitTestablePoint point)
                {
                    point.MouseLeave();
                    _snappedSigPointVertices.Remove(point.SigPointVertex);
                }
            }
        }
        private void ResetSnappedObjects()
        {
            if (_snappedHitTestableObject is not null)
            {
                UnsnapObject(_snappedHitTestableObject);
                _snappedHitTestableObject = null;
            }
            _lineGlowVertices.Clear();
            _textGlowVertices.Clear();
            _circleGlowVertices.Clear();
            _snappedSigPointVertices.Clear();
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
                if (hitTestableObject is DxfPoint dxfPoint)
                {
                    
                }
                if (hitTestableObject is HitTestablePoint point)
                {
                    point.Select();
                    _selectedSigPointVertices.Add(point.SigPointVertex);
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
                if (hitTestableObject is DxfPoint dxfPoint)
                {

                }
                if (hitTestableObject is HitTestablePoint point)
                {
                    point.Deselect();
                    _selectedSigPointVertices.Remove(point.SigPointVertex);
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

            _selectedSigPointVertices.Clear();

            _lineVerticesDirty = _lineGlowVerticesDirty = _textVerticesDirty = D3dIsDirty = true;
        }

        private void SetVerticesDirty(HitTestableObject hitTestableObject)
        {
            if (hitTestableObject is not null)
            {
                if (hitTestableObject is DrawingGeometry3D)
                {
                    _lineVerticesDirty = true;
                    _lineGlowVerticesDirty = true;
                }
                if (hitTestableObject is DrawingBlock3D)
                {
                    _lineVerticesDirty = true;
                    _lineGlowVerticesDirty = true;
                    _textVerticesDirty = true;
                    _textGlowVerticesDirty = true;
                }
                if (hitTestableObject is DrawingText3D)
                {
                    _textVerticesDirty = true;
                    _textGlowVerticesDirty = true;
                }
                if (hitTestableObject is DxfPoint)
                {
                    _circleVerticesDirty = true;
                    _circleGlowVerticesDirty = true;
                    _pointTextVerticesDirty = true;
                    _textGlowVerticesDirty = true;
                }
                if (hitTestableObject is HitTestablePoint)
                {
                    _sigPointVerticesDirty = true;
                }
            }
        }

        private void ClearDxf()
        {
            _camera.ResetView(Matrix.Identity);
            ResetSnappedObjects();
            _lineVerticesDirty = true;
            _textVerticesDirty = true;
            _lineGlowVerticesDirty = true;
            _pointTextVerticesDirty = true;
            _circleVerticesDirty = true;
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
            if (e.PropertyName == nameof(CadManager3D.DxfNeedsReload))
            {
                DxfNeedsReload = CadManager3D.DxfNeedsReload;
            }
            if (e.PropertyName == nameof(CadManager3D.LineVerticesDirty))
            {
                _lineVerticesDirty = CadManager3D.LineVerticesDirty;
            }
            if (e.PropertyName == nameof(CadManager3D.TextVerticesDirty))
            {
                _textVerticesDirty = CadManager3D.TextVerticesDirty;
            }
            if (e.PropertyName == nameof(CadManager3D.PointTextVerticesDirty))
            {
                _pointTextVerticesDirty = CadManager3D.PointTextVerticesDirty;
            }
            if (e.PropertyName == nameof(CadManager3D.PointCircleVerticesDirty))
            {
                _circleVerticesDirty = CadManager3D.PointCircleVerticesDirty;
            }
            if (e.PropertyName == nameof(CadManager3D.HitTestableObjectTreeDirty))
            {
                HitTestableObjectTreeDirty = CadManager3D.HitTestableObjectTreeDirty;
            }
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

                    _pointTextVertexBuffer?.Dispose();
                    _pointTextVertexBuffer = null;

                    _pointTextSettingsBuffer?.Dispose();
                    _pointTextSettingsBuffer = null;

                    _pointTextVertexShader?.Dispose();
                    _pointTextVertexShader = null;

                    _pointTextPixelShader?.Dispose();
                    _pointTextPixelShader = null;

                    _pointTextInputLayout?.Dispose();
                    _pointTextInputLayout = null;

                    _circleVertexBuffer?.Dispose();
                    _circleVertexBuffer = null;

                    _circleVertexShader?.Dispose();
                    _circleVertexShader = null;

                    _circlePixelShader?.Dispose();
                    _circlePixelShader = null;

                    _circleGeometryShader?.Dispose();
                    _circleGeometryShader = null;

                    _circleInputLayout?.Dispose();
                    _circleInputLayout = null;

                    _circleSettingsBuffer?.Dispose();
                    _circleSettingsBuffer = null;

                    _circleGlowVertexBuffer?.Dispose();
                    _circleGlowVertexBuffer = null;

                    _circleGlowSettingsBuffer?.Dispose();
                    _circleGlowSettingsBuffer = null;

                    _circleGlowVertexBuffer?.Dispose();
                    _circleGlowVertexBuffer = null;

                    _circleGlowVertexShader?.Dispose();
                    _circleGlowVertexShader = null;

                    _circleGlowPixelShader?.Dispose();
                    _circleGlowPixelShader = null;

                    _circleGlowGeometryShader?.Dispose();
                    _circleGlowGeometryShader = null;

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
