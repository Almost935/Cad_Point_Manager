using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.Printing;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using FeatureLevel = SharpDX.Direct3D.FeatureLevel;


namespace Cad_Point_Manager.Controls.D3DControl
{
    public abstract class Direct3DControl : System.Windows.Controls.Image
    {
        #region Fields
        private SharpDX.Direct3D11.Device _device;
        private DeviceContext _deviceContext;
        private Texture2D _texture2D;
        private Texture2D _dxfTexture;
        private RenderTargetView _renderTargetView;
        private RenderTargetView _dxfRenderTargetView;
        private Dx11ImageSource _d3DSurface;
        private SharpDX.Direct2D1.Factory2 _d2dFactory;
        private SharpDX.Direct2D1.Device1 _d2dDevice;
        private SharpDX.Direct2D1.DeviceContext1 _d2dDeviceContext;

        private Texture2D _dxfPreviewTexture;
        private RenderTargetView _dxfPreviewRenderTargetView;
        private Texture2D _combinedPreviewTexture;
        private RenderTargetView _combinedPreviewRenderTargetView;
        private int _previewW, _previewH;

        private readonly Stopwatch _renderTimer = new();

        private long _lastFrameTime = 0;
        private long _lastRenderTime = 0;
        private int _frameCount = 0;
        private int _frameCountHistTotal = 0;
        private Queue<int> _frameCountHist = new();

        private int _rtPixelW = -1;
        private int _rtPixelH = -1;

        private DispatcherTimer _resizeTimer;
        #endregion

        #region Properties
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

        public static readonly DependencyProperty ResCacheProperty = DependencyProperty.Register(
            "ResCache",
            typeof(ResCache),
            typeof(Direct3DControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None)
            );
        public ResCache ResCache
        {
            get { return (ResCache)GetValue(ResCacheProperty); }
            set { SetValue(ResCacheProperty, value); }
        }

        protected int RenderPixelWidth => _rtPixelW;
        protected int RenderPixelHeight => _rtPixelH;
        #endregion

        #region Methods
        public Direct3DControl()
        {
            _resizeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(120)
            };
            _resizeTimer.Tick += (_, __) =>
            {
                _resizeTimer.Stop();
                CreateAndBindTargets();
                InvalidateVisual();
            };

            Loaded += (_, __) =>
            {
                var w = Window.GetWindow(this);
                if (w != null)
                {
                    w.DpiChanged += (_, __) =>
                    {
                        _resizeTimer.Stop();
                        _resizeTimer.Start();
                    };
                }
            };

            base.Loaded += Window_Loaded;
            base.Unloaded += Window_Closing;

            base.Stretch = System.Windows.Media.Stretch.Fill;
        }

        public abstract void Render();

        protected abstract void OnFrontBufferRestored();
        // - event handler ---------------------------------------------------------------
        public int MaxTextureSize => ResCache?.MaxSize ?? 8192;


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
            if (!_renderTimer.IsRunning || !_d3DSurface.IsFrontBufferAvailable)
            {
                return;
            }

            _d3DSurface.Lock();
            PrepareAndCallRender();
            _d3DSurface.InvalidateD3DImage();
            _d3DSurface.Unlock();

            _device.ImmediateContext.Flush();

            _lastRenderTime = _renderTimer.ElapsedMilliseconds;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            if (IsInDesignMode) return;
            if (_d3DSurface == null) return;

            _resizeTimer.Stop();
            _resizeTimer.Start();
        }

        private void OnIsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_d3DSurface.IsFrontBufferAvailable)
            {
                Dx11ImageSource.ResetD3D();
                CreateAndBindTargets();
                OnFrontBufferRestored();

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
            ResCache ??= new ResCache();

            _device = new SharpDX.Direct3D11.Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport);
            ResCache.Device = _device;
            ResCache.MaxSize = GetMaxSize(_device.FeatureLevel);

            var rasterizerStateDescription = new RasterizerStateDescription
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None,
                IsFrontCounterClockwise = false,
                IsMultisampleEnabled = false
            };

            var rasterizerState = new RasterizerState(_device, rasterizerStateDescription);
            _device.ImmediateContext.Rasterizer.State = rasterizerState;

            _deviceContext = _device.ImmediateContext;
            ResCache.DeviceContext = _deviceContext;
            ResCache.WriteFactory = new();

            var baseBlendDesc = new BlendStateDescription();
            baseBlendDesc.RenderTarget[0].IsBlendEnabled = true; // Enable blending
            baseBlendDesc.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
            baseBlendDesc.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
            baseBlendDesc.RenderTarget[0].BlendOperation = BlendOperation.Add;
            baseBlendDesc.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
            baseBlendDesc.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
            baseBlendDesc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
            baseBlendDesc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
            var baseBlendState = new BlendState(_device, baseBlendDesc);
            ResCache.BaseBlendState = baseBlendState;

            // Create the max blend state for hover objects to avoid additive alpha
            var maxDesc = new BlendStateDescription();
            var rt = maxDesc.RenderTarget[0];
            rt.IsBlendEnabled = true;
            // Color channels: One + One with MAX op -> per-pixel maximum
            rt.SourceBlend = BlendOption.One;
            rt.DestinationBlend = BlendOption.One;
            rt.BlendOperation = BlendOperation.Maximum;
            // Alpha channels mirror color (we store alpha in the mask too)
            rt.SourceAlphaBlend = BlendOption.One;
            rt.DestinationAlphaBlend = BlendOption.One;
            rt.AlphaBlendOperation = BlendOperation.Maximum;
            rt.RenderTargetWriteMask = ColorWriteMaskFlags.All;
            ResCache.MaxBlendState = new BlendState(_device, maxDesc);

            _deviceContext.OutputMerger.SetBlendState(ResCache.BaseBlendState);

            _d3DSurface = new Dx11ImageSource();
            _d3DSurface.IsFrontBufferAvailableChanged += OnIsFrontBufferAvailableChanged;

            CreateAndBindTargets();

            InitializeDirect2D();
            InitializeGlyphAtlas();

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
            Disposer.SafeDispose(ref _dxfRenderTargetView);
            Disposer.SafeDispose(ref _dxfTexture);
            Disposer.SafeDispose(ref _d2dFactory);
            Disposer.SafeDispose(ref _d2dDevice);
            Disposer.SafeDispose(ref _d2dDeviceContext);
            Disposer.SafeDispose(ref _dxfPreviewTexture);
            Disposer.SafeDispose(ref _dxfPreviewRenderTargetView);
            Disposer.SafeDispose(ref _combinedPreviewTexture);
            Disposer.SafeDispose(ref _combinedPreviewRenderTargetView);
        }

        private void CreateAndBindTargets()
        {
            if (_d3DSurface == null || !_d3DSurface.IsFrontBufferAvailable) { return; }

            var (w, h) = GetPixelSize();

            // Guard: do nothing unless the *pixel* size changed
            if (w == _rtPixelW && h == _rtPixelH) { return; }

            _rtPixelW = w;
            _rtPixelH = h;

            _d3DSurface.SetRenderTarget(null);

            // 2) Dispose ALL old RTs & textures (not just the on-screen pair)
            Disposer.SafeDispose(ref _dxfRenderTargetView);
            Disposer.SafeDispose(ref _renderTargetView);
            Disposer.SafeDispose(ref _dxfPreviewRenderTargetView);
            Disposer.SafeDispose(ref _combinedPreviewRenderTargetView);

            Disposer.SafeDispose(ref _dxfTexture);
            Disposer.SafeDispose(ref _texture2D);
            Disposer.SafeDispose(ref _dxfPreviewTexture);
            Disposer.SafeDispose(ref _combinedPreviewTexture);

            var width = Math.Max((int)w, 100);
            var height = Math.Max((int)h, 100);

            var texture2DRenderDesc = new Texture2DDescription
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
            _texture2D = new Texture2D(_device, texture2DRenderDesc);

            var offscreenRenderDesc = new Texture2DDescription
            {
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                Format = Format.B8G8R8A8_UNorm,
                Width = width,
                Height = height,
                MipLevels = 1,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                OptionFlags = ResourceOptionFlags.None,
                CpuAccessFlags = CpuAccessFlags.None,
                ArraySize = 1
            };

            _dxfTexture = new Texture2D(_device, offscreenRenderDesc);
            ResCache.DxfTexture = _dxfTexture;

            RenderTargetViewDescription rtvDesc = new RenderTargetViewDescription
            {
                Dimension = RenderTargetViewDimension.Texture2D,
                Format = texture2DRenderDesc.Format,
                Texture2D = { MipSlice = 0 }
            };
            _renderTargetView = new RenderTargetView(_device, _texture2D, rtvDesc);
            ResCache.RenderTargetView = _renderTargetView;

            _dxfRenderTargetView = new(_device, _dxfTexture, rtvDesc);
            ResCache.DxfRenderTargetView = _dxfRenderTargetView;

            _deviceContext.OutputMerger.SetRenderTargets(_renderTargetView);
            ResCache.Texture2D = _texture2D;

            _d3DSurface.Lock();
            _d3DSurface.SetRenderTarget(_texture2D);
            _d3DSurface.Unlock();

            _device.ImmediateContext.Rasterizer.SetViewport(0, 0, width, height, 0.0f, 1.0f);

            OnFrontBufferRestored();

            OnTargetsResized(width, height);
        }
        public void EnsurePreviewTargets(int w, int h, Format fmt = Format.B8G8R8A8_UNorm)
        {
            if (_dxfPreviewTexture is not null && _combinedPreviewTexture is not null
                && _previewW == w && _previewH == h) { return; }

            Disposer.SafeDispose(ref _dxfPreviewTexture);
            Disposer.SafeDispose(ref _dxfPreviewRenderTargetView);
            Disposer.SafeDispose(ref _combinedPreviewTexture);
            Disposer.SafeDispose(ref _combinedPreviewRenderTargetView);

            _previewW = w; _previewH = h;

            var finalDesc = new Texture2DDescription
            {
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                Format = fmt,
                Width = w,
                Height = h,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            _dxfPreviewTexture = new Texture2D(_device, finalDesc);
            ResCache.DxfPreviewTexture = _dxfPreviewTexture;
            _dxfPreviewRenderTargetView = new RenderTargetView(_device, _dxfPreviewTexture);
            ResCache.DxfPreviewRenderTargetView = _dxfPreviewRenderTargetView;

            _combinedPreviewTexture = new Texture2D(_device, finalDesc);
            ResCache.PreviewTexture = _combinedPreviewTexture;
            _combinedPreviewRenderTargetView = new RenderTargetView(_device, _combinedPreviewTexture);
            ResCache.PreviewRenderTargetView = _combinedPreviewRenderTargetView;
        }

        private void InitializeDirect2D()
        {
            Disposer.SafeDispose(ref _d2dFactory);
            Disposer.SafeDispose(ref _d2dDevice);
            Disposer.SafeDispose(ref _d2dDeviceContext);

            using (var dxgiDevice = _device.QueryInterface<SharpDX.DXGI.Device>())
            {
                _d2dFactory = new SharpDX.Direct2D1.Factory2();
                ResCache.D2dFactory = _d2dFactory;
                _d2dDevice = new(_d2dFactory, dxgiDevice);
                ResCache.D2DDevice = _d2dDevice;
                _d2dDeviceContext = new(ResCache.D2DDevice, SharpDX.Direct2D1.DeviceContextOptions.EnableMultithreadedOptimizations);
                ResCache.D2DDeviceContext = _d2dDeviceContext;
            }
        }

        private void InitializeGlyphAtlas()
        {
            ResCache.GlyphTessellator?.Dispose();
            ResCache.GlyphTessellator = new DWriteGlyphTessellator(ResCache.D2dFactory);
            ResCache.CogoPointFontFace?.Dispose();
            ResCache.CogoPointFontFace = ResCache.GetFontFace("Arial", SharpDX.DirectWrite.FontWeight.Normal, SharpDX.DirectWrite.FontStretch.Normal, SharpDX.DirectWrite.FontStyle.Normal);
            ResCache.AsciiGlyphAtlas?.Dispose();
            ResCache.AsciiGlyphAtlas = GlyphAtlas.CreateForAscii(ResCache.Device, ResCache.CogoPointFontFace, ResCache.GlyphTessellator);
            ResCache.AdvanceWidthCache = AdvanceWidthCache.CreateForAscii(ResCache.CogoPointFontFace);
        }

        private void StartRendering()
        {
            if (_renderTimer.IsRunning)
            {
                return;
            }

            IsRendering = true;
            CompositionTarget.Rendering += OnRendering;
            _renderTimer.Start();
        }

        private void StopRendering()
        {
            if (!_renderTimer.IsRunning)
            {
                return;
            }

            IsRendering = false;
            CompositionTarget.Rendering -= OnRendering;
            _renderTimer.Stop();
        }

        private void PrepareAndCallRender()
        {
            if (_device == null)
            {
                return;
            }

            Render();
            CalcFps();
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

        private static int GetMaxSize(FeatureLevel featureLevel)
        {
            switch (featureLevel)
            {
                case FeatureLevel.Level_10_0:
                case FeatureLevel.Level_10_1:
                    return 8192;
                case FeatureLevel.Level_11_0:
                case FeatureLevel.Level_11_1:
                case FeatureLevel.Level_12_0:
                case FeatureLevel.Level_12_1:
                    return 16384;
                default:
                    throw new NotSupportedException("Unsupported feature level");
            }
        }

        private (int w, int h) GetPixelSize()
        {
            var dpi = VisualTreeHelper.GetDpi(this);
            int w = Math.Max((int)Math.Ceiling(ActualWidth * dpi.DpiScaleX), 1);
            int h = Math.Max((int)Math.Ceiling(ActualHeight * dpi.DpiScaleY), 1);

            return (w, h);
        }


        protected virtual void OnTargetsResized(int wPx, int hPx) { }
        #endregion

    }
}
