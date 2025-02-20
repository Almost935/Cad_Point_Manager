using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.DrawingObjects3D;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDX.WIC;
using System.Diagnostics;
using System.IO;

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
        public Bounds Extents { get; set; } = Bounds.Empty;
        public Matrix3x2 ExtentsMatrix { get; set; } = Matrix3x2.Identity;
        #endregion

        #region Constructors
        public TextAtlasManager(Device device, Bounds extents, Size2F viewportSize)
        {
            _device = device;
            Extents = extents;
            AtlasSize = viewportSize;

            GetInitialMatrix();
            CreateAtlas();
        }
        #endregion

        #region Methods
        private void GetInitialMatrix()
        {
            ExtentsMatrix = MathHelpers.GetFitTransform(new RawRectangleF(Extents.Left, Extents.Bottom, Extents.Right, Extents.Top),
                new RawRectangleF(0, 0, AtlasSize.Width, AtlasSize.Height));
            //ExtentsMatrix = new(ExtentsMatrix.M11, ExtentsMatrix.M12, ExtentsMatrix.M21, -ExtentsMatrix.M22, ExtentsMatrix.M31, ExtentsMatrix.M32);
        }


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
            CurrentTexture.RenderTarget.Clear(new RawColor4(1.0f, 0.0f, 1.0f, 0.8f));
            CurrentTexture.RenderTarget.Transform = ExtentsMatrix;

            var currentView = GetVisibleRegion(CurrentTexture.RenderTarget);

            foreach (var text in textList)
            {
                CurrentTexture.AddTextToAtlas(text);
            }

            // Testing
            Brush brush = new SolidColorBrush(CurrentTexture.RenderTarget, new RawColor4(0, 1, 0, 1));
            int increment = (int)(Extents.Width / 10);
            for (int i = (int)Extents.Left; i < Extents.Right; i += increment)
            {
                CurrentTexture.RenderTarget.DrawLine(new RawVector2(i, Extents.Bottom + 20), new RawVector2(i, Extents.Top - 20), brush, 1.0f);
            }
            brush.Dispose();

            CurrentTexture.RenderTarget.EndDraw();

            SaveTextureToPng(_device, _device.ImmediateContext, CurrentTexture.Texture, @"C:\Users\fcraw\source\repos\Cad_Point_Manager\Cad_Point_Manager\Resources\Testing\Test.png");

            LoadTextVertices();
        }

        public void LoadTextVertices()
        {
            var textureBounds = new RectangleF(0, 0, 1, 1);
            var drawingBounds = new RectangleF(Extents.Left, Extents.Bottom, Extents.Width, Extents.Height);

            TextVertices =
            [
                new(new Vector3(drawingBounds.Left, drawingBounds.Bottom, 0.0f), new Vector2(textureBounds.Left, textureBounds.Bottom)),
                new(new Vector3(drawingBounds.Right, drawingBounds.Bottom, 0.0f), new Vector2(textureBounds.Right, textureBounds.Bottom)),
                new(new Vector3(drawingBounds.Left, drawingBounds.Top, 0.0f), new Vector2(textureBounds.Left, textureBounds.Top)),

                new(new Vector3(drawingBounds.Right, drawingBounds.Bottom, 0.0f), new Vector2(textureBounds.Right, textureBounds.Bottom)),
                new(new Vector3(drawingBounds.Right, drawingBounds.Top, 0.0f), new Vector2(textureBounds.Right, textureBounds.Top)),
                new(new Vector3(drawingBounds.Left, drawingBounds.Top, 0.0f), new Vector2(textureBounds.Left, textureBounds.Top))
            ];
        }


        // Test Methods
        public static RawRectangleF GetVisibleRegion(RenderTarget renderTarget)
        {
            // Get the current transformation matrix applied to the RenderTarget
            Matrix3x2 transform = renderTarget.Transform;

            // Get the size of the RenderTarget
            float width = renderTarget.Size.Width;
            float height = renderTarget.Size.Height;

            // Define the four corners of the original viewport before transformation
            Vector2[] corners = new Vector2[]
            {
                    new Vector2(0, 0),       // Top-left
                    new Vector2(width, 0),   // Top-right
                    new Vector2(0, height),  // Bottom-left
                    new Vector2(width, height) // Bottom-right
            };

            // Manually apply the transformation
            for (int i = 0; i < corners.Length; i++)
            {
                corners[i] = new Vector2(
                    (corners[i].X * transform.M11) + (corners[i].Y * transform.M21) + transform.M31,
                    (corners[i].X * transform.M12) + (corners[i].Y * transform.M22) + transform.M32
                    );
            }

            // Compute the bounding box of the transformed points
            float minX = corners.Min(v => v.X);
            float minY = corners.Min(v => v.Y);
            float maxX = corners.Max(v => v.X);
            float maxY = corners.Max(v => v.Y);

            return new RawRectangleF(minX, minY, maxX, maxY);
        }
        public static void SaveTextureToPng(Device device, SharpDX.Direct3D11.DeviceContext context, Texture2D texture, string filePath)
        {
            var textureDesc = texture.Description;

            // Create a staging texture that allows CPU read access
            var stagingDesc = new Texture2DDescription
            {
                Width = textureDesc.Width,
                Height = textureDesc.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = textureDesc.Format,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read,
                OptionFlags = ResourceOptionFlags.None
            };

            using (var stagingTexture = new Texture2D(device, stagingDesc))
            {
                // Copy the original texture to the staging texture
                context.CopyResource(texture, stagingTexture);

                // Map the staging texture to access its data
                DataBox mappedData = context.MapSubresource(stagingTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
                Guid pixelFormat = SharpDX.WIC.PixelFormat.Format32bppBGRA;

                // Use WIC to encode the texture to a PNG
                using (var wicFactory = new ImagingFactory())
                using (var stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                using (var wicStream = new WICStream(wicFactory, stream))
                using (var encoder = new PngBitmapEncoder(wicFactory))
                {
                    encoder.Initialize(wicStream);

                    using (var frame = new BitmapFrameEncode(encoder))
                    using (var bitmap = new SharpDX.WIC.Bitmap(wicFactory, textureDesc.Width, textureDesc.Height, pixelFormat, new DataRectangle(mappedData.DataPointer, mappedData.RowPitch)))
                    {
                        frame.Initialize();
                        frame.SetSize(textureDesc.Width, textureDesc.Height);
                        frame.SetPixelFormat(ref pixelFormat);
                        frame.WriteSource(bitmap);
                        frame.Commit();
                    }

                    encoder.Commit();
                }

                // Unmap the resource
                context.UnmapSubresource(stagingTexture, 0);
            }
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
