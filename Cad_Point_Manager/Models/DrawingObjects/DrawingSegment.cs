using Cad_Point_Manager.Models.SerializableObjects;
using SharpDX.Mathematics.Interop;


namespace Cad_Point_Manager.Models.DrawingObjects
{
    public abstract class DrawingSegment : DrawingObject
    {
        #region Fields
        private bool _disposed = false;

        private RawVector2 _startPoint;
        private RawVector2 _endPoint;
        #endregion

        #region Properties
        public RawVector2 StartPoint
        {
            get { return _startPoint; }
            set
            {
                _startPoint = value;
                OnPropertyChanged(nameof(StartPoint));
            }
        }
        public RawVector2 EndPoint
        {
            get { return _endPoint; }
            set
            {
                _endPoint = value;
                OnPropertyChanged(nameof(EndPoint));
            }
        }

        public bool IsPartOfPolyline { get; set; }
        public DrawingPolyline DrawingPolyline { get; set; }
        #endregion

        #region Methods
        public abstract DrawingSegmentData GetDrawingSegmentData();
        #endregion
    }
    public abstract class DrawingSegmentData : DrawingObjectData
    {
        public SerializablePoint StartPoint { get; set; }
        public SerializablePoint EndPoint { get; set; }
        public bool IsPartOfPolyline { get; set; }
        public DrawingPolylineData DrawingPolylineData { get; set; }

        public abstract DrawingSegment CreateDrawingSegment(ObjectLayer layer, DrawingBlock block = null, DrawingPolyline pline = null);
    }
}
