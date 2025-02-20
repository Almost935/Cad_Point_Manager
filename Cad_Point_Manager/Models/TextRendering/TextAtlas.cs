using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Models.DrawingObjects3D;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;

using Device = SharpDX.Direct3D11.Device;
using Factory1 = SharpDX.Direct2D1.Factory1;

namespace Cad_Point_Manager.Models.TextRendering
{
    public class TextAtlas : IDisposable
    {
        #region Fields
        private Factory1 _factory;
        #endregion

        #region Properties
        public Texture2D Texture { get; set; }
        public RenderTarget RenderTarget { get; set; }
        public List<TextVertex> TextVertices { get; set; } = [];
        public float CurrentX { get; set; }
        public float CurrentY { get; set; }
        public RectangleF CurrentBounds { get; set; } = RectangleF.Empty;
        public Dictionary<(float r, float g, float b, float a), Brush> BrushDict { get; set; } = [];

        public bool IsDisposed { get; private set; } = false;
        #endregion

        #region Constructors
        public TextAtlas(Texture2D texture2D)
        {
            Texture = texture2D;
            InitializeRenderTarget();
        }
        #endregion

        #region Methods
        private void InitializeRenderTarget()
        {
            var surface = Texture.QueryInterface<Surface>();
            var rtp = new RenderTargetProperties(new PixelFormat(Format.Unknown, SharpDX.Direct2D1.AlphaMode.Premultiplied));
            _factory = new Factory1(FactoryType.MultiThreaded, DebugLevel.Information);
            RenderTarget = new(_factory, surface, rtp);
        }

        public void AddTextToAtlas(DrawingText3D drawingText3D)
        {
            var textLayout = drawingText3D.TextLayout;
            if (textLayout is null) { return; }

            RawVector2 point = new(drawingText3D.Position.X, drawingText3D.Position.Y);
            var brush = GetBrush(drawingText3D.Color);
            RenderTarget.DrawTextLayout(point, textLayout, brush);
        }

        public Brush GetBrush(Vector4 color)
        {
            if (BrushDict.TryGetValue((color.X, color.Y, color.Z, color.W), out Brush brush)) { return brush; }
            else 
            {                 
                brush = new SolidColorBrush(RenderTarget, new RawColor4(color.X, color.Y, color.Z, color.W));
                BrushDict.Add((color.X, color.Y, color.Z, color.W), brush);

                return brush;
            }
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Texture?.Dispose();
                }
                disposedValue = true;
                IsDisposed = true;
            }
        }

        ~TextAtlas()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
