using System.ComponentModel;
using SharpDX.Direct3D11;
using SharpDX.Direct2D1;

using Factory1 = SharpDX.Direct2D1.Factory1;
using Device = SharpDX.Direct3D11.Device;
using DeviceContext = SharpDX.Direct3D11.DeviceContext;

namespace Cad_Point_Manager.Controls.D3DControl
{
    public class D3dResCache : IDisposable, INotifyPropertyChanged
    {
        #region Fields
        private bool disposed = false;

        private Device _device = null;
        private DeviceContext _deviceContext = null;
        private Texture2D _texture2D = null;
        private RenderTargetView _renderTargetView = null;
        private SharpDX.Direct2D1.Device _d2DDevice = null;
        private SharpDX.Direct2D1.DeviceContext _d2DDeviceContext = null;
        private Factory1 _d2DFactory = null;
        private SharpDX.Direct2D1.Bitmap1 _d2dTargetBitmap = null;
        private BitmapRenderTarget _bitmapRenderTarget = null;
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
        public RenderTargetView RenderTargetView
        {
            get { return _renderTargetView; }
            set
            {
                _renderTargetView = value;
                OnPropertyChanged(nameof(RenderTargetView));
            }
        }
        public SharpDX.Direct2D1.Device D2DDevice
        {
            get { return _d2DDevice; }
            set
            {
                _d2DDevice = value;
                OnPropertyChanged(nameof(D2DDevice));
            }
        }
        public SharpDX.Direct2D1.DeviceContext D2DDeviceContext
        {
            get { return _d2DDeviceContext; }
            set
            {
                _d2DDeviceContext = value;
                OnPropertyChanged(nameof(D2DDeviceContext));
            }
        }
        public Factory1 D2DFactory
        {
            get { return _d2DFactory; }
            set
            {
                _d2DFactory = value;
                OnPropertyChanged(nameof(D2DFactory));
            }
        }
        public SharpDX.Direct2D1.Bitmap1 D2DTargetBitmap
        {
            get { return _d2dTargetBitmap; }
            set
            {
                _d2dTargetBitmap = value;
                OnPropertyChanged(nameof(D2DTargetBitmap));
            }
        }
        public BitmapRenderTarget BitmapRenderTarget
        {
            get { return _bitmapRenderTarget; }
            set
            {
                _bitmapRenderTarget = value;
                OnPropertyChanged(nameof(BitmapRenderTarget));
            }
        }
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Methods
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
                    _renderTargetView?.Dispose();
                    _d2DDevice?.Dispose();
                    _d2DDeviceContext?.Dispose();
                    _d2DFactory?.Dispose();
                    _d2dTargetBitmap?.Dispose();
                }

                disposed = true;
            }
        }
        

        ~D3dResCache()
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
