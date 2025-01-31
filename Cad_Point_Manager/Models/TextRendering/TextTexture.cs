using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Models.DrawingObjects3D;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Device = SharpDX.Direct3D11.Device;
using Factory1 = SharpDX.Direct2D1.Factory1;

namespace Cad_Point_Manager.Models.TextRendering
{
    public class TextTexture : IDisposable
    {
        #region Fields
        private Device _device;
        private Factory1 _factory;
        private SharpDX.Direct2D1.Device _d2dDevice;
        private RenderTarget _renderTarget;
        private bool _disposed = false;
        #endregion

        #region Properties
        public Texture2D Texture { get; set; }
        public Size2F AtlasSize { get; set; } = new Size2F();
        public List<TextQuadVertex> TextQuadVertices { get; set; } = [];
        public float CurrentX { get; set; }
        public float CurrentY { get; set; }
        public RectangleF CurrentBounds { get; set; } = RectangleF.Empty;
        public Dictionary<(float r, float g, float b, float a), Brush> BrushDict { get; set; } = [];

        public bool IsDisposed { get; private set; } = false;
        #endregion

        #region Constructors
        public TextTexture(Texture2D texture2D)
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
            _renderTarget = new(_factory, surface, rtp);
        }

        public bool AddTextToAtlas(DrawingText3D drawingText3D)
        {
            var width = drawingText3D.TextLayout.Metrics.WidthIncludingTrailingWhitespace;
            var height = drawingText3D.TextLayout.Metrics.Height;

            if (CurrentX + width > AtlasSize.Width)
            {
                if (CurrentY + height > AtlasSize.Height)
                {
                    return false;
                }
                else
                {
                    CurrentX = 0;
                    CurrentY = CurrentBounds.Bottom;
                }
            }
            RawVector2 point = new(CurrentX, CurrentY);

            Brush brush = BrushDict.TryGetValue((drawingText3D.Color.X, drawingText3D.Color.Y, drawingText3D.Color.Z, drawingText3D.Color.W), out brush) ? brush : new SolidColorBrush(_renderTarget, new RawColor4(drawingText3D.Color.X, drawingText3D.Color.Y, drawingText3D.Color.Z, drawingText3D.Color.W));

            _renderTarget.DrawTextLayout(point, drawingText3D.TextLayout, brush);

            CurrentBounds = RectangleF.Union(CurrentBounds, new RectangleF(CurrentX, CurrentY, width, height));

            return true;
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

        ~TextTexture()
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
