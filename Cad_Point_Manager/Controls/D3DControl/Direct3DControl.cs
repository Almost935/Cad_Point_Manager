using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

using FeatureLevel = SharpDX.Direct3D.FeatureLevel;


namespace Cad_Point_Manager.Controls.D3DControl
{
    public abstract class Direct3DControl : System.Windows.Controls.Image
    {
        // - field -----------------------------------------------------------------------
        private SharpDX.Direct3D11.Device _device;
        private DeviceContext _deviceContext;
        private Texture2D _texture2D;
        private RenderTargetView _renderTargetView;
        private Dx11ImageSource _d3DSurface;

        private readonly Stopwatch _renderTimer = new();

        protected D3dResCache _d3dResCache = new();

        private long _lastFrameTime = 0;
        private long _lastRenderTime = 0;
        private int _frameCount = 0;
        private int _frameCountHistTotal = 0;
        private Queue<int> _frameCountHist = new();

        // - property --------------------------------------------------------------------
        public static bool IsInDesignMode
        {
            get
            {
                var prop = DesignerProperties.IsInDesignModeProperty;
                var isDesignMode = (bool)DependencyPropertyDescriptor.FromProperty(prop, typeof(FrameworkElement)).Metadata.DefaultValue;
                return isDesignMode;
            }
        }
        public bool IsRendering = false;

        private static readonly DependencyPropertyKey FpsPropertyKey = DependencyProperty.RegisterReadOnly(
            "Fps",
            typeof(int),
            typeof(Direct3DControl),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.None)
            );

        public static readonly DependencyProperty FpsProperty = FpsPropertyKey.DependencyProperty;

        public int Fps
        {
            get { return (int)GetValue(FpsProperty); }
            protected set { SetValue(FpsPropertyKey, value); }
        }

        public static DependencyProperty RenderWaitProperty = DependencyProperty.Register(
            "RenderWait",
            typeof(int),
            typeof(Direct3DControl),
            new FrameworkPropertyMetadata(2, OnRenderWaitChanged)
            );

        public int RenderWait
        {
            get { return (int)GetValue(RenderWaitProperty); }
            set { SetValue(RenderWaitProperty, value); }
        }

        // - public methods --------------------------------------------------------------

        public Direct3DControl()
        {
            base.Loaded += Window_Loaded;
            base.Unloaded += Window_Closing;

            base.Stretch = System.Windows.Media.Stretch.Fill;
        }

        public abstract void Render();

        // - event handler ---------------------------------------------------------------

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Direct3DControl.IsInDesignMode)
            {
                return;
            }

            StartD3D();
            StartRendering();
        }

        private void Window_Closing(object sender, RoutedEventArgs e)
        {
            if (Direct3DControl.IsInDesignMode)
            {
                return;
            }

            StopRendering();
            EndD3D();
        }

        private void OnRendering(object sender, EventArgs e)
        {
            if (!_renderTimer.IsRunning)
            {
                return;
            }
            PrepareAndCallRender();
            _d3DSurface.InvalidateD3DImage();

            _lastRenderTime = _renderTimer.ElapsedMilliseconds;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            CreateAndBindTargets();
            base.OnRenderSizeChanged(sizeInfo);
        }

        private void OnIsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_d3DSurface.IsFrontBufferAvailable)
            {
                StartRendering();
            }
            else
            {
                StopRendering();
            }
        }

        private static void OnRenderWaitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (Direct3DControl)d;
            control._d3DSurface.RenderWait = (int)e.NewValue;
        }

        // - private methods -------------------------------------------------------------

        private void StartD3D()
        {
            _device = new SharpDX.Direct3D11.Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport);
            _d3dResCache.Device = _device;

            var rasterizerStateDescription = new RasterizerStateDescription
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.Back,
                IsFrontCounterClockwise = false,
                IsMultisampleEnabled = false
            };

            var rasterizerState = new RasterizerState(_device, rasterizerStateDescription);
            _device.ImmediateContext.Rasterizer.State = rasterizerState;

            _deviceContext = _device.ImmediateContext;
            _d3dResCache.DeviceContext = _deviceContext;

            _d3DSurface = new Dx11ImageSource();
            _d3DSurface.IsFrontBufferAvailableChanged += OnIsFrontBufferAvailableChanged;

            CreateAndBindTargets();

            base.Source = _d3DSurface;
        }

        private void EndD3D()
        {
            _d3DSurface.IsFrontBufferAvailableChanged -= OnIsFrontBufferAvailableChanged;
            base.Source = null;

            Disposer.SafeDispose(ref _d3DSurface);
            Disposer.SafeDispose(ref _device);
            Disposer.SafeDispose(ref _deviceContext);
            Disposer.SafeDispose(ref _texture2D);
            Disposer.SafeDispose(ref _renderTargetView);
        }

        private void CreateAndBindTargets()
        {
            if (_d3DSurface == null)
            {
                return;
            }

            _d3DSurface.SetRenderTarget(null);

            Disposer.SafeDispose(ref _texture2D);
            Disposer.SafeDispose(ref _renderTargetView);

            var width = Math.Max((int)ActualWidth, 100);
            var height = Math.Max((int)ActualHeight, 100);

            var renderDesc = new Texture2DDescription
            {
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                Format = Format.B8G8R8A8_UNorm,
                Width = width,
                Height = height,
                MipLevels = 1,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                OptionFlags = ResourceOptionFlags.Shared,
                CpuAccessFlags = CpuAccessFlags.None,
                ArraySize = 1
            };

            _texture2D = new Texture2D(_device, renderDesc);

            _renderTargetView = new RenderTargetView(_device, _texture2D);
            _d3dResCache.RenderTargetView = _renderTargetView;

            _deviceContext.OutputMerger.SetRenderTargets(_renderTargetView);
            _d3dResCache.Texture2D = _texture2D;
            _d3DSurface.SetRenderTarget(_texture2D);

            _device.ImmediateContext.Rasterizer.SetViewport(0, 0, width, height, 0.0f, 1.0f);

            InitializeDirect2D();
        }
        private void InitializeDirect2D()
        {
            using (var dxgiDevice = _device.QueryInterface<SharpDX.DXGI.Device>())
            {
                var d2dFactory = new SharpDX.Direct2D1.Factory1();
                _d3dResCache.D2DDevice = new SharpDX.Direct2D1.Device(d2dFactory, dxgiDevice);
                _d3dResCache.D2DDeviceContext = new SharpDX.Direct2D1.DeviceContext(_d3dResCache.D2DDevice, SharpDX.Direct2D1.DeviceContextOptions.None);
            }

            var bitmapProperties = new SharpDX.Direct2D1.BitmapProperties1(
                new SharpDX.Direct2D1.PixelFormat(SharpDX.DXGI.Format.B8G8R8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Premultiplied),
                dpiX: 96, dpiY: 96,
                bitmapOptions: SharpDX.Direct2D1.BitmapOptions.Target | SharpDX.Direct2D1.BitmapOptions.CannotDraw);

            using (var surface = _texture2D.QueryInterface<SharpDX.DXGI.Surface>())
            {
                _d3dResCache.D2DTargetBitmap = new SharpDX.Direct2D1.Bitmap1(_d3dResCache.D2DDeviceContext, surface, bitmapProperties);
            }

            _d3dResCache.D2DDeviceContext.Target = _d3dResCache.D2DTargetBitmap;
            _d3dResCache.D2DDeviceContext.TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode.Grayscale;
        }

        private void StartRendering()
        {
            if (_renderTimer.IsRunning)
            {
                return;
            }

            IsRendering = true;
            System.Windows.Media.CompositionTarget.Rendering += OnRendering;
            _renderTimer.Start();
        }

        private void StopRendering()
        {
            if (!_renderTimer.IsRunning)
            {
                return;
            }

            IsRendering = false;
            System.Windows.Media.CompositionTarget.Rendering -= OnRendering;
            _renderTimer.Stop();
        }

        private void PrepareAndCallRender()
        {
            if (_device == null)
            {
                return;
            }

            _d3dResCache.D2DDeviceContext.BeginDraw();

            Render();

            _d3dResCache.D2DDeviceContext.EndDraw();

            CalcFps();

            _device.ImmediateContext.Flush();
        }

        private void CalcFps()
        {
            _frameCount++;
            if (_renderTimer.ElapsedMilliseconds - _lastFrameTime > 1000)
            {
                _frameCountHist.Enqueue(_frameCount);
                _frameCountHistTotal += _frameCount;
                if (_frameCountHist.Count > 5)
                {
                    _frameCountHistTotal -= _frameCountHist.Dequeue();
                }

                Fps = _frameCountHistTotal / _frameCountHist.Count;

                _frameCount = 0;
                _lastFrameTime = _renderTimer.ElapsedMilliseconds;
            }
        }
    }
}
