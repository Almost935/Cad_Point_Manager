using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;
using SharpDX.Mathematics.Interop;
using SharpDX.DXGI;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

using Buffer = SharpDX.Direct3D11.Buffer;

using Resource = SharpDX.Direct3D11.Resource;

namespace Cad_Point_Manager.Controls.D3DControl
{
    public abstract class D3dControl : System.Windows.Controls.Image
    {
        // - field -----------------------------------------------------------------------
        private SharpDX.Direct3D11.Device _device;
        private DeviceContext _context;
        private SwapChain2 _swapChain;
        private RenderTargetView _renderTargetView;
        private Texture2D _backBuffer;
        private Dx11ImageSource _d3dImage;
        private IntPtr _renderTargetHandle;


        private Buffer _vertexBuffer;
        private ShaderBytecode _vertexShaderBytecode;
        private ShaderBytecode _pixelShaderBytecode;
        private VertexShader _vertexShader;
        private PixelShader _pixelShader;
        private InputLayout _inputLayout;
        
        private Matrix _projectionMatrix;
        private Matrix _viewMatrix;
        private Matrix _worldMatrix;

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
            _d3dImage.InvalidateD3DImage();

            lastRenderTime = renderTimer.ElapsedMilliseconds;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            CreateAndBindTargets();
            base.OnRenderSizeChanged(sizeInfo);
        }

        private void OnIsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_d3dImage.IsFrontBufferAvailable)
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
            control._d3dImage.RenderWait = (int)e.NewValue;
        }

        // - private methods -------------------------------------------------------------

        private void StartD3D()
        {
            var width = Math.Max((int)ActualWidth, 100);
            var height = Math.Max((int)ActualHeight, 100);

            // Create the Direct3D device and context
            _device = new(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.BgraSupport);
            _context = _device.ImmediateContext;

            //// describe swap chain
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
                Usage = Usage.RenderTargetOutput, 
            };

            using (var factory4 = new Factory4())
            {
                SwapChain1 swapChain1 = new(factory4, _device, ref swapChainDescription);
                _swapChain = swapChain1.QueryInterface<SwapChain2>();
            }

            _backBuffer = Resource.FromSwapChain<Texture2D>(_swapChain, 0);
            _renderTargetView = new RenderTargetView(_device, _backBuffer);
            _context = _device.ImmediateContext;

            // Create D3DImage to display on WPF
            _d3dImage = new();
            _d3dImage.SetRenderTarget(_backBuffer);

            // Set up the projection matrix (Orthographic for 2D)
            _projectionMatrix = Matrix.OrthoLH(width, height, 0.1f, 100f);
            _viewMatrix = Matrix.Identity;
            _worldMatrix = Matrix.Identity;

            // Create vertex buffer, shaders, and input layout
            CreateGeometry();
            CreateShaders();
        }

        private void EndD3D()
        {
            _d3dImage.IsFrontBufferAvailableChanged -= OnIsFrontBufferAvailableChanged;
            base.Source = null;

            Disposer.SafeDispose(ref _d3dImage);
            Disposer.SafeDispose(ref _backBuffer);
            Disposer.SafeDispose(ref _renderTargetView);
            Disposer.SafeDispose(ref _swapChain);
            Disposer.SafeDispose(ref _context);
            Disposer.SafeDispose(ref _device);
            Disposer.SafeDispose(ref _vertexBuffer);
            Disposer.SafeDispose(ref _vertexShaderBytecode);
            Disposer.SafeDispose(ref _pixelShaderBytecode);
            Disposer.SafeDispose(ref _vertexShader);
            Disposer.SafeDispose(ref _pixelShader);
            Disposer.SafeDispose(ref _inputLayout);
        }

        private void CreateAndBindTargets()
        {
            if (_d3dImage == null)
            {
                return;
            }


            using (var backBuffer = _swapChain.GetBackBuffer<Texture2D>(0))
            {
                _renderTargetView = new RenderTargetView(_device, backBuffer);
            }
            _context.OutputMerger.SetRenderTargets(_renderTargetView);

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

        private void CreateGeometry()
        {
            // Define vertices for a line
            Vertex[] vertices = new[]
            { 
            new Vertex(new Vector3(-0.5f, 0.5f, 0.0f), new Color4(1.0f, 0.0f, 0.0f, 1.0f)), // Start
            new Vertex(new Vector3(0.5f, -0.5f, 0.0f), new Color4(0.0f, 1.0f, 0.0f, 1.0f))  // End
        };

            // Create vertex buffer
            _vertexBuffer = Buffer.Create(_device, BindFlags.VertexBuffer, vertices);

            // Create shaders
            CreateShaders();
        }

        private void CreateShaders()
        {
            // Compile the vertex shader
            var vertexShaderByteCode = ShaderBytecode.CompileFromFile("Shaders/VertexShader.hlsl", "VSMain", "vs_5_0");
            _vertexShader = new VertexShader(_device, vertexShaderByteCode);

            // Define the input layout
            var layout = new InputElement[]
            {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 12, 0)
            };

            // Create the input layout
            _inputLayout = new InputLayout(_device, ShaderSignature.GetInputSignature(vertexShaderByteCode), layout);

            // Compile the pixel shader
            var pixelShaderByteCode = ShaderBytecode.CompileFromFile("Shaders/PixelShader.hlsl", "PSMain", "ps_5_0");
            _pixelShader = new PixelShader(_device, pixelShaderByteCode);

            // Set shaders to the device context
            _device.ImmediateContext.InputAssembler.InputLayout = _inputLayout;
            _device.ImmediateContext.VertexShader.Set(_vertexShader);
            _device.ImmediateContext.PixelShader.Set(_pixelShader);
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
            if (_device == null)
            {
                return;
            }

            _device.ImmediateContext.ClearRenderTargetView(_renderTargetView, SharpDX.Color.Wheat);

            CalcFps();

            _device.ImmediateContext.Flush();
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
