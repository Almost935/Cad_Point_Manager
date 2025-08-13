using Cad_Point_Manager.Commands;
using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.HitTesting;
using Cad_Point_Manager.Models.PointRendering;
using netDxf;
using SharpDX;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections;
using System.Linq;

using Point = System.Windows.Point;

namespace Cad_Point_Manager.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        #region Fields
        private CogoPoint? _draggingPoint;
        private Point _lastTextDragUpdatePosition = new();
        private Point _latestMouseWorldPosition = new();
        private bool _isRenderingAttached = false;
        private readonly Dictionary<string, List<string>> _errors = new();

        private JobFileManager _jobFileManager = new();
        private bool _jobFileLoaded = false;
        private string _dxfFilePath;
        private string _dxfFileName;
        private DxfDocument _dxfDocument;
        private Size _viewportSize = Size.Empty;
        private Camera _camera;
        private ObservableCollection<CogoPoint> _cogoPoints;
        private ObservableCollection<CogoPoint> _selectedCogoPoints = [];
        private HitTestablePoint _snappedHitTestablePoint;
        private ObservableCollection<HitTestablePoint> _selectedHitTestablePoints = [];
        private Point _mousePosition = new();

        private int _newCogoPointsStartCount;
        private double _newCogoPointsElevation = 0.0;
        private string _newCogoPointsDescription = "";
        private PointGroup _newCogoPointsActivePointGroup = null;
        private int _newCogoPointsIntermediatePointsCount = 0;
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
        public ObservableCollection<CogoPoint> CogoPoints
        {
            get => _cogoPoints;
            set
            {
                //if (_cogoPoints != null)
                //{
                    _cogoPoints = value;
                    OnPropertyChanged(nameof(CogoPoints));
                //}
            }
        }
        public ObservableCollection<CogoPoint> SelectedCogoPoints
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
        public Point MousePosition
        {
            get => _mousePosition;
            set
            {
                _mousePosition = value;
                OnPropertyChanged(nameof(MousePosition));
            }
        }

        public int NewCogoPointsStartNumber
        {
            get => _newCogoPointsStartCount;
            set
            {
                _newCogoPointsStartCount = value;
                OnPropertyChanged(nameof(NewCogoPointsStartNumber));
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
        public string NewCogoPointsDescription
        {
            get => _newCogoPointsDescription;
            set
            {
                _newCogoPointsDescription = value;
                OnPropertyChanged(nameof(NewCogoPointsDescription));
            }
        }
        public PointGroup NewCogoPointsActivePointGroup
        {
            get => _newCogoPointsActivePointGroup;
            set
            {
                _newCogoPointsActivePointGroup = value;
                OnPropertyChanged(nameof(NewCogoPointsActivePointGroup));
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

        public ICommand SubmitPointCommand => new RelayCommand(OnSubmitCogoPointClicked);
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

            SelectedHitTestablePoints.Add(new HitTestablePoint(new Point(200, 50), Enums.SignificantPointType.Intersection));
            SelectedHitTestablePoints.Add(new HitTestablePoint(new Point(1000, 5000), Enums.SignificantPointType.Intersection));
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
            _draggingPoint = point;
            _draggingPoint.TextBeingMoved = true;
            point.MouseLeave(); 
            _lastTextDragUpdatePosition = MousePosition;
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
            _lastTextDragUpdatePosition = MousePosition;
        }


        // Point Creation Methods
        private void OnSubmitCogoPointClicked()
        {
            if (JobFileManager.CadManager3D.SnapSelectionMode == Enums.SelectionMode.Points)
            {
                if (SelectedHitTestablePoints.Count > 0)
                {
                    //ValidateNewCogoPoints();
                    var errors = GetErrors(nameof(NewCogoPointsStartNumber));
                    
                }
            }
            else if (JobFileManager.CadManager3D.SnapSelectionMode == Enums.SelectionMode.Geometries)
            {
                
            }
        }

        // Key up event handling
        public void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                EndDraggingText();
            }
        }

        // Validation
        private void ValidateNewCogoPoints()
        {
            ClearErrors(nameof(NewCogoPointsStartNumber));
            ClearErrors(nameof(NewCogoPointsElevation));
            ClearErrors(nameof(NewCogoPointsDescription));

            if (NewCogoPointsStartNumber < 1)
            {
                AddError(nameof(NewCogoPointsStartNumber), "Start number must be greater than 0.");
            }
            if (string.IsNullOrWhiteSpace(NewCogoPointsDescription))
            {
                AddError(nameof(NewCogoPointsDescription), "Description cannot be empty.");
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
