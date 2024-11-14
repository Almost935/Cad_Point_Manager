using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using Device = SharpDX.Direct3D11.Device;
using Format = SharpDX.DXGI.Format;
using System.Windows.Media;
using System.Windows.Threading;
using SharpDX.DXGI;
using SharpDX.Direct3D;

namespace Cad_Point_Manager.Controls
{
    public class Direct3DControl : UserControl
    {
        private Device device;
        private DeviceContext context;
        private SwapChain swapChain;
        private RenderTargetView renderTargetView;
        private bool isRendering;
        private Task renderTask;
        private CancellationTokenSource cancellationTokenSource;
        private DispatcherTimer renderTimer;

        public Direct3DControl()
        {
            this.Loaded += Direct3DControl_Loaded;
            this.Unloaded += Direct3DControl_Unloaded;
        }

        private void Direct3DControl_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeDirect3D();
            StartRenderingAsync();
        }

        private void Direct3DControl_Unloaded(object sender, RoutedEventArgs e)
        {
            StopRendering();
            CleanupDirect3D();
        }

        private void InitializeDirec3D()
        {
            var swapChainDesc = new SwapChainDescription
            {
                BufferCount = 1,
                ModeDescription = new ModeDescription((int)this.ActualWidth, (int)this.ActualHeight, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                IsWindowed = true,
                OutputHandle = new System.IntPtr(),
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            };

            // Create the Direct3D device and context
            device = new Device(DriverType.Hardware, DeviceCreationFlags.None);
            context = device.ImmediateContext;

            var factory = new Factory1();
            swapChain = new SwapChain(factory, device, swapChainDesc);
            var backBuffer = swapChain.GetBackBuffer<Texture2D>(0);
            renderTargetView = new RenderTargetView(device, backBuffer);

            context.OutputMerger.SetRenderTargets(renderTargetView);
        }
    }
}
