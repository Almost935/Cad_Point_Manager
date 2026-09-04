using Cad_Point_Manager.Commands.UndoRedo;
using Cad_Point_Manager.Common;
using Cad_Point_Manager.Common.Collections;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Helpers;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Msdf;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.DrawingObjects;
using Cad_Point_Manager.Models.DrawingObjects.Dimensioning;
using Cad_Point_Manager.Models.DrawingObjects.HelperClasses;
using Cad_Point_Manager.Models.DxfImport;
using Cad_Point_Manager.Models.HitTesting;
using Cad_Point_Manager.Models.Importing;
using Cad_Point_Manager.Models.PointRendering;
using Cad_Point_Manager.Models.Printing;
using netDxf.Entities;
using netDxf.Tables;
using SharpDX;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
using Vector2 = SharpDX.Vector2;
using Vector3 = SharpDX.Vector3;
using Vector4 = SharpDX.Vector4;

namespace Cad_Point_Manager.Models
{
    public class CadManager : INotifyPropertyChanged
    {
        #region Fields
        private const float _pointSizeToExtentsFactor = 0.001f;

        private LineTypeCache _lineTypeCache;

        private bool _dxfLoaded = false;
        private bool _lineVerticesDirty = false;
        private bool _textVerticesDirty = false;
        private bool _solidVerticesDirty = false;
        private bool _cogoPointTextVerticesDirty = false;
        private bool _cogoPointCircleVerticesDirty = false;
        private bool _drawingObjectTreeDirty = false;
        private bool _dxfNeedsReload = false;
        private Rect _extents = RectExtensions.Zero;
        private Rect _dxfExtents = RectExtensions.Zero;
        private Rect _pointExtents = RectExtensions.Zero;
        private BatchableObservableCollection<KeyValuePair<string, ObjectLayer>> _layers = [];
        private BatchableObservableCollection<PointGroup> _pointGroups = [];
        private BatchableObservableCollection<CogoPoint> _cogoPoints = [];
        private ICollectionView _layersView;
        private ICollectionView _pointGroupsView;
        private ICollectionView _pointsView;
        private ICollectionView _groupedPointsView;
        private Size2F _viewportSize = Size2F.Empty;
        private SelectionMode _snapSelectionMode = SelectionMode.CogoPoints;
        private bool _hitTestingEnabled = true;
        private BatchableObservableCollection<Layout> _layouts = [];
        private ICollectionView _layoutsView;
        private Camera _camera;
        private PointGroup _activePointGroup;
        private double _pointBaseScale = 1;

        // Testing Fields
        private List<LineInstance> _cogoPointBoundsLines = [];
        // End Testing Fields
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
        public bool SolidVerticesDirty
        {
            get => _solidVerticesDirty;
            set
            {
                _solidVerticesDirty = value;
                OnPropertyChanged(nameof(SolidVerticesDirty));
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
        public Rect DxfExtents
        {
            get => _dxfExtents;
            set
            {
                _dxfExtents = value;
                OnPropertyChanged(nameof(DxfExtents));
            }
        }
        public Rect PointExtents
        {
            get => _pointExtents;
            set
            {
                _pointExtents = value;
                OnPropertyChanged(nameof(PointExtents));
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
        public BatchableObservableCollection<PointGroup> PointGroups
        {
            get => _pointGroups;
            private set
            {
                _pointGroups = value;
                OnPropertyChanged(nameof(PointGroups));
            }
        }
        public BatchableObservableCollection<CogoPoint> CogoPoints
        {
            get => _cogoPoints;
            set
            {
                if (_cogoPoints != value)
                {
                    _cogoPoints = value;
                    OnPropertyChanged(nameof(CogoPoints));
                }
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
        public Size2F ViewportSize
        {
            get => _viewportSize;
            set
            {
                _viewportSize = value;
                OnPropertyChanged(nameof(ViewportSize));
            }
        }
        public SelectionMode SnapSelectionMode
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
        public PointGroup ActivePointGroup
        {
            get => _activePointGroup;
            set
            {
                if (_activePointGroup != value)
                {
                    _activePointGroup = value;
                    OnPropertyChanged(nameof(ActivePointGroup));
                }
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

        public DxfImportResult DxfImportResult { get; private set; }
        public HitTestableObjectTree HitTestableObjectTree { get; private set; }
        public TextVertex[] NumberVertices { get; set; } = [];
        public UndoRedoManager UndoRedoManager { get; } = new();
        public CogoPointTree CogoPointTree { get; set; }
        public float OverallDrawingLineTypeScale { get; set; }

        public List<int> UsedPointNumbers => PointGroups.SelectMany(pg => GetPoints(pg)).Select(p => p.PointNumber).ToList();
        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;

        public event Action ZoomToExtentsRequested;
        public event Action ZoomToPointRequested;
        #endregion

        #region Constructor
        public CadManager()
        {
            CogoPoints.CollectionChanged += CogoPoints_CollectionChanged;
            GetCollectionViews();
        }
        #endregion

        #region Methods
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void LoadDxf(DxfImportResult dxfImportResult)
        {
            ClearDxf();
            DxfImportResult = dxfImportResult;
            Extents = DxfHelpers.GetBoundsFromHeader(DxfImportResult.DxfDocument);
            _lineTypeCache = new(DxfImportResult.DxfDocument);
            GetPointScale();

            GetTestDxfPoints();

            OverallDrawingLineTypeScale = DxfImportResult.DxfDocument.DrawingVariables.LtScale.ToFloat();

            foreach (var layer in dxfImportResult.DxfDocument.Layers)
            {
                GetLayer(layer);
            }

            foreach (var e in DxfImportResult.DxfDocument.Entities.All)
            {
                if (e is MText mtext && string.IsNullOrWhiteSpace(mtext.Value)) { continue; }
                if (e is Text text && string.IsNullOrWhiteSpace(text.Value)) { continue; }

                var layer = GetLayer(e.Layer);
                var dxfLineType = DxfHelpers.GetLineType(e, dxfImportResult.DxfDocument);

                var drawingObj = DxfHelpers.GetDrawingObject(e, layer, DxfHelpers.GetEntityObjectColor(e),
                    DxfHelpers.GetColorType(e), _lineTypeCache.GetLineType(dxfLineType));

                if (layer is not null && drawingObj is not null)
                {
                    layer.AddDrawingObject(drawingObj);
                }
            }

            foreach (var mleader in dxfImportResult.MLeaders)
            {
                if (dxfImportResult.MLeaderStyles.TryGetValue(mleader.LeaderStyleId, out var style))
                {
                    mleader.Style = style;
                }

                var textStyle = dxfImportResult.DxfDocument.TextStyles.FirstOrDefault(ts => ts.Handle == mleader.TextStyleId);

                if (!dxfImportResult.DxfDocument.Layers.TryGetValue(mleader.LayerName, out Layer dxfLayer))
                {
                    throw new Exception($"Layer \"{mleader.LayerName}\" not found in DXF document.");
                }
                else
                {
                    var layer = GetLayer(dxfLayer);

                    bool lineTypeResolved = GetParsedMleaderLineType(mleader, layer, dxfImportResult.DxfDocument.Linetypes, out var dxfLineType);
                    if (!lineTypeResolved) { throw new Exception($"Line type not found in DXF document."); }

                    var lineType = _lineTypeCache.GetLineType(dxfLineType);

                    var blockExists = ArrowheadToNetDxfBlockNameResolver.ResolveArrowhead(mleader.Style.ArrowheadType, out string blockName);
                    DrawingBlock? arrowHeadBlock = null;

                    if (blockExists && dxfImportResult.DxfDocument.Blocks.TryGetValue(blockName, out var dxfBlock))
                    {
                        Insert insert = new(dxfBlock);

                        arrowHeadBlock = DxfHelpers.GetDrawingObject(
                            insert, layer, DxfHelpers.GetEntityObjectColor(insert), DxfHelpers.GetColorType(insert),
                            _lineTypeCache.GetLineType(DxfHelpers.GetLineType(insert, dxfImportResult.DxfDocument))) as DrawingBlock;
                    }
                    DrawingMleader drawingMleader = new(
                        mleader, layer, textStyle, lineType, false, null, arrowHeadBlock);

                    if (layer is not null && drawingMleader is not null)
                    {
                        layer.AddDrawingObject(drawingMleader);
                    }
                }
            }

            UpdateDxfExtents();
            UpdateExtents();

            DxfLoaded = true;
            LineVerticesDirty = true;
            TextVerticesDirty = true;
            SolidVerticesDirty = true;
            CogoPointTextVerticesDirty = true;
            CogoPointCircleVerticesDirty = true;
            HitTestableObjectTreeDirty = true;
            DxfNeedsReload = true;
        }

        #region CogoPoint related methods
        public bool PointExists(int pointNumber) => PointGroups.SelectMany(pg => GetPoints(pg)).Any(p => p.PointNumber == pointNumber);
        public bool TryCreatePoint(int pointNumber, Vector3 position, PointGroup pg, out CogoPoint? point, float elevation = 0, string description = "")
        {
            var cmd = new CreatePointCommand(
                this, pointNumber, position, pg.Name, elevation, description);

            UndoRedoManager.Execute(cmd);

            point = cmd.CreatedPoint;

            return cmd.Succeeded;
        }
        internal bool TryCreatePointInternal(int pointNumber, Vector3 position, string pgName, out CogoPoint? point,
            out string? errorMessage, float elevation = 0, string description = "")
        {
            var pgExists = TryGetPointGroup(pgName, out var pg);

            if (!pgExists)
            {
                point = null;
                errorMessage = "Point group does not exist.";
                return false;
            }
            if (!IsValidPointName(pointNumber, out errorMessage))
            {
                point = null;
                return false;
            }

            point = new(pg, pointNumber, position, this, elevation, description);
            CogoPoints.Add(point);
            errorMessage = null;

            return true;
        }
        public bool TryCreatePoints(
            IEnumerable<(int pointNumber, Vector3 position, PointGroup pg, float elevation, string description)> pointData,
            out List<CogoPoint> createdPoints,
            out List<string> errorMessages)
        {
            createdPoints = [];
            errorMessages = [];
            var commands = new List<IUndoableCommand>();

            foreach (var p in pointData)
            {
                var cmd = new CreatePointCommand(
                    this,
                    p.pointNumber,
                    p.position,
                    p.pg.Name,
                    p.elevation,
                    p.description);

                commands.Add(cmd);

                if (cmd.ErrorMessage is not null)
                {
                    errorMessages.Add(cmd.ErrorMessage);
                }
            }

            var composite = new CompositeCommand(
                this,
                "Create Multiple Points",
                commands);

            using (CogoPoints.DeferNotifications())
            {
                UndoRedoManager.Execute(composite);
            }

            foreach (var cmd in commands.OfType<CreatePointCommand>())
            {
                if (cmd.CreatedPoint != null)
                {
                    createdPoints.Add(cmd.CreatedPoint);
                }
            }

            return composite.Succeeded;
        }
        public bool TryImportPoints(IEnumerable<ParsedPointImportRow> points)
        {
            var cmd = new ImportPointsCommand(this, points);
            UndoRedoManager.Execute(cmd);

            return cmd.Succeeded;
        }
        public bool TryAddPoint(CogoPoint p, PointGroup pg)
        {
            if (pg == null || !PointGroupExists(pg))
            {
                return false;
            }

            if (PointNumberExists(p.PointNumber) || !IsValidPointName(p.PointNumber, out _))
            {
                return false;
            }

            CogoPoints.Add(p);

            return true;
        }
        public bool TryDeletePoint(CogoPoint point)
        {
            var cmd = new DeletePointCommand(this, point);
            UndoRedoManager.Execute(cmd);

            return cmd.Disposed;
        }
        internal bool TryDeletePointInternal(CogoPoint point)
        {
            bool deleted = false;
            if (point != null && point.PointGroup != null)
            {
                CogoPoints.Remove(point);
            }
            return deleted;
        }
        public List<CogoPointDto> GetCogoPointDtos()
        {
            return CogoPoints.Select(p => new CogoPointDto(p)).ToList();
        }
        public int GetNextAvailablePointNumber(int startCount)
        {
            int num = startCount;
            while (PointNumberExists(num)) { num++; }
            return num;
        }
        public bool PointNumberExists(int num)
        {
            return PointGroups.SelectMany(pg => GetPoints(pg)).Any(p => p.PointNumber == num);
        }
        public bool ValidatePointNameChange(int pointNumber, CogoPoint p, out string? errorMessage)
        {
            errorMessage = null;

            if (pointNumber == p.PointNumber) { return true; }

            if (!IsValidPointName(pointNumber, out errorMessage))
            {
                return false;
            }
            return true;
        }
        public bool IsValidPointName(int pointNumber, out string? errorMessage)
        {
            errorMessage = null;
            if (pointNumber <= 0)
            {
                errorMessage = "Point number must be greater than zero.";
                return false;
            }
            if (PointNumberExists(pointNumber))
            {
                errorMessage = $"Point number \"{pointNumber}\" already exists.";
                return false;
            }
            return true;
        }
        public IEnumerable<CogoPoint> GetPoints(PointGroup group)
        {
            return CogoPoints.Where(p => p.PointGroup == group);
        }
        private void CogoPoints_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (var pg in PointGroups)
            {
                pg.NotifyPointCountChanged();
            }
        }
        #endregion

        #region PointGroup Related Methods
        public bool TryCreatePointGroup(string name, Color color, out PointGroup? pointGroup)
        {
            var cmd = new CreatePointGroupCommand(this, name, color);
            UndoRedoManager.Execute(cmd);
            pointGroup = cmd.CreatedPointGroup;

            return pointGroup is not null;
        }
        internal bool TryCreatePointGroupInternal(string name, Color color, out PointGroup? pointGroup, out string? errorMessage)
        {
            if (!IsValidPointGroupName(name, out errorMessage))
            {
                pointGroup = null;
                return false;
            }

            pointGroup = new PointGroup(name, color, this, PointBaseScale);
            PointGroups.Add(pointGroup);

            return true;
        }
        public bool TryGetPointGroup(string groupName, out PointGroup pointGroup)
        {
            pointGroup = PointGroups.FirstOrDefault(pg => pg.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));
            if (pointGroup is null)
            {
                return false;
            }
            return true;
        }
        internal PointGroup GetPointGroup(string groupName)
        {
            var pgExists = TryGetPointGroup(groupName, out PointGroup pointGroup);
            if (!pgExists)
            {
                var isValidName = IsValidPointGroupName(groupName, out string? errorMessage);
                if (!isValidName)
                {
                    throw new ArgumentException($"Invalid point group name: {errorMessage}");
                }
                pointGroup = new(groupName, Colors.Black, this, PointBaseScale);
                PointGroups.Add(pointGroup);
            }
            return pointGroup;
        }
        public void DeletePointGroup(PointGroup pg)
        {
            var pts = GetPoints(pg);

            if (pts.Count() > 0)
            {
                var copy = pts.ToList();

                foreach (var p in copy)
                {
                    TryDeletePoint(p);
                }
            }
            PointGroups.Remove(pg);
        }
        public void TryDeletePointGroup(PointGroup pg)
        {
            var pts = GetPoints(pg);

            if (pts.Count() > 0)
            {
                var result = MessageBox.Show(
                    "This will delete all points associated with this group. Continue?",
                    "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

                if (result != MessageBoxResult.Yes)
                {
                    foreach (var p in pts)
                    {
                        TryDeletePoint(p);
                    }
                }
                else { return; }
            }
            PointGroups.Remove(pg);
        }
        public void MergePointGroups(List<PointGroup> mergePGs, PointGroup destinationPG)
        {
            var copy = mergePGs.ToList();
            foreach (var pg in copy) // Enumerate a copy
            {
                bool removed = PointGroups.Remove(pg);
                if (removed)
                {
                    foreach (var p in GetPoints(pg).ToList())
                    {
                        p.UpdatePointGroup(destinationPG);
                    }
                }
            }

        }
        public List<PointGroupDto> GetPointGroupDtos()
        {
            return PointGroups.Select(pg => new PointGroupDto(pg)).ToList();
        }
        public bool TrySetActivePointGroup(PointGroup pointGroup)
        {
            bool exists = TryGetPointGroup(pointGroup.Name, out PointGroup verifiedPointGroup);
            if (exists)
            {
                ActivePointGroup = verifiedPointGroup;
                return true;
            }
            return false;
        }
        public bool TryAddPointToActiveGroup(int pointNum, Vector3 position, out CogoPoint cogoPoint, float elevation = 0, string description = "")
        {
            if (ActivePointGroup == null || PointNumberExists(pointNum))
            {
                cogoPoint = null;
                return false;
            }

            cogoPoint = new(ActivePointGroup, pointNum, position, this, elevation, description);
            CogoPoints.Add(cogoPoint);

            return true;
        }
        public string GetTempPointGroupName()
        {
            string baseName = "New Group";
            int counter = 1;
            string groupName = baseName + $" {counter}";
            while (PointGroupNameExists(groupName))
            {
                groupName = $"{baseName} {counter}";
                counter++;
            }
            return groupName;
        }
        public bool PointGroupExists(PointGroup pg)
        {
            return PointGroups.Any(p => p == pg);
        }
        public bool IsValidPointGroupName(string name, out string? errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(name))
            {
                errorMessage = "Name cannot be empty or whitespace.";
                return false;
            }

            // Trim spaces just for validation purposes
            name = name.Trim();

            // Disallowed characters
            char[] invalidChars = Path.GetInvalidFileNameChars(); // includes \ / : * ? " < > | and control characters
            if (name.IndexOfAny(invalidChars) >= 0)
            {
                errorMessage = $"Name contains invalid characters: {string.Join(" ", invalidChars)}";
                return false;
            }

            // Verify uniqueness
            if (PointGroups.Any(pg => pg.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                errorMessage = "A point group with this name already exists.";
                return false;
            }

            return true;
        }
        public bool PointGroupNameExists(string name)
        {
            return PointGroups.Any(pg => pg.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
        public bool IsValidPointScale(string input, out string? errorMessage)
        {
            errorMessage = null;
            if (string.IsNullOrWhiteSpace(input))
            {
                errorMessage = "Point scale cannot be empty.";
                return false;
            }
            if (!double.TryParse(input, out double scale))
            {
                errorMessage = "Point scale must be a valid number.";
                return false;
            }
            if (scale <= 0)
            {
                errorMessage = "Point scale must be greater than zero.";
                return false;
            }
            return true;
        }
        public void ChangePointGroupName(PointGroup group, string newName)
        {
            List<IUndoableCommand> commands = [];

            if (group.Name == newName)
            {
                return;
            }

            if (!IsValidPointGroupName(
                    newName,
                    out string error))
            {
                return;
            }

            if (UndoRedoManager.LastCommand is CreatePointGroupCommand createCmd && createCmd.CreatedPointGroup == group)
            {
                createCmd.SetFinalName(newName);
                return;
            }

            commands.Add(
                new PropertyChangeCommand<string>(
                    "Edit Point Group Name",
                    v => group.Name = v,
                    group.Name,
                    newName));

            if (commands.Count == 0)
            {
                return;
            }

            UndoRedoManager.Execute(
                new CompositeCommand(
                    this,
                    "Edit Point Group Name",
                    commands));
        }
        public void ChangePointGroupScale(IEnumerable<PointGroup> groups, double newScale)
        {
            List<IUndoableCommand> commands = [];

            foreach (var pg in groups)
            {
                if (Math.Abs(pg.PointScale - newScale) < 0.0001)
                {
                    continue;
                }

                commands.Add(
                    new PropertyChangeCommand<double>(
                        "Edit Point Group Scale",
                        v => pg.PointScale = v,
                        pg.PointScale,
                        newScale));
            }

            if (commands.Count == 0)
            {
                return;
            }

            UndoRedoManager.Execute(
                new CompositeCommand(
                    this,
                    "Edit Point Group Scale",
                    commands));
        }
        public void ChangePointGroupVisibility(IEnumerable<PointGroup> groups, bool isVisible)
        {
            List<IUndoableCommand> commands = [];

            foreach (var pg in groups)
            {
                if (pg.IsVisible == isVisible)
                {
                    continue;
                }

                commands.Add(
                    new PropertyChangeCommand<bool>(
                        "Edit Point Group Visibility",
                        v => pg.IsVisible = v,
                        pg.IsVisible,
                        isVisible));
            }

            if (commands.Count == 0)
            {
                return;
            }

            UndoRedoManager.Execute(
                new CompositeCommand(
                    this,
                    "Edit Point Group Visibility",
                    commands));
        }
        public void ChangePointGroupColor(IEnumerable<PointGroup> groups, Color color)
        {
            List<IUndoableCommand> commands = [];

            foreach (var pg in groups)
            {
                if (pg.Color == color)
                {
                    continue;
                }

                commands.Add(
                    new PropertyChangeCommand<Color>(
                        "Edit Point Group Color",
                        v => pg.Color = v,
                        pg.Color,
                        color));
            }

            if (commands.Count == 0)
            {
                return;
            }

            UndoRedoManager.Execute(
                new CompositeCommand(
                    this,
                    "Edit Point Group Color",
                    commands));
        }
        #endregion

        #region Layer related methods
        public void ChangeLayerColor(IEnumerable<ObjectLayer> layers, Vector4 newColor)
        {
            List<IUndoableCommand> commands = [];

            foreach (var layer in layers)
            {
                if (layer.Color == newColor) { continue; }

                commands.Add(
                    new PropertyChangeCommand<Vector4>(
                        "Edit Layer Color",
                        v => layer.Color = v,
                        layer.Color,
                        newColor));
            }

            if (commands.Count == 0)
            {
                return;
            }

            UndoRedoManager.Execute(
                new CompositeCommand(
                    this,
                    "Edit Point Group Name",
                    commands));
        }
        public void ChangeLayerVisibility(IEnumerable<ObjectLayer> layers, bool isVisible)
        {
            List<IUndoableCommand> commands = [];

            foreach (var layer in layers)
            {
                if (layer.IsVisible == isVisible)
                {
                    continue;
                }

                commands.Add(
                    new PropertyChangeCommand<bool>(
                        "Edit Layer Visibility",
                        v => layer.IsVisible = v,
                        layer.IsVisible,
                        isVisible));
            }

            if (commands.Count == 0) { return; }

            UndoRedoManager.Execute(
                new CompositeCommand(
                    this,
                    "Edit Layer Visibility",
                    commands));
        }
        public ObjectLayer GetLayer(Layer dxfLayer)
        {
            ObjectLayer layer = Layers.FirstOrDefault(x => x.Value.Name == dxfLayer.Name).Value;

            if (layer is not null) { return layer; }
            else
            {
                layer = new(dxfLayer, _lineTypeCache.GetLineType(dxfLayer.Linetype));
                Layers.Add(new KeyValuePair<string, ObjectLayer>(dxfLayer.Name, layer));

                return layer;
            }
        }
        #endregion

        #region Layout related methods
        public bool TryCreateLayout(string layoutName, LayoutViewport viewport, out Layout layout)
        {
            var cmd = new CreateLayoutCommand(this, layoutName, viewport);
            UndoRedoManager.Execute(cmd);
            layout = cmd.CreatedLayout;
            return layout != null;
        }
        public bool TryCreateLayoutInternal(string layoutName, LayoutViewport viewport, out Layout layout, out string? errorMessage)
        {
            if (Layouts.Any(x => string.Equals(x.Name, layoutName, StringComparison.OrdinalIgnoreCase)))
            {
                errorMessage = $"Layout name \"{layoutName}\" already exists.";
                layout = null;
                return false;
            }
            layout = new Layout() { Name = layoutName, Viewport = viewport };
            Layouts.Add(layout);
            errorMessage = null;
            return true;
        }
        public void ChangeLayoutName(IEnumerable<PointGroup> groups, string newName)
        {
            List<IUndoableCommand> commands = [];

            foreach (var pg in groups)
            {
                if (pg.Name == newName)
                {
                    continue;
                }

                if (!IsValidPointGroupName(
                        newName,
                        out string error))
                {
                    return;
                }

                commands.Add(
                    new PropertyChangeCommand<string>(
                        "Edit Point Group Name",
                        v => pg.Name = v,
                        pg.Name,
                        newName));
            }

            if (commands.Count == 0)
            {
                return;
            }

            UndoRedoManager.Execute(
                new CompositeCommand(
                    this,
                    "Edit Point Group Name",
                    commands));
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
        public bool IsValidLayoutName(string name, out string? errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(name))
            {
                errorMessage = "Name cannot be empty or whitespace.";
                return false;
            }

            // Trim spaces just for validation purposes
            name = name.Trim();

            // Disallowed characters
            char[] invalidChars = Path.GetInvalidFileNameChars(); // includes \ / : * ? " < > | and control characters
            if (name.IndexOfAny(invalidChars) >= 0)
            {
                errorMessage = $"Name contains invalid characters: {string.Join(" ", invalidChars)}";
                return false;
            }

            // Verify uniqueness
            if (Layouts.Any(layout => layout.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                errorMessage = "A layout with this name already exists.";
                return false;
            }

            return true;
        }
        public bool ValidateLayoutNameChange(string newLayoutName, Layout layout, out string? errorMessage)
        {
            errorMessage = null;

            if (newLayoutName == layout.Name) { return true; }

            if (!IsValidLayoutName(newLayoutName, out errorMessage))
            {
                return false;
            }
            return true;
        }
        public bool TryDeleteLayout(Layout layout)
        {
            return Layouts.Remove(layout);
        }
        #endregion
        public void ZoomToPoint(CogoPoint p, double paddingFactor)
        {
            double centerX = p.Bounds.Left + (p.Bounds.Width * 0.5);
            double centerY = p.Bounds.Top + (p.Bounds.Height * 0.5);

            Camera.ZoomToBounds(new Rect(centerX - (p.Bounds.Width * paddingFactor * 0.5), centerY - (p.Bounds.Height * paddingFactor * 0.5),
                p.Bounds.Width * paddingFactor, p.Bounds.Height * paddingFactor));
            Camera.IsDirty = true;

            ZoomToPointRequested?.Invoke();
        }

        public void ResetTemplates()
        {
            Layouts.Clear();

            Rect viewportBounds = new(0.5, 0.5, 28.938, 23);
            LayoutViewport viewport = new(viewportBounds, Camera.OverviewScene);
            TryCreateLayout(GetNextAvailableLayoutName(), viewport, out _);
            UndoRedoManager.Clear();
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

            PointGroupsView = new ListCollectionView(PointGroups);
            PointGroupsView.SortDescriptions.Clear();
            PointGroupsView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            PointsView = CollectionViewSource.GetDefaultView(CogoPoints);
            PointsView.SortDescriptions.Clear();
            PointsView.SortDescriptions.Add(new SortDescription("PointNumber", ListSortDirection.Ascending));

            GroupedPointsView = new ListCollectionView(CogoPoints);
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

            List<(SignificantPointType pointType, double distance, Vector2 vector)> significantPoints = [];
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
        public List<(double distance, CogoPoint points)> HitTestCogoPoints(Point p, double tolerance, MsdfAtlas atlas)
        {
            List<(double distance, CogoPoint points)> hits = [];

            if (HitTestableObjectTree is null) { return hits; }

            Rect rect = new(p.X - tolerance, p.Y - tolerance, tolerance * 2, tolerance * 2);
            var nodes = CogoPointTree.GetIntersectingNodes(rect);

            foreach (var node in nodes)
            {
                hits.AddRange(node.HitTestPoint(p, rect, atlas));
            }
            hits.Sort((x, y) => x.distance.CompareTo(y.distance));

            return hits;
        }

        public List<CogoPoint> HitTestDragCogoPoints(Rect rect, MsdfAtlas atlas)
        {
            List<CogoPoint> points = [];

            if (CogoPointTree is null) { return points; }

            var nodes = CogoPointTree.GetIntersectingNodes(rect);

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
            DxfImportResult = null;

            Layers.Clear();

            DxfLoaded = false;
            LineVerticesDirty = true;
            TextVerticesDirty = true;
            SolidVerticesDirty = true;
        }
        public void ClearDxfPoints()
        {
            PointGroups.Clear();
            CogoPoints.Clear();

            CogoPointTextVerticesDirty = true;
            CogoPointCircleVerticesDirty = true;
        }

        public void ZoomToExtents()
        {
            ZoomToExtentsRequested?.Invoke();
        }

        public void BuildLineInstances(List<LineInstance> instances, ResCache resCache, SceneIdMap sceneIdMap, D3dStateBuffers stateBuffers)
        {
            if (LineVerticesDirty)
            {
                instances.Clear();

                foreach (var keyValuePair in Layers)
                {
                    var layer = keyValuePair.Value;
                    var lId = sceneIdMap.GetOrAddLayerId(layer, out var isNewLayer);
                    if (isNewLayer) { stateBuffers.InitializeLayerState(sceneIdMap.MaxLayerId, layer, lId); }

                    foreach (var obj in layer.DrawingObjects)
                    {
                        uint ltId = sceneIdMap.GetOrAddLineTypeId(obj.LineType, out var isNewLtype);
                        if (isNewLtype) { stateBuffers.InitializeLineTypeState(sceneIdMap.MaxLineTypeId, obj.LineType, ltId); }

                        if (obj is DrawingGeometry drawingGeometry)
                        {
                            var objectId = sceneIdMap.GetOrAddObjectId(obj, out var isNewObj);
                            if (isNewObj) { stateBuffers.InitializeObjectState(sceneIdMap.MaxObjectId, obj, objectId); }

                            drawingGeometry.UpdateVertices(resCache, lId, objectId, ltId);
                            drawingGeometry.StartVertexIndex = instances.Count;
                            instances.AddRange(drawingGeometry.LineInstances);
                            drawingGeometry.EndVertexIndex = instances.Count - 1;
                        }
                        if (obj is DrawingMtext drawingMtext)
                        {
                            drawingMtext.UpdateVertices(resCache, lId, ltId, sceneIdMap, stateBuffers);
                            drawingMtext.StartLineVertexIndex = instances.Count;
                            instances.AddRange(drawingMtext.LineInstances);
                            drawingMtext.EndLineVertexIndex = instances.Count - 1;
                        }
                        if (obj is DrawingSText drawingSText)
                        {
                            drawingSText.UpdateVertices(resCache, lId, ltId, sceneIdMap, stateBuffers);
                            drawingSText.StartLineVertexIndex = instances.Count;
                            instances.AddRange(drawingSText.LineInstances);
                            drawingSText.EndLineVertexIndex = instances.Count - 1;
                        }
                        if (obj is DrawingBlock drawingBlock)
                        {
                            drawingBlock.UpdateGeometryVertices(resCache, lId, ltId, sceneIdMap, stateBuffers);
                            drawingBlock.StartLineVertexIndex = instances.Count;
                            instances.AddRange(drawingBlock.LineInstances);
                            drawingBlock.EndLineVertexIndex = instances.Count - 1;
                        }
                        if (obj is DrawingDimension drawingDimension)
                        {
                            drawingDimension.UpdateGeometryVertices(resCache, lId, ltId, sceneIdMap, stateBuffers);
                            drawingDimension.StartLineVertexIndex = instances.Count;
                            instances.AddRange(drawingDimension.LineInstances);
                            drawingDimension.EndLineVertexIndex = instances.Count - 1;
                        }
                        if (obj is DrawingMleader drawingMleader)
                        {
                            drawingMleader.UpdateGeometryVertices(resCache, lId, ltId, sceneIdMap, stateBuffers);
                            drawingMleader.StartLineVertexIndex = instances.Count;
                            instances.AddRange(drawingMleader.LineInstances);
                            drawingMleader.EndLineVertexIndex = instances.Count - 1;
                        }
                    }
                }

                instances.AddRange(_cogoPointBoundsLines);

                LineVerticesDirty = false;
            }
        }
        public void BuildTextVertices(List<TextVertex> vertices, ResCache resCache, SceneIdMap sceneIdMap, D3dStateBuffers stateBuffers)
        {
            vertices.Clear();

            foreach (var kvp in Layers)
            {
                var layer = kvp.Value;
                var lid = sceneIdMap.GetOrAddLayerId(layer, out var isNewLayer);
                if (isNewLayer) { stateBuffers.InitializeLayerState(sceneIdMap.MaxLayerId, layer, lid); }

                if (!layer.IsVisible) { continue; }

                foreach (var obj in layer.DrawingObjects)
                {
                    uint ltId = sceneIdMap.GetOrAddLineTypeId(obj.LineType, out var isNewLtype);
                    if (isNewLtype) { stateBuffers.InitializeLineTypeState(sceneIdMap.MaxLineTypeId, obj.LineType, ltId); }

                    if (obj is DrawingSText text)
                    {
                        text.UpdateVertices(resCache, lid, ltId, sceneIdMap, stateBuffers);
                        text.StartTextVertexIndex = vertices.Count;
                        vertices.AddRange(text.TextVertices);
                        text.EndTextVertexIndex = vertices.Count - 1;
                    }
                    if (obj is DrawingMtext mtext)
                    {
                        mtext.UpdateVertices(resCache, lid, ltId, sceneIdMap, stateBuffers);
                        mtext.StartTextVertexIndex = vertices.Count;
                        vertices.AddRange(mtext.TextVertices);
                        mtext.EndTextVertexIndex = vertices.Count - 1;
                    }
                    if (obj is DrawingBlock drawingBlock)
                    {
                        drawingBlock.UpdateTextVertices(resCache, lid, ltId, sceneIdMap, stateBuffers);
                        drawingBlock.StartTextVertexIndex = vertices.Count;
                        vertices.AddRange(drawingBlock.TextVertices);
                        drawingBlock.EndTextVertexIndex = vertices.Count - 1;
                    }
                    if (obj is DrawingDimension dimension)
                    {
                        dimension.UpdateTextVertices(resCache, lid, ltId, sceneIdMap, stateBuffers);
                        dimension.StartTextVertexIndex = vertices.Count;
                        vertices.AddRange(dimension.TextVertices);
                        dimension.EndTextVertexIndex = vertices.Count - 1;
                    }
                    if (obj is DrawingMleader drawingMleader)
                    {
                        drawingMleader.UpdateTextVertices(resCache, lid, ltId, sceneIdMap, stateBuffers);
                        drawingMleader.StartTextVertexIndex = vertices.Count;
                        vertices.AddRange(drawingMleader.TextVertices);
                        drawingMleader.EndTextVertexIndex = vertices.Count - 1;
                    }
                }
            }

            TextVerticesDirty = false;
        }
        public void BuildSolidVertices(List<SolidVertex> vertices, ResCache resCache, SceneIdMap sceneIdMap, D3dStateBuffers stateBuffers)
        {
            vertices.Clear();

            foreach (var keyValuePair in Layers)
            {
                var layer = keyValuePair.Value;
                var lId = sceneIdMap.GetOrAddLayerId(layer, out var isNewLayer);
                if (isNewLayer) { stateBuffers.InitializeLayerState(sceneIdMap.MaxLayerId, layer, lId); }

                foreach (var obj in layer.DrawingObjects)
                {
                    var objectId = sceneIdMap.GetOrAddObjectId(obj, out var isNewObj);
                    if (isNewObj) { stateBuffers.InitializeObjectState(sceneIdMap.MaxObjectId, obj, objectId); }

                    if (obj is DrawingSolid drawingSolid)
                    {
                        drawingSolid.UpdateVertices(lId, objectId);
                        drawingSolid.StartVertexIndex = vertices.Count;
                        vertices.AddRange(drawingSolid.Vertices);
                        drawingSolid.EndVertexIndex = vertices.Count - 1;
                    }
                    if (obj is DrawingMleader mleader)
                    {
                        uint ltId = sceneIdMap.GetOrAddLineTypeId(obj.LineType, out var isNewLtype);
                        if (isNewLtype) { stateBuffers.InitializeLineTypeState(sceneIdMap.MaxLineTypeId, obj.LineType, ltId); }

                        mleader.UpdateSolidVertices(resCache, lId, objectId, ltId);
                        mleader.StartSolidVertexIndex = vertices.Count;
                        vertices.AddRange(mleader.SolidVertices);
                        mleader.EndSolidVertexIndex = vertices.Count - 1;
                    }
                    if (obj is DrawingBlock drawingBlock)
                    {
                        uint ltId = sceneIdMap.GetOrAddLineTypeId(obj.LineType, out var isNewLtype);
                        if (isNewLtype) { stateBuffers.InitializeLineTypeState(sceneIdMap.MaxLineTypeId, obj.LineType, ltId); }

                        drawingBlock.UpdateSolidVertices(resCache, lId, objectId, ltId);
                        drawingBlock.StartSolidVertexIndex = vertices.Count;
                        vertices.AddRange(drawingBlock.SolidVertices);
                        drawingBlock.EndSolidVertexIndex = vertices.Count - 1;
                    }
                    if (obj is DrawingWidePolyline drawingWidePolyline)
                    {
                        uint ltId = sceneIdMap.GetOrAddLineTypeId(obj.LineType, out var isNewLtype);
                        if (isNewLtype) { stateBuffers.InitializeLineTypeState(sceneIdMap.MaxLineTypeId, obj.LineType, ltId); }

                        drawingWidePolyline.UpdateVertices(resCache, lId, objectId, ltId);
                        drawingWidePolyline.StartVertexIndex = vertices.Count;
                        vertices.AddRange(drawingWidePolyline.SolidVertices);
                        drawingWidePolyline.EndVertexIndex = vertices.Count - 1;
                    }
                }
            }
            SolidVerticesDirty = false;
        }
        public void BuildPointMarkerInstances(List<PointMarkerInstance> instances, D3dStateController stateController)
        {
            instances.Clear();

            foreach (var pg in PointGroups)
            {
                if (!pg.IsVisible || pg is null) { continue; }

                foreach (CogoPoint p in GetPoints(pg))
                {
                    var pointRegistration = stateController.EnsurePointRegistered(p);

                    instances.Add(new PointMarkerInstance
                    {
                        Position = Vector3.Zero,
                        Radius = GlobalHelperProperties.CogoPointCirclePixelRadius,
                        PointId = pointRegistration.PointId,
                    });
                }
            }
            CogoPointCircleVerticesDirty = false;
        }

        // CogoPoint Test Methods
        public void GetTestDxfPoints()
        {
            ClearDxfPoints();

            var inflatedExtents = Rect.Inflate(Extents, Extents.Width * 0.1, Extents.Height * 0.1);
            int maxPoints = 1;
            float rows = 15;
            float cols = 15;
            float yIncrement = (inflatedExtents.Height / (rows - 1)).ToFloat();
            float xIncrement = (inflatedExtents.Width / (cols - 1)).ToFloat();
            int pointNum = 1;
            int testPointCount = 0;
            string description = "Test Point";
            Random random = new();

            string pointGroupName = $"TestGroup";
            TryCreatePointGroup(pointGroupName, Colors.Red, out var pointGroup);

            for (int i = 0; i < rows; i++)
            {
                var groupActivated = TrySetActivePointGroup(pointGroup);
                if (!groupActivated) { continue; }

                float y = inflatedExtents.Top.ToFloat() + (yIncrement * i);

                for (int j = 0; j < cols; j++)
                {
                    float x = inflatedExtents.Left.ToFloat() + (xIncrement * j);

                    if (CreateTestPoint(pointNum, x, y, description, random))
                    {
                        pointNum++;
                        testPointCount++;

                        if (testPointCount >= maxPoints) { return; }

                        continue;
                    }
                }
            }
        }
        public bool CreateTestPoint(int pointNum, float x, float y, string description, Random random)
        {
            return TryAddPointToActiveGroup(pointNum, new Vector3(x, y, 0), out _,
                            (Math.Round(300 + random.NextDouble() * 100, 3)).ToFloat(), description);
        }
        public void UpdateCogoPointBoundingLines(SceneIdMap sceneIdMap)
        {
            _cogoPointBoundsLines.Clear();

            var layer = Layers.First().Value;
            sceneIdMap.TryGetLayerId(layer, out var lId);

            foreach (var p in CogoPoints)
            {
                // PointNumber
                LineInstance top = new(p.PointNumberBounds.TopLeft.ToSharpDXVector2(), p.PointNumberBounds.TopRight.ToSharpDXVector2(), lId, 0, 0, 0, 0);
                LineInstance bottom = new(p.PointNumberBounds.BottomLeft.ToSharpDXVector2(), p.PointNumberBounds.BottomRight.ToSharpDXVector2(), lId, 0, 0, 0, 0);
                LineInstance right = new(p.PointNumberBounds.TopRight.ToSharpDXVector2(), p.PointNumberBounds.BottomRight.ToSharpDXVector2(), lId, 0, 0, 0, 0);
                LineInstance left = new(p.PointNumberBounds.TopLeft.ToSharpDXVector2(), p.PointNumberBounds.BottomLeft.ToSharpDXVector2(), lId, 0, 0, 0, 0);
                _cogoPointBoundsLines.AddRange(new[] { top, bottom, right, left });

                // Elevation
                top = new(p.ElevationBounds.TopLeft.ToSharpDXVector2(), p.ElevationBounds.TopRight.ToSharpDXVector2(), lId, 0, 0, 0, 0);
                bottom = new(p.ElevationBounds.BottomLeft.ToSharpDXVector2(), p.ElevationBounds.BottomRight.ToSharpDXVector2(), lId, 0, 0, 0, 0);
                right = new(p.ElevationBounds.TopRight.ToSharpDXVector2(), p.ElevationBounds.BottomRight.ToSharpDXVector2(), lId, 0, 0, 0, 0);
                left = new(p.ElevationBounds.TopLeft.ToSharpDXVector2(), p.ElevationBounds.BottomLeft.ToSharpDXVector2(), lId, 0, 0, 0, 0);
                _cogoPointBoundsLines.AddRange(new[] { top, bottom, right, left });

                // Description
                top = new(p.DescriptionBounds.TopLeft.ToSharpDXVector2(), p.DescriptionBounds.TopRight.ToSharpDXVector2(), lId, 0, 0, 0, 0);
                bottom = new(p.DescriptionBounds.BottomLeft.ToSharpDXVector2(), p.DescriptionBounds.BottomRight.ToSharpDXVector2(), lId, 0, 0, 0, 0);
                right = new(p.DescriptionBounds.TopRight.ToSharpDXVector2(), p.DescriptionBounds.BottomRight.ToSharpDXVector2(), lId, 0, 0, 0, 0);
                left = new(p.DescriptionBounds.TopLeft.ToSharpDXVector2(), p.DescriptionBounds.BottomLeft.ToSharpDXVector2(), lId, 0, 0, 0, 0);
                _cogoPointBoundsLines.AddRange(new[] { top, bottom, right, left });

                // Ellipse
                top = new(p.EllipseBounds.TopLeft.ToSharpDXVector2(), p.EllipseBounds.TopRight.ToSharpDXVector2(), lId, 0, 0, 0, 0);
                bottom = new(p.EllipseBounds.BottomLeft.ToSharpDXVector2(), p.EllipseBounds.BottomRight.ToSharpDXVector2(), lId, 0, 0, 0, 0);
                right = new(p.EllipseBounds.TopRight.ToSharpDXVector2(), p.EllipseBounds.BottomRight.ToSharpDXVector2(), lId, 0, 0, 0, 0);
                left = new(p.EllipseBounds.TopLeft.ToSharpDXVector2(), p.EllipseBounds.BottomLeft.ToSharpDXVector2(), lId, 0, 0, 0, 0);
                _cogoPointBoundsLines.AddRange(new[] { top, bottom, right, left });
            }
        }
        // End CogoPoint Test Methods

        // Hit testing tree related methods
        public void UpdateExtents()
        {
            DxfExtents = DxfHelpers.GetBoundsFromHeader(DxfImportResult.DxfDocument);
            UpdatePointExtents();
            Extents = Rect.Union(DxfExtents, PointExtents);
        }
        public void UpdateHitTestableObjectTree()
        {
            HitTestableObjectTree = new(this, Extents, 5);
            HitTestableObjectTreeDirty = false;
        }
        public void UpdateCogoPointTree()
        {
            UpdatePointExtents();
            CogoPointTree = new(this, Extents, 5);
        }
        public void UpdateDxfExtents()
        {
            //var testExtents = DxfHelpers.GetBoundsFromHeader(DxfDocument);

            DxfExtents = Rect.Empty;
            foreach (var keyValuePair in Layers)
            {
                var layer = keyValuePair.Value;
                foreach (var obj in layer.DrawingObjects)
                {
                    DxfExtents = Rect.Union(obj.Bounds, DxfExtents);
                }
            }
        }
        public void UpdatePointExtents()
        {
            if (PointGroups == null || PointGroups.Count == 0) { PointExtents = Rect.Empty; }

            int processorCount = Environment.ProcessorCount;
            var partialResults = new Rect[processorCount];

            Parallel.For(0, processorCount, i =>
            {
                Rect localUnion = Rect.Empty;

                // Use stride to balance uneven group sizes
                for (int g = i; g < PointGroups.Count; g += processorCount)
                {
                    var group = PointGroups[g];
                    if (group is null) { continue; }

                    foreach (var point in GetPoints(group))
                    {
                        localUnion.Union(point.Bounds);
                    }
                }
                partialResults[i] = localUnion;
            });

            Rect finalUnion = Rect.Empty;
            foreach (var r in partialResults)
            {
                finalUnion.Union(r);
            }

            PointExtents = finalUnion;
        }

        // Helper methods
        private static bool GetParsedMleaderLineType(
            ParsedMLeader parsedMLeader, ObjectLayer layer, IEnumerable<Linetype> lineTypes,
            out Linetype lineType)
        {
            if (parsedMLeader.EffectiveLineTypeReference.ValueType == ParsedLineTypeKind.ByLayer)
            {
                lineType = layer.LineType.DxfLineType;
                return lineType is not null;
            }
            else if (parsedMLeader.EffectiveLineTypeReference.ValueType == ParsedLineTypeKind.ByBlock)
            {
                lineType = lineTypes.FirstOrDefault(
                    lt => lt.Handle.Equals(parsedMLeader.EffectiveLineTypeReference.Value, StringComparison.OrdinalIgnoreCase));
                return lineType is not null;
            }
            else if (parsedMLeader.EffectiveLineTypeReference.ValueType == ParsedLineTypeKind.ByObject)
            {
                lineType = lineTypes.FirstOrDefault(lt => lt.Name.Equals(parsedMLeader.EffectiveLineTypeReference.Value, StringComparison.OrdinalIgnoreCase));
                return lineType is not null;
            }

            lineType = null;
            return false;
        }
        #endregion
    }
}
