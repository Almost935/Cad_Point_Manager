using Cad_Point_Manager.Models.DrawingObjects3D;
using netDxf;
using netDxf.Entities;
using netDxf.Header;
using System.Windows;
using Vector3 = netDxf.Vector3;

namespace Cad_Point_Manager.Helpers
{
    public static class DxfHelpers
    {
        public static Rect GetExtentsFromHeader(DxfDocument doc)
        {
            if (doc == null) return Rect.Empty;

            if (doc.DrawingVariables.TryGetCustomVariable("$EXTMIN", out HeaderVariable extMinHeaderVariable) &&
                doc.DrawingVariables.TryGetCustomVariable("$EXTMAX", out HeaderVariable extMaxHeaderVariable))
            {
                Vector3 extMin = (Vector3)extMinHeaderVariable.Value;
                Vector3 extMax = (Vector3)extMaxHeaderVariable.Value;

                return new Rect(extMin.X, extMin.Y, extMax.X - extMin.X, extMax.Y - extMin.Y);
            }

            return Rect.Empty;
        }

        public static Rect GetBoundsFromHeader(DxfDocument doc)
        {
            if (doc == null) { return Rect.Empty; }

            if (doc.DrawingVariables.TryGetCustomVariable("$EXTMIN", out HeaderVariable extMinHeaderVariable) &&
                doc.DrawingVariables.TryGetCustomVariable("$EXTMAX", out HeaderVariable extMaxHeaderVariable))
            {
                Vector3 extMin = (Vector3)extMinHeaderVariable.Value;
                Vector3 extMax = (Vector3)extMaxHeaderVariable.Value;

                return new Rect((float)extMin.X, (float)extMin.Y, (float)(extMax.X - extMin.X), (float)(extMax.Y - extMin.Y));
            }

            return Rect.Empty;
        }

        // DrawingObject3D getters
        public static DrawingObject3D GetDrawingObject3D(EntityObject e, ObjectLayer3D layer)
        {
            return e switch
            {
                Line line => new DrawingLine3D(line, layer),
                Arc arc => new DrawingArc3D(arc, layer),
                Polyline2D polyline2D => new DrawingPolyline3D(polyline2D, layer),
                Polyline3D polyline3D => new DrawingPolyline3D(polyline3D, layer),
                Circle circle => new DrawingCircle3D(circle, layer),
                Insert block => new DrawingBlock3D(block, layer),
                MText mtext => new DrawingMtext3D(mtext, layer),
                Text text => new DrawingSText3D(text, layer),
                Spline spline => new DrawingSpline3D(spline, layer),
                _ => null,
            };
        }
        public static DrawingSegment3D GetDrawingSegment3D(EntityObject e, ObjectLayer3D layer)
        {
            return e switch
            {
                Line line => new DrawingLine3D(line, layer),
                Arc arc => new DrawingArc3D(arc, layer),
                Circle circle => new DrawingCircle3D(circle, layer),
                _ => null,
            };
        }

        public static (byte r, byte g, byte b, byte a) GetRGBAColor(EntityObject entity)
        {
            byte r, g, b, a;
            if (entity.Color.IsByLayer)
            {
                if (entity.Layer.Color.R == 255 && entity.Layer.Color.G == 255 && entity.Layer.Color.B == 255)
                {
                    r = g = b = 0; a = 255;
                }
                else
                {
                    r = entity.Layer.Color.R; g = entity.Layer.Color.G; b = entity.Layer.Color.B; a = 255;
                }
            }
            else
            {
                if (entity.Color.R == 255 && entity.Color.G == 255 && entity.Color.B == 255)
                {
                    r = g = b = 0; a = 255;
                }
                else
                {
                    r = entity.Color.R; g = entity.Color.G; b = entity.Color.B; a = 255;
                }
            }

            return (r, g, b, a);
        }

        public static (byte r, byte g, byte b, byte a) GetRGBAColor(netDxf.Tables.Layer layer)
        {
            byte r, g, b, a;

            if (layer.Color.R == 255 && layer.Color.G == 255 && layer.Color.B == 255)
            {
                r = g = b = 0; a = 255;
            }
            else
            {
                r = layer.Color.R; g = layer.Color.G; b = layer.Color.B; a = 255;
            }


            return (r, g, b, a);
        }
    }
}