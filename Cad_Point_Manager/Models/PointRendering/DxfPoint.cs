using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.HitTesting;
using SharpDX;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Cad_Point_Manager.Models.PointRendering
{
    public class DxfPoint : HitTestableObject
    {
        #region Fields
        private float _markerToTextOffset = 0.25f;

        private int _pointNumber;
        private Vector3 _position = Vector3.Zero;
        private float _elevation = 0.0f;
        private float _textHeight;
        private float _markerSize;
        private PointGroup _pointGroup;
        private string _description;
        private CogoPointManager _cogoPointManager;
        #endregion

        #region Properties
        [Required(ErrorMessage = "Point name is required.")]
        [RegularExpression(@"^[1-9]\d*$", ErrorMessage = "Point number must be a positive integer.")]
        public int PointNumber
        {
            get { return _pointNumber; }
            set
            {
                if (_pointNumber != value)
                {
                    if (ValidatePointName(value))
                    {
                        _pointNumber = value;
                        OnPropertyChanged(nameof(PointNumber));
                    }
                }
                else
                {
                    ClearErrors(nameof(PointNumber));
                }
            }
        }

        public Vector3 Position
        {
            get { return _position; }
            set
            {
                if (_position != value)
                {
                    _position = value;
                    OnPropertyChanged(nameof(Position));
                    OnPropertyChanged(nameof(Easting));
                    OnPropertyChanged(nameof(Northing));
                }
            }
        }
        public float Elevation
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

        public float Easting => Position.X;
        public float Northing => Position.Y;
        public bool HasPointNumberError => HasErrorsFor(nameof(PointNumber));

        public TextVertex[] TextVertices { get; set; } = [];
        public CircleVertex[] MarkerVertices { get; set; } = new CircleVertex[1];
        public int TextStartIndex { get; set; }
        public int TextEndIndex { get; set; }
        public int MarkerStartIndex { get; set; }
        public int MarkerEndIndex { get; set; }
        #endregion

        #region Constructors
        public DxfPoint(PointGroup pointGroup, int pointNum, Vector3 position, float textHeight, float markerSize, CogoPointManager cogoPointManager, float elevation = 0,
            string description = "")
        {
            PointGroup = pointGroup;
            PointNumber = pointNum;
            Position = position;
            TextHeight = textHeight;
            MarkerSize = markerSize;
            CogoPointManager = cogoPointManager;
            Elevation = elevation;
            Description = description;
        }
        #endregion

        #region Methods
        public override double DistanceToPoint(System.Windows.Point p)
        {
            if (MathHelpers.PointToPointDistance(p, Position.ToPoint()) < MarkerSize) { return 0.0; }

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

            Rect circleBounds = new Rect(new System.Windows.Point(Position.X - MarkerSize, Position.Y - MarkerSize),
                new System.Windows.Point(Position.X + MarkerSize, Position.Y + MarkerSize));
            Bounds.Union(circleBounds);
        }

        public override void MouseEnter()
        {
            this.IsMouseOver = true;

            Span<TextVertex> textSpan = TextVertices;
            for (int i = 0; i < textSpan.Length; i++)
            {
                textSpan[i].SetIsMouseOver(true);
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
        }
        public override void Deselect()
        {
            this.IsSelected = false;

            Span<TextVertex> textSpan = TextVertices;
            for (int i = 0; i < textSpan.Length; i++)
            {
                textSpan[i].SetIsSelected(false);
            }
        }

        public void UpdateTextVertices(DxfPointTextVerticesDict textDict)
        {
            TextVertices ??= Array.Empty<TextVertex>();
            Array.Clear(TextVertices);

            TextVertices = textDict.GetIntTextVertices(PointNumber, TextHeight, new Vector2(Position.X + _markerToTextOffset, Position.Y), PointGroup.Color);

            UpdateBounds();
        }
        public void UpdateMarkerVertices()
        {
            MarkerVertices[0] = new(Position, PointGroup.Color, MarkerSize, PointGroup.IsVisible ? 1 : 0,
                IsMouseOver ? 1 : 0, IsSelected ? 1 : 0);
        }
        #endregion

        #region Validation
        public bool ValidatePointName(int value)
        {
            ClearErrors(nameof(PointNumber));
            ValidateProperty(value, nameof(PointNumber));

            if (CogoPointManager?.UsedPointNumbers.Contains(value) == true)
            {
                AddError(nameof(PointNumber), $"Point number {value} is already in use.");
            }

            return !HasErrorsFor(nameof(PointNumber));
        }
        #endregion
    }
}
