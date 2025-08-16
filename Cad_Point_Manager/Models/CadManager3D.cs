using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.DrawingObjects3D;
using Cad_Point_Manager.Models.HitTesting;
using Cad_Point_Manager.Models.PointRendering;
using netDxf;
using netDxf.Entities;
using netDxf.Tables;
using SharpDX;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;

using Point = System.Windows.Point;
using Vector2 = SharpDX.Vector2;
using Vector3 = SharpDX.Vector3;
using Vector4 = SharpDX.Vector4;

namespace Cad_Point_Manager.Models
{
    public class CadManager3D : INotifyPropertyChanged
    {
        #region Fields
        private const float _pointSizeToExtentsFactor = 0.001f;

        private bool _dxfLoaded = false;
        private bool _lineVerticesDirty = true;
        private bool _textVerticesDirty = true;
        private bool _dxfPointTextVerticesDirty = true;
        private bool _dxfPointCircleVerticesDirty = true;
        private bool _drawingObjectTreeDirty = true;
        private bool _dxfNeedsReload = true;
        private Rect _extents = RectExtensions.Zero;
        private ObservableCollection<KeyValuePair<string, ObjectLayer3D>> _layers = [];
        private ICollectionView _layersView;
        private ICollectionView _pointGroupsView;
        private ICollectionView _pointsView;
        private CogoPointManager _cogoPointManager;
        private Size2F _viewportSize = Size2F.Empty;
        private Enums.SelectionMode _snapSelectionMode = Enums.SelectionMode.CogoPoints;

        private float _pointBaseScale;

        private readonly List<LineVertex> _cachedLineVertices = [];
        private readonly List<TextVertex> _cachedTextVertices = [];
        private readonly List<TextVertex> _cachedPointTextVertices = [];
        private readonly List<CircleVertex> _cachedPointMarkerVertices = [];

        private CogoPointTextVerticesDict _pointTextVerticesDict;
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
        public Rect Extents
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
        public ICollectionView PointGroupsView
        {
            get => _pointGroupsView;
            set
            {
                _pointGroupsView = value;
                OnPropertyChanged(nameof(PointGroupsView));
            }
        }
        public ICollectionView PointsView
        {
            get => _pointsView;
            set
            {
                _pointsView = value;
                OnPropertyChanged(nameof(PointsView));
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

        #region Constructor
        public CadManager3D()
        {
            CogoPointManager = new(this);
            CogoPointManager.PropertyChanged += CogoPointManager_PropertyChanged;
        }

        private void CogoPointManager_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CogoPointManager.PointsDirty))
            {
                if (CogoPointManager.PointsDirty)
                {
                    PointTextVerticesDirty = true;
                    PointCircleVerticesDirty = true;
                    CogoPointManager.PointsDirty = false;
                }
            }
        }
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

            CogoPointManager.UpdatePointExtents();

            foreach (var e in DxfDocument.Entities.All)
            {
                if (e is MText mtext && string.IsNullOrWhiteSpace(mtext.Value)) { continue; }
                if (e is Text text && string.IsNullOrWhiteSpace(text.Value)) { continue; }

                var layer = GetLayer(e.Layer);
                var drawingObj3d = DxfHelpers.GetDrawingObject3D(e, layer);

                if (layer is not null && drawingObj3d is not null)
                {
                    layer.AddDrawingObject(drawingObj3d);
                }
            }
            GetCollectionViews();

            DxfLoaded = true;
            LineVerticesDirty = true;
            TextVerticesDirty = true;
            PointTextVerticesDirty = true;
            PointCircleVerticesDirty = true;
            HitTestableObjectTreeDirty = true;
            DxfNeedsReload = true;
        }

        public void UpdateExtents()
        {
            Extents = Rect.Union(Extents, CogoPointManager.Extents);
        }

        public void GetPointScale()
        {
            if (Extents.IsEmpty)
            {
                _pointBaseScale = 1;
                return;
            }
            if (Extents.Width > Extents.Height)
            {
                _pointBaseScale = Extents.Width.ToFloat() * _pointSizeToExtentsFactor;
            }
            else
            {
                _pointBaseScale = Extents.Height.ToFloat() * _pointSizeToExtentsFactor;
            }
        }

        public void GetCollectionViews()
        {
            LayersView = CollectionViewSource.GetDefaultView(Layers);
            LayersView.SortDescriptions.Clear();
            LayersView.SortDescriptions.Add(new SortDescription("Key", ListSortDirection.Ascending));

            PointGroupsView = new ListCollectionView(CogoPointManager.PointGroups);
            PointGroupsView.SortDescriptions.Clear();
            PointGroupsView.SortDescriptions.Add(new SortDescription("Key", ListSortDirection.Ascending));

            PointsView = new ListCollectionView(CogoPointManager.CogoPoints);
            PointsView.GroupDescriptions.Clear();
            PointsView.GroupDescriptions.Add(new PropertyGroupDescription("PointGroup.Name"));
        }

        public List<(double distance, HitTestablePoint point)> HitTestSignficantPoints(Point p, float tolerance)
        {
            List<(double distance, HitTestablePoint point)> hits = [];

            if (HitTestableObjectTree is null) { return hits; }

            Rect rect = new(p.X - tolerance, p.Y - tolerance, tolerance * 2, tolerance * 2);
            var nodes = HitTestableObjectTree.GetIntersectingNodes(rect);

            List<(Enums.SignificantPointType pointType, double distance, Vector2 vector)> significantPoints = [];
            foreach (var node in nodes)
            {
                significantPoints.AddRange(node.HitTestSignificantPoints(p, rect));
            }
            foreach (var (pointType, distance, coords) in significantPoints)
            {
                hits.Add((distance, new HitTestablePoint(coords.ToPoint(), pointType)));
            }
            hits.Sort((x, y) => x.distance.CompareTo(y.distance));
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
            geometries.Sort((x, y) => x.distance.CompareTo(y.distance));
            return geometries;
        }
        public List<(double distance, CogoPoint points)> HitTestCogoPoints(Point p, float tolerance)
        {
            List<(double distance, CogoPoint points)> hits = [];

            if (HitTestableObjectTree is null) { return hits; }

            Rect rect = new(p.X - tolerance, p.Y - tolerance, tolerance * 2, tolerance * 2);
            var nodes = HitTestableObjectTree.GetIntersectingNodes(rect);

            foreach (var node in nodes)
            {
                hits.AddRange(node.HitTestCogoPoints(p, rect));
            }
            hits.Sort((x, y) => x.distance.CompareTo(y.distance));
            return hits;
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

        public List<CogoPoint> HitTestDragCogoPoints(Rect rect)
        {
            List<CogoPoint> hits = [];

            if (HitTestableObjectTree is null) { return hits; }

            var nodes = HitTestableObjectTree.GetIntersectingNodes(rect);

            foreach (var node in nodes)
            {
                if (rect.Contains(node.Extents))
                {
                    foreach (var obj in node.HitTestableObjects)
                    {
                        if (obj is CogoPoint cogoPoint) { hits.Add(cogoPoint); }
                    }
                }
                else
                {
                    hits.AddRange(node.HitTestCogoPointsInRect(rect));
                }
            }
            return hits;


            //if (HitTestableObjectTree is null) { return []; }
            //List<CogoPoint> hits = [];
            //ConcurrentBag<CogoPoint> concurrentHits = [];
            //var nodes = HitTestableObjectTree.GetIntersectingNodes(rect);

            //Parallel.For(0, nodes.Count, i =>
            //{
            //    var node = nodes[i];

            //    if (rect.Contains(node.Extents))
            //    {
            //        foreach (var obj in node.HitTestableObjects)
            //        {
            //            if (obj is CogoPoint cogoPoint)
            //            {
            //                concurrentHits.Add(cogoPoint);
            //            }
            //        }
            //    }
            //    else
            //    {
            //        foreach (var point in node.HitTestCogoPointsInRect(rect))
            //        {
            //            concurrentHits.Add(point);
            //        }
            //    }
            //});

            //hits = concurrentHits.ToList();

            //return hits;
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
                        if (_cachedLineVertices is null || _cachedLineVertices.Count == 0) { continue; }
                        ref var vertex = ref GetLineVertexRef(i);
                        vertex.IsMouseOver = isMouseOver ? 1.0f : 0.0f;
                    }
                }
                if (drawingObject is DrawingText3D drawingText)
                {
                    for (int i = drawingText.StartVertexIndex; i <= drawingText.EndVertexIndex; i++)
                    {
                        if (_cachedTextVertices is null || _cachedTextVertices.Count == 0) { continue; }
                        ref var vertex = ref GetTextVertexRef(i);
                        vertex.IsMouseOver = isMouseOver ? 1.0f : 0.0f;
                    }
                }
                if (drawingObject is DrawingBlock3D drawingBlock)
                {
                    for (int i = drawingBlock.StartLineVertexIndex; i <= drawingBlock.EndLineVertexIndex; i++)
                    {
                        if (_cachedLineVertices is null || _cachedLineVertices.Count == 0) { continue; }
                        ref var vertex = ref GetLineVertexRef(i);
                        vertex.IsMouseOver = isMouseOver ? 1.0f : 0.0f;
                    }
                    for (int i = drawingBlock.StartTextVertexIndex; i <= drawingBlock.EndTextVertexIndex; i++)
                    {
                        if (_cachedTextVertices is null || _cachedTextVertices.Count == 0) { continue; }
                        ref var vertex = ref GetTextVertexRef(i);
                        vertex.IsMouseOver = isMouseOver ? 1.0f : 0.0f;
                    }
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
                        if (_cachedLineVertices is null || _cachedLineVertices.Count == 0) { continue; }
                        ref var vertex = ref GetLineVertexRef(i);
                        vertex.IsSelected = isSelected ? 1.0f : 0.0f;
                    }
                }
                if (drawingObject is DrawingText3D drawingText)
                {
                    for (int i = drawingText.StartVertexIndex; i <= drawingText.EndVertexIndex; i++)
                    {
                        if (_cachedTextVertices is null || _cachedTextVertices.Count == 0) { continue; }
                        ref var vertex = ref GetTextVertexRef(i);
                        vertex.IsSelected = isSelected ? 1.0f : 0.0f;
                    }
                }
                if (drawingObject is DrawingBlock3D drawingBlock)
                {
                    for (int i = drawingBlock.StartLineVertexIndex; i <= drawingBlock.EndLineVertexIndex; i++)
                    {
                        if (_cachedLineVertices is null || _cachedLineVertices.Count == 0) { continue; }
                        ref var vertex = ref GetLineVertexRef(i);
                        vertex.IsSelected = isSelected ? 1.0f : 0.0f;
                    }
                    for (int i = drawingBlock.StartTextVertexIndex; i <= drawingBlock.EndTextVertexIndex; i++)
                    {
                        if (_cachedTextVertices is null || _cachedTextVertices.Count == 0) { continue; }
                        ref var vertex = ref GetTextVertexRef(i);
                        vertex.IsSelected = isSelected ? 1.0f : 0.0f;
                    }
                }
            }
            if (hitTestableObject is CogoPoint dxfPoint)
            {

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

                //// For Testing
                //AddObjectTreeNodeLayoutVertices();
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

                PointTextVerticesDirty = false;
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

                    foreach (CogoPoint point in pointGroup.Points)
                    {

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
            float yIncrement = Extents.Height.ToFloat() / (rows - 1);
            float xIncrement = Extents.Width.ToFloat() / (cols - 1);
            int pointNum = 1;
            float elevation = 0;
            string description = "Test Point";

            for (int i = 0; i < rows; i++)
            {
                string pointGroupName = $"TestGroup {i + 1}";
                bool created = CogoPointManager.TryCreatePointGroup(pointGroupName, new SharpDX.Vector4(1.0f, 0.0f, 0.0f, 1.0f), _pointBaseScale, out var pointGroup);
                if (created)
                {
                    var groupActivated = CogoPointManager.TrySetActivePointGroup(pointGroup);
                    if (!groupActivated) { continue; }

                    float y = Extents.Top.ToFloat() + (yIncrement * i);

                    for (int j = 0; j < cols; j++)
                    {
                        float x = Extents.Left.ToFloat() + (xIncrement * j);
                        var pointCreated = CogoPointManager.TryAddPointToActiveGroup(pointNum, new Vector3(x, y, 0), out var point, elevation, description);
                        if (pointCreated) { pointNum++; continue; }
                    }
                }
            }

            CogoPointManager.UpdateCogoPointsList();
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
            HitTestableObjectTree = new(this, Extents, 5);
            HitTestableObjectTreeDirty = false;

            //// For Testing
            //LineVerticesDirty = true;
        }

        private void AddObjectTreeNodeLayoutVertices()
        {
            if (HitTestableObjectTree is null) { return; }

            foreach (var node in HitTestableObjectTree.BaseLevelNodes)
            {
                Vector4 color = new(1, 0, 0, 1);
                var topLeft = new Vector3((float)node.Extents.Left, (float)node.Extents.Top, 0);
                var bottomRight = new Vector3((float)node.Extents.Right, (float)node.Extents.Bottom, 0);
                var bottomLeft = new Vector3((float)node.Extents.Left, (float)node.Extents.Bottom, 0);
                var topRight = new Vector3((float)node.Extents.Right, (float)node.Extents.Top, 0);

                LineVertex topLeftVertex = new(topLeft, color);
                LineVertex bottomRightVertex = new(bottomRight, color);
                LineVertex bottomLeftVertex = new(bottomLeft, color);
                LineVertex topRightVertex = new(topRight, color);

                _cachedLineVertices.Add(topLeftVertex);
                _cachedLineVertices.Add(topRightVertex);

                _cachedLineVertices.Add(bottomLeftVertex);
                _cachedLineVertices.Add(bottomRightVertex);

                _cachedLineVertices.Add(topLeftVertex);
                _cachedLineVertices.Add(bottomLeftVertex);

                _cachedLineVertices.Add(topRightVertex);
                _cachedLineVertices.Add(bottomRightVertex);

                LineVerticesDirty = true;
            }
        }
        #endregion
    }
}
