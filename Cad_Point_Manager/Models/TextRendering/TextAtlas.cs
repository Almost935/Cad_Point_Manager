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
    public class TextAtlas : IDisposable
    {
        #region Fields
        private Device _device;
        private Factory1 _factory;
        private SharpDX.Direct2D1.Device _d2dDevice;
        private RenderTarget _renderTarget;
        private bool _disposed = false;
        #endregion

        #region Properties
        public Size2F AtlasSize { get; set; } = new Size2F();
        public List<TextTexture> Textures { get; set; }
        public TextTexture CurrentTexture { get; set; }

        public float CurrentX { get; set; }
        public float CurrentY { get; set; }
        public RectangleF CurrentBounds { get; set; } = RectangleF.Empty;
        #endregion

        #region Constructor
        public TextAtlas(Device device, Size2F atlasSize)
        {
            _device = device;
            AtlasSize = atlasSize;

            CreateAtlas();
            InitializeRenderTarget();
        }
        #endregion

        #region Methods
        public void CreateAtlas()
        {
            GetNextTexture();
        }

        private void InitializeRenderTarget()
        {
            var surface = CurrentTexture.Texture.QueryInterface<Surface>();
            //var rtp = new RenderTargetProperties(new PixelFormat(Format.Unknown, SharpDX.Direct2D1.AlphaMode.Premultiplied));
            var rtp = new RenderTargetProperties(new PixelFormat(Format.Unknown, SharpDX.Direct2D1.AlphaMode.Premultiplied));
            _factory = new Factory1(FactoryType.MultiThreaded, DebugLevel.Information);
            _renderTarget = new(_factory, surface, rtp);
        }

        private void GetNextTexture()
        {
            // Create a texture for the atlas
            var texture = new Texture2D(_device, new Texture2DDescription()
            {
                Width = (int)AtlasSize.Width,
                Height = (int)AtlasSize.Height,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                Format = Format.B8G8R8A8_UNorm,
                MipLevels = 1,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                OptionFlags = ResourceOptionFlags.Shared,
                CpuAccessFlags = CpuAccessFlags.None,
                ArraySize = 1
            });
            CurrentTexture = new TextTexture(texture);
        }

        public void LoadTextListToAtlas(List<DrawingText3D> textList)
        {
            _renderTarget.BeginDraw();
            foreach (var text in textList)
            {
                AddTextToAtlas(text);
            }
            _renderTarget.EndDraw();
        }

        public void AddTextToAtlas(DrawingText3D drawingText3D)
        {
            var width = drawingText3D.TextLayout.Metrics.WidthIncludingTrailingWhitespace;
            var height = drawingText3D.TextLayout.Metrics.Height;

            if (CurrentX + width > AtlasSize.Width)
            {
                if (CurrentY + height > AtlasSize.Height)
                {
                    GetNextTexture();

                    CurrentX = 0;
                    CurrentY = 0;
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
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _renderTarget?.Dispose();
                    _factory?.Dispose();
                    foreach (var texture in Textures)
                    {
                        texture.Dispose();
                    }
                    CurrentTexture?.Dispose();
                }

                // Dispose unmanaged resources

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~TextAtlas()
        {
            Dispose(false);
        }
        #endregion
    }
}
