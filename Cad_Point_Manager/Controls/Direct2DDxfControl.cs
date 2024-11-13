using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Cad_Point_Manager.Controls.D2DControl;

using Point = System.Windows.Point;
using Brush = SharpDX.Direct2D1.Brush;
using SolidColorBrush = SharpDX.Direct2D1.SolidColorBrush;
using Matrix = System.Windows.Media.Matrix;
using Border = System.Windows.Controls.Border;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.DrawingObjects;


namespace Cad_Point_Manager.Controls
{
    public class Direct2DDxfControl : Direct2DControl, INotifyPropertyChanged, IDisposable
    {
        #region Fields
        private const float _zoomFactor = 1.3f;
        private const float _baseLineThickness = 1;
        private const float _baseSnappedThickness = 5;
        private const float _baseHighlightedThickness = 2;

        // Offscreen bitmap fields
        private BitmapRenderTarget _offscreenRenderTarget;
        private OffscreenBitmap _currentOffscreenBitmap;
        private Matrix _currentOffscreenBitmapTransform = new();
        /// <summary>
        /// Represents the size factor of the offscreen bitmap in relation to the screen size.
        /// </summary>
        private const float _offscreenBitmapSizeFactor = 4;
        private bool _offscreenBitmapIsDirty = true;
        private Vector _distFromOffscreenBitmapUpdate = new();
        private Vector _maxDistFromOffscreenBitmapUpdate;
        private (float x, float y) _offscreenBitmapCenteringOffset;
        private bool _updateOffscreenBitmapThreadRunning = false;

        // Zooming and panning matrices
        private Matrix _transformMatrix = new();
        private Matrix _overallMatrix = new();

        // Device Dependent Resources
        private bool _resourcesLoaded = false;
        private Brush _highlightedBrush;
        private Brush _highlightedOuterEdgeBrush;
        private Brush _snappedouterEdgeBrush;

        private bool _isPanning = false;
        private bool _deviceContextIsDirty = true;
        private Point _lastTranslatePos = new();
        private bool _dxfLoaded = false;
        private Rect _currentView;
        private Rect _currentDxfView;
        private List<DrawingObject> _visibleDrawingObjects = new();
        private bool _visibleObjectsDirty = true;
        private int _objectDetailLevelTransitionNum = 500;
        private DrawingObjectTree _drawingObjectTree;
        private bool _clipSet = false;
        private float _lineThickness;
        private float _snappedThickness;
        private float _highlightedThickness;

        // Hit testing fields
        private Point _lastHitTestPos = new();
        private DrawingObjectNode _lastHitTestNode;
        private float _hittestStrokeThickness;

        private Point _pointerCoords = new();
        private Point _dxfPointerCoords = new();
        private Rect _extents = new();
        private DrawingObject _snappedObject;
        private int _currentZoomStep = 0;

        private enum SnapMode { Point, Object };
        private SnapMode _snapMode = SnapMode.Object;
        #endregion

        #region Properties
        public Point PointerCoords
        {
            get { return _pointerCoords; }
            set
            {
                _pointerCoords = value;
                OnPropertyChanged(nameof(PointerCoords));
            }
        }
        public Point DxfPointerCoords
        {
            get { return _dxfPointerCoords; }
            set
            {
                _dxfPointerCoords = value;
                OnPropertyChanged(nameof(DxfPointerCoords));
            }
        }
        /// <summary>
        /// The extents of the drawing objects in the DXF file.
        /// </summary>
        public Rect Extents
        {
            get { return _extents; }
            set
            {
                _extents = value;
                OnPropertyChanged(nameof(Extents));
            }
        }  
        
        public DrawingObject SnappedObject
        {
            get { return _snappedObject; }
            set
            {
                _snappedObject = value;
                OnPropertyChanged(nameof(SnappedObject));
            }
        }
        public int CurrentZoomStep
        {
            get { return _currentZoomStep; }
            set
            {
                _currentZoomStep = value;
                OnPropertyChanged(nameof(CurrentZoomStep));
            }
        }

        public List<DrawingObject> HighlightedObjects { get; set; } = new();
        public Matrix ExtentsMatrix { get; set; } = new();
        public Rect InitialView { get; set; }
        #endregion

        #region Dependency Properties
        public CadManager CadManager
        {
            get { return (CadManager)GetValue(CadManagerProperty); }
            set { SetValue(CadManagerProperty, value); }
        }

        public static readonly DependencyProperty CadManagerProperty =
        DependencyProperty.Register(
            nameof(CadManager),           
            typeof(CadManager),           
            typeof(Direct2DDxfControl),   
            new PropertyMetadata(null, OnCadManagerChanged));
        #endregion

        #region Constructor
        public Direct2DDxfControl()
        {
            _overallMatrix = new((float)_overallMatrix.M11, (float)_overallMatrix.M12, (float)_overallMatrix.M21, (float)_overallMatrix.M22, (float)_overallMatrix.OffsetX, (float)_overallMatrix.OffsetY);

            UpdateDxfCoordsAsync();
            RunHitTestAsync();

            //Window window = Application.Current.MainWindow;
            //window.KeyUp += Window_KeyUp;
        }
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Methods
        private static void OnCadManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as Direct2DDxfControl;
            if (control == null) return;

            if (e.OldValue is CadManager oldCadManager)
            {
                oldCadManager.PropertyChanged -= control.CadManager_PropertyChanged;
            }

            if (e.NewValue is CadManager newCadManager)
            {
                newCadManager.PropertyChanged += control.CadManager_PropertyChanged;
            }
        }

        private void CadManager_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Debug.WriteLine(e.PropertyName);
            if (e.PropertyName == nameof(CadManager.DxfDirty) && CadManager.DxfDirty) { _deviceContextIsDirty = true; _offscreenBitmapIsDirty = true; }
        }

        private void LoadDxfResources(ResourceCache resCache)
        {
            if (CadManager is not null && CadManager.DxfLoaded)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                _dxfLoaded = true;
                Extents = CadManager.Extents;
                ExtentsMatrix = GetInitialMatrix();
                _overallMatrix = ExtentsMatrix;

                UpdateLineThicknesses();

                _hittestStrokeThickness = (float)(8 / ExtentsMatrix.M11);

                CadManager.InitializeDeviceResources(resCache);

                _drawingObjectTree = new(CadManager, Extents, 4);

                _offscreenBitmapIsDirty = true;

                stopwatch.Stop();
                Debug.WriteLine($"LoadDxfResources Elapsed Time: {stopwatch.ElapsedMilliseconds} ms");
            }
        }
        public Matrix GetInitialMatrix()
        {
            if (Extents == Rect.Empty)
            {
                return new Matrix();
            }
            else
            {
                Matrix matrix = new();

                double scaleX = this.ActualWidth / Extents.Width;
                double scaleY = this.ActualHeight / Extents.Height;

                double centerX = Extents.Left - (this.ActualWidth - Extents.Width) * 0.5;
                double centerY = Extents.Top - (this.ActualHeight - Extents.Height) * 0.5;
                matrix.Translate(-centerX, -centerY);

                if (scaleX < scaleY)
                {
                    matrix.ScaleAt(scaleX, -scaleX, this.ActualWidth / 2, this.ActualHeight / 2);
                }
                else
                {
                    matrix.ScaleAt(scaleY, -scaleY, this.ActualWidth / 2, this.ActualHeight / 2);
                }

                return matrix;
            }
        }
        public void GetInitialView()
        {
            double centerX = (Extents.Left + Extents.Right) * 0.5;
            double centerY = (Extents.Top + Extents.Bottom) * 0.5;
            double scaledWidth = Math.Abs(this.ActualWidth / ExtentsMatrix.M11);
            double scaledHeight = Math.Abs(this.ActualHeight / ExtentsMatrix.M22);

            double left = centerX - scaledWidth / 2;
            double top = centerY - scaledHeight / 2;

            InitialView = new(left, top, scaledWidth, scaledHeight);
            _currentDxfView = InitialView;
            _currentView = new(0, 0, ActualWidth, ActualHeight);
        }

        public override void Render()
        {
            if (CadManager is not null)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                if (!_resourcesLoaded)
                {
                    GetResources(d2DDeviceContext);
                    _resourcesLoaded = true;
                }

                if (_offscreenRenderTarget is null)
                {
                    _offscreenRenderTarget = new(d2DDeviceContext, CompatibleRenderTargetOptions.None, new Size2F((float)ActualWidth * _offscreenBitmapSizeFactor,
                    (float)ActualHeight * _offscreenBitmapSizeFactor));
                    _maxDistFromOffscreenBitmapUpdate = new(((_offscreenRenderTarget.Size.Width / 2) - (d2DDeviceContext.Size.Width / 2)), ((_offscreenRenderTarget.Size.Height / 2) - (d2DDeviceContext.Size.Height / 2)));
                    _offscreenBitmapCenteringOffset = ((float)_maxDistFromOffscreenBitmapUpdate.X, (float)_maxDistFromOffscreenBitmapUpdate.Y);
                }
                
                if (!_dxfLoaded)
                {
                    LoadDxfResources(resCache);
                    GetInitialView();

                    UpdateOffscreenRenderTarget();

                    if (!_updateOffscreenBitmapThreadRunning) { RunUpdateOffscreenRenderTargetAsync(); }
                }

                if (!_clipSet) { SetClip(); }
                if (d2DDeviceContext is null) { return; }
                if (d2DDeviceContext.IsDisposed) { return; }

                if (CadManager is not null && d2DDeviceContext is not null && !d2DDeviceContext.IsDisposed && _deviceContextIsDirty)
                {
                    if (_currentOffscreenBitmap is null) { UpdateOffscreenRenderTarget(); }

                    d2DDeviceContext.Clear(new RawColor4(1, 1, 1, 1));

                    RenderOffscreenBitmap(d2DDeviceContext);

                    if (_currentOffscreenBitmap.ZoomStep == _currentZoomStep)
                    {
                        RenderInteractiveObjects(d2DDeviceContext);
                    }

                    _deviceContextIsDirty = false;
                }

                //stopwatch.Stop();
                //Debug.WriteLine($"Render Elapsed Time: {stopwatch.ElapsedMilliseconds} ms");
            }
        }
        private void SetClip()
        {
            var parent = VisualTreeHelper.GetParent(this);
            while (parent is not null)
            {
                if (parent is Border border && border.Name == "dxfBorder")
                {
                    this.Clip = new System.Windows.Media.RectangleGeometry(new Rect(0, 0, border.ActualWidth, border.ActualHeight),
                        border.CornerRadius.TopRight, border.CornerRadius.TopRight);
                    _clipSet = true;
                    break;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
        }
        private async Task RunUpdateOffscreenRenderTargetAsync()
        {
            while (true)
            {
                await Task.Run(() => UpdateOffscreenRenderTarget());
                await Task.Delay(100);
            }
        }
        private void UpdateOffscreenRenderTarget()
        {
            if (_offscreenBitmapIsDirty && _offscreenRenderTarget is not null && !_offscreenRenderTarget.IsDisposed)
            {
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(() => UpdateOffscreenRenderTarget());
                    return;
                }

                //Stopwatch stopwatch = Stopwatch.StartNew();

                _offscreenRenderTarget.BeginDraw();
                _offscreenRenderTarget.Clear(new RawColor4(1, 1, 1, 0));

                int zoomStep = _currentZoomStep;
                Matrix matrix = _overallMatrix;
                matrix.Translate(_offscreenBitmapCenteringOffset.x, _offscreenBitmapCenteringOffset.y); // Translation to center the bitmap in the render target
                RawMatrix3x2 rawMatrix = new((float)matrix.M11, (float)matrix.M12, (float)matrix.M21, (float)matrix.M22, (float)matrix.OffsetX, (float)matrix.OffsetY);
                _offscreenRenderTarget.Transform = rawMatrix;

                float thickness = (float)(_baseLineThickness / _overallMatrix.M11);

                foreach (var layer in CadManager.Layers.Values)
                {
                    if (layer.GeometryGroup is not null)
                    {
                        _offscreenRenderTarget.DrawGeometry(layer.GeometryGroup, layer.LayerBrush, thickness);
                    }
                }

                _offscreenRenderTarget.EndDraw();

                var prevBitmap = _currentOffscreenBitmap;
                _currentOffscreenBitmap = new(zoomStep, _offscreenRenderTarget.Bitmap);
                _distFromOffscreenBitmapUpdate = new();
                _currentOffscreenBitmapTransform = new();
                _deviceContextIsDirty = true;
                prevBitmap?.Dispose();

                // Verify that the bitmap was updated correctly
                if (_currentOffscreenBitmap.ZoomStep == _currentZoomStep) { _offscreenBitmapIsDirty = false; }

                //stopwatch.Stop();
                //Debug.WriteLine($"UpdateOffscreenRenderTarget Elapsed Time: {stopwatch.ElapsedMilliseconds} ms");
            }
        }

        private void RenderOffscreenBitmap(DeviceContext1 deviceContext)
        {
            //Stopwatch stopwatch = Stopwatch.StartNew();

            Matrix matrix = _currentOffscreenBitmapTransform;
            matrix.Translate(-_offscreenBitmapCenteringOffset.x, -_offscreenBitmapCenteringOffset.y); // Translation is to center the bitmap in the render target
            RawMatrix3x2 rawMatrix = new((float)matrix.M11, (float)matrix.M12, (float)matrix.M21, (float)matrix.M22, (float)matrix.OffsetX, (float)matrix.OffsetY);
            deviceContext.Transform = rawMatrix;

            (float X, float Y) sourceRectOffset = new(0, 0);
            RawRectangleF sourceRect = new(0 + sourceRectOffset.X, 0 + sourceRectOffset.Y, _currentOffscreenBitmap.Bitmap.Size.Width + sourceRectOffset.X, _currentOffscreenBitmap.Bitmap.Size.Height + sourceRectOffset.Y);

            deviceContext.DrawBitmap(_currentOffscreenBitmap.Bitmap, 1.0f, BitmapInterpolationMode.Linear, sourceRect);

            //stopwatch.Stop();
            //Debug.WriteLine($"RenderOffscreenBitmap Elapsed Time: {stopwatch.ElapsedMilliseconds} ms");
        }
        private void RenderInteractiveObjects(DeviceContext1 deviceContext)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            deviceContext.Transform = new((float)_overallMatrix.M11, (float)_overallMatrix.M12, (float)_overallMatrix.M21, (float)_overallMatrix.M22, (float)_overallMatrix.OffsetX, (float)_overallMatrix.OffsetY);
            RenderSnappedObjects(deviceContext);
            RenderHighlightedObjects(deviceContext);

            stopwatch.Stop();
            //Debug.WriteLine($"DrawInteractiveObjects Elapsed Time: {stopwatch.ElapsedMilliseconds} ms");
        }
        private void RenderSnappedObjects(DeviceContext1 deviceContext)
        {
            var objCopy = SnappedObject;
            if (objCopy is not null)
            {
                objCopy.DrawToDeviceContext(_snappedThickness, _snappedouterEdgeBrush);
            }
        }
        private void RenderHighlightedObjects(DeviceContext1 deviceContext)
        {
            var copy = HighlightedObjects.ToList();
            foreach (var obj in copy)
            {
                obj.DrawToDeviceContext(_highlightedThickness, _highlightedBrush);
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateDeviceContext(resCache);

            this.Clip = null;
            _clipSet = false;

            _offscreenRenderTarget?.Dispose();
            _offscreenRenderTarget = null;
            _currentOffscreenBitmap?.Dispose();
            _currentOffscreenBitmap = null;

            _highlightedBrush?.Dispose();
            _highlightedBrush = null;
            _highlightedOuterEdgeBrush?.Dispose();
            _highlightedOuterEdgeBrush = null;
            _snappedouterEdgeBrush?.Dispose();
            _snappedouterEdgeBrush = null;

            UpdateLineThicknesses();

            _offscreenBitmapIsDirty = true;
            UpdateOffscreenRenderTarget();
            _deviceContextIsDirty = true;
        }
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            float zoom;
            int zoomStepDelta = Math.Abs(e.Delta / 120);

            if (e.Delta > 0)
            {
                zoom = _zoomFactor * zoomStepDelta;
                CurrentZoomStep += zoomStepDelta;
            }
            else
            {
                zoom = 1 / (_zoomFactor * zoomStepDelta);
                CurrentZoomStep -= zoomStepDelta;
            }

            UpdateZoom(zoom);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            PointerCoords = e.GetPosition(this);

            if (_isPanning)
            {
                var translate = PointerCoords - _lastTranslatePos;

                if (translate.LengthSquared < 1) { return; } //Prevent unneccessary translations

                UpdateTranslate(translate);
                _lastTranslatePos = PointerCoords;
            }
        }
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                _isPanning = true;
                _lastTranslatePos = e.GetPosition(this);
            }
        }
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                _isPanning = false;
            }
            if (e.ChangedButton == MouseButton.Left)
            {
                if (SnappedObject is not null)
                {
                    if (HighlightedObjects.Contains(SnappedObject))
                    {
                        SnappedObject.IsHighlighted = false;
                        HighlightedObjects.Remove(SnappedObject);
                    }
                    else
                    {
                        SnappedObject.IsHighlighted = true;
                        HighlightedObjects.Add(SnappedObject);
                    }
                }
            }
        }
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            _isPanning = false;
        }
        protected override void OnMouseEnter(MouseEventArgs e)
        {
            if (Mouse.MiddleButton == MouseButtonState.Pressed)
            {
                _isPanning = true;
            }
        }
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ResetHighlightedObjects();
            }
        }

        private async void RunHitTestAsync()
        {
            while (true)
            {
                //if (_snapMode == SnapMode.Point)
                //{
                //    await Task.Run(() => HitTestPoints());
                //}
                if (_snapMode == SnapMode.Object)
                {
                    await Task.Run(() => HitTestGeometry());
                }
                await Task.Delay(10);
            }
        }
        private void HitTestGeometry()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => HitTestGeometry());
                return;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();

            if (_drawingObjectTree is null) { return; }
            if (_offscreenBitmapIsDirty) { return; }
            if (_isPanning) { return; }
            if (CadManager is null) { return; }

            var mousePos = DxfPointerCoords;
            var rawMousePos = new RawVector2((float)mousePos.X, (float)mousePos.Y);
            float thickness = (float)(_hittestStrokeThickness / _transformMatrix.M11);

            // Check if mouse is still over the same object
            var snappedCopy = SnappedObject;
            if (snappedCopy is not null)
            {
                if (snappedCopy.Hittest(rawMousePos, thickness))
                {
                    return;
                }
                else
                {
                    ResetSnappedObjects();
                    _deviceContextIsDirty = true;
                }
            }
            if (_lastHitTestNode is null)
            {
                _lastHitTestNode = _drawingObjectTree.GetIntersectingNode(mousePos);
                if (_lastHitTestNode is null) { return; }
            }
            else if (!_lastHitTestNode.Extents.Contains(mousePos))
            {
                _lastHitTestNode = _drawingObjectTree.GetIntersectingNode(mousePos);
                if (_lastHitTestNode is null) { return; }
            }

            Parallel.ForEach(_lastHitTestNode.DrawingObjects, obj =>
            {
                if (obj.Layer.IsVisible && obj.Bounds.Contains(mousePos))
                {
                    if (obj.Hittest(rawMousePos, thickness))
                    {
                        SnappedObject = obj;
                        SnappedObject.IsSnapped = true;
                        _deviceContextIsDirty = true;

                        return;
                    }
                }
            });

            stopwatch.Stop();
        }
        private void HitTestPoints()
        {

        }

        private async void UpdateDxfCoordsAsync()
        {
            while (true)
            {
                await Task.Delay(10);
                await Task.Run(() => UpdateDxfPointerCoords());
            }
        }
        private void UpdateDxfPointerCoords()
        {
            var newMatrix = _overallMatrix;
            newMatrix.Invert();
            DxfPointerCoords = newMatrix.Transform(PointerCoords);
        }
        private void UpdateZoom(float zoom)
        {
            if (!_isPanning)
            {
                _overallMatrix.ScaleAt(zoom, zoom, PointerCoords.X, PointerCoords.Y);
                _transformMatrix.ScaleAt(zoom, zoom, PointerCoords.X, PointerCoords.Y);

                UpdateLineThicknesses();
                ResetSnappedObjects();

                _visibleObjectsDirty = true;
                _deviceContextIsDirty = true;
                _offscreenBitmapIsDirty = true;
            }
        }
        private void UpdateTranslate(Vector translate)
        {
            if (translate.LengthSquared < 1) return; // Prevent unnecessary translations

            _overallMatrix.Translate(translate.X, translate.Y);
            _transformMatrix.Translate(translate.X, translate.Y);
            _currentOffscreenBitmapTransform.Translate(translate.X, translate.Y);
            _distFromOffscreenBitmapUpdate += translate;

            ResetSnappedObjects();

            _visibleObjectsDirty = true;
            _deviceContextIsDirty = true;

            if (Math.Abs(_distFromOffscreenBitmapUpdate.X) > _maxDistFromOffscreenBitmapUpdate.X + 200 ||
                Math.Abs(_distFromOffscreenBitmapUpdate.Y) > _maxDistFromOffscreenBitmapUpdate.Y + 200) { _offscreenBitmapIsDirty = true; }
        }
        private void UpdateLineThicknesses()
        {
            _lineThickness = (float)(_baseLineThickness / _overallMatrix.M11);
            _snappedThickness = (float)(_baseSnappedThickness / _overallMatrix.M11);
            _highlightedThickness = (float)(_baseHighlightedThickness / _overallMatrix.M11);
        }
        private void GetResources(DeviceContext1 deviceContext)
        {
            _highlightedBrush?.Dispose();
            _highlightedOuterEdgeBrush?.Dispose();
            _snappedouterEdgeBrush?.Dispose();

            _highlightedBrush = new SolidColorBrush(deviceContext, new RawColor4((97 / 255), 1.0f, 0.0f, 1.0f));
            _highlightedOuterEdgeBrush = new SolidColorBrush(deviceContext, new RawColor4((97 / 255), 1.0f, 0.0f, 1.0f))
            { Opacity = 0.2f };
            _snappedouterEdgeBrush = new SolidColorBrush(deviceContext, new RawColor4(0.0f, 0.0f, 0.0f, 1.0f))
            { Opacity = 0.2f };

        }
        private void UpdateDeviceContext(ResourceCache resCache)
        {
            if (CadManager is null || resCache is null) { return; }

            foreach (var layer in CadManager.Layers.Values)
            {
                layer.UpdateDeviceDependentResources(resCache);
            }
        }
        public void ZoomToExtents()
        {
            _overallMatrix = ExtentsMatrix;
            _transformMatrix = new();
        }
        public void ResetInteractiveObjects()
        {
            ResetHighlightedObjects();
            ResetSnappedObjects();
        }
        private void ResetHighlightedObjects()
        {
            var copy = HighlightedObjects.ToList();
            foreach (var o in HighlightedObjects)
            {
                o.IsHighlighted = false;
            }
            HighlightedObjects.Clear();
        }
        private void ResetSnappedObjects()
        {
            var snappedCopy = SnappedObject;
            if (snappedCopy is not null)
            {
                snappedCopy.IsSnapped = false;
                SnappedObject = null;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {

        }

        ~Direct2DDxfControl()
        {
            Dispose(false);
        }
        #endregion
    }
}

