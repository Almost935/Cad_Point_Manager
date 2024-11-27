using System.ComponentModel;
using SharpDX.Direct3D11;

using Factory1 = SharpDX.Direct2D1.Factory1;

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
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Methods
        public void ChangeDeviceContext(DeviceContext1 newDeviceContext)
        {
            
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
                    _texture2D?.Dispose();
                    _renderTargetView?.Dispose();
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
