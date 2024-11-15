using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using System.Collections.Generic;
using System.Linq;
using SharpDX.Direct3D9;

namespace Cad_Point_Manager.Controls
{
    public class Direct3DControl : UserControl
    {
        private RenderTargetView renderTargetView;
        private Device device;
        private SwapChain swapChain;
        private DeviceContext context;
        private RenderTarget2D renderTarget;
        private SharpDX.Direct3D11.Buffer vertexBuffer;
        private ShaderBytecode vertexShaderBytecode;
        private ShaderBytecode pixelShaderBytecode;
        private VertexShader vertexShader;
        private PixelShader pixelShader;
        private List<Line> lines = new List<Line>();
        private List<Arc> arcs = new List<Arc>();

        public Direct3DControl()
        {
            this.Loaded += Direct3DControl_Loaded;
            this.Unloaded += Direct3DControl_Unloaded;
        }

        private void Direct3DControl_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeDirect3D();
        }

        private void Direct3DControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Dispose(true);
        }

        private void InitializeDirect3D()
        {
            // Create Device and SwapChain
            var factory = new Factory1();
            var description = new SwapChainDescription
            {
                BufferCount = 1,
                ModeDescription = new ModeDescription(Width, Height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                IsWindowed = true,
                OutputHandle = this.GetWindowHandle(),
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            };
            swapChain = new SwapChain(factory, new Device(DriverType.Hardware), DeviceCreationFlags.None, description);
            device = swapChain.Device;
            context = device.ImmediateContext;

            // Create Render Target
            var backBuffer = swapChain.GetBackBuffer<Texture2D>(0);
            renderTargetView = new RenderTargetView(device, backBuffer);
            context.OutputMerger.SetRenderTargets(renderTargetView);

            // Set up shaders
            var vertexShaderSource = ShaderBytecode.CompileFromFile("vertexShader.hlsl", "main", "vs_4_0");
            var pixelShaderSource = ShaderBytecode.CompileFromFile("pixelShader.hlsl", "main", "ps_4_0");

            vertexShaderBytecode = vertexShaderSource;
            pixelShaderBytecode = pixelShaderSource;

            vertexShader = new VertexShader(device, vertexShaderBytecode);
            pixelShader = new PixelShader(device, pixelShaderBytecode);

            context.VertexShader.Set(vertexShader);
            context.PixelShader.Set(pixelShader);

            // Create buffers
            CreateBuffers();

            // Set up the view
            this.SizeChanged += (s, e) => { if (renderTargetView != null) renderTargetView.Dispose(); InitializeDirect3D(); };

            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            // Clear screen
            context.ClearRenderTargetView(renderTargetView, new RawColor4(0.0f, 0.0f, 0.0f, 1.0f));

            // Draw lines and arcs
            DrawLinesAndArcs();

            // Present the result
            swapChain.Present(0, PresentFlags.None);
        }

        private void DrawLinesAndArcs()
        {
            // Set up primitive topology
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, SharpDX.Utilities.SizeOf<LineVertex>(), 0));

            // Draw the lines
            context.Draw(lines.Count * 2, 0);

            // Additional logic for arcs or other 2D primitives can go here...
        }

        private void CreateBuffers()
        {
            // Example: Create a simple line vertex buffer
            var vertices = new[]
            {
                new LineVertex(new Vector3(100, 100, 0)),
                new LineVertex(new Vector3(200, 200, 0)),
                new LineVertex(new Vector3(200, 100, 0)),
                new LineVertex(new Vector3(300, 200, 0))
            };

            vertexBuffer = new SharpDX.Direct3D11.Buffer(device, new DataStream(vertices, true, true), new BufferDescription
            {
                SizeInBytes = (int)(vertices.Length * SharpDX.Utilities.SizeOf<LineVertex>()),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None
            });
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                vertexBuffer?.Dispose();
                vertexShader?.Dispose();
                pixelShader?.Dispose();
                renderTargetView?.Dispose();
                swapChain?.Dispose();
                device?.Dispose();
            }
        }

        private IntPtr GetWindowHandle()
        {
            // To get the window handle, which is necessary for Direct3D swap chain.
            // For WPF, you can use the following code:
            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            return hwndSource?.Handle ?? IntPtr.Zero;
        }

        public class Line
        {
            public Vector2 Start { get; set; }
            public Vector2 End { get; set; }
        }

        public class Arc
        {
            public Vector2 Center { get; set; }
            public float Radius { get; set; }
            public float StartAngle { get; set; }
            public float EndAngle { get; set; }
        }

        public struct LineVertex
        {
            public Vector3 Position;

            public LineVertex(Vector3 position)
            {
                Position = position;
            }
        }
    }
}
