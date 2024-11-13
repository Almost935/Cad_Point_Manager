using SharpDX.Direct2D1;
using System.ComponentModel;

using Factory1 = SharpDX.Direct2D1.Factory1;

namespace Cad_Point_Manager.Controls.D2DControl
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
        //private Dictionary<(byte r, byte g, byte b, byte a), Brush> _brushes = [];
        //private Dictionary<(Enums.LineType lineType, StrokeTransformType strokeTransformType), StrokeStyle1> _strokeStyles = [];
        //private Dictionary<(int fontSize, string fontName), TextFormat> _textFormats = [];
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
        public int MaxBitmapSize
        {
            get { return _maxBitmapSize; }
            set
            {
                _maxBitmapSize = value;
                OnPropertyChanged(nameof(MaxBitmapSize));
            }
        }
        //public Dictionary<(byte r, byte g, byte b, byte a), Brush> Brushes
        //{
        //    get { return _brushes; }
        //    set
        //    {
        //        _brushes = value;
        //        OnPropertyChanged(nameof(Brushes));
        //    }
        //}
        //public Dictionary<(Enums.LineType lineType, StrokeTransformType strokeTransformType), StrokeStyle1> StrokeStyles
        //{
        //    get { return _strokeStyles; }
        //    set
        //    {
        //        _strokeStyles = value;
        //        OnPropertyChanged(nameof(StrokeStyles));
        //    }
        //}
        //public Dictionary<(int fontSize, string fontName), TextFormat> TextFormats
        //{
        //    get { return _textFormats; }
        //    set
        //    {
        //        _textFormats = value;
        //        OnPropertyChanged(nameof(TextFormats));
        //    }
        //}
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Methods
        //public Brush GetBrush(byte r, byte g, byte b, byte a)
        //{
        //    bool brushExists = Brushes.TryGetValue((r, g, b, a), out Brush brush);
        //    if (!brushExists || brush is null)
        //    {
        //        brush = new SolidColorBrush(DeviceContext, new RawColor4((float)r / 255, (float)g / 255, (float)b / 255, (float)a / 255));
        //        Brushes.Add((r, g, b, a), brush);
        //    }
            
        //    return brush;
        //}
        //public StrokeStyle1 GetStrokeStyle(LineType lineType, StrokeTransformType strokeTransformType)
        //{
        //    bool strokeStyleExists = StrokeStyles.TryGetValue((lineType, strokeTransformType), value: out StrokeStyle1 strokeStyle);

        //    if (!strokeStyleExists || strokeStyle is null)
        //    {
        //        DashStyle dashStyle; float dashOffset;

        //        if (lineType is LineType.Dash) { dashStyle = DashStyle.Dash; dashOffset = 1; }
        //        else { dashStyle = DashStyle.Solid; dashOffset = 0; }

        //        StrokeStyleProperties1 ssp = new()
        //        {
        //            StartCap = CapStyle.Round,
        //            EndCap = CapStyle.Round,
        //            DashCap = CapStyle.Flat,
        //            LineJoin = LineJoin.Round,
        //            MiterLimit = 10.0f,
        //            DashStyle = dashStyle,
        //            DashOffset = dashOffset,
        //            TransformType = strokeTransformType
        //        };
        //        strokeStyle = new StrokeStyle1(Factory, ssp);
        //        StrokeStyles.Add((lineType, strokeTransformType), strokeStyle);
        //    }

        //    return strokeStyle;
        //}
        //public TextFormat GetTextFormat(int fontSize, string fontName)
        //{
        //    bool textFormatExists = TextFormats.TryGetValue((fontSize, fontName), value: out TextFormat textFormat);
        //    if (!textFormatExists || textFormat is null)
        //    {
        //        textFormat = new TextFormat(FactoryWrite, fontName, fontSize);
        //        TextFormats.Add((fontSize, fontName), textFormat);
        //    }
        //    return textFormat;
        //}

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
