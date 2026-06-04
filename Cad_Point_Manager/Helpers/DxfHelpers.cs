using Cad_Point_Manager.Models.DrawingObjects;
using Cad_Point_Manager.Models.DrawingObjects.Dimensioning;
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
        public static DrawingObject GetDrawingObject(EntityObject e, ObjectLayer layer)
        {
            return e switch
            {
                Line line => new DrawingLine(line, layer, GetObjectColor(line), GetColorType(line)),
                Arc arc => new DrawingArc(arc, layer, GetObjectColor(arc), GetColorType(arc)),
                Polyline2D polyline2D => new DrawingPolyline(polyline2D, layer, GetObjectColor(polyline2D), GetColorType(polyline2D)),
                Polyline3D polyline3D => new DrawingPolyline(polyline3D, layer, GetObjectColor(polyline3D), GetColorType(polyline3D)),
                Circle circle => new DrawingCircle(circle, layer, GetObjectColor(circle), GetColorType(circle)),
                Insert block => new DrawingBlock(block, layer, GetObjectColor(block), GetColorType(block)),
                MText mtext => new DrawingMtext(mtext, layer, GetObjectColor(mtext), GetColorType(mtext)),
                Text text => new DrawingSText(text, layer, GetObjectColor(text), GetColorType(text)),
                Spline spline => new DrawingSpline(spline, layer, GetObjectColor(spline), GetColorType(spline)),
                AlignedDimension alignedDimension => new DrawingAlignedDimension(alignedDimension, layer, GetObjectColor(alignedDimension), GetColorType(alignedDimension)),
                LinearDimension linearDimension => new DrawingLinearDimension(linearDimension, layer, GetObjectColor(linearDimension), GetColorType(linearDimension)),
                _ => null,
            };
        }
        public static DrawingSegment GetDrawingSegment3D(EntityObject e, ObjectLayer layer)
        {
            return e switch
            {
                Line line => new DrawingLine(line, layer, GetObjectColor(line), GetColorType(line)),
                Arc arc => new DrawingArc(arc, layer, GetObjectColor(arc), GetColorType(arc)),
                Circle circle => new DrawingCircle(circle, layer, GetObjectColor(circle), GetColorType(circle)),
                _ => null,
            };
        }

        public static ColorType GetColorType(EntityObject entity)
        {
            if (entity.Color.IsByLayer)
            {
                return ColorType.ByLayer;
            }
            else if (entity.Color.IsByBlock)
            {
                return ColorType.ByBlock;
            }
            else
            {
                return ColorType.ByObject;
            }
        }

        public static SharpDX.Vector4 GetObjectColor(EntityObject entity)
        {
           return new(entity.Color.R / 255.0f, entity.Color.G / 255.0f, entity.Color.B / 255.0f, 1);
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