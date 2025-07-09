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

namespace Cad_Point_Manager.Models.PointRendering
{
    public class CogoPoint : HitTestableObject
    {
        #region Fields
        private int _pointNumber;
        private double _northing;
        private double _easting;
        private double _elevation = 0.0f;
        private PointGroup _pointGroup;
        private string _description;
        private CogoPointManager _cogoPointManager;
        private Point _textInfoPosition = new();
        private double _textInfoOffset = 0.65;
        private Point _textToggleButtonPosition = new();
        private Point _textToggleButtonScreenPosition = new();
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
                _northing = value;
                OnPropertyChanged(nameof(Northing));
            }
        }
        public double Easting
        {
            get { return _easting; }
            set
            {
                if (_easting != value)
                {
                    _easting = value;
                    OnPropertyChanged(nameof(Easting));
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
        public Point TextInfoPosition
        {
            get => _textInfoPosition;
            set
            {
                if (_textInfoPosition != value)
                {
                    _textInfoPosition = value;
                    OnPropertyChanged(nameof(TextInfoPosition));
                }
            }
        }
        public double TextInfoOffset
        {
            get => _textInfoOffset;
            set
            {
                if (_textInfoOffset != value)
                {
                    _textInfoOffset = value;
                    OnPropertyChanged(nameof(TextInfoOffset));
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

        public Point Position => new(Easting, Northing); 
        public bool HasPointNumberError => HasErrorsFor(nameof(PointNumber));

        public CogoPointVisualGroup VisualGroup { get; set; }
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
            GetTextLocations();

            VisualGroup = new(this);
            UpdateBounds();
        }
        #endregion

        #region Methods
        private void GetTextLocations()
        {
            TextInfoPosition = new(Position.X + _textInfoOffset, Position.Y);
            TextToggleButtonPosition = new(Position.X + TextInfoOffset / 1.5, Position.Y);
        }

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
            this.IsMouseOver = true;
            //VisualGroup.Visual.Effect = new DropShadowEffect()
            //{
            //    BlurRadius = 1,
            //    Color = Colors.Black,
            //    ShadowDepth = 0, 
            //    RenderingBias = RenderingBias.Performance
            //};
            RedrawVisual();
        }
        public override void MouseLeave()
        {
            this.IsMouseOver = false;
            //VisualGroup.Visual.Effect = null;
            RedrawVisual();
        }

        public override void Select()
        {
            this.IsSelected = true;
            RedrawVisual();
        }
        public override void Deselect()
        {
            this.IsSelected = false;
            RedrawVisual();
        }

        public void UpdateVisualTransform(Matrix matrix)
        {
            VisualGroup.UpdateTransform(matrix);
        }
        public void RedrawVisual()
        {
           VisualGroup.Redraw();
        }

        protected override void OnPropertyChanged(string propertyName)
        {
            base.OnPropertyChanged(propertyName);

            if (propertyName == nameof(PointNumber) || propertyName == nameof(Northing) || propertyName == nameof(Easting) ||
                propertyName == nameof(Elevation) || propertyName == nameof(Description))
            {
                _cogoPointManager.SetCadManagerPointVerticesDirty();
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
