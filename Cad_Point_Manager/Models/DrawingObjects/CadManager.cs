
using netDxf;
using netDxf.Entities;
using netDxf.Units;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Controls.D2DControl;
using System.IO;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class CadManager : IDisposable, INotifyPropertyChanged
    {
        #region Fields
        private bool _disposed = false;

        private DxfDocument _dxfDocument;
        private ResourceCache _resCache;
        private Rect _extents;
        private Dictionary<(byte r, byte g, byte b, byte a), Brush> _brushes = [];
        private Dictionary<(Enums.LineType lineType, StrokeTransformType strokeTransformType), StrokeStyle1> _strokeStyles = [];
        private Dictionary<(int fontSize, string fontName), TextFormat> _textFormats = [];
        #endregion

        #region Properties
        public DxfDocument DxfDocument
        {
            get { return _dxfDocument; }
            set
            {
                _dxfDocument = value;
                OnPropertyChanged(nameof(DxfDocument));
            }
        }
        public Rect Extents
        {
            get { return _extents; }
            set
            {
                _extents = value;
                OnPropertyChanged(nameof(Extents));
            }
        }
        public Dictionary<(byte r, byte g, byte b, byte a), Brush> Brushes
        {
            get { return _brushes; }
            set
            {
                _brushes = value;
                OnPropertyChanged(nameof(Brushes));
            }
        }
        public Dictionary<(Enums.LineType lineType, StrokeTransformType strokeTransformType), StrokeStyle1> StrokeStyles
        {
            get { return _strokeStyles; }
            set
            {
                _strokeStyles = value;
                OnPropertyChanged(nameof(StrokeStyles));
            }
        }
        public Dictionary<(int fontSize, string fontName), TextFormat> TextFormats
        {
            get { return _textFormats; }
            set
            {
                _textFormats = value;
                OnPropertyChanged(nameof(TextFormats));
            }
        }

        public Dictionary<string, ObjectLayer> Layers { get; set; } = new();
        public List<DrawingObject> DrawingObjects => Layers.Values.SelectMany(layer => layer.DrawingObjects).ToList();
        public bool DxfLoaded { get; set; } = false;
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Methods
        public void LoadDxfDocument(DxfDocument dxfDocument)
        {       
            Stopwatch stopwatch = Stopwatch.StartNew();

            ClearDxfDocument();

            _dxfDocument = dxfDocument;
            Layers.Clear();
            Extents = new();
            Extents = DxfHelpers.GetExtentsFromHeader(DxfDocument);

            foreach (var e in _dxfDocument.Entities.All)
            {
                var layer = GetLayer(e.Layer);
                var obj = DxfHelpers.GetDrawingObject(e, layer);
                if (obj is not null)
                {
                    layer.DrawingObjects.Add(obj);
                }
            }
            DxfLoaded = true;

            stopwatch.Stop();
            Debug.WriteLine($"LoadDxfDocument: {stopwatch.ElapsedMilliseconds} ms");
        }
        public ObjectLayer GetLayer(netDxf.Tables.Layer dxfLayer)
        {
            if (Layers.TryGetValue(dxfLayer.Name, out ObjectLayer layer)) { return layer; }
            else
            {
                ObjectLayer objectLayer = new(dxfLayer, this);
                Layers.Add(dxfLayer.Name, objectLayer);
                return objectLayer;
            }
        }
        public Brush GetBrush(byte r, byte g, byte b, byte a)
        {
            bool brushExists = Brushes.TryGetValue((r, g, b, a), out Brush brush);
            if (!brushExists || brush is null)
            {
                brush = new SolidColorBrush(_resCache.DeviceContext, new RawColor4((float)r / 255, (float)g / 255, (float)b / 255, (float)a / 255));
                Brushes.Add((r, g, b, a), brush);
            }

            return brush;
        }

        public StrokeStyle1 GetStrokeStyle(Enums.LineType lineType, StrokeTransformType strokeTransformType)
        {
            bool strokeStyleExists = StrokeStyles.TryGetValue((lineType, strokeTransformType), value: out StrokeStyle1 strokeStyle);

            if (!strokeStyleExists || strokeStyle is null)
            {
                DashStyle dashStyle; float dashOffset;

                if (lineType is Enums.LineType.Dash) { dashStyle = DashStyle.Dash; dashOffset = 1; }
                else { dashStyle = DashStyle.Solid; dashOffset = 0; }

                StrokeStyleProperties1 ssp = new()
                {
                    StartCap = CapStyle.Round,
                    EndCap = CapStyle.Round,
                    DashCap = CapStyle.Flat,
                    LineJoin = LineJoin.Round,
                    MiterLimit = 10.0f,
                    DashStyle = dashStyle,
                    DashOffset = dashOffset,
                    TransformType = strokeTransformType
                };
                strokeStyle = new StrokeStyle1(_resCache.Factory, ssp);
                StrokeStyles.Add((lineType, strokeTransformType), strokeStyle);
            }

            return strokeStyle;
        }
        public TextFormat GetTextFormat(int fontSize, string fontName)
        {
            bool textFormatExists = TextFormats.TryGetValue((fontSize, fontName), value: out TextFormat textFormat);
            if (!textFormatExists || textFormat is null)
            {
                textFormat = new TextFormat(_resCache.FactoryWrite, fontName, fontSize);
                TextFormats.Add((fontSize, fontName), textFormat);
            }
            return textFormat;
        }

        public void InitializeDeviceResources(ResourceCache resCache)
        {
            Stopwatch stopwatch = new();

            _resCache = resCache;
            foreach (var layer in Layers.Values)
            {
                layer.InitializeResources(resCache);
            }

            var tasks = Layers.Values.Select(layer => Task.Run(() => layer.InitializeGeometries())).ToArray();
            Task.WhenAll(tasks).Wait();

            stopwatch.Stop();
            Debug.WriteLine($"InitializeDeviceResources: {stopwatch.ElapsedMilliseconds} ms");
        }
        public void UpdateDeviceDependentResources(ResourceCache resCache)
        {
            ClearDeviceResources();
            foreach (var layer in Layers.Values)
            {
                layer?.UpdateDeviceDependentResources(resCache);
            }
        }
        public void UpdateDeviceIndependentResources(ResourceCache resCache)
        {
            foreach (var layer in Layers.Values)
            {
                layer?.UpdateDeviceIndependentResources(resCache);
            }
        }


        public void ClearDxfDocument()
        {
            DxfDocument = null;
            Extents = new();
            foreach (var layer in Layers.Values) {  layer?.Dispose(); }
            Layers.Clear();
            foreach (var brush in Brushes.Values) { brush?.Dispose(); }
            Brushes.Clear();
            foreach (var strokeStyle in StrokeStyles.Values) { strokeStyle?.Dispose(); }
            StrokeStyles.Clear();
            foreach (var textFormat in TextFormats.Values) { textFormat?.Dispose(); }
            TextFormats.Clear();
            DxfLoaded = false;
        }
        public void ClearDeviceResources()
        {
            foreach (var brush in Brushes.Values) { brush?.Dispose(); }
            Brushes.Clear();
        }
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // Dispose managed resources
                if (Layers != null)
                {
                    foreach (var layer in Layers.Values)
                    {
                        layer?.Dispose();
                    }
                    Layers.Clear();
                }

                if (_brushes != null)
                {
                    foreach (var brush in _brushes.Values)
                    {
                        brush?.Dispose();
                    }
                    _brushes.Clear();
                }

                if (_strokeStyles != null)
                {
                    foreach (var strokeStyle in _strokeStyles.Values)
                    {
                        strokeStyle?.Dispose();
                    }
                    _strokeStyles.Clear();
                }

                if (_textFormats != null)
                {
                    foreach (var textFormat in _textFormats.Values)
                    {
                        textFormat?.Dispose();
                    }
                    _textFormats.Clear();
                }
            }

            _disposed = true;
        }
        ~CadManager()
        {
            Dispose(false);
        }
        #endregion
    }
}
