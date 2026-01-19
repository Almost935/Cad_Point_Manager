using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;
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

        private Device _device = null;
        private DeviceContext _deviceContext = null;
        private Texture2D _texture2D = null;
        private Texture2D _dxfTexture = null;
        private RenderTargetView _renderTargetView = null;
        private RenderTargetView _dxfRenderTargetView = null;
        private SharpDX.Direct2D1.Device1 _d2DDevice = null;
        private SharpDX.Direct2D1.DeviceContext1 _d2DDeviceContext = null;
        private RenderTarget _d2dRenderTarget = null;
        private Factory2 _d2DFactory = null;
        private Bitmap1 _d2dTargetBitmap = null;
        private SharpDX.DirectWrite.Factory1 _writeFactory = null;
        #endregion

        #region Properties
        public Device Device
        {
            get { return _device; }
            set
            {
                _device = value;
                OnPropertyChanged(nameof(Device));
            }
        }
        public DeviceContext DeviceContext
        {
            get { return _deviceContext; }
            set
            {
                _deviceContext = value;
                OnPropertyChanged(nameof(DeviceContext));
            }
        }
        public Texture2D Texture2D
        {
            get { return _texture2D; }
            set
            {
                _texture2D = value;
                OnPropertyChanged(nameof(Texture2D));
            }
        }
        public Texture2D DxfTexture
        {
            get { return _dxfTexture; }
            set
            {
                _dxfTexture = value;
                OnPropertyChanged(nameof(DxfTexture));
            }
        }
        public RenderTargetView RenderTargetView
        {
            get { return _renderTargetView; }
            set
            {
                _renderTargetView = value;
                OnPropertyChanged(nameof(RenderTargetView));
            }
        }
        public RenderTargetView DxfRenderTargetView
        {
            get { return _dxfRenderTargetView; }
            set
            {
                _dxfRenderTargetView = value;
                OnPropertyChanged(nameof(DxfRenderTargetView));
            }
        }

        public SharpDX.Direct2D1.Device1 D2DDevice
        {
            get { return _d2DDevice; }
            set
            {
                _d2DDevice = value;
                OnPropertyChanged(nameof(D2DDevice));
            }
        }
        public SharpDX.Direct2D1.DeviceContext1 D2DDeviceContext
        {
            get { return _d2DDeviceContext; }
            set
            {
                _d2DDeviceContext = value;
                OnPropertyChanged(nameof(D2DDeviceContext));
            }
        }
        public RenderTarget D2DRenderTarget
        {
            get { return _d2dRenderTarget; }
            set
            {
                _d2dRenderTarget = value;
                OnPropertyChanged(nameof(D2DRenderTarget));
            }
        }
        public Factory2 D2dFactory
        {
            get { return _d2DFactory; }
            set
            {
                _d2DFactory = value;
                OnPropertyChanged(nameof(D2dFactory));
            }
        }
        public Bitmap1 D2DTargetBitmap
        {
            get { return _d2dTargetBitmap; }
            set
            {
                _d2dTargetBitmap = value;
                OnPropertyChanged(nameof(D2DTargetBitmap));
            }
        }
        public SharpDX.DirectWrite.Factory1 WriteFactory
        {
            get { return _writeFactory; }
            set
            {
                _writeFactory = value;
                OnPropertyChanged(nameof(WriteFactory));
            }
        }

        public int MaxSize { get; set; }
        public BlendState BaseBlendState { get; set; }
        public BlendState MaxBlendState { get; set; }
        public GlyphAtlas AsciiGlyphAtlas { get; set; }
        public DWriteGlyphTessellator GlyphTessellator { get; set; }
        public AdvanceWidthCache AdvanceWidthCache { get; set; }

        // Preview related properties
        public Texture2D DxfPreviewTexture { get; set; }
        public RenderTargetView DxfPreviewRenderTargetView { get; set; }
        public Texture2D PreviewTexture { get; set; }
        public RenderTargetView PreviewRenderTargetView { get; set; }

        public FontFace CogoPointFontFace { get; set; }
        public ConcurrentDictionary<(string fontName, FontWeight fontWeight, FontStretch fontStretch, FontStyle fontStyle), FontFace> FontFaceDict = [];
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
                    _device?.Dispose();
                    _deviceContext?.Dispose();
                    _texture2D?.Dispose();
                    _dxfTexture?.Dispose();
                    _renderTargetView?.Dispose();
                    _dxfRenderTargetView?.Dispose(); // Fixed null check
                    _d2DDevice?.Dispose();
                    _d2DDeviceContext?.Dispose();
                    _d2DFactory?.Dispose();
                    _d2dRenderTarget?.Dispose();
                    _d2dTargetBitmap?.Dispose();
                    _writeFactory?.Dispose();

                    BaseBlendState?.Dispose();
                    AsciiGlyphAtlas?.Dispose();
                    GlyphTessellator?.Dispose();
                    CogoPointFontFace.Dispose();

                    foreach (var fontFace in FontFaceDict.Values)
                    {
                        fontFace.Dispose();
                    }

                    DxfPreviewRenderTargetView?.Dispose();
                    DxfPreviewTexture?.Dispose();
                    PreviewRenderTargetView?.Dispose();
                    PreviewTexture?.Dispose();

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
