using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.DrawingObjects3D;
using netDxf;
using netDxf.Tables;
using SharpDX;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;

using Point = System.Windows.Point;

namespace Cad_Point_Manager.Models
{
    public class CadManager3D : INotifyPropertyChanged
    {
        #region Fields
        private D3dResCache _d3dResCache;

        private bool _dxfLoaded = false;
        private bool _dxfDirty = true;
        private bool _dxfNeedsReload = true;
        private Bounds _extents;
        private List<LineVertex> _lineVertices = [];
        private List<TextVertex> _textVertices = [];
        private ObservableCollection<KeyValuePair<string, ObjectLayer3D>> _layers = [];
        private ICollectionView _layersView;
        private Size2F _viewportSize = Size2F.Empty;
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
        public List<TextVertex> TextVertices
        {
            get => _textVertices;
            set
            {
                _textVertices = value;
                OnPropertyChanged(nameof(TextVertices));
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
        public Size2F ViewportSize
        {
            get => _viewportSize;
            set
            {
                _viewportSize = value;
                OnPropertyChanged(nameof(ViewportSize));
            }
        }

        public DxfDocument DxfDocument { get; set; }
        public DrawingObjectTree3D DrawingObjectTree3D { get; set; }
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;

        public event Action ZoomToExtentsRequested; // This event is used to reset the camera in the 3D view
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

        public void ZoomToExtents()
        {
            ZoomToExtentsRequested?.Invoke();
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
                            drawingBlock.StartLineVertexIndex = LineVertices.Count;
                            LineVertices.AddRange(drawingBlock.GeometryVertices);
                            drawingBlock.EndLineVertexIndex = LineVertices.Count - 1;
                        }
                    }
                }
            }
        }

        public void UpdateTextVerticesList(D3dResCache d3DResCache)
        {
            if (d3DResCache.Device is null) { return; }

            _d3dResCache = d3DResCache;

            TextVertices.Clear();

            foreach (var keyValuePair in Layers)
            {
                var layer = keyValuePair.Value;
                if (layer.IsVisible)
                {
                    foreach (var obj in layer.DrawingObject3Ds)
                    {
                        if (obj is DrawingText3D drawingText)
                        {
                            drawingText.UpdateTextVertices(_d3dResCache.FactoryWrite, _d3dResCache.D2dFactory);
                            drawingText.StartVertexIndex = TextVertices.Count;
                            TextVertices.AddRange(drawingText.TextVertices);
                            drawingText.EndVertexIndex = TextVertices.Count - 1;
                        }
                        if (obj is DrawingMtext3D drawingMtext)
                        {
                            drawingMtext.UpdateTextVertices(_d3dResCache.FactoryWrite, _d3dResCache.D2dFactory);
                            drawingMtext.StartVertexIndex = TextVertices.Count;

                            foreach (var row in drawingMtext.MtextBlock.Rows)
                            {
                                foreach (var segment in row.Segments)
                                {
                                    TextVertices.AddRange(segment.TextVertices);
                                }
                            }

                            drawingMtext.EndVertexIndex = TextVertices.Count - 1;
                        }
                        if (obj is DrawingBlock3D drawingBlock)
                        {
                            drawingBlock.UpdateTextVertices(_d3dResCache.FactoryWrite, _d3dResCache.D2dFactory);
                            drawingBlock.StartTextVertexIndex = TextVertices.Count;
                            TextVertices.AddRange(drawingBlock.TextVertices);
                            drawingBlock.EndTextVertexIndex = TextVertices.Count - 1;
                        }
                    }
                }
            }

            UpdateDrawingObjectTree();
        }

        public void UpdateDrawingObjectTree()
        {
            DrawingObjectTree3D = new(this, Extents.ToRect(), 5);
        }
        #endregion
    }
}
