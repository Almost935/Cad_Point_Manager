using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.HitTesting;
using Cad_Point_Manager.Views.UserControls;
using SharpDX;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Point = System.Windows.Point;

namespace Cad_Point_Manager.Models.PointRendering
{
    public class CogoPoint : HitTestableObject
    {
        #region Fields
        private const float _textRowDistanceFactor = 1.2f;

        private float _markerToTextOffset;

        private int _pointNumber;
        private double _northing;
        private double _easting;
        private double _elevation = 0.0f;
        private float _textHeight;
        private float _markerSize;
        private PointGroup _pointGroup;
        private string _description;
        private CogoPointManager _cogoPointManager;
        private Vector2 _textInfoBaseLocation;
        private Vector2 _pointNameLocation;
        private Vector2 _pointElevationLocation;
        private Vector2 _pointDescriptionLocation;
        private double _pointScale;
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
        public float TextHeight
        {
            get { return _textHeight; }
            set
            {
                if (_textHeight != value)
                {
                    _textHeight = value;
                    OnPropertyChanged(nameof(TextHeight));
                }
            }
        }
        public float MarkerSize
        {
            get { return _markerSize; }
            set
            {
                if (_markerSize != value)
                {
                    _markerSize = value;
                    OnPropertyChanged(nameof(MarkerSize));
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
        public Vector2 TextInfoBaseLocation
        {
            get => _textInfoBaseLocation;
            set
            {
                if (value != _textInfoBaseLocation)
                {
                    _textInfoBaseLocation = value;
                    OnPropertyChanged(nameof(TextInfoBaseLocation));
                }
            }
        }
        public Vector2 PointNumberLocation
        {
            get => _pointNameLocation;
            set
            {
                if (value != _pointNameLocation)
                {
                    _pointNameLocation = value;
                    OnPropertyChanged(nameof(PointNumberLocation));
                }
            }
        }
        public Vector2 PointElevationLocation
        {
            get => _pointElevationLocation;
            set
            {
                if (value != _pointElevationLocation)
                {
                    _pointElevationLocation = value;
                    OnPropertyChanged(nameof(PointElevationLocation));
                }
            }
        }
        public Vector2 PointDescriptionLocation
        {
            get => _pointDescriptionLocation;
            set
            {
                if (value != _pointDescriptionLocation)
                {
                    _pointDescriptionLocation = value;
                    OnPropertyChanged(nameof(PointDescriptionLocation));
                }
            }
        }
        public double PointScale
        {
            get => _pointScale;
            set
            {
                if (_pointScale != value)
                {
                    _pointScale = value;
                    OnPropertyChanged(nameof(PointScale));
                }
            }
        }

        public Vector3 RenderPosition => new((float)Easting, (float)Northing, 0);
        public Point PointPosition => new(Easting, Northing); 
        public bool HasPointNumberError => HasErrorsFor(nameof(PointNumber));

        public TextVertex[] TextVertices { get; set; } = [];
        public CircleVertex[] MarkerVertices { get; set; } = new CircleVertex[1];
        public int TextStartIndex { get; set; }
        public int TextEndIndex { get; set; }
        public int MarkerStartIndex { get; set; }
        public int MarkerEndIndex { get; set; }
        #endregion

        #region Constructors
        public CogoPoint(PointGroup pointGroup, int pointNum, Vector3 position, float textHeight, float markerSize, 
            CogoPointManager cogoPointManager, float elevation = 0, string description = "")
        { 
            CogoPointManager = cogoPointManager;
            PointGroup = pointGroup;
            PointNumber = pointNum;
            Northing = position.Y;
            Easting = position.X;
            TextHeight = textHeight;
            MarkerSize = markerSize;
            Elevation = elevation;
            Description = description;
            _markerToTextOffset = textHeight * 0.2f;
            GetTextLocations();
        }
        #endregion

        #region Methods
        private void GetTextLocations()
        {
            TextInfoBaseLocation = new((float)Easting + _markerToTextOffset, (float)Northing);
            PointNumberLocation = TextInfoBaseLocation;
            PointElevationLocation = new(TextInfoBaseLocation.X, TextInfoBaseLocation.Y + TextHeight * _textRowDistanceFactor);
            PointDescriptionLocation = new(TextInfoBaseLocation.X, TextInfoBaseLocation.Y + TextHeight * 2 * _textRowDistanceFactor);
        }

        public override double DistanceToPoint(System.Windows.Point p)
        {
            if (MathHelpers.PointToPointDistance(p, RenderPosition.ToPoint()) < MarkerSize) { return 0.0; }

            if (TextVertices.Length < 3) { return double.MaxValue; };

            Vector2 testPoint = new((float)p.X, (float)p.Y);
            double minDistance = double.MaxValue;
            bool pointInside = false;
            object locker = new();

            Parallel.For(0, TextVertices.Length / 3, (i, state) =>
            {
                if (pointInside) return;

                Vector2 v0 = TextVertices[i * 3 + 0].Position.ToVector2();
                Vector2 v1 = TextVertices[i * 3 + 1].Position.ToVector2();
                Vector2 v2 = TextVertices[i * 3 + 2].Position.ToVector2();

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
            if (TextVertices.Length == 0)
            {
                Bounds = Rect.Empty;
                return;
            }

            Span<TextVertex> span = TextVertices.AsSpan();

            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            for (int i = 0; i < span.Length; i++)
            {
                var pos = span[i].Position;
                minX = Math.Min(minX, pos.X);
                minY = Math.Min(minY, pos.Y);
                maxX = Math.Max(maxX, pos.X);
                maxY = Math.Max(maxY, pos.Y);
            }

            Bounds = new Rect(new System.Windows.Point(minX, minY), new System.Windows.Point(maxX, maxY));

            Rect circleBounds = new Rect(new System.Windows.Point(RenderPosition.X - MarkerSize, RenderPosition.Y - MarkerSize),
                new System.Windows.Point(RenderPosition.X + MarkerSize, RenderPosition.Y + MarkerSize));
            Bounds.Union(circleBounds);
        }

        public override void MouseEnter()
        {
            this.IsMouseOver = true;

            Span<TextVertex> textSpan = TextVertices;
            for (int i = 0; i < textSpan.Length; i++)
            {
                textSpan[i].SetIsMouseOver(true);
                TextVertices[i].SetIsMouseOver(true);
            }

            Span<CircleVertex> markerSpan = MarkerVertices;
            for (int i = 0; i < markerSpan.Length; i++)
            {
                markerSpan[i].SetIsMouseOver(true);
            }
        }
        public override void MouseLeave()
        {
            this.IsMouseOver = false;

            Span<TextVertex> textSpan = TextVertices;
            for (int i = 0; i < textSpan.Length; i++)
            {
                textSpan[i].SetIsMouseOver(false);
            }

            Span<CircleVertex> markerSpan = MarkerVertices;
            for (int i = 0; i < markerSpan.Length; i++)
            {
                markerSpan[i].SetIsMouseOver(false);
            }
        }

        public override void Select()
        {
            this.IsSelected = true;

            Span<TextVertex> textSpan = TextVertices;
            for (int i = 0; i < textSpan.Length; i++)
            {
                textSpan[i].SetIsSelected(true);
            }

            Span<CircleVertex> markerSpan = MarkerVertices;
            for (int i = 0; i < markerSpan.Length; i++)
            {
                markerSpan[i].SetIsSelected(true);
            }
        }
        public override void Deselect()
        {
            this.IsSelected = false;

            Span<TextVertex> textSpan = TextVertices;
            for (int i = 0; i < textSpan.Length; i++)
            {
                textSpan[i].SetIsSelected(false);
            }

            Span<CircleVertex> markerSpan = MarkerVertices;
            for (int i = 0; i < markerSpan.Length; i++)
            {
                markerSpan[i].SetIsSelected(false);
            }
        }

        public void UpdateTextVertices(CogoPointTextVerticesDict textDict)
        {
            TextVertices ??= Array.Empty<TextVertex>();
            Array.Clear(TextVertices);

            List<TextVertex> textVertices = [];

            textVertices.AddRange(textDict.GetTextVertices(PointNumber.ToString(), TextHeight, PointNumberLocation, PointGroup.Color));
            textVertices.AddRange(textDict.GetTextVertices(Elevation.ToString("F3"), TextHeight, PointElevationLocation, PointGroup.Color));
            textVertices.AddRange(textDict.GetTextVertices(Description, TextHeight, PointDescriptionLocation, PointGroup.Color));
            //TextVertices = textDict.GetIntTextVertices(PointNumber, TextHeight, new Vector2(RenderPosition.X + _markerToTextOffset, RenderPosition.Y), PointGroup.Color);

            //TextVertices = textVertices.ToArray();
            UpdateBounds();
        }
        public void UpdateMarkerVertices()
        {
            MarkerVertices[0] = new(RenderPosition, PointGroup.Color, MarkerSize, PointGroup.IsVisible ? 1 : 0,
                IsMouseOver ? 1 : 0, IsSelected ? 1 : 0);
        }

        protected override void OnPropertyChanged(string propertyName)
        {
            base.OnPropertyChanged(propertyName);

            if (propertyName == nameof(PointNumber) || propertyName == nameof(Northing) || propertyName == nameof(Easting) ||
                propertyName == nameof(Elevation) || propertyName == nameof(Description) || propertyName == nameof(TextHeight) ||
                propertyName == nameof(MarkerSize) || propertyName == nameof(PointGroup))
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
