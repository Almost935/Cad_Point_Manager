using Cad_Point_Manager.Common;
using Cad_Point_Manager.Common.Collections;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.DrawingObjects3D;
using Cad_Point_Manager.Models.HitTesting;
using Cad_Point_Manager.Models.PointRendering;
using netDxf;
using netDxf.Entities;
using netDxf.Tables;
using SharpDX;
using System.ComponentModel;
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
    public class CadManager3D : INotifyPropertyChanged
    {
        #region Fields
        private const float _pointSizeToExtentsFactor = 0.001f;

        private bool _dxfLoaded = false;
        //private bool 
        private bool _lineVerticesDirty = false;
        private bool _textVerticesDirty = false;
        private bool _cogoPointTextVerticesDirty = false;
        private bool _cogoPointCircleVerticesDirty = false;
        private bool _drawingObjectTreeDirty = false;
        private bool _dxfNeedsReload = false;
        private Rect _extents = RectExtensions.Zero;
        private BatchableObservableCollection<KeyValuePair<string, ObjectLayer3D>> _layers = [];
        private ICollectionView _layersView;
        private ICollectionView _pointGroupsView;
        private ICollectionView _pointsView;
        private CogoPointManager _cogoPointManager;
        private Size2F _viewportSize = Size2F.Empty;
        private Enums.SelectionMode _snapSelectionMode = Enums.SelectionMode.CogoPoints;
        private double _pointBaseScale = 1;
        private bool _hitTestingEnabled = true;

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
        public BatchableObservableCollection<KeyValuePair<string, ObjectLayer3D>> Layers
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
        public double PointBaseScale
        {
            get => _pointBaseScale;
            set
            {
                _pointBaseScale = value;
                OnPropertyChanged(nameof(PointBaseScale));
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
            //GetTestDxfPoints();

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
            CogoPointTextVerticesDirty = true;
            CogoPointCircleVerticesDirty = true;
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
                PointBaseScale = 1;
                return;
            }
            if (Extents.Width > Extents.Height)
            {
                PointBaseScale = Extents.Width * _pointSizeToExtentsFactor;
            }
            else
            {
                PointBaseScale = Extents.Height * _pointSizeToExtentsFactor;
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

            PointsView = new ListCollectionView(CogoPointManager.CogoPoints);
            PointsView.GroupDescriptions.Clear();
            PointsView.GroupDescriptions.Add(new PropertyGroupDescription("PointGroup"));
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
            List<CogoPoint> points = [];

            if (HitTestableObjectTree is null) { return points; }

            var nodes = HitTestableObjectTree.GetIntersectingNodes(rect);

            foreach (var node in nodes)
            {
                if (rect.Contains(node.Extents))
                {
                    foreach (var obj in node.HitTestableObjects)
                    {
                        if (obj is CogoPoint cogoPoint) { points.Add(cogoPoint); }
                    }
                }
                else
                {
                    points.AddRange(node.HitTestCogoPointsInRect(rect));
                }
            }

            return points;
        }
        public List<DrawingGeometry3D> HitTestDragGeometries(Rect rect)
        {
            List<DrawingGeometry3D> hits = [];

            if (HitTestableObjectTree is null) { return hits; }

            var nodes = HitTestableObjectTree.GetIntersectingNodes(rect);

            foreach (var node in nodes)
            {
                foreach (var obj in node.HitTestableObjects)
                {
                    if (obj is DrawingGeometry3D geometry &&
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

        public ReadOnlySpan<LineVertex> UpdateLineVerticesList(SceneIdMap sceneIdMap)
        {
            if (LineVerticesDirty)
            {
                _cachedLineVertices.Clear();

                foreach (var keyValuePair in Layers)
                {
                    var layer = keyValuePair.Value;
                    var lId = sceneIdMap.GetOrAddLayerId(layer);

                    foreach (var obj in layer.DrawingObject3Ds)
                    {
                        var objectId = sceneIdMap.GetOrAddObjectId(obj);

                        if (obj is DrawingGeometry3D drawingGeometry)
                        {
                            drawingGeometry.UpdateVertices(lId, objectId);
                            drawingGeometry.StartVertexIndex = _cachedLineVertices.Count;
                            _cachedLineVertices.AddRange(drawingGeometry.Vertices);
                            drawingGeometry.EndVertexIndex = _cachedLineVertices.Count - 1;
                        }

                        if (obj is DrawingBlock3D drawingBlock)
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
                //AddObjectTreeNodeLayoutVertices();
            }
            return CollectionsMarshal.AsSpan(_cachedLineVertices);
        }
        public ReadOnlySpan<TextVertex> UpdateTextVerticesList(ResCache d3DResCache, SceneIdMap sceneIdMap)
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
                    var lid = sceneIdMap.GetOrAddLayerId(layer);
                    if (!layer.IsVisible) continue;

                    foreach (var obj in layer.DrawingObject3Ds)
                    {
                        var objectId = sceneIdMap.GetOrAddObjectId(obj);
                        int start = _cachedTextVertices.Count;

                        if (obj is DrawingText3D text3D)
                        {
                            text3D.UpdateTextVertices(d3DResCache, lid, objectId);
                            text3D.StartVertexIndex = start;
                            _cachedTextVertices.AddRange(text3D.TextVertices);
                            text3D.EndVertexIndex = _cachedTextVertices.Count - 1;
                        }
                        if (obj is DrawingBlock3D drawingBlock)
                        {
                            drawingBlock.UpdateTextVertices(d3DResCache, lid, objectId);
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
        public ReadOnlySpan<PointMarkerInstance> UpdatePointCircleVerticesList(SceneIdMap sceneIdMap)
        {
            _cachedPointMarkerVertices.Clear();

            foreach (var pg in CogoPointManager.PointGroups)
            {
                if (!pg.IsVisible || pg is null) { continue; }
                uint gid = sceneIdMap.GetOrAddGroupId(pg);

                foreach (CogoPoint p in pg.Points)
                {
                    uint pid = sceneIdMap.GetOrAddPointId(p);
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
            CogoPointManager.PointGroups.Clear();

            float rows = 15;
            float cols = 15;
            float yIncrement = (Extents.Height.ToFloat() * 1.5f) / (rows - 1);
            float xIncrement = (Extents.Width.ToFloat() * 1.5f) / (cols - 1);
            int pointNum = 1;
            float elevation = 0;
            string description = "Test Point";

            for (int i = 0; i < rows; i++)
            {
                string pointGroupName = $"TestGroup {i + 1}";
                bool created = CogoPointManager.TryCreatePointGroup(pointGroupName, Colors.Red, out var pointGroup);
                if (created)
                {
                    var groupActivated = CogoPointManager.TrySetActivePointGroup(pointGroup);
                    if (!groupActivated) { continue; }

                    float y = Extents.Top.ToFloat() + (yIncrement * i);

                    for (int j = 0; j < cols; j++)
                    {
                        float x = Extents.Left.ToFloat() + (xIncrement * j);
                        var pointCreated = CogoPointManager.TryAddPointToActiveGroup(pointNum, new Vector3(x, y, 0), out _, elevation, description);
                        if (pointCreated) { pointNum++; continue; }
                    }
                }
            }

            //CogoPointTextVerticesDirty = true;
            //CogoPointCircleVerticesDirty = true;
        }

        public void UpdateHitTestableObjectTree()
        {
            HitTestableObjectTree = new(this, Extents, 5);
            HitTestableObjectTreeDirty = false;

            //// For Testing
            //LineVerticesDirty = true;
        }
        #endregion
    }
}
