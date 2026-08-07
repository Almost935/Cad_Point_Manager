using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Models.DrawingObjects;
using Cad_Point_Manager.Models.DrawingObjects.Dimensioning;
using Cad_Point_Manager.Models.DrawingObjects.HelperClasses;
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

        public static DrawingObject GetDrawingObject(EntityObject e, ObjectLayer layer, SharpDX.Vector4 color,
            ColorType colorType, LineType lineType, DrawingBlock ownerBlock = null, bool isPartOfDimension = false)
        {
            return e switch
            {
                Line line => new DrawingLine(
                    line, layer, color, colorType, lineType, ownerBlock is not null, ownerBlock),
                Arc arc => new DrawingArc(
                    arc, layer, color, colorType, lineType, ownerBlock is not null, ownerBlock),
                Polyline2D polyline2D => PolylineResolver(
                    polyline2D, layer, color, colorType, lineType, ownerBlock, isPartOfDimension),
                Polyline3D polyline3D => new DrawingPolyline(
                    polyline3D, layer, color, colorType, lineType, ownerBlock is not null, ownerBlock),
                Circle circle => new DrawingCircle(
                    circle, layer, color, colorType, lineType, ownerBlock is not null, ownerBlock),
                Insert block => new DrawingBlock(
                    block, layer, color, colorType, lineType, ownerBlock is not null, ownerBlock, isPartOfDimension: isPartOfDimension),
                MText mtext => new DrawingMtext(
                    mtext, layer, color, colorType, lineType, !isPartOfDimension,
                    TextRenderingHelpers.GetAttachmentPoint(mtext.AttachmentPoint),
                    Common.TextAlignment.Left, ownerBlock is not null, ownerBlock),
                Text text => new DrawingSText(
                    text, layer, color, colorType, lineType, ownerBlock is not null, ownerBlock),
                Spline spline => new DrawingSpline(
                    spline, layer, color, colorType, lineType, ownerBlock is not null, ownerBlock),
                Dimension dimension => new DrawingDimension(
                    dimension, layer, color, colorType, lineType, ownerBlock is not null, ownerBlock),
                Solid solid => new DrawingSolid(
                    solid, layer, color, colorType, lineType, ownerBlock is not null, ownerBlock),
                _ => null,
            };
        }
        public static DrawingSegment GetDrawingSegment(EntityObject e, ObjectLayer layer, SharpDX.Vector4 color,
            ColorType colorType, LineType lineType, DrawingBlock ownerBlock = null)
        {
            return e switch
            {
                Line line => new DrawingLine(line, layer, color, colorType, lineType, ownerBlock is not null, ownerBlock),
                Arc arc => new DrawingArc(arc, layer, color, colorType, lineType, ownerBlock is not null, ownerBlock),
                Circle circle => new DrawingCircle(circle, layer, color, colorType, lineType, ownerBlock is not null, ownerBlock),
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

        public static SharpDX.Vector4 GetEntityObjectColor(EntityObject entity, Insert owner = null)
        {
            SharpDX.Vector4 color = new(entity.Color.R / 255.0f, entity.Color.G / 255.0f, entity.Color.B / 255.0f, 1);

            if (entity.Color.IsByLayer)
            {
                color = new(entity.Layer.Color.R / 255.0f, entity.Layer.Color.G / 255.0f, entity.Layer.Color.B / 255.0f, 1);
            }
            else if (entity.Color.IsByBlock)
            {
                if (owner is not null)
                {
                    color = GetEntityObjectColor(owner, null);
                }
                else
                {
                    // If the block reference is not provided, default to black
                    color = new(0, 0, 0, 1);
                }
            }

            // if the color is white, set it to black
            if (entity.Color.R == 255 && entity.Color.G == 255 && entity.Color.B == 255)
            {
                color = new(0, 0, 0, 1);
            }

            return color;
        }

        public static SharpDX.Vector4 GetDrawingObjectColor(DrawingObject obj)
        {
            SharpDX.Vector4 color = new(obj.EntityObject.Color.R / 255.0f, obj.EntityObject.Color.G / 255.0f, obj.EntityObject.Color.B / 255.0f, 1);

            if (obj.ColorType == ColorType.ByLayer)
            {
                color = obj.Layer.Color;
            }
            else if (obj.ColorType == ColorType.ByBlock)
            {
                if (obj.DrawingBlock is not null)
                {
                    color = GetDrawingObjectColor(obj.DrawingBlock);
                }
                else
                {
                    color = new(0, 0, 0, 1);
                }
            }

            if (color.X == 1 && color.Y == 1 && color.Z == 1)
            {
                color = new(0, 0, 0, 1);
            }

            return color;
        }

        public static netDxf.Tables.Linetype GetLineType(EntityObject entity, DxfDocument doc, Insert owner = null)
        {
            if (entity.Linetype.IsByLayer)
            {
                return entity.Layer.Linetype;
            }
            else if (entity.Linetype.IsByBlock)
            {
                if (owner is not null)
                {
                    return GetLineType(owner, doc, owner);
                }
                else
                {
                    return doc.Linetypes["Continuous"];
                }
            }
            else
            {
                return entity.Linetype;
            }
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

        public static DrawingObject PolylineResolver(Polyline2D polyline2D, ObjectLayer layer, SharpDX.Vector4 color, 
            ColorType colorType, LineType lineType, DrawingBlock ownerBlock = null, bool isPartOfDimension = false)
        {
            if (polyline2D == null) { return null; }

            if (polyline2D.Vertexes.Any(v => v.StartWidth > 0 || v.EndWidth > 0))
            {
                var width = polyline2D.Vertexes.Max(v => Math.Max(v.StartWidth, v.EndWidth)).ToFloat();
                return new DrawingWidePolyline(
                    polyline2D, layer, color, colorType, lineType, width, isPartOfDimension, ownerBlock);
            }

            return new DrawingPolyline(
                polyline2D, layer, color, colorType, lineType, ownerBlock is not null, ownerBlock);
        }
    }
}