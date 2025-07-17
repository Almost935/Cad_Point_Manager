using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.HitTesting;
using Cad_Point_Manager.Views;
using SharpDX;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Media.Effects;
using System.Windows.Media;
using Matrix = System.Windows.Media.Matrix;
using Point = System.Windows.Point;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Windows.Controls.Primitives;

namespace Cad_Point_Manager.Models.PointRendering
{
    public class CogoPoint : HitTestableObject
    {
        #region Fields
        private const double _textToggleButtonOffset = 0;

        private int _pointNumber;
        private double _northing;
        private double _easting;
        private double _elevation = 0.0f;
        private PointGroup _pointGroup;
        private string _description;
        private CogoPointManager _cogoPointManager;
        private Point _textInfoBasePosition = new();
        private Point _textInfoCurrentPosition = new();
        private Point _textToggleButtonPosition = new();
        private Point _textToggleButtonScreenPosition = new();
        private bool _textBeingMoved = false;
        private double _textInfoBaseOffset = 1.25;
        #endregion

        #region Properties
        [Required(ErrorMessage = "Point name is required.")]
        [RegularExpression(@"^[1-9]\d*$", ErrorMessage = "Point number must be a positive integer.")]
        public int PointNumber
        {
            get { return _pointNumber; }
            set
            {
                _pointNumber = value;
                OnPropertyChanged(nameof(PointNumber));
                ValidatePointName(value);
            }
        }
        public double Northing
        {
            get { return _northing; }
            set
            {
                var preValue = _northing;
                var newValue = value;

                _northing = value;
                OnPropertyChanged(nameof(Northing));

                double delta = newValue - preValue;
                if (delta > double.MinValue) { UpdatePointPosition(new Vector(0, delta)); }
            }
        }
        public double Easting
        {
            get { return _easting; }
            set
            {
                if (_easting != value)
                {
                    var preValue = _easting;
                    var newValue = value;

                    _easting = value;
                    OnPropertyChanged(nameof(Easting));

                    double delta = newValue - preValue;
                    if (delta > double.MinValue) { UpdatePointPosition(new Vector(delta, 0)); }
                }
            }
        }
        public double Elevation
        {
            get { return _elevation; }
            set
            {
                if (_elevation != value)
                {
                    _elevation = value;
                    OnPropertyChanged(nameof(Elevation));
                }
            }
        }
        public string Description
        {
            get { return _description; }
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }
        public PointGroup PointGroup
        {
            get { return _pointGroup; }
            set
            {
                if (_pointGroup != value)
                {
                    _pointGroup = value;
                    OnPropertyChanged(nameof(PointGroup));
                }
            }
        }
        public CogoPointManager CogoPointManager
        {
            get { return _cogoPointManager; }
            set
            {
                if (_cogoPointManager != value)
                {
                    _cogoPointManager = value;
                    OnPropertyChanged(nameof(CogoPointManager));
                }
            }
        }
        public Point TextInfoBasePosition
        {
            get => _textInfoBasePosition;
            set
            {
                if (_textInfoBasePosition != value)
                {
                    _textInfoBasePosition = value;
                    OnPropertyChanged(nameof(TextInfoBasePosition));
                }
            }
        }
        public Point TextInfoCurrentPosition
        {
            get => _textInfoCurrentPosition;
            set
            {
                if (_textInfoCurrentPosition != value)
                {
                    _textInfoCurrentPosition = value;
                    OnPropertyChanged(nameof(TextInfoCurrentPosition));
                }
            }
        }
        public Point TextToggleButtonPosition
        {
            get => _textToggleButtonPosition;
            set
            {
                if (_textToggleButtonPosition != value)
                {
                    _textToggleButtonPosition = value;
                    OnPropertyChanged(nameof(TextToggleButtonPosition));
                }
            }
        }
        public Point TextToggleButtonScreenPosition
        {
            get => _textToggleButtonScreenPosition;
            set
            {
                if (_textToggleButtonScreenPosition != value)
                {
                    _textToggleButtonScreenPosition = value;
                    OnPropertyChanged(nameof(TextToggleButtonScreenPosition));
                }
            }
        }
        public bool TextBeingMoved
        {
            get => _textBeingMoved;
            set
            {
                if (_textBeingMoved != value)
                {
                    _textBeingMoved = value;
                    OnPropertyChanged(nameof(TextBeingMoved));
                }
            }
        }
        public double TextInfoBaseOffset
        {
            get => _textInfoBaseOffset;
            set
            {
                if (_textInfoBaseOffset != value)
                {
                    _textInfoBaseOffset = value;
                    OnPropertyChanged(nameof(TextInfoBaseOffset));
                }
            }
        }

        public Point Position => new(Easting, Northing);
        public bool HasPointNumberError => HasErrorsFor(nameof(PointNumber));

        public CogoPointVisualGroup VisualGroup { get; set; }
        public Matrix CurrentlyAppliedMarkerMatrix { get; set; } = Matrix.Identity;
        public Matrix CurrentlyAppliedTextMatrix { get; set; } = Matrix.Identity;
        public bool TextInfoInBasePosition { get; set; } = true;
        #endregion

        #region Constructors
        public CogoPoint(PointGroup pointGroup, int pointNum, Vector3 position,
            CogoPointManager cogoPointManager, float elevation = 0, string description = "")
        {
            CogoPointManager = cogoPointManager;
            PointGroup = pointGroup;
            PointNumber = pointNum;
            Northing = position.Y;
            Easting = position.X;
            Elevation = elevation;
            Description = description;
            ResetTextLocations();

            VisualGroup = new(this);
            VisualGroup.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CogoPointVisualGroup.Bounds))
                {
                    UpdateBounds();
                }
            };
            UpdateBounds();
        }
        #endregion

        #region Methods
        public override double DistanceToPoint(Point p)
        {
            return VisualGroup.DistanceToPoint(p);
        }
        public override void UpdateBounds()
        {
            Bounds = VisualGroup.Bounds;
        }

        public override void MouseEnter()
        {
            if (TextBeingMoved || IsMouseOver) { return; }
            this.IsMouseOver = true;
            RedrawAllVisuals();
        }
        public override void MouseLeave()
        {
            if (!IsMouseOver) { return; }
            this.IsMouseOver = false;
            RedrawAllVisuals();
        }

        public override void Select()
        {
            //if (TextBeingMoved) { return; }
            if (IsSelected) { return; }
            this.IsSelected = true;
            RedrawAllVisuals();
        }
        public override void Deselect()
        {
            //if (TextBeingMoved) { return; }
            if (!IsSelected) { return; }
            this.IsSelected = false;
            RedrawAllVisuals();
        }

        public void ResetTextLocations()
        {
            TextInfoBasePosition = Position;
            TextInfoCurrentPosition = Position;
            TextToggleButtonPosition = new(Position.X + (_textToggleButtonOffset * PointGroup.PointScale), Position.Y);
            TextInfoInBasePosition = true;
        }

        public void MoveTextInfoToPoint(Point point)
        {
            TextInfoCurrentPosition = point;
            TextToggleButtonPosition = point;
            UpdateTextVisualTransform(CurrentlyAppliedTextMatrix);
            TextInfoInBasePosition = false;
        }
        public void UpdateMarkerVisualTransform(Matrix matrix)
        {
            CurrentlyAppliedMarkerMatrix = matrix;
            VisualGroup.UpdateMarkerTransform(matrix);
        }
        public void UpdateTextVisualTransform(Matrix matrix)
        {
            CurrentlyAppliedTextMatrix = matrix;
            TextToggleButtonScreenPosition = matrix.Transform(TextToggleButtonPosition);
            VisualGroup.UpdateTextTransform(matrix);
        }

        public void RedrawAllVisuals()
        {
            VisualGroup?.RedrawAll();
        }
        public void RedrawEllipseVisual()
        {
            VisualGroup.RedrawEllipse();
        }
        public void RedrawTextVisual()
        {
            VisualGroup.RedrawText();
        }

        private void UpdatePointPosition(Vector translate)
        {
            TextInfoBasePosition = Position;
            TextInfoCurrentPosition += translate;
            TextToggleButtonPosition += translate;
            TextToggleButtonScreenPosition = CurrentlyAppliedTextMatrix.Transform(TextToggleButtonPosition);
            RedrawAllVisuals();
        }

        protected override void OnPropertyChanged(string propertyName)
        {
            base.OnPropertyChanged(propertyName);

            if (propertyName == nameof(PointNumber) || propertyName == nameof(Elevation) || propertyName == nameof(Description))
            {
                RedrawAllVisuals();
            }
        }
        #endregion

        #region Validation
        public bool ValidatePointName(int value)
        {
            ClearErrors(nameof(PointNumber));
            ValidateProperty(value, nameof(PointNumber));

            if (CogoPointManager?.UsedPointNumbers.Count(x => x == value) > 1)
            {
                AddError(nameof(PointNumber), $"Point number {value} is already in use.");
            }

            return !HasErrorsFor(nameof(PointNumber));
        }
        #endregion
    }
}
