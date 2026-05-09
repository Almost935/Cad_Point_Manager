using Cad_Point_Manager.Commands.UndoRedo;
using Cad_Point_Manager.Common;
using Cad_Point_Manager.Common.Collections;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.DrawingObjects;
using Cad_Point_Manager.Models.HitTesting;
using Cad_Point_Manager.Models.PointRendering;
using Cad_Point_Manager.Models.Printing;
using netDxf;
using netDxf.Entities;
using netDxf.Tables;
using SharpDX;
using SharpDX.Direct3D9;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Point = System.Windows.Point;
using Vector2 = SharpDX.Vector2;
using Vector3 = SharpDX.Vector3;

namespace Cad_Point_Manager.Models
{
    public class CadManager : INotifyPropertyChanged
    {
        #region Fields
        private const float _pointSizeToExtentsFactor = 0.001f;

        private bool _dxfLoaded = false;
        private bool _lineVerticesDirty = false;
        private bool _textVerticesDirty = false;
        private bool _cogoPointTextVerticesDirty = false;
        private bool _cogoPointCircleVerticesDirty = false;
        private bool _drawingObjectTreeDirty = false;
        private bool _dxfNeedsReload = false;
        private Rect _extents = RectExtensions.Zero;
        private BatchableObservableCollection<KeyValuePair<string, ObjectLayer>> _layers = [];
        private ICollectionView _layersView;
        private ICollectionView _pointGroupsView;
        private ICollectionView _pointsView;
        private ICollectionView _groupedPointsView;
        private CogoPointManager _cogoPointManager;
        private Size2F _viewportSize = Size2F.Empty;
        private Enums.SelectionMode _snapSelectionMode = Enums.SelectionMode.CogoPoints;
        private bool _hitTestingEnabled = true;
        private BatchableObservableCollection<Layout> _layouts = [];
        private ICollectionView _layoutsView;
        private Camera _camera;

        private readonly List<LineVertex> _cachedLineVertices = [];
        private readonly List<TextVertex> _cachedTextVertices = [];
        private readonly List<TextVertex> _cachedPointTextVertices = [];
        private readonly List<PointMarkerInstance> _cachedPointMarkerVertices = [];
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
        public bool CogoPointTextVerticesDirty
        {
            get => _cogoPointTextVerticesDirty;
            set
            {
                _cogoPointTextVerticesDirty = value;
                OnPropertyChanged(nameof(CogoPointTextVerticesDirty));
            }
        }
        public bool CogoPointCircleVerticesDirty
        {
            get => _cogoPointCircleVerticesDirty;
            set
            {
                _cogoPointCircleVerticesDirty = value;
                OnPropertyChanged(nameof(CogoPointCircleVerticesDirty));
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
        public BatchableObservableCollection<KeyValuePair<string, ObjectLayer>> Layers
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
        public ICollectionView GroupedPointsView
        {
            get => _groupedPointsView;
            set
            {
                _groupedPointsView = value;
                OnPropertyChanged(nameof(GroupedPointsView));
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
        public bool HitTestingEnabled
        {
            get => _hitTestingEnabled;
            set
            {
                _hitTestingEnabled = value;
                OnPropertyChanged(nameof(HitTestingEnabled));
            }
        }
        public BatchableObservableCollection<Layout> Layouts
        {
            get => _layouts;
            set
            {
                _layouts = value;
                OnPropertyChanged(nameof(Layouts));
            }
        }
        public ICollectionView LayoutsView
        {
            get => _layoutsView;
            set
            {
                _layoutsView = value;
                OnPropertyChanged(nameof(LayoutsView));
            }
        }
        public Camera Camera
        {
            get => _camera;
            set
            {
                if (_camera != value)
                {
                    _camera = value;
                    OnPropertyChanged(nameof(Camera));
                }
            }
        }

        public DxfDocument DxfDocument { get; private set; }
        public HitTestableObjectTree HitTestableObjectTree { get; private set; }
        public TextVertex[] NumberVertices { get; set; } = [];
        public UndoRedoManager UndoRedoManager { get; } = new();
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;

        public event Action ZoomToExtentsRequested;
        public event Action ZoomToPointRequested;
        #endregion

        #region Constructor
        public CadManager()
        {
            CogoPointManager = new(this);

            GetCollectionViews();
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

            DxfLoaded = true;
            LineVerticesDirty = true;
            TextVerticesDirty = true;
            CogoPointTextVerticesDirty = true;
            CogoPointCircleVerticesDirty = true;
            HitTestableObjectTreeDirty = true;
            DxfNeedsReload = true;
        }

        public void UpdateExtents()
        {
            var dxfExtents = DxfHelpers.GetBoundsFromHeader(DxfDocument);
            var pointsExtents = CogoPointManager.Extents;
            Extents = Rect.Union(dxfExtents, pointsExtents);
        }

        public bool TryAddLayout(string layoutName, LayoutViewport viewport, out Layout layout)
        {
            if (!Layouts.Any(x => string.Equals(x.Name, layoutName, StringComparison.OrdinalIgnoreCase)))
            {
                layout = new Layout() { Name = layoutName, Viewport = viewport };
                Layouts.Add(layout);
                return true;
            }
            layout = null;
            return false;
        }
        public string GetNextAvailableLayoutName(int startCount = 1)
        {
            int num = startCount;
            string layoutName = GetLayoutName(num);

            while (LayoutNameExists(layoutName))
            {
                num++;
                layoutName = GetLayoutName(num);
            }

            return layoutName;

            string GetLayoutName(int num) { return $"Layout {num}"; }
            bool LayoutNameExists(string name) { return Layouts.Any(layout => layout.Name == name); }

        }
        public bool ValidateLayoutNameChange(string newLayoutName, Layout layout, out string? errorMessage)
        {
            errorMessage = null;

            if (newLayoutName == layout.Name) { return true; }

            if (Layouts.Any(x => x.Name == newLayoutName))
            {
                errorMessage = $"Layout name \"{newLayoutName}\" already exists.";
                return false;
            }
            return true;
        }
        public bool TryDeleteLayout(Layout layout)
        {
            return Layouts.Remove(layout);
        }

        public void ZoomToPoint(CogoPoint p, double paddingFactor)
        {
            double centerX = p.Bounds.Left + (p.Bounds.Width * 0.5);
            double centerY = p.Bounds.Top + (p.Bounds.Height * 0.5);

            Camera.ZoomToBounds(new Rect(centerX - (p.Bounds.Width * paddingFactor * 0.5), centerY - (p.Bounds.Height * paddingFactor * 0.5),
                p.Bounds.Width * paddingFactor, p.Bounds.Height * paddingFactor));
            Camera.IsDirty = true;
            //Zoom to point sometimes not working
            ZoomToPointRequested?.Invoke();
        }

        public void ResetTemplates()
        {
            Layouts.Clear();

            Rect viewportBounds = new(0.5, 0.5, 28.938, 23);
            LayoutViewport viewport = new(viewportBounds, Camera.OverviewScene);
            TryAddLayout(GetNextAvailableLayoutName(), viewport, out var layout);
            TryAddLayout(GetNextAvailableLayoutName(), viewport, out layout);
            TryAddLayout(GetNextAvailableLayoutName(), viewport, out layout);
        }

        public void GetPointScale()
        {
            if (Extents.IsEmpty)
            {
                CogoPointManager.PointBaseScale = 1;
                return;
            }
            if (Extents.Width > Extents.Height)
            {
                CogoPointManager.PointBaseScale = Extents.Width * _pointSizeToExtentsFactor;
            }
            else
            {
                CogoPointManager.PointBaseScale = Extents.Height * _pointSizeToExtentsFactor;
            }
        }

        public void GetCollectionViews()
        {
            LayersView = CollectionViewSource.GetDefaultView(Layers);
            LayersView.SortDescriptions.Clear();
            LayersView.SortDescriptions.Add(new SortDescription("Key", ListSortDirection.Ascending));

            PointGroupsView = new ListCollectionView(CogoPointManager.PointGroups);
            PointGroupsView.SortDescriptions.Clear();
            PointGroupsView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            PointsView = CollectionViewSource.GetDefaultView(CogoPointManager.CogoPoints);
            PointsView.SortDescriptions.Clear();
            PointsView.SortDescriptions.Add(new SortDescription("PointNumber", ListSortDirection.Ascending));

            GroupedPointsView = new ListCollectionView(CogoPointManager.CogoPoints);
            GroupedPointsView.GroupDescriptions.Clear();
            GroupedPointsView.GroupDescriptions.Add(new PropertyGroupDescription("PointGroup"));

            LayoutsView = new ListCollectionView(Layouts);
            LayoutsView.SortDescriptions.Clear();
            LayoutsView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
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
        public List<(double distance, DrawingGeometry geometries)> HitTestGeometries(Point p, float tolerance)
        {
            List<(double distance, DrawingGeometry geometries)> geometries = [];

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
            var nodes = CogoPointManager.CogoPointTree.GetIntersectingNodes(rect);

            foreach (var node in nodes)
            {
                hits.AddRange(node.HitTestPoint(p, rect));
            }
            hits.Sort((x, y) => x.distance.CompareTo(y.distance));
            return hits;
        }

        public List<CogoPoint> HitTestDragCogoPoints(Rect rect)
        {
            List<CogoPoint> points = [];

            if (CogoPointManager.CogoPointTree is null) { return points; }

            var nodes = CogoPointManager.CogoPointTree.GetIntersectingNodes(rect);

            foreach (var node in nodes)
            {
                if (rect.Contains(node.Extents))
                {
                    foreach (var p in node.CogoPoints)
                    {
                        if (p.PointGroup.IsVisible)
                        { points.Add(p); }
                    }
                }
                else
                {
                    points.AddRange(node.HitTestRect(rect));
                }
            }

            return points;
        }
        public List<DrawingGeometry> HitTestDragGeometries(Rect rect)
        {
            List<DrawingGeometry> hits = [];

            if (HitTestableObjectTree is null) { return hits; }

            var nodes = HitTestableObjectTree.GetIntersectingNodes(rect);

            foreach (var node in nodes)
            {
                foreach (var obj in node.HitTestableObjects)
                {
                    if (obj is DrawingGeometry geometry &&
                        geometry.BoundsInRect(rect))
                    {
                        hits.Add(geometry);
                    }
                }
            }
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

            CogoPointTextVerticesDirty = true;
            CogoPointCircleVerticesDirty = true;
        }

        public void ZoomToExtents()
        {
            ZoomToExtentsRequested?.Invoke();
        }

        public ObjectLayer GetLayer(Layer dxfLayer)
        {
            ObjectLayer layer = Layers.FirstOrDefault(x => x.Value.Name == dxfLayer.Name).Value;

            if (layer is not null) { return layer; }
            else
            {
                layer = new(dxfLayer);
                Layers.Add(new KeyValuePair<string, ObjectLayer>(dxfLayer.Name, layer));

                return layer;
            }
        }

        public ReadOnlySpan<LineVertex> UpdateLineVerticesList(SceneIdMap sceneIdMap, D3dStateBuffers stateBuffers)
        {
            if (LineVerticesDirty)
            {
                _cachedLineVertices.Clear();

                foreach (var keyValuePair in Layers)
                {
                    var layer = keyValuePair.Value;
                    var lId = sceneIdMap.GetOrAddLayerId(layer, out var isNewLayer);
                    if (isNewLayer) { stateBuffers.InitializeLayerState(sceneIdMap.LayerCount, layer, lId); }

                    foreach (var obj in layer.DrawingObjects)
                    {
                        var objectId = sceneIdMap.GetOrAddObjectId(obj, out var isNewObj);
                        if (isNewObj) { stateBuffers.InitializeObjectState(sceneIdMap.ObjectCount, obj, objectId); }

                        if (obj is DrawingGeometry drawingGeometry)
                        {
                            drawingGeometry.UpdateVertices(lId, objectId);
                            drawingGeometry.StartVertexIndex = _cachedLineVertices.Count;
                            _cachedLineVertices.AddRange(drawingGeometry.Vertices);
                            drawingGeometry.EndVertexIndex = _cachedLineVertices.Count - 1;
                        }

                        if (obj is DrawingBlock drawingBlock)
                        {
                            drawingBlock.UpdateGeometryVertices(lId, objectId);
                            drawingBlock.StartLineVertexIndex = _cachedLineVertices.Count;
                            _cachedLineVertices.AddRange(drawingBlock.LineVertices);
                            drawingBlock.EndLineVertexIndex = _cachedLineVertices.Count - 1;
                        }
                    }
                }

                LineVerticesDirty = false;

                //// For Testing
                //var tl = new LineVertex(new Vector3((float)Extents.Left, (float)Extents.Top, 0), 0, 0);
                //var tr = new LineVertex(new Vector3((float)Extents.Right, (float)Extents.Top, 0), 0, 0);
                //var bl = new LineVertex(new Vector3((float)Extents.Left, (float)Extents.Bottom, 0), 0, 0);
                //var br = new LineVertex(new Vector3((float)Extents.Right, (float)Extents.Bottom, 0), 0, 0);
                //_cachedLineVertices.Add(tl); _cachedLineVertices.Add(tr);
                //_cachedLineVertices.Add(bl); _cachedLineVertices.Add(br);
                //_cachedLineVertices.Add(br); _cachedLineVertices.Add(tr);
                //_cachedLineVertices.Add(bl); _cachedLineVertices.Add(tl);
            }
            return CollectionsMarshal.AsSpan(_cachedLineVertices);
        }
        public ReadOnlySpan<TextVertex> UpdateTextVerticesList(ResCache d3DResCache, SceneIdMap sceneIdMap, D3dStateBuffers stateBuffers)
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
                    var lid = sceneIdMap.GetOrAddLayerId(layer, out var isNewLayer);
                    if (isNewLayer) { stateBuffers.InitializeLayerState(sceneIdMap.LayerCount, layer, lid); }

                    if (!layer.IsVisible) { continue; }

                    foreach (var obj in layer.DrawingObjects)
                    {
                        if (obj is DrawingSText text)
                        {
                            text.UpdateTextVertices(d3DResCache, lid, sceneIdMap, stateBuffers);
                            text.StartVertexIndex = _cachedTextVertices.Count;
                            _cachedTextVertices.AddRange(text.TextVertices);
                            text.EndVertexIndex = _cachedTextVertices.Count - 1;
                        }
                        if (obj is DrawingMtext mtext)
                        {
                            mtext.UpdateTextVertices(d3DResCache, lid, sceneIdMap, stateBuffers);
                            mtext.StartVertexIndex = _cachedTextVertices.Count;
                            _cachedTextVertices.AddRange(mtext.TextVertices);
                            mtext.EndVertexIndex = _cachedTextVertices.Count - 1;
                        }
                        if (obj is DrawingBlock drawingBlock)
                        {
                            drawingBlock.UpdateTextVertices(d3DResCache, lid, sceneIdMap, stateBuffers);
                            drawingBlock.StartTextVertexIndex = _cachedTextVertices.Count;
                            _cachedTextVertices.AddRange(drawingBlock.TextVertices);
                            drawingBlock.EndTextVertexIndex = _cachedTextVertices.Count - 1;
                        }
                    }
                }
                TextVerticesDirty = false;
            }

            return CollectionsMarshal.AsSpan(_cachedTextVertices);
        }
        public ReadOnlySpan<PointMarkerInstance> UpdatePointCircleVerticesList(SceneIdMap sceneIdMap, D3dStateBuffers stateBuffers)
        {
            _cachedPointMarkerVertices.Clear();

            foreach (var pg in CogoPointManager.PointGroups)
            {
                if (!pg.IsVisible || pg is null) { continue; }
                uint gid = sceneIdMap.GetOrAddGroupId(pg, out var isNewGroup);
                if (isNewGroup) { stateBuffers.InitializeGroupState(sceneIdMap.GroupCount, pg, gid); }

                foreach (CogoPoint p in pg.Points)
                {
                    uint pid = sceneIdMap.GetOrAddPointId(p, out var isNewPoint);
                    if (isNewPoint) { stateBuffers.InitializePointState(sceneIdMap.PointCount, p, pid, gid); }

                    _cachedPointMarkerVertices.Add(new PointMarkerInstance
                    {
                        Position = Vector3.Zero,
                        Radius = GlobalHelperProperties.CogoPointCirclePixelRadius,
                        PointId = pid,
                    });
                }
            }
            CogoPointCircleVerticesDirty = false;
            return CollectionsMarshal.AsSpan(_cachedPointMarkerVertices);
        }

        public void GetTestDxfPoints()
        {
            CogoPointManager.Reset();

            var inflatedExtents = Rect.Inflate(Extents, Extents.Width * 0.1, Extents.Height * 0.1);
            float rows = 15;
            float cols = 15;
            float yIncrement = (inflatedExtents.Height / (rows - 1)).ToFloat();
            float xIncrement = (inflatedExtents.Width / (cols - 1)).ToFloat();
            int pointNum = 1;
            string description = "Test Point";
            Random random = new();

            for (int i = 0; i < rows; i++)
            {
                string pointGroupName = $"TestGroup {i + 1}";
                bool created = CogoPointManager.TryCreatePointGroup(pointGroupName, Colors.Red, out var pointGroup);
                if (created)
                {
                    var groupActivated = CogoPointManager.TrySetActivePointGroup(pointGroup);
                    if (!groupActivated) { continue; }

                    float y = inflatedExtents.Top.ToFloat() + (yIncrement * i);

                    for (int j = 0; j < cols; j++)
                    {
                        float x = inflatedExtents.Left.ToFloat() + (xIncrement * j);
                        var pointCreated = CogoPointManager.TryAddPointToActiveGroup(pointNum, new Vector3(x, y, 0), out _,
                            (Math.Round(300 + random.NextDouble() * 100, 3)).ToFloat(), description);
                        if (pointCreated) { pointNum++; continue; }
                    }
                }
            }
        }

        public void UpdateHitTestableObjectTree()
        {
            HitTestableObjectTree = new(this, Extents, 5);
            HitTestableObjectTreeDirty = false;

            //// DrawingObjectTree Testing
            //foreach (var node in CogoPointManager.CogoPointTree.BaseLevelNodes)
            //{
            //    Vector4 color = new(0, 0, 0, 1);
            //    var topLeft = new Vector3((float)node.Extents.Left, (float)node.Extents.Top, 0);
            //    var bottomRight = new Vector3((float)node.Extents.Right, (float)node.Extents.Bottom, 0);
            //    var bottomLeft = new Vector3((float)node.Extents.Left, (float)node.Extents.Bottom, 0);
            //    var topRight = new Vector3((float)node.Extents.Right, (float)node.Extents.Top, 0);

            //    LineVertex topLeftVertex = new(topLeft, color);
            //    LineVertex bottomRightVertex = new(bottomRight, color);
            //    LineVertex bottomLeftVertex = new(bottomLeft, color);
            //    LineVertex topRightVertex = new(topRight, color);

            //    _cachedLineVertices.Add(topLeftVertex);
            //    _cachedLineVertices.Add(topRightVertex);

            //    _cachedLineVertices.Add(bottomLeftVertex);
            //    _cachedLineVertices.Add(bottomRightVertex);

            //    _cachedLineVertices.Add(topLeftVertex);
            //    _cachedLineVertices.Add(bottomLeftVertex);

            //    _cachedLineVertices.Add(topRightVertex);
            //    _cachedLineVertices.Add(bottomRightVertex);
            //}
        }
        public void UpdateCogoPointTree()
        {
            CogoPointManager.UpdateCogoPointTree();
        }
        #endregion
    }
}
