using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Helpers;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Models.DrawingObjects;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System.Diagnostics;

namespace Cad_Point_Manager.Helpers
{
    public class WidenedGeometryRenderingHelpers
    {
        #region Fields
        const float _tesselationFactor = 5f;
        #endregion

        #region Methods
        public static List<Vector2d> GetWidenedPolylineVertices(
            ResCache resCache, DrawingWidePolyline widePolyline, double width, Vector2d renderOrigin)
        {
            using var geometry = GetWidenedPolylineGeometry(
                resCache, widePolyline, width, _tesselationFactor, renderOrigin);

            return TesselatePolylineGeometry(resCache, geometry, _tesselationFactor);
        }
        private static List<Vector2d> TesselatePolylineGeometry(ResCache resCache, PathGeometry geometry, float tesselationFactor = 1)
        {
            List<Vector2d> vertices = [];

            using (var sink = new CustomTessellationSink())
            {
                geometry.Tessellate(1e-8f, sink);
                foreach (var vertex in sink.Vertices)
                {
                    vertices.Add(vertex.ToVector2d());
                }
            }

            double scaleFactor = 1.0 / tesselationFactor;
            var scale = new System.Windows.Media.Matrix(
                scaleFactor, 0,
                0, scaleFactor,
                0, 0);

            for (int i = 0; i < vertices.Count; i++)
            {
                var vertex = vertices[i];
                vertices[i] = Vector2d.TransformCoordinate(vertex, scale);
            }

            return vertices;
        }

        private static PathGeometry GetWidenedPolylineGeometry(
            ResCache resCache, DrawingWidePolyline widePolyline, double width, float tesselationFactor, Vector2d renderOrigin)
        {
            PathGeometry source = new(resCache.D2dFactory);

            RawVector2 ToRelative(double x, double y)
                => new((float)(x - renderOrigin.X), (float)(y - renderOrigin.Y));

            var vertices = widePolyline.Polyline2D.Vertexes;

            if (vertices.Count < 2)
            {
                source.Dispose();
                throw new ArgumentException("Polyline must contain at least two vertices.");
            }

            using (GeometrySink sink = source.Open())
            {
                var start = vertices[0].Position;

                sink.BeginFigure(ToRelative(start.X, start.Y), FigureBegin.Hollow);

                int segmentCount = Math.Min(
                    widePolyline.DrawingSegments.Count, widePolyline.IsClosed ? vertices.Count : vertices.Count - 1);

                for (int i = 0; i < segmentCount; i++)
                {
                    int next = (i + 1) % vertices.Count;
                    var endpoint = vertices[next].Position;
                    RawVector2 end = ToRelative(endpoint.X, endpoint.Y);
                    var segment = widePolyline.DrawingSegments[i];

                    switch (segment)
                    {
                        case DrawingLine:
                            sink.AddLine(end);
                            break;

                        case DrawingArc arc:
                            sink.AddArc(new ArcSegment
                            {
                                Point = end,
                                Size = new Size2F((float)arc.Radius, (float)arc.Radius),
                                SweepDirection = SweepDirection.CounterClockwise,
                                ArcSize = ArcSize.Small
                            });
                            break;
                    }
                }

                sink.EndFigure(widePolyline.IsClosed ? FigureEnd.Closed : FigureEnd.Open);

                sink.Close();
            }

            var matrix = Matrix3x2.Scaling(tesselationFactor, tesselationFactor);

            using TransformedGeometry transformed = new(resCache.D2dFactory, source, matrix);

            PathGeometry widened = new(resCache.D2dFactory);

            using (GeometrySink sink = widened.Open())
            {
                transformed.Widen((float)(width * tesselationFactor), sink);
                sink.Close();
            }

            source.Dispose();

            return widened;
        }

        #endregion
    }
}
