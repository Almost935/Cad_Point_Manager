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
    public class TextAtlasManager : IDisposable
    {
        #region Fields
        private Device _device;
        private bool _disposed = false;
        #endregion

        #region Properties
        public Size2F AtlasSize { get; set; } = new Size2F();
        public List<TextAtlas> TextAtlases { get; set; } = [];
        public List<Texture2D> Texture2Ds { get; set; } = [];
        public List<TextVertex> TextVertices { get; set; } = [];
        public TextAtlas CurrentTexture { get; set; }
        public bool IsDisposed { get; private set; } = false;
        #endregion

        #region Constructor
        public TextAtlasManager(Device device, Size2F atlasSize)
        {
            _device = device;
            AtlasSize = atlasSize;

            CreateAtlas();
        }
        #endregion

        #region Methods
        public void CreateAtlas()
        {
            GetTexture();
        }

        private void GetTexture()
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
            CurrentTexture = new TextAtlas(texture);
            TextAtlases.Add(CurrentTexture);
        }

        public void LoadTextListToAtlas(List<DrawingText3D> textList)
        {
            CurrentTexture.RenderTarget.BeginDraw();
            CurrentTexture.RenderTarget.Clear(new RawColor4(1.0f, 0.0f, 0.0f, 1.0f));
            foreach (var text in textList)
            {
                CurrentTexture.AddTextToAtlas(text);
            }
            CurrentTexture.RenderTarget.EndDraw();

            LoadTextVertices();
        }

        public void LoadTextVertices()
        {
            TextVertices = new()
            {
                new(new Vector3(980, 4950, 0.0f), new Vector2(980, 4950)),
                new(new Vector3(980, 5230, 0.0f), new Vector2(980, 5230)),
                new(new Vector3(1340, 5230, 0.0f), new Vector2(1340, 5230)),
                new(new Vector3(980, 4950, 0.0f), new Vector2(980, 4950)),
                new(new Vector3(1340, 5230, 0.0f), new Vector2(1340, 5230)),
                new(new Vector3(1340, 4950, 0.0f), new Vector2(1340, 4950))
            };
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    foreach (var texture in TextAtlases)
                    {
                        texture.Dispose();
                    }
                    CurrentTexture?.Dispose();
                }

                // Dispose unmanaged resources
                _disposed = true;
                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~TextAtlasManager()
        {
            Dispose(false);
        }
        #endregion
    }
}
