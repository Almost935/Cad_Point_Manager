using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.DrawingObjects3D;
using Cad_Point_Manager.Models.HitTesting;
using Cad_Point_Manager.Models.PointRendering;
using netDxf;
using netDxf.Tables;
using SharpDX;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;

using Point = System.Windows.Point;

namespace Cad_Point_Manager.Models
{
    public class CadManager3D : INotifyPropertyChanged
    {
        #region Fields
        private const float _pointSizeToExtentsFactor = 0.01f;

        private D3dResCache _d3dResCache;

        private bool _dxfLoaded = false;
        private bool _lineVerticesDirty = true;
        private bool _textVerticesDirty = true;
        private bool _dxfPointTextVerticesDirty = true;
        private bool _dxfPointCircleVerticesDirty = true;
        private bool _drawingObjectTreeDirty = true;
        private bool _dxfNeedsReload = true;
        private Bounds _extents = Bounds.Empty;
        private ObservableCollection<KeyValuePair<string, ObjectLayer3D>> _layers = [];
        private ICollectionView _layersView;
        private ObservableCollection<KeyValuePair<string, PointGroup>> _pointGroups = [];
        private ICollectionView _pointGroupsView;
        private Size2F _viewportSize = Size2F.Empty;
        
        private float _pointBaseTextHeight = 1.0f;
        private float _pointBaseMarkerSize = 0.05f;

        private readonly List<LineVertex> _cachedLineVertices = [];
        private readonly List<TextVertex> _cachedTextVertices = [];
        private readonly List<CircleVertex> _cachedCircleVertices = [];

        private DxfPointTextVerticesDict _pointTextVerticesDict;
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
        public bool DxfPointTextVerticesDirty
        {
            get => _dxfPointTextVerticesDirty;
            set
            {
                _dxfPointTextVerticesDirty = value;
                OnPropertyChanged(nameof(DxfPointTextVerticesDirty));
            }
        }
        public bool DxfPointCircleVerticesDirty
        {
            get => _dxfPointCircleVerticesDirty;
            set
            {
                _dxfPointCircleVerticesDirty = value;
                OnPropertyChanged(nameof(DxfPointCircleVerticesDirty));
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
        public ObservableCollection<KeyValuePair<string, PointGroup>> PointGroups
        {
            get => _pointGroups;
            set
            {
                _pointGroups = value;
                OnPropertyChanged(nameof(PointGroups));
            }
        }
        public ICollectionView PointGroupsView
        {
            get => _pointGroupsView;
            set
            {
                _pointGroupsView = value;
                OnPropertyChanged(nameof(PointGroupsView));
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
        public HitTestableObjectTree HitTestableObjectTree { get; set; }
        public TextVertex[] NumberVertices { get; set; } = [];
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;

        public event Action ZoomToExtentsRequested;
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
            GetPointScale();

            // Testing
            GetTestDxfPoints();
            DxfPointTextVerticesDirty = true;
            DxfPointCircleVerticesDirty = true;

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
            UpdatePointGroupsView();

            DxfLoaded = true;
            LineVerticesDirty = true;
            TextVerticesDirty = true;
            DxfPointCircleVerticesDirty = true;
            DrawingObjectTreeDirty = true;
            DxfNeedsReload = true;
        }
        public void GetPointScale()
        {
            if (Extents.IsEmpty)
            {
                _pointBaseTextHeight = 1;
                _pointBaseMarkerSize = 0.05f;
                return;
            }
            if (Extents.Width > Extents.Height)
            {
                _pointBaseTextHeight = Extents.Width * _pointSizeToExtentsFactor;
                _pointBaseMarkerSize = _pointBaseTextHeight * 0.05f;
            }
            else
            {
                _pointBaseTextHeight = Extents.Height * _pointSizeToExtentsFactor;
                _pointBaseMarkerSize = _pointBaseTextHeight * 0.05f;
            }
        }

        public void UpdateLayerView()
        {
            LayersView = CollectionViewSource.GetDefaultView(Layers);
            LayersView.SortDescriptions.Clear();
            LayersView.SortDescriptions.Add(new SortDescription("Key", ListSortDirection.Ascending));
        }
        public void UpdatePointGroupsView()
        {
            PointGroupsView = CollectionViewSource.GetDefaultView(PointGroups.Select(kvp => kvp.Value).ToList());
            PointGroupsView.SortDescriptions.Clear();
            PointGroupsView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }

        public List<(double distance, HitTestableObject hitTestableObject)> GetNearestHitTestableObjects(Point p, float tolerance)
        {
            List<(double distance, HitTestableObject hitTestableObject)> hits = [];

            if (HitTestableObjectTree is null) { return hits; }

            Rect rect = new(p.X - tolerance, p.Y - tolerance, tolerance * 2, tolerance * 2);
            var nodes = HitTestableObjectTree.GetIntersectingNodes(rect);

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
            _cachedCircleVertices.Clear();

            DxfLoaded = false;
            LineVerticesDirty = true;
            TextVerticesDirty = true;
            DxfPointTextVerticesDirty = true;
            DxfPointCircleVerticesDirty = true;
        }
        public void ClearDxfPoints()
        {
            PointGroups.Clear();
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

        public void UpdateVerticesIsMouseOver(HitTestableObject hitTestableObject, bool isMouseOver)
        {
            if (hitTestableObject is DrawingObject3D drawingObject)
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
            if (hitTestableObject is DxfPoint dxfPoint)
            {
                for (int i = dxfPoint.TextStartIndex; i <= dxfPoint.TextEndIndex; i++)
                {
                    ref var vertex = ref GetTextVertexRef(i);
                    vertex.IsMouseOver = isMouseOver ? 1.0f : 0.0f;
                }
                for (int i = dxfPoint.MarkerStartIndex; i <= dxfPoint.MarkerEndIndex; i++)
                {
                    ref var vertex = ref GetCircleVertexRef(i);
                    vertex.IsMouseOver = isMouseOver ? 1.0f : 0.0f;
                }
            }
        }

        public void UpdateVerticesIsSelected(HitTestableObject hitTestableObject, bool isSelected)
        {
            if (hitTestableObject is DrawingObject3D drawingObject)
            {
                if (drawingObject is DrawingGeometry3D drawingGeometry)
                {
                    for (int i = drawingGeometry.StartVertexIndex; i <= drawingGeometry.EndVertexIndex; i++)
                    {
                        ref var vertex = ref GetLineVertexRef(i);
                        vertex.IsSelected = isSelected ? 1.0f : 0.0f;
                    }
                }
                if (drawingObject is DrawingMtext3D drawingMtext)
                {
                    for (int i = drawingMtext.StartVertexIndex; i <= drawingMtext.EndVertexIndex; i++)
                    {
                        ref var vertex = ref GetTextVertexRef(i);
                        vertex.IsSelected = isSelected ? 1.0f : 0.0f;
                    }
                }
                if (drawingObject is DrawingBlock3D drawingBlock)
                {
                    for (int i = drawingBlock.StartLineVertexIndex; i <= drawingBlock.EndLineVertexIndex; i++)
                    {
                        ref var vertex = ref GetLineVertexRef(i);
                        vertex.IsSelected = isSelected ? 1.0f : 0.0f;
                    }
                    for (int i = drawingBlock.StartTextVertexIndex; i <= drawingBlock.EndTextVertexIndex; i++)
                    {
                        ref var vertex = ref GetTextVertexRef(i);
                        vertex.IsSelected = isSelected ? 1.0f : 0.0f;
                    }
                }
            }
            if (hitTestableObject is DxfPoint dxfPoint)
            {
                for (int i = dxfPoint.TextStartIndex; i <= dxfPoint.TextEndIndex; i++)
                {
                    ref var vertex = ref GetTextVertexRef(i);
                    vertex.IsMouseOver = isSelected ? 1.0f : 0.0f;
                }
                for (int i = dxfPoint.MarkerStartIndex; i <= dxfPoint.MarkerEndIndex; i++)
                {
                    ref var vertex = ref GetCircleVertexRef(i);
                    vertex.IsMouseOver = isSelected ? 1.0f : 0.0f;
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
            if (TextVerticesDirty || DxfPointTextVerticesDirty)
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

                        if (obj is DrawingSText3D drawingText)
                        {
                            drawingText.UpdateTextVertices(_d3dResCache);
                            drawingText.StartVertexIndex = start;
                            _cachedTextVertices.AddRange(drawingText.TextVertices);
                            drawingText.EndVertexIndex = _cachedTextVertices.Count - 1;
                        }
                        else if (obj is DrawingMtext3D drawingMtext)
                        {
                            drawingMtext.UpdateTextVertices(_d3dResCache);
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
                            drawingBlock.UpdateTextVertices(_d3dResCache);
                            drawingBlock.StartTextVertexIndex = start;
                            _cachedTextVertices.AddRange(drawingBlock.TextVertices);
                            drawingBlock.EndTextVertexIndex = _cachedTextVertices.Count - 1;
                        }
                    }
                }
                UpdateDxfTextPointVertices(d3DResCache);

                TextVerticesDirty = false;
                DxfPointTextVerticesDirty = false;
                DrawingObjectTreeDirty = true;
            }

            return CollectionsMarshal.AsSpan(_cachedTextVertices);
        }

        public void UpdateDxfTextPointVertices(D3dResCache d3DResCache)
        {
            _pointTextVerticesDict ??= new(d3DResCache);

            foreach (var keyValuePair in PointGroups)
            {
                var pointGroup = keyValuePair.Value;

                if (!pointGroup.IsVisible || pointGroup is null) { continue; }

                foreach (var point in pointGroup.Points)
                {
                    point.TextStartIndex = _cachedTextVertices.Count;
                    point.UpdateTextVertices(_pointTextVerticesDict);
                    _cachedTextVertices.AddRange(point.TextVertices);
                    point.TextEndIndex = _cachedTextVertices.Count - 1;
                }
            }
        }

        public ReadOnlySpan<CircleVertex> UpdateCircleVerticesList()
        {
            if (DxfPointCircleVerticesDirty)
            {
                _cachedCircleVertices.Clear();

                foreach (var keyValuePair in PointGroups)
                {
                    var pointGroup = keyValuePair.Value;

                    if (!pointGroup.IsVisible || pointGroup is null) { continue; }

                    foreach (DxfPoint point in pointGroup.Points)
                    {
                        point.UpdateMarkerVertices();
                        point.MarkerStartIndex = _cachedCircleVertices.Count;
                        _cachedCircleVertices.AddRange(point.MarkerVertices);
                        point.MarkerEndIndex = _cachedCircleVertices.Count - 1;
                    }
                }
                DxfPointCircleVerticesDirty = false;
            }
            return CollectionsMarshal.AsSpan(_cachedCircleVertices);
        }

        public void GetTestDxfPoints()
        {
            PointGroups.Clear();

            List<DxfPoint> points = [];
            float rows = 5;
            float cols = 15;
            float yIncrement = Extents.Width / rows;
            float xIncrement = Extents.Height / cols;
            int pointNum = 1;

            for (int i = 0; i < rows; i++)
            {
                points.Clear();

                string pointGroupName = $"TestGroup {i + 1}";
                PointGroup pointGroup = new(pointGroupName, new SharpDX.Vector4(1.0f, 0.0f, 0.0f, 1.0f), _pointBaseTextHeight, _pointBaseMarkerSize);
                float y = Extents.Bottom + (yIncrement * i);

                for (int j = 0; j < cols; j++)
                {
                    float x = Extents.Left + (xIncrement * j);
                    DxfPoint point = new(pointGroup, pointNum, new SharpDX.Vector3(x, y, 0), pointGroup.TextHeight, pointGroup.PointMarkerSize);
                    points.Add(point);
                    pointNum++;
                }
                pointGroup.Points = points.ToArray();
                PointGroups.Add(new KeyValuePair<string, PointGroup>(pointGroupName, pointGroup));
            }
        }

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
        public ref CircleVertex GetCircleVertexRef(int index)
        {
            Span<CircleVertex> span = CollectionsMarshal.AsSpan(_cachedCircleVertices);
            if ((uint)index >= (uint)span.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
            }

            return ref span[index];
        }

        public void UpdateHitTestableObjectTree()
        {
            HitTestableObjectTree = new(this, Extents.ToRect(), 5);
            DrawingObjectTreeDirty = false;
        }
        #endregion
    }
}
