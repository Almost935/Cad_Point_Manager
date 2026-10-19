using Cad_Point_Manager.Controls.D3DControl.Rendering.Msdf;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.HitTesting;
using SharpDX;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Windows;
using Point = System.Windows.Point;

namespace Cad_Point_Manager.Models.PointRendering
{
    #region Structures
    public readonly record struct CogoPointInfoState(
         Vector2 Offset, CogoLabelQuadrant Quadrant, bool HasLeaderLine);
    #endregion

    #region Enums
    public enum CogoLabelQuadrant
    {
        UpperRight = 0,
        UpperLeft = 1,
        LowerLeft = 2,
        LowerRight = 3
    }
    #endregion

    public class CogoPoint : HitTestableObject
    {
        #region Fields
        private const float _textLineSpacingFactor = 1.0f;
        private const float _textBaseHeight = 4;

        private volatile CogoPointBoundsSnapshot _cogoPointBounds;
        private static readonly CogoPointBoundsSnapshot _empty = new()
        {
            Name = Rect.Empty,
            Elevation = Rect.Empty,
            Description = Rect.Empty,
            Ellipse = Rect.Empty
        };

        private int _pointNumber;
        private double _northing;
        private double _easting;
        private double _elevation = 0.0f;
        private PointGroup _pointGroup;
        private string _description;
        private bool _isEditing = false;
        private CogoLabelQuadrant _labelQuadrant = CogoLabelQuadrant.UpperRight;
        #endregion

        #region Properties
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
                if (_northing != value)
                {
                    _northing = value;
                    OnPropertyChanged(nameof(Northing));
                }
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
            get => _pointGroup;
            set
            {
                if (_pointGroup == value)
                {
                    return;
                }

                var oldGroup = _pointGroup;

                _pointGroup = value;

                OnPropertyChanged(nameof(PointGroup));

                oldGroup?.NotifyPointCountChanged();
                _pointGroup?.NotifyPointCountChanged();
            }
        }
        public bool IsEditing
        {
            get { return _isEditing; }
            set
            {
                if (_isEditing != value)
                {
                    _isEditing = value;
                    OnPropertyChanged(nameof(IsEditing));
                }
            }
        }
        public CogoLabelQuadrant LabelQuadrant
        {
            get => _labelQuadrant;
            set
            {
                if (_labelQuadrant == value) { return; }

                _labelQuadrant = value;
                OnPropertyChanged(nameof(LabelQuadrant));
            }
        }

        public bool IsElevationVisible => PointGroup.ShowElevation;
        public bool IsDescriptionVisible => PointGroup.ShowDescription && HasDescription;
        public Point Position => new(Easting, Northing);
        public bool HasPointNumberError => HasErrorsFor(nameof(PointNumber));
        public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
        public bool IsLabelLeft =>
            LabelQuadrant == CogoLabelQuadrant.UpperLeft || LabelQuadrant == CogoLabelQuadrant.LowerLeft;
        public bool IsLabelBelow =>
            LabelQuadrant == CogoLabelQuadrant.LowerLeft || LabelQuadrant == CogoLabelQuadrant.LowerRight;

        public CadManager CadManager { get; }
        public CogoPointBoundsSnapshot CogoPointBounds => _cogoPointBounds ?? _empty;
        public Rect PointNumberBounds { get; set; } = Rect.Empty;
        public Rect ElevationBounds { get; set; } = Rect.Empty;
        public Rect DescriptionBounds { get; set; } = Rect.Empty;
        public Rect EllipseBounds { get; set; } = Rect.Empty;
        public Rect ToggleBounds { get; set; } = Rect.Empty;
        public Vector2 TextInfoOffset { get; set; }
        public Vector2 PointNumberOffset { get; set; }
        public Vector2 ElevationOffset { get; set; }
        public Vector2 DescriptionOffset { get; set; }
        public bool TextVerticesInitialized { get; set; }
        public int TextStartIndex { get; set; }
        public int TextEndIndex { get; set; }
        public int MarkerIndex { get; set; }
        public bool IsMouseOverToggleButton { get; set; } = false;
        public bool IsToggleButtonPressed { get; set; } = false;
        public bool HasLeaderLine { get; set; } = false;
        public MsdfGlyphHitRegion[] PointNumberGlyphs { get; set; } = [];
        public MsdfGlyphHitRegion[] ElevationGlyphs { get; set; } = [];
        public MsdfGlyphHitRegion[] DescriptionGlyphs { get; set; } = [];

        public float BasePointNumberOffset_Y => _textBaseHeight * _textLineSpacingFactor * 2;
        public float BaseElevationOffset_Y => _textBaseHeight * _textLineSpacingFactor;
        public float BaseDescriptionOffset_Y => 0;
        #endregion

        #region Constructors
        public CogoPoint(PointGroup pointGroup, int pointNum, Vector3 position,
            CadManager cadManager, float elevation = 0, string description = "")
        {
            CadManager = cadManager;
            PointGroup = pointGroup;
            PointNumber = pointNum;
            Northing = position.Y;
            Easting = position.X;
            Elevation = elevation;
            Description = description;
            ResetTextLocations();
            UpdateBounds();
        }
        #endregion

        #region Methods
        public override string ToString()
        {
            return $"CogoPoint PointNumber: {PointNumber}";
        }

        public override double DistanceToPoint(Point p, MsdfAtlas atlas = null)
        {
            if (atlas is not null)
            {
                return DistanceToCogoPointViaAtlas(this, p, atlas);
            }

            if (PointNumberBounds.Contains(p) ||
                ElevationBounds.Contains(p) ||
                DescriptionBounds.Contains(p) ||
                EllipseBounds.Contains(p))
            {
                return 0.0;
            }
            else if (HasLeaderLine)
            {
                return MathHelpers.PointToLineDistance(p, Position, Point.Add(Position, TextInfoOffset.ToVector()));
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
        public bool IntersectsHitRange(Rect hitTestRange)
        {
            if (hitTestRange.IsEmpty)
                return false;

            var testRect = EllipseBounds;
            testRect.Union(PointNumberBounds);
            testRect.Union(ElevationBounds);
            testRect.Union(DescriptionBounds);
            testRect.Union(ToggleBounds);

            if (!testRect.IsEmpty && testRect.IntersectsWith(hitTestRange))
                return true;

            //if (!EllipseBounds.IsEmpty && EllipseBounds.IntersectsWith(hitTestRange))
            //    return true;

            //if (!PointNumberBounds.IsEmpty && PointNumberBounds.IntersectsWith(hitTestRange))
            //    return true;

            //if (!ElevationBounds.IsEmpty && ElevationBounds.IntersectsWith(hitTestRange))
            //    return true;

            //if (!DescriptionBounds.IsEmpty && DescriptionBounds.IntersectsWith(hitTestRange))
            //    return true;

            //if (!ToggleBounds.IsEmpty && IsSelected && ToggleBounds.IntersectsWith(hitTestRange))
            //    return true;

            //if (HasLeaderLine)
            //{

            //}

            return false;
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
        }

        public void SetTextInfoOffset(Vector2 offset)
        {
            TextInfoOffset = offset;
        }
        public void UpdateOffsetOrientation()
        {
            float lineSpacing = _textBaseHeight * _textLineSpacingFactor;

            bool showElevation = IsElevationVisible;
            bool showDescription = IsDescriptionVisible;

            int visibleCount = 1 + (showElevation ? 1 : 0) + (showDescription ? 1 : 0);

            int pointNumberIndex = 0;
            int elevationIndex = 1;
            int descriptionIndex = 1 + (showElevation ? 1 : 0);

            float GetYOffset(int index)
            {
                if (!IsLabelBelow)
                    return (visibleCount - 1 - index) * lineSpacing;

                return -index * lineSpacing;
            }

            float translation = 0;

            if (IsLabelBelow)
            {
                Rect bottomLabelBounds =
                    showDescription ? DescriptionBounds : showElevation ? ElevationBounds : PointNumberBounds;

                if (!bottomLabelBounds.IsEmpty)
                {
                    translation = (float)(bottomLabelBounds.Height / PointGroup.PointScale);
                }
            }

            float yTranslation = IsLabelBelow ? -translation : 0;

            PointNumberOffset = new Vector2(
                IsLabelLeft ? -(float)PointNumberBounds.Width : 0, GetYOffset(pointNumberIndex) + yTranslation);
            ElevationOffset = new Vector2(
                IsLabelLeft ? -(float)ElevationBounds.Width : 0, showElevation ? GetYOffset(elevationIndex) + yTranslation : 0);

            DescriptionOffset = new Vector2(
                IsLabelLeft ? -(float)DescriptionBounds.Width : 0, showDescription ? GetYOffset(descriptionIndex) + yTranslation : 0);
        }

        public void ResetTextLocations()
        {
            HasLeaderLine = false;

            PointNumberOffset = Vector2.Zero;
            ElevationOffset = Vector2.Zero;
            DescriptionOffset = Vector2.Zero;

            UpdateOffsetOrientation();
        }

        protected override void OnPropertyChanged(string propertyName)
        {
            base.OnPropertyChanged(propertyName);

            //if (propertyName == nameof(Easting) || propertyName == nameof(Northing))
            //{
            //    TextInfoBasePosition = new(Position.X.ToFloat() + TextInfoBaseOffset_X, Position.Y.ToFloat());
            //}
        }

        private static double DistanceToCogoPointViaAtlas(CogoPoint point, Point p, MsdfAtlas atlas)
        {
            double minDistance = double.MaxValue;

            minDistance = Math.Min(minDistance, MsdfHitTester.DistanceToGlyphs(atlas, point.PointNumberGlyphs, p));
            minDistance = Math.Min(minDistance, MsdfHitTester.DistanceToGlyphs(atlas, point.ElevationGlyphs, p));
            minDistance = Math.Min(minDistance, MsdfHitTester.DistanceToGlyphs(atlas, point.DescriptionGlyphs, p));

            if (point.HasLeaderLine)
            {
                double leaderDistance = MathHelpers.PointToLineDistance(
                    p, point.Position, Point.Add(point.Position, point.TextInfoOffset.ToVector()));

                minDistance = Math.Min(minDistance, leaderDistance);
            }

            // Toggle.
            if (point.IsSelected && point.ToggleBounds.Contains(p))
            {
                minDistance = 0.0;
            }

            if (point.EllipseBounds.Contains(p))
            {
                minDistance = 0.0;
            }

            return minDistance;
        }
        #endregion
    }

    public sealed class CogoPointDto
    {
        public int PointNumber { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Elevation { get; set; }
        public string Description { get; set; } = string.Empty;
        public string PointGroupName { get; set; } = string.Empty;

        public CogoPointDto() { }

        public CogoPointDto(CogoPoint cogoPoint)
        {
            if (cogoPoint is null) { return; }
            PointNumber = cogoPoint.PointNumber;
            X = (float)cogoPoint.Easting;
            Y = (float)cogoPoint.Northing;
            Z = (float)cogoPoint.Elevation;
            Elevation = (float)cogoPoint.Elevation;
            Description = cogoPoint.Description ?? string.Empty;
            PointGroupName = cogoPoint.PointGroup?.Name ?? string.Empty;
        }
    }
}
