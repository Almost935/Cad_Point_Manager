using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.DrawingObjects3D;
using Cad_Point_Manager.Models.TextRendering;
using netDxf;
using netDxf.Entities;
using netDxf.Tables;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using Point = System.Windows.Point;
using Vector2 = SharpDX.Vector2;

namespace Cad_Point_Manager.Models
{
    public class CadManager3D : INotifyPropertyChanged
    {
        #region Fields
        private D3dResCache _d3dResCache;
        private bool _dxfTextLoading = false;
        private FontCache _fontCache = new();

        private bool _dxfLoaded = false;
        private bool _dxfTextLoaded = false;
        private bool _dxfDirty = true;
        private bool _dxfNeedsReload = true;
        private Bounds _extents;
        private List<LineVertex> _lineVertices = [];
        private List<DrawingText3D> _drawingText = [];
        private ObservableCollection<KeyValuePair<string, ObjectLayer3D>> _layers = [];
        private ICollectionView _layersView;
        #endregion

        #region Properties
        public bool DxfLoaded
        {
            get => _dxfLoaded;
            set
            {
                _dxfLoaded = value;
                OnPropertyChanged(nameof(DxfLoaded));
            }
        }
        public bool DxfTextLoaded
        {
            get => _dxfTextLoaded;
            set
            {
                _dxfTextLoaded = value;
                OnPropertyChanged(nameof(DxfTextLoaded));
            }

        }
        public bool DxfDirty
        {
            get => _dxfDirty;
            set
            {
                _dxfDirty = value;
                OnPropertyChanged();
            }
        }
        public bool DxfNeedsReload
        {
            get => _dxfNeedsReload;
            set
            {
                _dxfNeedsReload = value;
                OnPropertyChanged();
            }
        }
        public Bounds Extents
        {
            get => _extents;
            set
            {
                _extents = value;
                OnPropertyChanged(nameof(Extents));
            }
        }
        public List<LineVertex> LineVertices
        {
            get => _lineVertices;
            set
            {
                _lineVertices = value;
                OnPropertyChanged(nameof(LineVertices));
            }
        }
        public List<DrawingText3D> DrawingText
        {
            get => _drawingText;
            set
            {
                _drawingText = value;
                OnPropertyChanged(nameof(DrawingText));
            }
        }
        public ObservableCollection<KeyValuePair<string, ObjectLayer3D>> Layers
        {
            get => _layers;
            set
            {
                _layers = value;
                OnPropertyChanged(nameof(Layers));
            }
        }
        public ICollectionView LayersView
        {
            get => _layersView;
            set
            {
                _layersView = value;
                OnPropertyChanged(nameof(LayersView));
            }
        }

        public DxfDocument DxfDocument { get; set; }
        public DrawingObjectTree3D DrawingObjectTree3D { get; set; }
        public TextAtlasManager TextAtlasManager { get; set; }
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Methods
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void LoadDxf(DxfDocument dxfDocument)
        {
            ClearDxf();

            DxfDocument = dxfDocument;
            Extents = DxfHelpers.GetBoundsFromHeader(DxfDocument);
            
            foreach (var e in DxfDocument.Entities.All)
            {
                var layer = GetLayer(e.Layer);
                var drawingObj3d = DxfHelpers.GetDrawingObject3D(e, layer);

                if (layer is not null && drawingObj3d is not null)
                {
                    layer.AddDrawingObject(drawingObj3d);
                }
            }
            UpdateLayerView();
            DrawingObjectTree3D = new(this, Extents.ToRect(), 5);

            UpdateLineVerticesList();

            DxfLoaded = true;
            DxfDirty = true;
            DxfNeedsReload = true;
        }

        public void UpdateLayerView()
        {
            LayersView = CollectionViewSource.GetDefaultView(Layers);
            LayersView.SortDescriptions.Clear();
            LayersView.SortDescriptions.Add(new SortDescription("Key", ListSortDirection.Ascending));
        }

        public (double distance, DrawingObject3D obj) HitTestPoint(Point p, float tolerance)
        {
            (double distance, DrawingObject3D obj) tup = (double.MaxValue, null);

            if (DrawingObjectTree3D is null) { return tup; }

            Rect rect = new(p.X - tolerance, p.Y - tolerance, tolerance * 2, tolerance * 2);
            List<DrawingObjectNode3D> nodes = DrawingObjectTree3D.GetIntersectingNodes(rect);

            foreach (var node in nodes)
            {
                (double distance, DrawingObject3D obj) objTup = node.HitTestNode(p);
                
                if (objTup.distance < tup.distance && objTup.distance < tolerance)
                {
                    tup = objTup;
                }
            }

            return tup;
        }

        public List<(double distance, DrawingObject3D obj)> GetNearestDrawingObjects(Point p, float tolerance) 
        {
            List<(double distance, DrawingObject3D obj)> hits = [];

            Rect rect = new(p.X - tolerance, p.Y - tolerance, tolerance * 2, tolerance * 2);
            var nodes = DrawingObjectTree3D.GetIntersectingNodes(rect);

            foreach (var node in nodes)
            {
                hits.AddRange(node.HitTestNode(p, rect));
            }

            hits.Sort((x, y) => x.distance.CompareTo(y.distance));

            return hits;
        }

        public void ClearDxf()
        {
            DxfDocument = null;

            Layers.Clear();
            LineVertices.Clear();

            DxfLoaded = false;
            DxfDirty = true;
        }

        public ObjectLayer3D GetLayer(Layer dxfLayer)
        {
            ObjectLayer3D layer = Layers.FirstOrDefault(x => x.Value.Name == dxfLayer.Name).Value;

            if (layer is not null) { return layer; }
            else
            {
                layer = new(dxfLayer);
                Layers.Add(new KeyValuePair<string, ObjectLayer3D>(dxfLayer.Name, layer));

                return layer;
            }
        }

        public void UpdateLineVerticesList()
        {
            LineVertices.Clear();
            DrawingText.Clear();

            foreach (var keyValuePair in Layers)
            {
                var layer = keyValuePair.Value;
                if (layer.IsVisible)
                {
                    foreach (var obj in layer.DrawingObject3Ds)
                    {
                        if (obj is DrawingGeometry3D drawingGeometry)
                        {
                            drawingGeometry.StartVertexIndex = LineVertices.Count;
                            LineVertices.AddRange(drawingGeometry.Vertices);
                            drawingGeometry.EndVertexIndex = LineVertices.Count - 1;
                        }

                        if (obj is DrawingBlock3D drawingBlock)
                        {
                            drawingBlock.StartVertexIndex = LineVertices.Count;
                            LineVertices.AddRange(drawingBlock.DrawingGeometryVerteces);
                            drawingBlock.EndVertexIndex = LineVertices.Count - 1;
                        }

                        if(obj is DrawingText3D drawingText)
                        {
                            DrawingText.Add(drawingText);
                        }
                    }
                }
            }
        }

        public unsafe void UpdateTextVerticesList(D3dResCache d3DResCache)
        {
            if (_dxfTextLoading || d3DResCache.Device is null) { return; }

            _dxfTextLoading = true;
            ResetTextVerticesDict();

            TextAtlasManager?.Dispose(); 

            TextAtlasManager = new(d3DResCache.Device, new Size2F(d3DResCache.MaxSize, d3DResCache.MaxSize));
            TextAtlasManager.LoadTextListToAtlas(DrawingText);
            
            DxfTextLoaded = true;
            _dxfTextLoading = false;
        }

        public void ResetTextVerticesDict()
        {
            TextAtlasManager?.Dispose();
            TextAtlasManager = null;
            DxfTextLoaded = false;
        }
        #endregion
    }
}
