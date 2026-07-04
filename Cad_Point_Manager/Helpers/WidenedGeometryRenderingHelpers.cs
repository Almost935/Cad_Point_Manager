using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Models.DrawingObjects;
using DocumentFormat.OpenXml.Bibliography;
using netDxf.Entities;
using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Helpers
{
    public class WidenedGeometryRenderingHelpers
    {
        public static List<Vector2> GetWidenedPolylineVertices(ResCache resCache, DrawingWidePolyline widePolyLine, float width, out List<Vector2> widenedVertices)
        {
            widenedVertices = [];

            using var geometry = GetWidenedPolylineGeometry(resCache, widePolyLine, width);

            using (var sink = new CustomTessellationSink())
            {
                geometry.Tessellate(0.0001f, sink);
                widenedVertices.AddRange(sink.Vertices);
            }

            return widenedVertices;
        }
        private static PathGeometry GetWidenedPolylineGeometry(ResCache resCache, DrawingWidePolyline widePolyLine, float width)
        {
            PathGeometry source = new(resCache.D2dFactory);

            using (GeometrySink sink = source.Open())
            {
                sink.BeginFigure(widePolyLine.DrawingSegments.First().Start.ToRawVector2(), FigureBegin.Hollow);

                foreach (DrawingSegment segment in widePolyLine.DrawingSegments)
                {
                    switch (segment)
                    {
                        case DrawingLine drawingLine:
                            sink.AddLine(drawingLine.End.ToRawVector2());
                            break;

                        case DrawingArc drawingArc:
                            for (int i = 1; i < drawingArc.Vertices.Length; i += 2)
                            {
                                sink.AddLine(drawingArc.Vertices[i].Position.ToRawVector2());
                            }
                            break;

                        default:
                            break;
                    }
                }

                if (widePolyLine.IsClosed) { sink.EndFigure(FigureEnd.Closed); }
                else { sink.EndFigure(FigureEnd.Open); }

                sink.Close();
            }

            PathGeometry widened = new(resCache.D2dFactory);

            using (var sink = widened.Open())
            {
                source.Widen(
                    width,
                    sink);

                sink.Close();
            }

            source.Dispose();

            return widened;
        }
    }
}
