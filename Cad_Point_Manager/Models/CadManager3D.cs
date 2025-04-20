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
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using Point = System.Windows.Point;

namespace Cad_Point_Manager.Models
{
    public class CadManager3D : INotifyPropertyChanged
    {
        #region Fields
        private D3dResCache _d3dResCache;

        private bool _dxfLoaded = false;
        private bool _lineVerticesDirty = true;
        private bool _textVerticesDirty = true;
        private bool _drawingObjectTreeDirty = true;
        private bool _dxfNeedsReload = true;
        private Bounds _extents;
        private ObservableCollection<KeyValuePair<string, ObjectLayer3D>> _layers = [];
        private ICollectionView _layersView;
        private Size2F _viewportSize = Size2F.Empty;

        private readonly List<LineVertex> _cachedLineVertices = [];
        private readonly List<TextVertex> _cachedTextVertices = [];
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
        public bool LineVerticesDirty
        {
            get => _lineVerticesDirty;
            set
            {
                _lineVerticesDirty = value;
                OnPropertyChanged(nameof(LineVerticesDirty));
            }
        }
        public bool TextVerticesDirty
        {
            get => _textVerticesDirty;
            set
            {
                _textVerticesDirty = value;
                OnPropertyChanged(nameof(TextVerticesDirty));
            }
        }
        public bool DrawingObjectTreeDirty
        {
            get => _drawingObjectTreeDirty;
            set
            {
                _drawingObjectTreeDirty = value;
                OnPropertyChanged(nameof(DrawingObjectTreeDirty));
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
            LineVerticesDirty = true;
            TextVerticesDirty = true;
            DrawingObjectTreeDirty = true;
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

            if (DrawingObjectTree3D is null) { return hits; }

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
            _cachedLineVertices.Clear();
            _cachedTextVertices.Clear();

            DxfLoaded = false;
            LineVerticesDirty = true;
            TextVerticesDirty = true;
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

        public void UpdateDrawingObjectVertices(DrawingObject3D drawingObject, bool isMouseOver)
        {
            if (drawingObject is DrawingGeometry3D drawingGeometry) 
            {
                for (int i = drawingGeometry.StartVertexIndex; i <= drawingGeometry.EndVertexIndex; i++)
                {
                    ref var vertex = ref GetLineVertexRef(i);
                    vertex.IsMouseOver = isMouseOver ? 1.0f : 0.0f;
                }
            }
            if (drawingObject is DrawingMtext3D drawingMtext)
            {
                for (int i = drawingMtext.StartVertexIndex; i <= drawingMtext.EndVertexIndex; i++)
                {
                    ref var vertex = ref GetTextVertexRef(i);
                    vertex.IsMouseOver = isMouseOver ? 1.0f : 0.0f;
                }
            }
            if (drawingObject is DrawingBlock3D drawingBlock)
            {
                for (int i = drawingBlock.StartLineVertexIndex; i <= drawingBlock.EndLineVertexIndex; i++)
                {
                    ref var vertex = ref GetLineVertexRef(i);
                    vertex.IsMouseOver = isMouseOver ? 1.0f : 0.0f;
                }
                for (int i = drawingBlock.StartTextVertexIndex; i <= drawingBlock.EndTextVertexIndex; i++)
                {
                    ref var vertex = ref GetTextVertexRef(i);
                    vertex.IsMouseOver = isMouseOver ? 1.0f : 0.0f;
                }
            }
        }

        public ReadOnlySpan<LineVertex> UpdateLineVerticesList()
        {
            if (LineVerticesDirty)
            {
                _cachedLineVertices.Clear();

                foreach (var keyValuePair in Layers)
                {
                    var layer = keyValuePair.Value;
                    if (layer.IsVisible)
                    {
                        foreach (var obj in layer.DrawingObject3Ds)
                        {
                            if (obj is DrawingGeometry3D drawingGeometry)
                            {
                                drawingGeometry.StartVertexIndex = _cachedLineVertices.Count;
                                _cachedLineVertices.AddRange(drawingGeometry.Vertices);
                                drawingGeometry.EndVertexIndex = _cachedLineVertices.Count - 1;
                            }

                            if (obj is DrawingBlock3D drawingBlock)
                            {
                                drawingBlock.StartLineVertexIndex = _cachedLineVertices.Count;
                                _cachedLineVertices.AddRange(drawingBlock.LineVertices);
                                drawingBlock.EndLineVertexIndex = _cachedLineVertices.Count - 1;
                            }
                        }
                    }
                }

                LineVerticesDirty = false;
                DrawingObjectTreeDirty = true;
            }

            return CollectionsMarshal.AsSpan(_cachedLineVertices);
        }

        public ReadOnlySpan<TextVertex> UpdateTextVerticesList(D3dResCache d3DResCache)
        {
            if (TextVerticesDirty)
            {
                if (d3DResCache.Device is null) 
                {
                    return CollectionsMarshal.AsSpan(_cachedTextVertices);
                }

                _d3dResCache = d3DResCache;
                _cachedTextVertices.Clear();

                foreach (var kvp in Layers)
                {
                    var layer = kvp.Value;
                    if (!layer.IsVisible) continue;

                    foreach (var obj in layer.DrawingObject3Ds)
                    {
                        int start = _cachedTextVertices.Count;

                        if (obj is DrawingText3D drawingText)
                        {
                            drawingText.UpdateTextVertices(_d3dResCache.FactoryWrite, _d3dResCache.D2dFactory);
                            drawingText.StartVertexIndex = start;
                            _cachedTextVertices.AddRange(drawingText.TextVertices);
                            drawingText.EndVertexIndex = _cachedTextVertices.Count - 1;
                        }
                        else if (obj is DrawingMtext3D drawingMtext)
                        {
                            drawingMtext.UpdateTextVertices(_d3dResCache.FactoryWrite, _d3dResCache.D2dFactory);
                            drawingMtext.StartVertexIndex = start;

                            foreach (var row in drawingMtext.MtextBlock.Rows)
                            {
                                foreach (var segment in row.Segments)
                                {
                                    _cachedTextVertices.AddRange(segment.TextVertices);
                                }
                            }
                            drawingMtext.EndVertexIndex = _cachedTextVertices.Count - 1;
                        }
                        else if (obj is DrawingBlock3D drawingBlock)
                        {
                            drawingBlock.UpdateTextVertices(_d3dResCache.FactoryWrite, _d3dResCache.D2dFactory);
                            drawingBlock.StartTextVertexIndex = start;
                            _cachedTextVertices.AddRange(drawingBlock.TextVertices);
                            drawingBlock.EndTextVertexIndex = _cachedTextVertices.Count - 1;
                        }
                    }
                }

                TextVerticesDirty = false;
                DrawingObjectTreeDirty = true;
            }

            return CollectionsMarshal.AsSpan(_cachedTextVertices);
        }

        //public void UpdateTextVerticesList(D3dResCache d3DResCache)
        //{
        //    if (d3DResCache.Device is null) { return; }

        //    _d3dResCache = d3DResCache;

        //    TextVertices.Clear();

        //    foreach (var keyValuePair in Layers)
        //    {
        //        var layer = keyValuePair.Value;
        //        if (layer.IsVisible)
        //        {
        //            foreach (var obj in layer.DrawingObject3Ds)
        //            {
        //                if (obj is DrawingText3D drawingText)
        //                {
        //                    drawingText.UpdateTextVertices(_d3dResCache.FactoryWrite, _d3dResCache.D2dFactory);
        //                    drawingText.StartVertexIndex = _cachedTextVertices.Count;
        //                    _cachedTextVertices.AddRange(drawingText.TextVertices);
        //                    drawingText.EndVertexIndex = _cachedTextVertices.Count - 1;
        //                }
        //                if (obj is DrawingMtext3D drawingMtext)
        //                {
        //                    drawingMtext.UpdateTextVertices(_d3dResCache.FactoryWrite, _d3dResCache.D2dFactory);
        //                    drawingMtext.StartVertexIndex = _cachedTextVertices.Count;

        //                    foreach (var row in drawingMtext.MtextBlock.Rows)
        //                    {
        //                        foreach (var segment in row.Segments)
        //                        {
        //                            _cachedTextVertices.AddRange(segment.TextVertices);
        //                        }
        //                    }

        //                    drawingMtext.EndVertexIndex = _cachedTextVertices.Count - 1;
        //                }
        //                if (obj is DrawingBlock3D drawingBlock)
        //                {
        //                    drawingBlock.UpdateTextVertices(_d3dResCache.FactoryWrite, _d3dResCache.D2dFactory);
        //                    drawingBlock.StartTextVertexIndex = _cachedTextVertices.Count;
        //                    _cachedTextVertices.AddRange(drawingBlock.TextVertices);
        //                    drawingBlock.EndTextVertexIndex = _cachedTextVertices.Count - 1;
        //                }
        //            }
        //        }
        //    }

        //    UpdateDrawingObjectTree();
        //}

        public ref TextVertex GetTextVertexRef(int index)
        {
            Span<TextVertex> span = CollectionsMarshal.AsSpan(_cachedTextVertices);
            if ((uint)index >= (uint)span.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
            }

            return ref span[index];
        }

        public ref LineVertex GetLineVertexRef(int index)
        {
            Span<LineVertex> span = CollectionsMarshal.AsSpan(_cachedLineVertices);
            if ((uint)index >= (uint)span.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
            }

            return ref span[index];
        }

        public void UpdateDrawingObjectTree()
        {
            DrawingObjectTree3D = new(this, Extents.ToRect(), 5);
            DrawingObjectTreeDirty = false;
        }
        #endregion
    }
}
