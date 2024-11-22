using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Direct3D9;
using SharpDX.DXGI;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using Buffer = SharpDX.Direct3D11.Buffer;
using FeatureLevel = SharpDX.Direct3D.FeatureLevel;

namespace Cad_Point_Manager.Controls.D3DControl
{
    public abstract class D3dControl : System.Windows.Controls.Image
    {
        // - field -----------------------------------------------------------------------
        private SharpDX.Direct3D11.Device device;
        private SwapChain2 swapChain;
        private RenderTargetView renderTargetView;
        private Dx11ImageSource d3DSurface;
        //private DeviceContext deviceContext;
        //private Texture2D texture2D;
        //private RenderTargetView renderTargetView;
        private Buffer vertexBuffer;
        private InputLayout inputLayout;
        private VertexShader vertexShader;
        private PixelShader pixelShader;
        private Buffer constantBuffer;

        private readonly Stopwatch renderTimer = new();

        protected ResourceCache resCache = new();

        private long lastFrameTime = 0;
        private long lastRenderTime = 0;
        private int frameCount = 0;
        private int frameCountHistTotal = 0;
        private Queue<int> frameCountHist = new();

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

        private static readonly DependencyPropertyKey FpsPropertyKey = DependencyProperty.RegisterReadOnly(
            "Fps",
            typeof(int),
            typeof(D3dControl),
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
            typeof(D3dControl),
            new FrameworkPropertyMetadata(2, OnRenderWaitChanged)
            );

        public int RenderWait
        {
            get { return (int)GetValue(RenderWaitProperty); }
            set { SetValue(RenderWaitProperty, value); }
        }

        // - public methods --------------------------------------------------------------

        public D3dControl()
        {
            base.Loaded += Window_Loaded;
            base.Unloaded += Window_Closing;

            base.Stretch = System.Windows.Media.Stretch.Fill;
        }

        public abstract void Render(DeviceContext deviceContext);

        // - event handler ---------------------------------------------------------------

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (D3dControl.IsInDesignMode)
            {
                return;
            }

            StartD3D();
            StartRendering();
        }

        private void Window_Closing(object sender, RoutedEventArgs e)
        {
            if (D3dControl.IsInDesignMode)
            {
                return;
            }

            StopRendering();
            EndD3D();
        }

        private void OnRendering(object sender, EventArgs e)
        {
            if (!renderTimer.IsRunning)
            {
                return;
            }

            PrepareAndCallRender();
            d3DSurface.InvalidateD3DImage();

            lastRenderTime = renderTimer.ElapsedMilliseconds;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            CreateAndBindTargets();
            base.OnRenderSizeChanged(sizeInfo);
        }

        private void OnIsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (d3DSurface.IsFrontBufferAvailable)
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
            var control = (D3dControl)d;
            control.d3DSurface.RenderWait = (int)e.NewValue;
        }

        // - private methods -------------------------------------------------------------

        private void StartD3D()
        {
            var width = Math.Max((int)ActualWidth, 500);
            var height = Math.Max((int)ActualHeight, 500);

            device = new SharpDX.Direct3D11.Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport);

            // describe swap chain
            SwapChainDescription1 swapChainDescription = new()
            {
                AlphaMode = AlphaMode.Premultiplied,
                BufferCount = 2,
                Format = Format.R8G8B8A8_UNorm,
                Height = height,
                Width = width,
                SampleDescription = new SampleDescription(1, 0),
                Scaling = Scaling.Stretch,
                Stereo = false,
                SwapEffect = SwapEffect.FlipSequential,
                Usage = Usage.RenderTargetOutput
            };

            using (var factory4 = new Factory4())
            {
                SwapChain1 swapChain1 = new(factory4, _device, ref swapChainDescription);
                swapChain = swapChain1.QueryInterface<SwapChain2>();
            }

            // Create render target view
            using (var backBuffer = _swapChain.GetBackBuffer<Texture2D>(0))
            {
                _renderTargetView = new RenderTargetView(_device, backBuffer);
            }

            // Create depth stencil view
            var depthBuffer = new Texture2D(_device, new Texture2DDescription
            {
                Format = Format.D32_Float,
                ArraySize = 1,
                MipLevels = 1,
                Width = width,
                Height = height,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil
            });
            _depthStencilView = new DepthStencilView(_device, depthBuffer);

            // Set render targets
            _device.ImmediateContext.OutputMerger.SetRenderTargets(_depthStencilView, _renderTargetView);

            // Create geometry
            CreateGeometry();

            // Set viewport
            _device.ImmediateContext.Rasterizer.SetViewport(0, 0, width, height);


            device = new(DriverType.Hardware, DeviceCreationFlags.BgraSupport);
            resCache.Device = device;

            d3DSurface = new Dx11ImageSource();
            d3DSurface.IsFrontBufferAvailableChanged += OnIsFrontBufferAvailableChanged;

            CreateAndBindTargets();
            
            base.Source = d3DSurface;
        }

        private void EndD3D()
        {
            d3DSurface.IsFrontBufferAvailableChanged -= OnIsFrontBufferAvailableChanged;
            base.Source = null;

            Disposer.SafeDispose(ref d3DSurface);
            Disposer.SafeDispose(ref texture2D);
            Disposer.SafeDispose(ref device);
            Disposer.SafeDispose(ref deviceContext);
        }

        private void CreateAndBindTargets()
        {
            if (d3DSurface == null)
            {
                return;
            }

            var swapChainDescription = new SwapChainDescription
            {
                BufferCount = 1,
                ModeDescription = new ModeDescription(
                (int)ActualWidth, (int)ActualHeight,
                new Rational(60, 1), Format.R8G8B8A8_UNorm),
                IsWindowed = true,
                OutputHandle = new WindowInteropHelper(System.Windows.Application.Current.MainWindow).Handle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            };

            device.CreateWithSwapChain(
                DriverType.Hardware,
                DeviceCreationFlags.BgraSupport,
            swapChainDescription,
                out device,
                out swapChain);
            context = device.ImmediateContext;

            using (var backBuffer = swapChain.GetBackBuffer<Texture2D>(0))
            {
                renderTargetView = new RenderTargetView(device, backBuffer);
            }

            context.OutputMerger.SetRenderTargets(renderTargetView);

            //d3DSurface.SetRenderTarget(null);

            //Disposer.SafeDispose(ref texture2D);

            //var width = Math.Max((int)ActualWidth, 100);
            //var height = Math.Max((int)ActualHeight, 100);

            //var renderDesc = new Texture2DDescription
            //{
            //    BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
            //    Format = Format.B8G8R8A8_UNorm,
            //    Width = width,
            //    Height = height,
            //    MipLevels = 1,
            //    SampleDescription = new SampleDescription(1, 0),
            //    Usage = ResourceUsage.Default,
            //    OptionFlags = ResourceOptionFlags.Shared,
            //    CpuAccessFlags = CpuAccessFlags.None,
            //    ArraySize = 1
            //};

            //texture2D = new Texture2D(device, renderDesc);
            //renderTargetView = new(device, texture2D);
            //device.ImmediateContext.OutputMerger.SetRenderTargets(renderTargetView);
            //d3DSurface.SetRenderTarget(texture2D);
            //device.ImmediateContext.Rasterizer.SetViewport(0, 0, width, height, 0.0f, 1.0f);
        }

        private void StartRendering()
        {
            if (renderTimer.IsRunning)
            {
                return;
            }

            System.Windows.Media.CompositionTarget.Rendering += OnRendering;
            renderTimer.Start();
        }

        private void StopRendering()
        {
            if (!renderTimer.IsRunning)
            {
                return;
            }

            System.Windows.Media.CompositionTarget.Rendering -= OnRendering;
            renderTimer.Stop();
        }

        private void PrepareAndCallRender()
        {
            if (device == null)
            {
                return;
            }

            device.ImmediateContext.ClearRenderTargetView(renderTargetView, SharpDX.Color.Wheat);
            
            CalcFps();

            device.ImmediateContext.Flush();
        }

        private void CalcFps()
        {
            frameCount++;
            if (renderTimer.ElapsedMilliseconds - lastFrameTime > 1000)
            {
                frameCountHist.Enqueue(frameCount);
                frameCountHistTotal += frameCount;
                if (frameCountHist.Count > 5)
                {
                    frameCountHistTotal -= frameCountHist.Dequeue();
                }

                Fps = frameCountHistTotal / frameCountHist.Count;

                frameCount = 0;
                lastFrameTime = renderTimer.ElapsedMilliseconds;
            }
        }
    }
}
