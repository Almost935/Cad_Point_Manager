using Cad_Point_Manager.Commands;
using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.HitTesting;
using Cad_Point_Manager.Models.PointRendering;
using netDxf;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections;
using Cad_Point_Manager.Services;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Models.DrawingObjects3D;

using Point = System.Windows.Point;
using TextBox = System.Windows.Controls.TextBox;
using Cad_Point_Manager.Common.Collections;


namespace Cad_Point_Manager.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        #region Fields
        private readonly ValidationService _validationService = new();
        private readonly SelectionConnectivityService _service = new();

        private CogoPoint? _draggingPoint;
        private bool _isRenderingAttached = false;
        private readonly Dictionary<string, List<string>> _errors = new();

        private JobFileManager _jobFileManager = new();
        private bool _jobFileLoaded = false;
        private string _dxfFilePath;
        private string _dxfFileName;
        private DxfDocument _dxfDocument;
        private Size _viewportSize = Size.Empty;
        private Camera _camera;
        private BatchableObservableCollection<KeyValuePair<string, PointGroup>> _pointGroups;
        private BatchableObservableCollection<CogoPoint> _cogoPoints;
        private BatchableObservableCollection<CogoPoint> _selectedCogoPoints = [];
        private HitTestablePoint _snappedHitTestablePoint;
        private ObservableCollection<HitTestablePoint> _selectedHitTestablePoints = [];
        private BatchableObservableCollection<DrawingGeometry3D> _selectedGeometries = [];
        private IReadOnlyList<ChainPath> _chainPaths = [];
        private double _vertexSnapTolerance = 1e-4;
        private Point _mousePosition = new();

        // CogoPoint Creation Fields
        private int _newCogoPointsStartCount = 1;
        private string _newCogoPointsStartNumberText = "1";
        private double _newCogoPointsElevation = 0.0;
        private string _newCogoPointsElevationText = "0.00";
        private string _newCogoPointsDescription = "";
        private string _newCogoPointsDescriptionText = "";
        private PointGroup _newCogoPointsActivePointGroup = null;
        private int _newCogoPointsIntermediatePointsCount = 0;
        private string _newCogoPointsIntermediatePointsCountText = 0.ToString();
        #endregion

        #region Properties
        public JobFileManager JobFileManager
        {
            get { return _jobFileManager; }
            set
            {
                _jobFileManager = value;
                OnPropertyChanged(nameof(JobFileManager));
            }
        }
        public bool JobFileLoaded
        {
            get { return _jobFileLoaded; }
            set
            {
                _jobFileLoaded = value;
                OnPropertyChanged(nameof(JobFileLoaded));
            }
        }
        public string DxfFilePath
        {
            get { return _dxfFilePath; }
            set
            {
                _dxfFilePath = value;
                OnPropertyChanged(nameof(DxfFilePath));
            }
        }
        public string DxfFileName
        {
            get { return _dxfFileName; }
            set
            {
                _dxfFileName = value;
                OnPropertyChanged(nameof(DxfFileName));
            }
        }
        public DxfDocument DxfDocument
        {
            get { return _dxfDocument; }
            set
            {
                _dxfDocument = value;
                OnPropertyChanged(nameof(DxfDocument));
            }
        }
        public Size ViewportSize
        {
            get { return _viewportSize; }
            set
            {
                _viewportSize = value;
                OnPropertyChanged(nameof(ViewportSize));
            }
        }
        public Camera Camera
        {
            get { return _camera; }
            set
            {
                _camera = value;
                OnPropertyChanged(nameof(Camera));
            }
        }
        public BatchableObservableCollection<KeyValuePair<string, PointGroup>> PointGroups
        {
            get => _pointGroups;
            set
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
                if (_cogoPoints != null)
                {
                    _cogoPoints = value;
                    OnPropertyChanged(nameof(CogoPoints));
                }
            }
        }
        public BatchableObservableCollection<CogoPoint> SelectedCogoPoints
        {
            get => _selectedCogoPoints;
            set
            {
                _selectedCogoPoints = value;
                OnPropertyChanged(nameof(SelectedCogoPoints));
            }
        }
        public HitTestablePoint SnappedHitTestablePoint
        {
            get => _snappedHitTestablePoint;
            set
            {
                _snappedHitTestablePoint = value;
                OnPropertyChanged(nameof(SnappedHitTestablePoint));
            }
        }
        public ObservableCollection<HitTestablePoint> SelectedHitTestablePoints
        {
            get => _selectedHitTestablePoints;
            set
            {
                _selectedHitTestablePoints = value;
                OnPropertyChanged(nameof(SelectedHitTestablePoints));
            }
        }
        public BatchableObservableCollection<DrawingGeometry3D> SelectedGeometries
        {
            get => _selectedGeometries;
            set
            {
                _selectedGeometries = value;
                OnPropertyChanged(nameof(SelectedGeometries));
            }
        }
        public IReadOnlyList<ChainPath> ChainPaths
        {
            get => _chainPaths;
            private set
            {
                _chainPaths = value;
                OnPropertyChanged(nameof(ChainPaths));
            }
        }
        public double VertexSnapTolerance
        {
            get => _vertexSnapTolerance;
            set
            {
                if (Math.Abs(_vertexSnapTolerance - value) > double.Epsilon)
                {
                    _vertexSnapTolerance = value;
                    OnPropertyChanged(nameof(VertexSnapTolerance));
                    RebuildChains();
                }
            }
        }
        public Point MousePosition
        {
            get => _mousePosition;
            set
            {
                _mousePosition = value;
                OnPropertyChanged(nameof(MousePosition));
            }
        }

        // CogoPoint Creation Properties
        public int NewCogoPointsStartNumber
        {
            get => _newCogoPointsStartCount;
            set
            {
                _newCogoPointsStartCount = value;
                OnPropertyChanged(nameof(NewCogoPointsStartNumber));
            }
        }
        public string NewCogoPointsStartNumberText
        {
            get => _newCogoPointsStartNumberText;
            set
            {
                if (_newCogoPointsStartNumberText == value) return;
                _newCogoPointsStartNumberText = value;

                ClearErrors(nameof(NewCogoPointsStartNumberText));

                if (!int.TryParse(value, out var n))
                {
                    AddError(nameof(NewCogoPointsStartNumberText), "Enter a valid integer.");
                }
                else if (n <= 0)
                {
                    AddError(nameof(NewCogoPointsStartNumberText), "Start number must be positive.");
                }
                else
                {
                    NewCogoPointsStartNumber = n;
                }

                OnPropertyChanged(nameof(NewCogoPointsStartNumberText));
            }
        }
        public double NewCogoPointsElevation
        {
            get => _newCogoPointsElevation;
            set
            {
                _newCogoPointsElevation = value;
                OnPropertyChanged(nameof(NewCogoPointsElevation));
            }
        }
        public string NewCogoPointsElevationText
        {
            get => _newCogoPointsElevationText;
            set
            {
                if (_newCogoPointsElevationText == value) return;
                _newCogoPointsElevationText = value;

                ClearErrors(nameof(NewCogoPointsElevationText));

                if (!double.TryParse(value, out var n))
                {
                    AddError(nameof(NewCogoPointsElevationText), "Enter a valid number.");
                }
                else
                {
                    NewCogoPointsElevation = n;
                }

                OnPropertyChanged(nameof(NewCogoPointsElevationText));
            }
        }
        public string NewCogoPointsDescription
        {
            get => _newCogoPointsDescription;
            set
            {
                _newCogoPointsDescription = value;
                OnPropertyChanged(nameof(NewCogoPointsDescription));
            }
        }
        public string NewCogoPointsDescriptionText
        {
            get => _newCogoPointsDescriptionText;
            set
            {
                if (_newCogoPointsDescriptionText == value) return;
                _newCogoPointsDescriptionText = value;

                ClearErrors(nameof(NewCogoPointsDescriptionText));
                bool isValid = _validationService.ValidateString(value, out string errorMessage);

                if (!isValid)
                {
                    AddError(nameof(NewCogoPointsDescriptionText), errorMessage);
                }
                else
                {
                    NewCogoPointsDescription = value;
                }

                OnPropertyChanged(nameof(NewCogoPointsDescriptionText));
            }
        }
        public PointGroup NewCogoPointsPointGroup
        {
            get => _newCogoPointsActivePointGroup;
            set
            {
                _newCogoPointsActivePointGroup = value;
                OnPropertyChanged(nameof(NewCogoPointsPointGroup));
            }
        }
        public int NewCogoPointsIntermediatePointsCount
        {
            get => _newCogoPointsIntermediatePointsCount;
            set
            {
                _newCogoPointsIntermediatePointsCount = value;
                OnPropertyChanged(nameof(NewCogoPointsIntermediatePointsCount));
            }
        }
        public string NewCogoPointsIntermediatePointsCountText
        {
            get => _newCogoPointsIntermediatePointsCountText;
            set
            {
                if (_newCogoPointsIntermediatePointsCountText == value) return;
                _newCogoPointsIntermediatePointsCountText = value;

                ClearErrors(nameof(NewCogoPointsIntermediatePointsCountText));

                if (!int.TryParse(value, out var n))
                {
                    AddError(nameof(NewCogoPointsIntermediatePointsCountText), "Enter a valid integer.");
                }
                else if (n < 0)
                {
                    AddError(nameof(NewCogoPointsIntermediatePointsCountText), "Intermediate points count must be positive.");
                }
                else
                {
                    NewCogoPointsIntermediatePointsCount = n;
                }

                OnPropertyChanged(nameof(NewCogoPointsIntermediatePointsCountText));
            }
        }
        #endregion

        #region Commands
        public ICommand NewJobCommand { get; set; }
        public ICommand LoadJobCommand { get; set; }
        public ICommand AttachDxfFileCommand { get; set; }
        public ICommand SaveJobCommand { get; set; }
        public ICommand SaveAsJobCommand { get; set; }
        public ICommand ZoomToExtentsCommand { get; set; }

        public ICommand SnapToggleCommand => new RelayCommand<object>(OnSnapTogglePressed);

        public ICommand CogoPointCheckedCommand { get; set; }
        public ICommand CogoPointUncheckedCommand { get; set; }

        // Cogo point creation commands
        public ICommand SubmitPointCommand => new RelayCommand(OnSubmitCogoPointClicked);
        public ICommand CogoCreationTextBoxLostFocusCommand => new RelayCommand<RoutedEventArgs>(OnCogoCreationTextBoxLostFocus);
        public ICommand CogoCreationTextBoxGotFocusCommand => new RelayCommand<RoutedEventArgs>(OnCogoCreationTextBoxGotFocus);
        public ICommand CogoCreationTextBoxGotKeyboardFocusCommand => new RelayCommand<RoutedEventArgs>(OnCogoCreationTextBoxKeyboardGotFocus);
        #endregion

        #region Events
        public event EventHandler? ResetSelectionRequested;
        #endregion

        #region Constructors
        public MainViewModel()
        {
            NewJobCommand = new RelayCommand<RoutedEventArgs>(NewJob);
            LoadJobCommand = new RelayCommand<RoutedEventArgs>(LoadJob);
            AttachDxfFileCommand = new RelayCommand<RoutedEventArgs>(AttachDxfFile);
            SaveJobCommand = new RelayCommand<RoutedEventArgs>(SaveJob);
            SaveAsJobCommand = new RelayCommand<RoutedEventArgs>(SaveJobAs);

            ZoomToExtentsCommand = new RelayCommand<RoutedEventArgs>(ZoomToExtents);

            CogoPointCheckedCommand = new RelayCommand<CogoPoint>(OnCogoPointToggleButtonChecked);
            CogoPointUncheckedCommand = new RelayCommand<CogoPoint>(OnCogoPointToggleButtonUnchecked);

            SelectedGeometries.CollectionChanged += SelectedGeometries_CollectionChanged;
        }
        #endregion

        #region Public Methods
        public void NewJob(RoutedEventArgs e)
        {
            var result = MessageBox.Show("Save current job before exiting?", "Warning", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                bool saved = JobFileManager.TrySaveJobFile();
                if (saved)
                {
                    JobFileManager.NewJobFile();
                }
                else { return; }
            }
            else if (result == MessageBoxResult.No)
            {
                JobFileManager.NewJobFile();
            }
            else
            {
                return;
            }
        }
        public void LoadJob(RoutedEventArgs e)
        {
            JobFileManager.TryLoadJobFile();
        }
        public void AttachDxfFile(RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".dxf";
            dlg.Filter = "DXF Files (*.dxf)|*.dxf";
            dlg.InitialDirectory = @"C:\Users\fcraw\source\repos\Cad_Point_Manager\Cad_Point_Manager\Resources\DXF";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                DxfFilePath = dlg.FileName;
                DxfFileName = dlg.SafeFileName;

                DxfDocument = DxfDocument.Load(DxfFilePath);
                if (DxfDocument is not null)
                {
                    DxfFileName = DxfDocument.Name;
                    JobFileManager.LoadDxf(DxfDocument);
                }
            }
        }
        public void SaveJob(RoutedEventArgs e)
        {
            JobFileManager.TrySaveJobFile();
        }
        public void SaveJobAs(RoutedEventArgs e)
        {

        }

        public void ZoomToExtents(RoutedEventArgs e)
        {
            JobFileManager.CadManager3D?.ZoomToExtents();
        }

        private void OnSnapTogglePressed(object param)
        {
            if (param is ToggleButton toggle)
            {
                string name = toggle.Name;
                bool isChecked = (bool)toggle.IsChecked;

                switch (name)
                {
                    case "PointCogoCreation":
                        if (!isChecked) { JobFileManager.CadManager3D.SnapSelectionMode = Enums.SelectionMode.CogoPoints; }
                        else { JobFileManager.CadManager3D.SnapSelectionMode = Enums.SelectionMode.Points; }
                        break;
                    case "GeometryCogoCreation":
                        if (!isChecked) { JobFileManager.CadManager3D.SnapSelectionMode = Enums.SelectionMode.CogoPoints; }
                        else { JobFileManager.CadManager3D.SnapSelectionMode = Enums.SelectionMode.Geometries; }
                        break;
                    default:
                        JobFileManager.CadManager3D.SnapSelectionMode = Enums.SelectionMode.CogoPoints;
                        break;
                }
            }
        }


        // CogoPoint Movement Methods
        private void OnCogoPointToggleButtonChecked(CogoPoint point)
        {
            EndDraggingText();
            BeginDraggingText(point);
        }
        private void OnCogoPointToggleButtonUnchecked(CogoPoint point)
        {
            EndDraggingText();
        }
        public void BeginDraggingText(CogoPoint point)
        {
            JobFileManager.CadManager3D.HitTestingEnabled = false;
            _draggingPoint = point;
            _draggingPoint.TextBeingMoved = true;
            point.MouseLeave();
            point.MoveTextInfoToPoint(MousePosition);
            point.RedrawTextVisual();


            if (!_isRenderingAttached)
            {
                CompositionTarget.Rendering += OnRenderFrame;
                _isRenderingAttached = true;
            }
        }
        public void EndDraggingText()
        {
            JobFileManager.CadManager3D.HitTestingEnabled = true;
            if (_draggingPoint != null)
            {
                _draggingPoint.TextBeingMoved = false;
                _draggingPoint = null;
            }

            JobFileManager.CadManager3D.UpdateHitTestableObjectTree();

            if (_isRenderingAttached)
            {
                CompositionTarget.Rendering -= OnRenderFrame;
                _isRenderingAttached = false;
            }
        }
        private void OnRenderFrame(object? sender, EventArgs e)
        {
            if (_draggingPoint == null) { return; }
            if (!_draggingPoint.TextBeingMoved) { EndDraggingText(); }

            _draggingPoint.MoveTextInfoToPoint(MousePosition);
            _draggingPoint.RedrawTextVisual();
        }


        // Point Creation Methods
        private void OnSubmitCogoPointClicked()
        {
            if (JobFileManager.CadManager3D.SnapSelectionMode == Enums.SelectionMode.Points)
            {
                if (SelectedHitTestablePoints.Count > 0)
                {
                    var startNumberErrors = GetErrors(nameof(NewCogoPointsStartNumberText));
                    var elevErrors = GetErrors(nameof(NewCogoPointsElevationText));
                    var descErrors = GetErrors(nameof(NewCogoPointsDescriptionText));

                    if (startNumberErrors is not null || elevErrors is not null || descErrors is not null || NewCogoPointsPointGroup is null)
                    {
                        if (NewCogoPointsPointGroup == null)
                        {
                            AddError(nameof(NewCogoPointsPointGroup), "A point group must be selected.");
                        }
                        if (startNumberErrors is not null || elevErrors is not null || descErrors is not null)
                        {
                            MessageBox.Show("Errors in point creation fields.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        return;
                    }
                    else
                    {
                        foreach (var hitPoint in SelectedHitTestablePoints)
                        {
                            int pointNum = JobFileManager.CadManager3D.CogoPointManager.GetNextAvailablePointNumber(NewCogoPointsStartNumber);
                            JobFileManager.CadManager3D.CogoPointManager.TryAddPoint(pointNum, hitPoint.Position.ToSharpDXVector3(), NewCogoPointsPointGroup,
                                out var cogoPoint, NewCogoPointsElevation.ToFloat(), NewCogoPointsDescription);
                            cogoPoint.UpdateAllVisualTransforms(JobFileManager.CadManager3D.CogoPointManager.CurrentlyAppliedMatrix);
                        }

                        ResetSelectionRequested?.Invoke(this, EventArgs.Empty);
                        JobFileManager.CadManager3D.UpdateHitTestableObjectTree();
                    }
                }
            }
            else if (JobFileManager.CadManager3D.SnapSelectionMode == Enums.SelectionMode.Geometries)
            {
                if (ChainPaths.Count > 0)
                {
                    var startNumberErrors = GetErrors(nameof(NewCogoPointsStartNumberText));
                    var elevErrors = GetErrors(nameof(NewCogoPointsElevationText));
                    var descErrors = GetErrors(nameof(NewCogoPointsDescriptionText));
                    var intermediatePointsErrors = GetErrors(nameof(NewCogoPointsIntermediatePointsCountText));

                    if (startNumberErrors is not null || elevErrors is not null || descErrors is not null || intermediatePointsErrors is not null || NewCogoPointsPointGroup is null)
                    {
                        if (NewCogoPointsPointGroup == null)
                        {
                            AddError(nameof(NewCogoPointsPointGroup), "A point group must be selected.");
                        }
                        if (startNumberErrors is not null || elevErrors is not null || descErrors is not null)
                        {
                            MessageBox.Show("Errors in point creation fields.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        return;
                    }
                    else
                    {
                        List<System.Numerics.Vector2> coords = [];
                        foreach (var chainPath in ChainPaths)
                        {
                            var pts = ChainBuilder.ExpandChainPoints(chainPath, NewCogoPointsIntermediatePointsCount);
                            coords.AddRange(pts.Select(p => new System.Numerics.Vector2((float)p.X, (float)p.Y)).ToList());
                        }
                        for (int i = 0; i < coords.Count; i++)
                        {
                            //CogoPoint cogoPoint = new(NewCogoPointsPointGroup, 1, coords[i].ToSharpDXVector3(), JobFileManager.CadManager3D.CogoPointManager);
                            //JobFileManager.CadManager3D.CogoPointManager.AddPoint(cogoPoint);
                            int pointNum = JobFileManager.CadManager3D.CogoPointManager.GetNextAvailablePointNumber(NewCogoPointsStartNumber);
                            JobFileManager.CadManager3D.CogoPointManager.TryAddPoint(pointNum, coords[i].ToSharpDXVector3(), NewCogoPointsPointGroup,
                            out var cogoPoint, NewCogoPointsElevation.ToFloat(), NewCogoPointsDescription);
                            cogoPoint.UpdateAllVisualTransforms(JobFileManager.CadManager3D.CogoPointManager.CurrentlyAppliedMatrix);
                        }

                        ResetSelectionRequested?.Invoke(this, EventArgs.Empty);
                        JobFileManager.CadManager3D.UpdateHitTestableObjectTree();
                    }
                }
            }
        }
        private void OnCogoCreationTextBoxLostFocus(RoutedEventArgs e)
        {
            //var tb = e.Source as TextBox ?? e.OriginalSource as TextBox;
            //ValidateNewCogoPoints();
        }
        private void OnCogoCreationTextBoxGotFocus(RoutedEventArgs e)
        {
            //var tb = e.Source as TextBox ?? e.OriginalSource as TextBox;
            //tb.Dispatcher.BeginInvoke(new Action(() =>
            //{
            //    tb.SelectAll();
            //}), DispatcherPriority.Input);
        }
        private void OnCogoCreationTextBoxKeyboardGotFocus(RoutedEventArgs e)
        {
            var tb = e.Source as TextBox ?? e.OriginalSource as TextBox;
            tb.SelectAll();
        }

        // Geometry Chain Methods
        private void SelectedGeometries_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RebuildChains();
        }
        public void RebuildChains()
        {
            var directed = _service.BuildChainsFromSelection(SelectedGeometries, VertexSnapTolerance);
            ChainPaths = directed;
        }

        // Key up event handling
        public void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                EndDraggingText();
            }
        }
        #endregion

        #region INotifyDataErrorInfo
        public bool HasErrors => _errors.Any();

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return _errors.SelectMany(kvp => kvp.Value);
            if (_errors.ContainsKey(propertyName))
                return _errors[propertyName];
            return null;
        }

        protected void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = new List<string>();

            if (!_errors[propertyName].Contains(error))
            {
                _errors[propertyName].Add(error);
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            }
        }

        protected void ClearErrors(string propertyName)
        {
            if (_errors.Remove(propertyName))
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Events
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        #endregion
    }
}
