using Cad_Point_Manager.Controls.D3DControl.Rendering.Msdf;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Helpers;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DirectWrite;
using SharpDX.DXGI;
using System.Collections.Concurrent;
using System.ComponentModel;

using Device = SharpDX.Direct3D11.Device;
using DeviceContext = SharpDX.Direct3D11.DeviceContext;
using Factory2 = SharpDX.Direct2D1.Factory2;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace Cad_Point_Manager.Controls.D3DControl
{
    public class ResCache : IDisposable, INotifyPropertyChanged
    {
        #region Fields
        private bool disposed = false;

        private Texture2D? _readbackStaging;
        private int _readbackW, _readbackH;
        private Format _readbackFmt;
        #endregion

        #region Properties
        public Device Device { get; set; }
        public DeviceContext DeviceContext { get; set; }
        public Texture2D Texture2D { get; set; }
        public Texture2D DxfTexture { get; set; }
        public RenderTargetView RenderTargetView { get; set; }
        public RenderTargetView DxfRenderTargetView { get; set; }
        public RenderTargetView FrameRenderTargetView { get; set; }

        public SharpDX.Direct2D1.Device1 D2DDevice { get; set; }
        public SharpDX.Direct2D1.DeviceContext1 D2DDeviceContext { get; set; }
        public RenderTarget D2DRenderTarget { get; set; }
        public Factory2 D2dFactory { get; set; }
        public Bitmap1 D2DTargetBitmap { get; set; }
        public SharpDX.DirectWrite.Factory1 WriteFactory { get; set; }

        public int MaxSize { get; set; }
        public BlendState BaseBlendState { get; set; }
        public BlendState MaxBlendState { get; set; }
        public GlyphAtlas AsciiGlyphAtlas { get; set; }
        public MsdfAtlas CogoPointMsdfAtlas { get; set; }
        public DWriteGlyphTessellator GlyphTessellator { get; set; }
        public AdvanceWidthCache AdvanceWidthCache { get; set; }

        public Texture2D GlowTexture { get; set; }
        public RenderTargetView GlowRenderTargetView { get; set; }
        public ShaderResourceView GlowShaderResourceView { get; set; }

        public FontFace CogoPointFontFace { get; set; }
        public ConcurrentDictionary<
            (string fontName, FontWeight fontWeight, FontStretch fontStretch,
            FontStyle fontStyle), FontFace> FontFaceDict = [];
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Methods
        public FontFace GetFontFace(string fontName, FontWeight fontWeight, FontStretch fontStretch, FontStyle fontStyle)
        {
            if (WriteFactory is null)
            {
                throw new InvalidOperationException("WriteFactory is not initialized.");
            }

            if (FontFaceDict.TryGetValue((fontName, fontWeight, fontStretch, fontStyle), out FontFace fontFace) &&
                !fontFace.IsDisposed)
            {
                return fontFace;
            }
            else
            {
                FontCollection fontCollection = WriteFactory.GetSystemFontCollection(false);
                bool exists = fontCollection.FindFamilyName(fontName, out int fontIndex);
                if (!exists) fontIndex = 0; // Fallback to the first font if not found
                FontFamily fontFamily = fontCollection.GetFontFamily(fontIndex);

                Font font = null;
                for (int i = 0; i < fontFamily.FontCount; i++)
                {
                    var potFont = fontFamily.GetFont(i);

                    if (potFont.Weight == fontWeight && potFont.Stretch == fontStretch && potFont.Style == fontStyle)
                    {
                        font = potFont;
                        break;
                    }

                    potFont.Dispose(); // Clean up if not returned
                }
                font ??= fontFamily.GetFont(0); // Fallback to the first font if not found

                FontFace newFontFace = new(font);
                FontFaceDict[(fontName, fontWeight, fontStretch, fontStyle)] = newFontFace;

                font.Dispose();
                fontCollection.Dispose();
                fontFamily.Dispose();

                return newFontFace;
            }
        }

        public void CopyToWriteableBitmap(Texture2D source, System.Windows.Media.Imaging.WriteableBitmap target)
        {
            var desc = source.Description;

            // sanity: BGRA8 only
            if (desc.Format != Format.B8G8R8A8_UNorm && desc.Format != Format.B8G8R8A8_UNorm_SRgb)
            { throw new NotSupportedException($"Format {desc.Format} not supported for WPF Bgra32."); }

            // Copy the overlap region only (prevents overflow if sizes differ)
            int copyWidth = Math.Min(desc.Width, target.PixelWidth);
            int copyHeight = Math.Min(desc.Height, target.PixelHeight);
            int bytesPerRow = copyWidth * 4;

            var staging = GetOrCreateReadbackStaging(desc.Width, desc.Height, desc.Format);

            DeviceContext.CopyResource(source, staging);

            var box = DeviceContext.MapSubresource(staging, 0, MapMode.Read, MapFlags.None);

            try
            {
                target.Lock();

                unsafe
                {
                    byte* srcBase = (byte*)box.DataPointer;
                    byte* dstBase = (byte*)target.BackBuffer;

                    for (int y = 0; y < copyHeight; y++)
                    {
                        byte* srcRow = srcBase + y * box.RowPitch;
                        byte* dstRow = dstBase + y * target.BackBufferStride;

                        System.Buffer.MemoryCopy(srcRow, dstRow, bytesPerRow, bytesPerRow);
                    }
                }

                target.AddDirtyRect(new System.Windows.Int32Rect(0, 0, copyWidth, copyHeight));
            }
            finally
            {
                target.Unlock();
                DeviceContext.UnmapSubresource(staging, 0);
            }
        }
        private Texture2D GetOrCreateReadbackStaging(int w, int h, Format fmt)
        {
            if (_readbackStaging != null &&
                !_readbackStaging.IsDisposed &&
                _readbackW == w && _readbackH == h && _readbackFmt == fmt)
            {
                return _readbackStaging;
            }

            _readbackStaging?.Dispose();

            _readbackW = w;
            _readbackH = h;
            _readbackFmt = fmt;

            _readbackStaging = new Texture2D(Device, new Texture2DDescription
            {
                Width = w,
                Height = h,
                MipLevels = 1,
                ArraySize = 1,
                Format = fmt,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read,
                OptionFlags = ResourceOptionFlags.None
            });

            return _readbackStaging;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Device?.Dispose();
                    DeviceContext?.Dispose();
                    Texture2D?.Dispose();
                    DxfTexture?.Dispose();
                    RenderTargetView?.Dispose();
                    DxfRenderTargetView?.Dispose();
                    FrameRenderTargetView?.Dispose();
                    D2DDevice?.Dispose();
                    D2DDeviceContext?.Dispose();
                    D2dFactory?.Dispose();
                    D2DRenderTarget?.Dispose();
                    D2DTargetBitmap?.Dispose();
                    WriteFactory?.Dispose();

                    BaseBlendState?.Dispose();
                    MaxBlendState?.Dispose();
                    AsciiGlyphAtlas?.Dispose();
                    GlyphTessellator?.Dispose();
                    CogoPointFontFace.Dispose();
                    CogoPointMsdfAtlas?.Dispose();

                    GlowRenderTargetView?.Dispose();
                    GlowShaderResourceView?.Dispose();
                    GlowTexture?.Dispose();

                    foreach (var fontFace in FontFaceDict.Values)
                    {
                        fontFace.Dispose();
                    }

                    _readbackStaging?.Dispose();
                    _readbackStaging = null;
                }

                disposed = true;
            }
        }
        ~ResCache()
        {
            Dispose(false);
        }


        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
