using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Helpers;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Models.DrawingObjects;
using SharpDX;
using SharpDX.Direct2D1;
using System.Diagnostics;

namespace Cad_Point_Manager.Helpers
{
    public class WidenedGeometryRenderingHelpers
    {
        #region Fields
        const float _tesselationFactor = 5f;
        #endregion

        #region Methods
        public static List<Vector2> GetWidenedPolylineVertices(ResCache resCache, DrawingWidePolyline widePolyLine, float width, out List<Vector2> vertices)
        {
            vertices = [];

            using var geometry = GetWidenedPolylineGeometry(resCache, widePolyLine, width, _tesselationFactor);
            vertices = TesselatePolylineGeometry(resCache, geometry, _tesselationFactor);

            return vertices;
        }
        private static List<Vector2> TesselatePolylineGeometry(ResCache resCache, PathGeometry geometry, float tesselationFactor = 1)
        {
            List<Vector2> vertices = [];

            using (var sink = new CustomTessellationSink())
            {
                geometry.Tessellate(1e-8f, sink);
                vertices.AddRange(sink.Vertices);
            }

            var scale = Matrix.Scaling(1 / tesselationFactor, 1 / tesselationFactor, 1);
            for (int i = 0; i < vertices.Count; i++)
            {
                var vertex = vertices[i];
                vertices[i] = Vector2.TransformCoordinate(vertex, scale);
            }

            return vertices;
        }
        private static PathGeometry GetWidenedPolylineGeometry(ResCache resCache, DrawingWidePolyline widePolyLine, float width, float tesselationFactor)
        {
            PathGeometry source = new(resCache.D2dFactory);

            using (GeometrySink sink = source.Open())
            {
                var start = widePolyLine.DrawingSegments.First().Start;
                var vertices = widePolyLine.Polyline2D.Vertexes.Select(v => v.Position).ToList();
                sink.BeginFigure(start.ToRawVector2(), FigureBegin.Hollow);

                for (int i = 0; i < widePolyLine.DrawingSegments.Count; i++)
                {
                    var segment = widePolyLine.DrawingSegments[i];
                    var end = default(Vector2);

                    var test1 = i == widePolyLine.DrawingSegments.Count - 1;
                    var test2 = (i + 1) >= vertices.Count;

                    if (i == widePolyLine.DrawingSegments.Count - 1 &&
                        (i + 1) >= vertices.Count)
                    {
                        if (widePolyLine.IsClosed)
                        {
                            end = vertices[0].ToRawVector2();
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        end = vertices[i + 1].ToRawVector2();
                    }

                    switch (segment)
                    {
                        case DrawingLine drawingLine:

                            sink.AddLine(end);
                            break;

                        case DrawingArc drawingArc:

                            sink.AddArc(new ArcSegment
                            {
                                Point = end,
                                Size = new Size2F(drawingArc.Radius, drawingArc.Radius),
                                SweepDirection = SweepDirection.CounterClockwise,
                                ArcSize = ArcSize.Small
                            });

                            break;

                        default:
                            break;
                    }
                }

                if (widePolyLine.IsClosed) { sink.EndFigure(FigureEnd.Closed); }
                else { sink.EndFigure(FigureEnd.Open); }

                sink.Close();
            }

            var matrix = Matrix3x2.Scaling(tesselationFactor, tesselationFactor);
            using TransformedGeometry transformed = new(resCache.D2dFactory, source, matrix);

            PathGeometry widened = new(resCache.D2dFactory);

            using (var sink = widened.Open())
            {
                transformed.Widen(
                    width * tesselationFactor,
                    sink);

                sink.Close();
            }

            source.Dispose();

            return widened;
        }
        #endregion
    }
}
