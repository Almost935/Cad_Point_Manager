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
    public class CogoPoint : HitTestableObject
    {
        #region Fields
        private const float _textLineSpacingFactor = 1.0f;
        private const float _markerToPointScaleFactor = 0.5f;
        private const float _markerBaseSize = 0.4f;
        private const float _textBaseHeight = 4;

        private int _pointNumber;
        private double _northing;
        private double _easting;
        private double _elevation = 0.0f;
        private PointGroup _pointGroup;
        private string _description;
        private CogoPointManager _cogoPointManager;
        private bool _textBeingMoved = false;
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

        public Point Position => new(Easting, Northing);
        public bool HasPointNumberError => HasErrorsFor(nameof(PointNumber));

        public CircleVertex MarkerVertex { get; set; }
        public List<TextVertex> TextVertices { get; set; } = [];
        public List<TextVertex> PointNumberVertices { get; set; } = [];
        public List<TextVertex> ElevationVertices { get; set; } = [];
        public List<TextVertex> DescriptionVertices { get; set; } = [];
        public Rect PointNumberBounds { get; set; } = Rect.Empty;
        public Rect ElevationBounds { get; set; } = Rect.Empty;
        public Rect DescriptionBounds { get; set; } = Rect.Empty;
        public Vector2 TextInfoBasePosition { get; set; }
        public Vector2 PointNumberPosition { get; set; }
        public Vector2 ElevationPosition { get; set; }
        public Vector2 DescriptionPosition { get; set; }
        public bool TextVerticesInitialized { get; set; }
        public int TextStartIndex { get; set; }
        public int TextEndIndex { get; set; }
        public int MarkerIndex { get; set; }
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
        public override double DistanceToPoint(Point p)
        {
            if (MathHelpers.PointToPointDistance(p,Position) < _markerBaseSize) { return 0.0; }

            if (TextVertices.Count < 3) { return double.MaxValue; }

            Vector2 testPoint = new((float)p.X, (float)p.Y);
            double minDistance = double.MaxValue;
            bool pointInside = false;
            object locker = new();

            Parallel.For(0, TextVertices.Count / 3, (i, state) =>
            {
                if (pointInside) return;

                Vector2 v0 = TextVertices[i * 3 + 0].Position.ToSharpDXVector2();
                Vector2 v1 = TextVertices[i * 3 + 1].Position.ToSharpDXVector2();
                Vector2 v2 = TextVertices[i * 3 + 2].Position.ToSharpDXVector2();

                if (MathHelpers.IsPointInTriangle(testPoint, v0, v1, v2))
                {
                    lock (locker)
                    {
                        pointInside = true;
                        minDistance = 0.0;
                    }

                    state.Stop();
                }
                else
                {
                    double dist = MathHelpers.DistanceToTriangle(testPoint, v0, v1, v2);

                    lock (locker)
                    {
                        if (dist < minDistance)
                            minDistance = dist;
                    }
                }
            });

            return minDistance;
        }
        public override void UpdateBounds()
        {
            if (TextVertices.Count == 0)
            {
                Bounds = Rect.Empty;
                return;
            }

            bool haveValid = false;
            double minX = 0, minY = 0, maxX = 0, maxY = 0;

            for (int i = 0; i < TextVertices.Count; i++)
            {
                var p = TextVertices[i].Position;
                if (!(double.IsFinite(p.X) && double.IsFinite(p.Y)))
                    continue;

                if (!haveValid)
                {
                    minX = maxX = p.X;
                    minY = maxY = p.Y;
                    haveValid = true;
                }
                else
                {
                    minX = Math.Min(minX, p.X);
                    minY = Math.Min(minY, p.Y);
                    maxX = Math.Max(maxX, p.X);
                    maxY = Math.Max(maxY, p.Y);
                }
            }

            var bounds = haveValid ? new Rect(new Point(minX, minY), new Point(maxX, maxY)) : Rect.Empty;

            // Only add the circle if its center is valid
            if (double.IsFinite(Position.X) && double.IsFinite(Position.Y) && double.IsFinite(_markerBaseSize))
            {
                var circleBounds = new Rect(
                    new Point(Position.X - _markerBaseSize, Position.Y - _markerBaseSize),
                    new Point(Position.X + _markerBaseSize, Position.Y + _markerBaseSize));

                bounds = Rect.Union(bounds, circleBounds); // <-- assign back
            }

            Bounds = bounds;
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

        public void ResetTextLocations()
        {
            TextInfoBasePosition = new(Position.X.ToFloat() + (_textBaseHeight * PointGroup.PointScale.ToFloat() * _markerToPointScaleFactor), Position.Y.ToFloat());
            PointNumberPosition = TextInfoBasePosition;
            ElevationPosition = new(PointNumberPosition.X, PointNumberPosition.Y + _textBaseHeight * PointGroup.PointScale.ToFloat() * _textLineSpacingFactor);
            DescriptionPosition = new(ElevationPosition.X, ElevationPosition.Y + _textBaseHeight * PointGroup.PointScale.ToFloat() * _textLineSpacingFactor);
        }

        public void MoveTextInfoToPoint(Point point)
        {

        }

        private void UpdatePointPosition(Vector translate)
        {

        }

        private void InitializeTextLocations()
        {
            TextInfoBasePosition = new(Position.X.ToFloat() + (_textBaseHeight * PointGroup.PointScale.ToFloat() * _markerToPointScaleFactor), Position.Y.ToFloat());
            DescriptionPosition = TextInfoBasePosition;
            ElevationPosition = new(DescriptionPosition.X, DescriptionPosition.Y + _textBaseHeight * PointGroup.PointScale.ToFloat() * _textLineSpacingFactor);
            PointNumberPosition = new(ElevationPosition.X, ElevationPosition.Y + _textBaseHeight * PointGroup.PointScale.ToFloat() * _textLineSpacingFactor);
        }

        public void InitializeTextVertices(CogoPointTextVerticesDict textDict)
        {
            TextVertices.Clear();
            List<TextVertex> pointNum = textDict.GetIntTextVertices(PointNumber, _textBaseHeight * PointGroup.PointScale.ToFloat(), PointNumberPosition, PointGroup.Color);
            List<TextVertex> elev = textDict.GetTextVertices(Elevation.ToString("F3"), _textBaseHeight * PointGroup.PointScale.ToFloat(), ElevationPosition, PointGroup.Color);
            List<TextVertex> description = textDict.GetTextVertices(Description, _textBaseHeight * PointGroup.PointScale.ToFloat(), DescriptionPosition, PointGroup.Color);
            TextVertices = pointNum.Concat(elev).Concat(description).ToList();
            TextVerticesInitialized = true;
            UpdateBounds();
        }
        public void InitializeMarkerVertices()
        {
            MarkerVertex = new(Position.ToSharpDXVector3(), PointGroup.Color, _markerBaseSize * PointGroup.PointScale.ToFloat(), PointGroup.IsVisible ? 1 : 0,
                IsMouseOver ? 1 : 0, IsSelected ? 1 : 0);
        }

        protected override void OnPropertyChanged(string propertyName)
        {
            base.OnPropertyChanged(propertyName);

            //if (propertyName == nameof(PointNumber) || propertyName == nameof(Elevation) || propertyName == nameof(Description))
            //{
            //    RedrawAllVisuals();
            //}
        }
        #endregion
    }
}
