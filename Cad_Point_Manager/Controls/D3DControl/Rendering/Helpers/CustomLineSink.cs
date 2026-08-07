using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Helpers
{
    public class CustomLineSink : CallbackBase, SimplifiedGeometrySink
    {
        public List<Vector2> Vertices = [];

        private RawVector2 _currentPoint;

        public void BeginFigure(
            RawVector2 startPoint,
            FigureBegin figureBegin)
        {
            _currentPoint = startPoint;
        }

        public void AddLines(
            RawVector2[] points)
        {
            foreach (var point in points)
            {
                Vertices.Add(
                    new Vector2(
                        _currentPoint.X,
                        _currentPoint.Y));

                Vertices.Add(
                    new Vector2(
                        point.X,
                        point.Y));

                _currentPoint = point;
            }
        }

        public void AddBeziers(
            BezierSegment[] beziers)
        {
        }

        public void EndFigure(
            FigureEnd figureEnd)
        {
        }

        public void SetFillMode(
            FillMode fillMode)
        {
        }

        public void SetSegmentFlags(
            PathSegment vertexFlags)
        {
        }

        public void Close()
        {
        }

        public new void QueryInterface(
            ref Guid guid,
            out IntPtr comObject)
        {
            comObject = IntPtr.Zero;
        }

        public new int AddRef()
        {
            return 1;
        }

        public new int Release()
        {
            return 1;
        }
    }
}