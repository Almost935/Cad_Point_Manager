using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Extensions;
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
using Vector2 = SharpDX.Vector2;
using Vector3 = SharpDX.Vector3;

namespace Cad_Point_Manager.Models
{
    public class CadManager3D : INotifyPropertyChanged
    {
        #region Fields
        private const float _pointSizeToExtentsFactor = 0.005f;

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
        //private ObservableCollection<KeyValuePair<string, PointGroup>> _pointGroups = [];
        private ICollectionView _pointGroupsView;
        private CogoPointManager _cogoPointManager = new();
        private Size2F _viewportSize = Size2F.Empty;
        private Enums.SelectionMode _snapSelectionMode = Enums.SelectionMode.All;

        private float _pointBaseTextHeight;
        private float _pointBaseMarkerSize;

        private readonly List<LineVertex> _cachedLineVertices = [];
        private readonly List<TextVertex> _cachedTextVertices = [];
        private readonly List<TextVertex> _cachedPointTextVertices = [];
        private readonly List<CircleVertex> _cachedPointMarkerVertices = [];

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
        public bool PointTextVerticesDirty
        {
            get => _dxfPointTextVerticesDirty;
            set
            {
                _dxfPointTextVerticesDirty = value;
                OnPropertyChanged(nameof(PointTextVerticesDirty));
            }
        }
        public bool PointCircleVerticesDirty
        {
            get => _dxfPointCircleVerticesDirty;
            set
            {
                _dxfPointCircleVerticesDirty = value;
                OnPropertyChanged(nameof(PointCircleVerticesDirty));
            }
        }
        public bool HitTestableObjectTreeDirty
        {
            get => _drawingObjectTreeDirty;
            set
            {
                _drawingObjectTreeDirty = value;
                OnPropertyChanged(nameof(HitTestableObjectTreeDirty));
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
        //public ObservableCollection<KeyValuePair<string, PointGroup>> PointGroups
        //{
        //    get => _pointGroups;
        //    set
        //    {
        //        _pointGroups = value;
        //        OnPropertyChanged(nameof(PointGroups));
        //    }
        //}
        public ICollectionView PointGroupsView
        {
            get => _pointGroupsView;
            set
            {
                _pointGroupsView = value;
                OnPropertyChanged(nameof(PointGroupsView));
            }
        }
        public CogoPointManager CogoPointManager
        {
            get => _cogoPointManager;
            set
            {
                _cogoPointManager = value;
                OnPropertyChanged(nameof(CogoPointManager));
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
        public Enums.SelectionMode SnapSelectionMode
        {
            get => _snapSelectionMode;
            set
            {
                _snapSelectionMode = value;
                OnPropertyChanged(nameof(SnapSelectionMode));
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
            PointTextVerticesDirty = true;
            PointCircleVerticesDirty = true;
            HitTestableObjectTreeDirty = true;
            DxfNeedsReload = true;
        }
        public void GetPointScale()
        {
            if (Extents.IsEmpty)
            {
                _pointBaseTextHeight = 1;
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
            PointGroupsView = CollectionViewSource.GetDefaultView(CogoPointManager.PointGroups.Select(kvp => kvp.Value).ToList());
            PointGroupsView.SortDescriptions.Clear();
            PointGroupsView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }

        public List<(double distance, HitTestablePoint point)> HitTestSignficantPoints(Point p, float tolerance)
        {
            List<(double distance, HitTestablePoint point)> hits = [];

            if (HitTestableObjectTree is null) { return hits; }

            Rect rect = new(p.X - tolerance, p.Y - tolerance, tolerance * 2, tolerance * 2);
            var nodes = HitTestableObjectTree.GetIntersectingNodes(rect);

            List<(double distance, Vector2 coords)> significantPoints = [];
            foreach (var node in nodes)
            {
                significantPoints.AddRange(node.HitTestSignificantPoints(p, rect));
            }
            foreach (var (distance, coords) in significantPoints)
            {
                hits.Add((distance, new HitTestablePoint(coords.ToVector3())));
            }

            return hits;
        }
        public List<(double distance, DrawingGeometry3D geometries)> HitTestGeometries(Point p, float tolerance)
        {
            List<(double distance, DrawingGeometry3D geometries)> geometries = [];

            if (HitTestableObjectTree is null) { return geometries; }

            Rect rect = new(p.X - tolerance, p.Y - tolerance, tolerance * 2, tolerance * 2);
            var nodes = HitTestableObjectTree.GetIntersectingNodes(rect);

            foreach (var node in nodes)
            {
                geometries.AddRange(node.HitTestGeometries(p, rect));
            }

            return geometries;
        }
        public List<(double distance, HitTestableObject hitTestableObject)> HitTestAll(Point p, float tolerance)
        {
            List<(double distance, HitTestableObject hitTestableObject)> hits = [];

            if (HitTestableObjectTree is null) { return hits; }

            Rect rect = new(p.X - tolerance, p.Y - tolerance, tolerance * 2, tolerance * 2);
            var nodes = HitTestableObjectTree.GetIntersectingNodes(rect);

            foreach (var node in nodes)
            {
                hits.AddRange(node.HitTestAll(p, rect));
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
        public void ClearDxfPoints()
        {
            CogoPointManager.Reset();
            _cachedPointTextVertices.Clear();
            _cachedPointMarkerVertices.Clear();

            PointTextVerticesDirty = true;
            PointCircleVerticesDirty = true;
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
                if (drawingObject is DrawingText3D drawingText)
                {
                    for (int i = drawingText.StartVertexIndex; i <= drawingText.EndVertexIndex; i++)
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
                    ref var vertex = ref GetPointTextVertexRef(i);
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
                if (drawingObject is DrawingText3D drawingText)
                {
                    for (int i = drawingText.StartVertexIndex; i <= drawingText.EndVertexIndex; i++)
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
                    ref var vertex = ref GetPointTextVertexRef(i);
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
                HitTestableObjectTreeDirty = true;
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

                _cachedTextVertices.Clear();

                foreach (var kvp in Layers)
                {
                    var layer = kvp.Value;
                    if (!layer.IsVisible) continue;

                    foreach (var obj in layer.DrawingObject3Ds)
                    {
                        int start = _cachedTextVertices.Count;

                        if (obj is DrawingText3D text3D)
                        {
                            text3D.UpdateTextVertices(d3DResCache);
                            text3D.StartVertexIndex = start;
                            _cachedTextVertices.AddRange(text3D.TextVertices);
                            text3D.EndVertexIndex = _cachedTextVertices.Count - 1;
                        }
                        if (obj is DrawingBlock3D drawingBlock)
                        {
                            drawingBlock.UpdateTextVertices(d3DResCache);
                            drawingBlock.StartTextVertexIndex = start;
                            _cachedTextVertices.AddRange(drawingBlock.TextVertices);
                            drawingBlock.EndTextVertexIndex = _cachedTextVertices.Count - 1;
                        }
                    }
                }
                TextVerticesDirty = false;
                HitTestableObjectTreeDirty = true;
            }

            return CollectionsMarshal.AsSpan(_cachedTextVertices);
        }

        public ReadOnlySpan<TextVertex> UpdatePointTextVertices(D3dResCache d3DResCache)
        {
            if (PointTextVerticesDirty)
            {
                _cachedPointTextVertices.Clear();

                if (d3DResCache.Device is null)
                {
                    return CollectionsMarshal.AsSpan(_cachedPointTextVertices);
                }

                _pointTextVerticesDict ??= new(d3DResCache);

                foreach (var keyValuePair in CogoPointManager.PointGroups)
                {
                    var pointGroup = keyValuePair.Value;

                    if (!pointGroup.IsVisible || pointGroup is null) { continue; }

                    foreach (var point in pointGroup.Points)
                    {
                        point.TextStartIndex = _cachedPointTextVertices.Count;
                        point.UpdateTextVertices(_pointTextVerticesDict);
                        _cachedPointTextVertices.AddRange(point.TextVertices);
                        point.TextEndIndex = _cachedPointTextVertices.Count - 1;
                    }
                }

                PointTextVerticesDirty = false;
                HitTestableObjectTreeDirty = true;
            }

            return CollectionsMarshal.AsSpan(_cachedPointTextVertices);
        }

        public ReadOnlySpan<CircleVertex> UpdateCircleVerticesList()
        {
            if (PointCircleVerticesDirty)
            {
                _cachedPointMarkerVertices.Clear();

                foreach (var keyValuePair in CogoPointManager.PointGroups)
                {
                    var pointGroup = keyValuePair.Value;

                    if (!pointGroup.IsVisible || pointGroup is null) { continue; }

                    foreach (DxfPoint point in pointGroup.Points)
                    {
                        point.UpdateMarkerVertices();
                        point.MarkerStartIndex = _cachedPointMarkerVertices.Count;
                        _cachedPointMarkerVertices.AddRange(point.MarkerVertices);
                        point.MarkerEndIndex = _cachedPointMarkerVertices.Count - 1;
                    }
                }
                PointCircleVerticesDirty = false;
            }
            return CollectionsMarshal.AsSpan(_cachedPointMarkerVertices);
        }

        public void GetTestDxfPoints()
        {
            CogoPointManager.PointGroups.Clear();

            float rows = 5;
            float cols = 15;
            float yIncrement = Extents.Width / rows;
            float xIncrement = Extents.Height / cols;
            int pointNum = 1;

            for (int i = 0; i < rows; i++)
            {
                string pointGroupName = $"TestGroup {i + 1}";
                bool created = CogoPointManager.TryCreatePointGroup(pointGroupName, new SharpDX.Vector4(1.0f, 0.0f, 0.0f, 1.0f), 
                    _pointBaseTextHeight, _pointBaseMarkerSize, out var pointGroup);
                if (created)
                {
                    var groupActivated = CogoPointManager.TrySetActivePointGroup(pointGroupName);
                    if (!groupActivated) { continue; }

                    float y = Extents.Bottom + (yIncrement * i);

                    for (int j = 0; j < cols; j++)
                    {
                        float x = Extents.Left + (xIncrement * j);
                        var pointCreated = CogoPointManager.TryAddPointToActiveGroup(pointNum, new Vector3(x, y, 0));
                        if (pointCreated) { pointNum++; continue; }
                    }
                }
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
        public ref TextVertex GetPointTextVertexRef(int index)
        {
            Span<TextVertex> span = CollectionsMarshal.AsSpan(_cachedPointTextVertices);
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
            Span<CircleVertex> span = CollectionsMarshal.AsSpan(_cachedPointMarkerVertices);
            if ((uint)index >= (uint)span.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
            }

            return ref span[index];
        }

        public void UpdateHitTestableObjectTree()
        {
            HitTestableObjectTree = new(this, Extents.ToRect(), 5);
            HitTestableObjectTreeDirty = false;
        }
        #endregion
    }
}
