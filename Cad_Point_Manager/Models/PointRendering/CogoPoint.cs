using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.HitTesting;
using Cad_Point_Manager.Views;
using SharpDX;
using SharpDX.DirectWrite;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using Matrix = System.Windows.Media.Matrix;
using Point = System.Windows.Point;

namespace Cad_Point_Manager.Models.PointRendering
{
    public class CogoPoint : HitTestableObject, IEquatable<CogoPoint>
    {
        #region Fields
        private const float _textLineSpacingFactor = 1.0f;
        private const float _markerToPointScaleFactor = 0.5f;
        private const float _textBaseHeight = 4;

        private volatile CogoPointBoundsSnapshot _cogoPointBounds;
        private static readonly CogoPointBoundsSnapshot _empty =
           new() { Name = Rect.Empty, Elevation = Rect.Empty, Description = Rect.Empty, Ellipse = Rect.Empty };

        private int _pointNumber;
        private double _northing;
        private double _easting;
        private double _elevation = 0.0f;
        private PointGroup _pointGroup;
        private string _description;
        private CogoPointManager _cogoPointManager;
        private Point _textTbBasePosition = new();
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

        public Point Position => new(Easting, Northing);
        public bool HasPointNumberError => HasErrorsFor(nameof(PointNumber));
        public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

        public CogoPointBoundsSnapshot CogoPointBounds => _cogoPointBounds ?? _empty;
        public List<TextVertex> TextVertices { get; set; } = [];
        public List<TextVertex> PointNumberVertices { get; set; } = [];
        public List<TextVertex> ElevationVertices { get; set; } = [];
        public List<TextVertex> DescriptionVertices { get; set; } = [];
        public Rect PointNumberBounds { get; set; } = Rect.Empty;
        public Rect ElevationBounds { get; set; } = Rect.Empty;
        public Rect DescriptionBounds { get; set; } = Rect.Empty;
        public Rect EllipseBounds { get; set; } = Rect.Empty;
        public Vector2 TextInfoBasePosition { get; set; }
        public Vector2 TextInfoOffset { get; set; }
        public Vector2 TextInfoCurrentPos { get; set; }
        public Vector2 PointNumberPosition { get; set; }
        public Vector2 ElevationPosition { get; set; }
        public Vector2 DescriptionPosition { get; set; }
        public bool TextVerticesInitialized { get; set; }
        public int TextStartIndex { get; set; }
        public int TextEndIndex { get; set; }
        public int MarkerIndex { get; set; }
        public Rect ToggleBounds { get; set; } = Rect.Empty;
        public bool IsMouseOverToggleButton { get; set; } = false;
        public bool IsToggleButtonPressed { get; set; } = false;
        public bool HasLeaderLine { get; set; } = false;
        public LabelState PointNumberLabelState { get; set; }
        public LabelState ElevationLabelState { get; set; }
        public LabelState DescriptionLabelState { get; set; }
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
            InitializeTextLocations();
            ResetTextLocations();
            UpdateBounds();
        }
        #endregion

        #region Methods
        public void SetBoundsSnapshot(CogoPointBoundsSnapshot snap) => _cogoPointBounds = snap;

        public override double DistanceToPoint(Point p)
        {
            if (PointNumberBounds.Contains(p) || ElevationBounds.Contains(p) || DescriptionBounds.Contains(p) || EllipseBounds.Contains(p))
            {
                return 0.0;
            }
            else
            {
                if (IsSelected)
                {
                    if (ToggleBounds.Contains(p))
                    {
                        return 0.0;
                    }
                }
                return double.MaxValue;
            }
        }
        public override void UpdateBounds()
        {
            Bounds = Rect.Empty;
            if (EllipseBounds != Rect.Empty) { Bounds = Rect.Union(Bounds, EllipseBounds); }
            if (PointNumberBounds != Rect.Empty) { Bounds = Rect.Union(Bounds, PointNumberBounds); }
            if (ElevationBounds != Rect.Empty) { Bounds = Rect.Union(Bounds, ElevationBounds); }
            if (DescriptionBounds != Rect.Empty) { Bounds = Rect.Union(Bounds, DescriptionBounds); }
        }
        public bool CogoPointIntersectsRect(Rect rect)
        {
            if (rect.IsEmpty) { return false; }

            // normalize in case user dragged "backwards"
            rect = new Rect(
                Math.Min(rect.Left, rect.Right),
                Math.Min(rect.Top, rect.Bottom),
                Math.Abs(rect.Width),
                Math.Abs(rect.Height));

            bool IntersectsOrContains(Rect part)
            {
                if (part.IsEmpty) { return false; }
                return rect.IntersectsWith(part) || rect.Contains(part) || part.Contains(rect);
            }

            if (IntersectsOrContains(EllipseBounds)) { return true; }
            if (IntersectsOrContains(PointNumberBounds)) { return true; }
            if (IntersectsOrContains(ElevationBounds)) { return true; }
            if (IntersectsOrContains(DescriptionBounds)) { return true; }

            return false;
        }

        public override void MouseEnter()
        {
            this.IsMouseOver = true;
        }
        public override void MouseLeave()
        {
            this.IsMouseOver = false;
        }

        public override void Select()
        {
            this.IsSelected = true;
        }
        public override void Deselect()
        {
            this.IsSelected = false;
        }

        public void UpdatePointGroup(PointGroup pointGroup)
        {
            if (pointGroup == null) { return; }
            if (PointGroup == pointGroup) { return; }
            PointGroup = pointGroup;
            PointGroup.TryAddPoint(this);
        }

        public void SetTextInfoOffset(Vector2 offset)
        {
            TextInfoOffset = offset;
        }

        public void ResetTextLocations()
        {
            HasLeaderLine = false;
            TextInfoBasePosition = new(Position.X.ToFloat() + (_textBaseHeight * PointGroup.PointScale.ToFloat() * _markerToPointScaleFactor), Position.Y.ToFloat());
            TextInfoCurrentPos = TextInfoBasePosition;
            DescriptionPosition = TextInfoBasePosition;
            ElevationPosition = new(DescriptionPosition.X, DescriptionPosition.Y + _textBaseHeight * PointGroup.PointScale.ToFloat() * _textLineSpacingFactor);
            PointNumberPosition = new(ElevationPosition.X, ElevationPosition.Y + _textBaseHeight * PointGroup.PointScale.ToFloat() * _textLineSpacingFactor);
        }

        public void MoveTextInfoToPoint(Point newBasePos)
        {
            HasLeaderLine = true;
            TextInfoCurrentPos = newBasePos.ToSharpDXVector2();

            // Recompute the three lines using your spacing + scale
            float h = _textBaseHeight * PointGroup.PointScale.ToFloat();

            DescriptionPosition = TextInfoCurrentPos;
            ElevationPosition = new(DescriptionPosition.X, DescriptionPosition.Y + h * _textLineSpacingFactor);
            PointNumberPosition = new(ElevationPosition.X, ElevationPosition.Y + h * _textLineSpacingFactor);

            // Bounds & verts will be rebuilt by the renderer on the next frame
            UpdateBounds();
        }

        private void UpdatePointPosition(Vector translate)
        {

        }

        private void InitializeTextLocations()
        {
            TextInfoBasePosition = new(Position.X.ToFloat() + (_textBaseHeight * PointGroup.PointScale.ToFloat() * _markerToPointScaleFactor), Position.Y.ToFloat());
            TextInfoCurrentPos = TextInfoBasePosition;
            DescriptionPosition = TextInfoBasePosition;
            ElevationPosition = new(DescriptionPosition.X, DescriptionPosition.Y + _textBaseHeight * PointGroup.PointScale.ToFloat() * _textLineSpacingFactor);
            PointNumberPosition = new(ElevationPosition.X, ElevationPosition.Y + _textBaseHeight * PointGroup.PointScale.ToFloat() * _textLineSpacingFactor);
        }

        public void InitializeTextVertices(CogoPointTextVerticesDict textDict)
        {
            TextVertices.Clear();
            List<TextVertex> pointNum = textDict.GetIntTextVertices(PointNumber, _textBaseHeight * PointGroup.PointScale.ToFloat(), 
                PointNumberPosition, PointGroup.Color);
            List<TextVertex> elev = textDict.GetTextVertices(Elevation.ToString("F3"), _textBaseHeight * PointGroup.PointScale.ToFloat(), 
                ElevationPosition, PointGroup.Color);
            List<TextVertex> description = textDict.GetTextVertices(Description, _textBaseHeight * PointGroup.PointScale.ToFloat(), 
                DescriptionPosition, PointGroup.Color);
            TextVertices = pointNum.Concat(elev).Concat(description).ToList();
            TextVerticesInitialized = true;
            UpdateBounds();
        }
        protected override void OnPropertyChanged(string propertyName)
        {
            base.OnPropertyChanged(propertyName);
        }
        #endregion

        #region IEquatable Implementation
        public bool Equals(CogoPoint other) =>
        other is not null && PointNumber == other.PointNumber;

        public override bool Equals(object obj) =>
            obj is CogoPoint other && Equals(other);

        public override int GetHashCode() => PointNumber; // int is fine

        public static bool operator ==(CogoPoint a, CogoPoint b) =>
            EqualityComparer<CogoPoint>.Default.Equals(a, b);
        public static bool operator !=(CogoPoint a, CogoPoint b) => !(a == b);
        #endregion
    }
}
