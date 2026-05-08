using Cad_Point_Manager.Commands;
using Cad_Point_Manager.Common;
using Cad_Point_Manager.Common.Collections;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models;
using Cad_Point_Manager.Models.DrawingObjects;
using Cad_Point_Manager.Models.HitTesting;
using Cad_Point_Manager.Models.PointRendering;
using Cad_Point_Manager.Models.Printing;
using Cad_Point_Manager.Services;
using Cad_Point_Manager.Services.Dialogs;
using Cad_Point_Manager.Services.LayoutExporting;
using netDxf;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using Point = System.Windows.Point;
using TextBox = System.Windows.Controls.TextBox;


namespace Cad_Point_Manager.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        #region Fields
        private readonly NewSelectionConnectivityService _service = new();

        private readonly Dictionary<string, List<string>> _errors = [];

        private readonly LayoutPdfVectorExporter _layoutPdfVectorExporter = new();

        private JobFileManager _jobFileManager = new();
        private bool _jobFileLoaded = false;
        private string _dxfFilePath;
        private string _dxfFileName;
        private DxfDocument _dxfDocument;
        private Size _viewportSize = Size.Empty;
        private BatchableObservableCollection<KeyValuePair<string, ObjectLayer>> _layers = [];
        private BatchableObservableCollection<PointGroup> _pointGroups = [];
        private BatchableObservableCollection<CogoPoint> _cogoPoints = [];
        private BatchableObservableCollection<CogoPoint> _selectedCogoPoints = [];
        private HitTestablePoint _snappedHitTestablePoint;
        private BatchableObservableCollection<HitTestablePoint> _selectedHitTestablePoints = [];
        private BatchableObservableCollection<DrawingGeometry> _selectedGeometries = [];
        private IReadOnlyList<ChainPath> _chainPaths = [];
        private double _vertexSnapTolerance = 1e-4;
        private Point _mousePosition = new();
        private ResCache _resCache = new ResCache();

        // CogoPoint Creation Fields
        private int _newCogoPointsStartCount = 1;
        private string _newCogoPointsStartNumberText = "1";
        private double _newCogoPointsElevation = 0.0;
        private string _newCogoPointsElevationText = "0.00";
        private string _newCogoPointsDescription = "";
        private string _newCogoPointsDescriptionText = "";
        private PointGroup _activePointGroup = null;
        private int _newCogoPointsIntermediatePointsCount = 0;
        private string _newCogoPointsIntermediatePointsCountText = 0.ToString();

        // Models + Layouts Mode Fields
        private bool _layoutsVisible = false;
        private bool _modelVisible = true;
        private bool _pointsVisible = false;

        // Layout fields
        private Layout _activeLayout;

        // File save fields
        private readonly IFileSaveDialogService _fileSaveDialogService = new FileSaveDialogService();
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
        public BatchableObservableCollection<HitTestablePoint> SelectedHitTestablePoints
        {
            get => _selectedHitTestablePoints;
            set
            {
                _selectedHitTestablePoints = value;
                OnPropertyChanged(nameof(SelectedHitTestablePoints));
            }
        }
        public BatchableObservableCollection<DrawingGeometry> SelectedGeometries
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
        public ResCache ResCache
        {
            get => _resCache;
            set { _resCache = value; OnPropertyChanged(); }
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
                bool isValid = ValidationService.ValidateString(value, out string errorMessage);

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
        public PointGroup ActivePointGroup
        {
            get => _activePointGroup;
            set
            {
                _activePointGroup = value;
                OnPropertyChanged(nameof(ActivePointGroup));
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

        // Models + Layouts Mode Properties
        public bool LayoutsVisible
        {
            get => _layoutsVisible;
            set
            {
                _layoutsVisible = value;
                OnPropertyChanged(nameof(LayoutsVisible));
            }
        }
        public bool ModelsVisible
        {
            get => _modelVisible;
            set
            {
                _modelVisible = value;
                OnPropertyChanged(nameof(ModelsVisible));
            }
        }
        public bool PointsVisible
        {
            get => _pointsVisible;
            set
            {
                _pointsVisible = value;
                OnPropertyChanged(nameof(PointsVisible));
            }
        }

        // Layout related properties
        public Layout ActiveLayout
        {
            get => _activeLayout;
            set
            {
                _activeLayout = value;
                OnPropertyChanged(nameof(ActiveLayout));
            }
        }

        public SceneIdMap SceneIdMap { get; set; }
        public D3dStateController StateController { get; set; }
        public D3dStateBuffers StateBuffers { get; set; }
        #endregion

        #region Commands
        public ICommand NewJobCommand { get; set; }
        public ICommand LoadJobCommand { get; set; }
        public ICommand AttachDxfFileCommand { get; set; }
        public ICommand SaveJobCommand { get; set; }
        public ICommand SaveAsJobCommand { get; set; }
        public ICommand PrintJobCommand { get; set; }
        public ICommand ExportPointsCommand { get; set; }
        public ICommand ImportPointsCommand { get; set; }
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
        public event EventHandler? ResetLayoutsViewRequested;
        public event EventHandler? RebuildLayoutsViewRequested;
        #endregion

        #region Constructors
        public MainViewModel()
        {
            NewJobCommand = new RelayCommand<RoutedEventArgs>(NewJob);
            LoadJobCommand = new RelayCommand<RoutedEventArgs>(LoadJob);
            AttachDxfFileCommand = new RelayCommand<RoutedEventArgs>(AttachDxfFile);
            SaveJobCommand = new RelayCommand<RoutedEventArgs>(SaveJob);
            SaveAsJobCommand = new RelayCommand<RoutedEventArgs>(SaveJobAs);
            PrintJobCommand = new RelayCommand<RoutedEventArgs>(PrintJob);
            ExportPointsCommand = new RelayCommand<RoutedEventArgs>(ExportPoints);

            ZoomToExtentsCommand = new RelayCommand<RoutedEventArgs>(ZoomToExtents);

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
                    SceneIdMap.Reset();
                    StateController.ClearDirty();
                    StateBuffers.ResetFull();
                    JobFileManager.NewJobFile();
                    RebuildLayoutsViewRequested?.Invoke(this, EventArgs.Empty);
                }
                else { return; }
            }
            else if (result == MessageBoxResult.No)
            {
                SceneIdMap.Reset();
                StateController.ClearDirty();
                StateBuffers.ResetFull();
                JobFileManager.NewJobFile();
                RebuildLayoutsViewRequested?.Invoke(this, EventArgs.Empty);
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

            bool? result = dlg.ShowDialog();

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
        public async void PrintJob(RoutedEventArgs e)
        {
            if (ActiveLayout == null) { return; }

            var jobName = JobFileManager?.JobName;
            var layoutName = ActiveLayout?.Name ?? "Layout";
            var safeJob = FileHelpers.MakeSafeFileName(string.IsNullOrWhiteSpace(jobName) ? "Job" : jobName);
            var safeLayout = FileHelpers.MakeSafeFileName(layoutName);
            var suggestedFileName = $"{safeJob}_{safeLayout}.pdf";

            var fallbackDir = @"C:\Users\fcraw\source\repos\Cad_Point_Manager\Cad_Point_Manager\Resources\Testing";
            var initialDir = LastExportPaths.GetInitialDirectoryOrFallback(fallbackDir);

            // 3) Ask user
            var request = new FileSaveDialogRequest
            {
                Title = "Save PDF",
                Filter = "PDF files (*.pdf)|*.pdf",
                DefaultExtension = ".pdf",
                InitialDirectory = initialDir,
                DefaultFileName = suggestedFileName,
                OverwritePrompt = true
            };

            if (!_fileSaveDialogService.TryPickSavePath(request, out var path)) { return; }

            await ExportActiveLayoutAsync(path);
        }
        public async void ExportPoints(RoutedEventArgs e)
        {
            var pointsView = JobFileManager?.CadManager?.PointsView;
            if (pointsView == null)
            {
                MessageBox.Show("No points are available to export.", "Export Points", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            pointsView.Refresh();

            var visiblePoints = pointsView.Cast<object>()
                                          .OfType<CogoPoint>()
                                          .ToList();

            if (visiblePoints.Count == 0)
            {
                MessageBox.Show("There are no visible points to export.", "Export Points", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var jobName = JobFileManager?.JobName;
            var safeJob = FileHelpers.MakeSafeFileName(string.IsNullOrWhiteSpace(jobName) ? "Points" : jobName);
            var suggestedFileName = $"{safeJob}_Points.csv";

            var fallbackDir = @"C:\Users\fcraw\source\repos\Cad_Point_Manager\Cad_Point_Manager\Resources\Testing";
            var initialDir = LastExportPaths.GetInitialDirectoryOrFallback(fallbackDir);

            var request = new FileSaveDialogRequest
            {
                Title = "Export Points to CSV",
                Filter = "CSV files (*.csv)|*.csv",
                DefaultExtension = ".csv",
                InitialDirectory = initialDir,
                DefaultFileName = suggestedFileName,
                OverwritePrompt = true
            };

            if (!_fileSaveDialogService.TryPickSavePath(request, out var path)) { return; }

            try
            {
                await Task.Run(() => ExportHelpers.ExportPointsToCsv(path, visiblePoints));
                MessageBox.Show($"Exported {visiblePoints.Count} points.", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export points.\n\n{ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ZoomToExtents(RoutedEventArgs e)
        {
            if (LayoutsVisible)
            {
                ResetLayoutsViewRequested?.Invoke(this, EventArgs.Empty);
                return;
            }
            if (ModelsVisible)
            {
                JobFileManager.CadManager?.ZoomToExtents();
            }
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
                        if (!isChecked) { JobFileManager.CadManager.SnapSelectionMode = Enums.SelectionMode.CogoPoints; }
                        else { JobFileManager.CadManager.SnapSelectionMode = Enums.SelectionMode.Points; }
                        break;
                    case "GeometryCogoCreation":
                        if (!isChecked) { JobFileManager.CadManager.SnapSelectionMode = Enums.SelectionMode.CogoPoints; }
                        else { JobFileManager.CadManager.SnapSelectionMode = Enums.SelectionMode.Geometries; }
                        break;
                    default:
                        JobFileManager.CadManager.SnapSelectionMode = Enums.SelectionMode.CogoPoints;
                        break;
                }
            }
        }

        // Point Creation Methods
        private void OnSubmitCogoPointClicked()
        {
            if (JobFileManager.CadManager.SnapSelectionMode == Enums.SelectionMode.Points)
            {
                if (SelectedHitTestablePoints.Count > 0)
                {
                    var startNumberErrors = GetErrors(nameof(NewCogoPointsStartNumberText));
                    var elevErrors = GetErrors(nameof(NewCogoPointsElevationText));
                    var descErrors = GetErrors(nameof(NewCogoPointsDescriptionText));

                    if (startNumberErrors is not null || elevErrors is not null || descErrors is not null || ActivePointGroup is null)
                    {
                        if (ActivePointGroup == null)
                        {
                            AddError(nameof(ActivePointGroup), "A point group must be selected.");
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
                            int pointNum = JobFileManager.CadManager.CogoPointManager.GetNextAvailablePointNumber(NewCogoPointsStartNumber);
                            JobFileManager.CadManager.CogoPointManager.TryAddPoint(pointNum, hitPoint.Position.ToSharpDXVector3(), ActivePointGroup,
                                out var cogoPoint, NewCogoPointsElevation.ToFloat(), NewCogoPointsDescription);
                        }

                        ResetSelectionRequested?.Invoke(this, EventArgs.Empty);
                        JobFileManager.CadManager.UpdateCogoPointTree();

                        ClearErrors(NewCogoPointsStartNumberText);
                        ClearErrors(NewCogoPointsElevationText);
                        ClearErrors(NewCogoPointsDescriptionText);
                        ClearErrors(nameof(ActivePointGroup));
                        ClearErrors(NewCogoPointsIntermediatePointsCountText);
                    }
                }
            }
            else if (JobFileManager.CadManager.SnapSelectionMode == Enums.SelectionMode.Geometries)
            {
                if (ChainPaths.Count > 0)
                {
                    var startNumberErrors = GetErrors(nameof(NewCogoPointsStartNumberText));
                    var elevErrors = GetErrors(nameof(NewCogoPointsElevationText));
                    var descErrors = GetErrors(nameof(NewCogoPointsDescriptionText));
                    var intermediatePointsErrors = GetErrors(nameof(NewCogoPointsIntermediatePointsCountText));

                    if (startNumberErrors is not null || elevErrors is not null || descErrors is not null || intermediatePointsErrors is not null || ActivePointGroup is null)
                    {
                        if (ActivePointGroup == null)
                        {
                            AddError(nameof(ActivePointGroup), "A point group must be selected.");
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
                            int pointNum = JobFileManager.CadManager.CogoPointManager.GetNextAvailablePointNumber(NewCogoPointsStartNumber);
                            JobFileManager.CadManager.CogoPointManager.TryAddPoint(pointNum, coords[i].ToSharpDXVector3(), ActivePointGroup,
                            out var cogoPoint, NewCogoPointsElevation.ToFloat(), NewCogoPointsDescription);
                        }

                        JobFileManager.CadManager.CogoPointTextVerticesDirty = true;
                        JobFileManager.CadManager.CogoPointCircleVerticesDirty = true;

                        ResetSelectionRequested?.Invoke(this, EventArgs.Empty);
                        JobFileManager.CadManager.UpdateCogoPointTree();

                        ClearErrors(NewCogoPointsStartNumberText);
                        ClearErrors(NewCogoPointsElevationText);
                        ClearErrors(NewCogoPointsDescriptionText);
                        ClearErrors(nameof(ActivePointGroup));
                        ClearErrors(NewCogoPointsIntermediatePointsCountText);
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

        // Printing Methods
        public async Task ExportActiveLayoutAsync(string path)
        {
            var imageUri = ImageHelpers.LoadPackImage("pack://application:,,,/Resources/Images/IQ Contracting - Logo (Square).jpg");
            var templatePrims = TitleblockPrimitiveBuilder.Build(ActiveLayout, imageUri);

            await _layoutPdfVectorExporter.ExportAsync(
                ActiveLayout,
                JobFileManager.CadManager,
                ActiveLayout.Viewport.Scene,
                StateController,
                SceneIdMap,
                ResCache,
                templatePrims,
                path);
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
                _errors[propertyName] = [];

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
