using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DirectWrite;
using System.Collections.Concurrent;
using System.ComponentModel;
using Device = SharpDX.Direct3D11.Device;
using DeviceContext = SharpDX.Direct3D11.DeviceContext;
using Factory2 = SharpDX.Direct2D1.Factory2;

namespace Cad_Point_Manager.Controls.D3DControl
{
    public class ResCache : IDisposable, INotifyPropertyChanged
    {
        #region Fields
        private bool disposed = false;

        private Device _device = null;
        private DeviceContext _deviceContext = null;
        private Texture2D _texture2D = null;
        private Texture2D _dxfTexture = null;
        private Texture2D _interactionTexture = null;
        private RenderTargetView _renderTargetView = null;
        private RenderTargetView _dxfRenderTargetView = null;
        private RenderTargetView _interactiveRenderTargetView = null;
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
        public Texture2D InteractionTexture
        {
            get { return _interactionTexture; }
            set
            {
                _interactionTexture = value;
                OnPropertyChanged(nameof(InteractionTexture));
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
        public RenderTargetView InteractiveRenderTargetView
        {
            get { return _interactiveRenderTargetView; }
            set
            {
                _interactiveRenderTargetView = value;
                OnPropertyChanged(nameof(InteractiveRenderTargetView));
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
                    _interactionTexture?.Dispose();
                    _renderTargetView?.Dispose();
                    _dxfRenderTargetView?.Dispose(); // Fixed null check
                    _interactiveRenderTargetView?.Dispose();
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
