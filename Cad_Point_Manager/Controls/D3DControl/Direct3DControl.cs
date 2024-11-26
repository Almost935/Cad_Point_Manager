using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using Cad_Point_Manager.Controls.D2DControl;

using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Cad_Point_Manager.Controls.D3DControl
{
    public class Direct3DControl : Image
    {
        private Device _device;
        private SwapChain2 _swapChain;
        private RenderTargetView _renderTargetView;
        private DepthStencilView _depthStencilView;
        private Dx11ImageSource _d3DSurface;
        private Buffer _vertexBuffer;
        private Buffer _indexBuffer;

        public Direct3DControl()
        {
            _d3dImage = new();
            Source = _d3dImage;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            base.Stretch = Stretch.Fill;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            InitializeDirect3D();
            CompositionTarget.Rendering += OnRendering;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering -= OnRendering;
            CleanupDirect3D();
        }

        private void InitializeDirect3D()
        {
            _device = new Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport);
            _d3DSurface = new Dx11ImageSource();
            _d3DSurface.IsFrontBufferAvailableChanged += OnIsFrontBufferAvailableChanged;

            CreateAndBindTargets();

            base.Source = _d3DSurface;
        }

        private void CreateAndBindTargets()
        {
            if (_d3DSurface == null)
            {
                return;
            }

            _d3DSurface.SetRenderTarget(null);

            Disposer.SafeDispose(ref d2DRenderTarget);
            Disposer.SafeDispose(ref d2DDeviceContext);
            Disposer.SafeDispose(ref renderTarget);

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

            renderTarget = new Texture2D(device, renderDesc);
            resCache.MaxBitmapSize = GetMaxSize(renderTarget.Device.FeatureLevel);
            var surface = renderTarget.QueryInterface<Surface>();

            if (d2DFactory is null)
            {
                d2DFactory = new SharpDX.Direct2D1.Factory1(FactoryType.MultiThreaded, DebugLevel.Information);
                resCache.Factory = d2DFactory;
            }
            if (resCache.FactoryWrite is null)
            {
                var factory = new SharpDX.DirectWrite.Factory1(SharpDX.DirectWrite.FactoryType.Shared);
                resCache.FactoryWrite = factory;
            }

            var rtp = new RenderTargetProperties(new PixelFormat(Format.Unknown, SharpDX.Direct2D1.AlphaMode.Premultiplied));
            d2DRenderTarget = new(d2DFactory, surface, rtp);
            resCache.RenderTarget = d2DRenderTarget;
            d2DDeviceContext = d2DRenderTarget.QueryInterface<DeviceContext1>();
            resCache.DeviceContext = d2DDeviceContext;

            d3DSurface.SetRenderTarget(renderTarget);

            device.ImmediateContext.Rasterizer.SetViewport(0, 0, width, height, 0.0f, 1.0f);
        }

        private void CreateGeometry()
        {
            // Define cube vertices
            var vertices = new[]
            {
            new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), // Bottom-left-front
            new Vector4(1.0f, -1.0f, -1.0f, 1.0f),  // Bottom-right-front
            new Vector4(-1.0f, 1.0f, -1.0f, 1.0f),  // Top-left-front
            new Vector4(1.0f, 1.0f, -1.0f, 1.0f),   // Top-right-front
            new Vector4(-1.0f, -1.0f, 1.0f, 1.0f),  // Bottom-left-back
            new Vector4(1.0f, -1.0f, 1.0f, 1.0f),   // Bottom-right-back
            new Vector4(-1.0f, 1.0f, 1.0f, 1.0f),   // Top-left-back
            new Vector4(1.0f, 1.0f, 1.0f, 1.0f)     // Top-right-back
            };

            // Define indices for cube faces
            var indices = new ushort[]
            {
            0, 1, 2, 2, 1, 3, // Front
            4, 5, 6, 6, 5, 7, // Back
            0, 2, 4, 4, 2, 6, // Left
            1, 3, 5, 5, 3, 7, // Right
            2, 3, 6, 6, 3, 7, // Top
            0, 1, 4, 4, 1, 5  // Bottom
            };

            // Create vertex buffer
            _vertexBuffer = Buffer.Create(_device, BindFlags.VertexBuffer, vertices);
            _indexBuffer = Buffer.Create(_device, BindFlags.IndexBuffer, indices);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            ResizeDirect3DResources((int)ActualWidth, (int)ActualHeight);
        }

        private void ResizeDirect3DResources(int width, int height)
        {
            if (width <= 0 || height <= 0)
                return;

            // Release existing views
            _renderTargetView?.Dispose();
            _depthStencilView?.Dispose();

            // Resize swap chain buffers
            _swapChain.ResizeBuffers(1, width, height, Format.B8G8R8A8_UNorm, SwapChainFlags.None);

            // Recreate render target view
            using (var backBuffer = _swapChain.GetBackBuffer<Texture2D>(0))
            {
                _renderTargetView = new RenderTargetView(_device, backBuffer);
            }
            
            // Recreate depth stencil view
            var depthBufferDesc = new Texture2DDescription
            {
                Format = Format.D32_Float,
                ArraySize = 1,
                MipLevels = 1,
                Width = width,
                Height = height,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil
            };

            using (var depthBuffer = new Texture2D(_device, depthBufferDesc))
            {
                _depthStencilView = new DepthStencilView(_device, depthBuffer);
            }

            // Set new render targets and viewport
            _device.ImmediateContext.OutputMerger.SetRenderTargets(_depthStencilView, _renderTargetView);
            _device.ImmediateContext.Rasterizer.SetViewport(0, 0, width, height);
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

        private void OnRendering(object sender, EventArgs e)
        {
            if (_d3dImage.IsFrontBufferAvailable)
            {
                Render();
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            return base.MeasureOverride(constraint);
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            return base.ArrangeOverride(arrangeBounds);
        }


        private void Render()
        {
            _device.ImmediateContext.ClearRenderTargetView(_renderTargetView, new RawColor4(1.0f, 1.0f, 0.0f, 1.0f));
            _device.ImmediateContext.ClearDepthStencilView(_depthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);

            // Bind buffers
            _device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertexBuffer, Utilities.SizeOf<Vector4>(), 0));
            _device.ImmediateContext.InputAssembler.SetIndexBuffer(_indexBuffer, Format.R16_UInt, 0);

            // Set topology
            _device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            // Draw indexed
            _device.ImmediateContext.DrawIndexed(36, 0, 0);

            // Present
            _swapChain.Present(1, PresentFlags.None);
        }

        private void CleanupDirect3D()
        {
            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();
            _renderTargetView?.Dispose();
            _depthStencilView?.Dispose();
            _swapChain?.Dispose();
            _device?.Dispose();
        }
    }
}
