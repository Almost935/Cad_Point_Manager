using SharpDX.Direct2D1;
using System.ComponentModel;

using Factory1 = SharpDX.Direct2D1.Factory1;

namespace Cad_Point_Manager.Controls.D3DControl
{
    public class ResourceCache : IDisposable, INotifyPropertyChanged
    {
        #region Fields
        private bool disposed = false;

        private SharpDX.Direct3D11.Device _device = null;
        private RenderTarget _renderTarget = null;
        private DeviceContext1 _deviceContext = null;
        private Factory1 _factory = null;
        private SharpDX.DirectWrite.Factory1 _factoryWrite = null;
        private int _maxBitmapSize;
        #endregion

        #region Properties
        public SharpDX.Direct3D11.Device Device
        {
            get { return _device; }
            set
            {
                _device = value;
                OnPropertyChanged(nameof(Device));
            }
        }
        public RenderTarget RenderTarget
        {
            get { return _renderTarget; }
            set
            {
                _renderTarget = value;
                OnPropertyChanged(nameof(RenderTarget));
            }
        }
        public DeviceContext1 DeviceContext
        {
            get { return _deviceContext; }
            set
            {
                _deviceContext = value;
                OnPropertyChanged(nameof(DeviceContext));
            }
        }
        public Factory1 Factory
        {
            get { return _factory; }
            set
            {
                _factory = value;
                OnPropertyChanged(nameof(Factory));
            }
        }
        public SharpDX.DirectWrite.Factory1 FactoryWrite
        {
            get { return _factoryWrite; }
            set
            {
                _factoryWrite = value;
                OnPropertyChanged(nameof(FactoryWrite));
            }
        }
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Methods
        public void ChangeDeviceContext(DeviceContext1 newDeviceContext)
        {
            // Dispose of the old device context and related resources
            DisposeDeviceDependentResources();

            // Assign the new device context
            DeviceContext = newDeviceContext;
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
                    DisposeDeviceDependentResources();
                    DisposeDeviceIndependentResources();

                    _deviceContext?.Dispose();
                    _factory?.Dispose();
                    _factoryWrite?.Dispose();
                    _device?.Dispose();
                }

                disposed = true;
            }
        }
        public void DisposeDeviceDependentResources()
        {
            //foreach (var brush in _brushes.Values)
            //{
            //    brush.Dispose();
            //}
            //_brushes.Clear();
        }
        public void DisposeDeviceIndependentResources()
        {
            //    foreach (var strokeStyle in _strokeStyles.Values)
            //    {
            //        strokeStyle.Dispose();
            //    }
            //    _strokeStyles.Clear();

            //    foreach (var textFormat in _textFormats.Values)
            //    {
            //        textFormat.Dispose();
            //    }
            //    _textFormats.Clear();
        }
        ~ResourceCache()
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
