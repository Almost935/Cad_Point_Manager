using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using netDxf;
using netDxf.Entities;
using netDxf.Header;
using SharpDX;
using SharpDX.Direct2D1;
using Cad_Point_Manager.Models.DrawingObjects;
using SharpDX.Mathematics.Interop;
using System.Windows;
using netDxf.Tables;
using System.Net;
using Cad_Point_Manager.Controls.D2DControl;

using PathGeometry = SharpDX.Direct2D1.PathGeometry;
using ArcSegment = SharpDX.Direct2D1.ArcSegment;
using SweepDirection = SharpDX.Direct2D1.SweepDirection;
using Geometry = SharpDX.Direct2D1.Geometry;
using Ellipse = SharpDX.Direct2D1.Ellipse;
using EllipseGeometry = SharpDX.Direct2D1.EllipseGeometry;
using Vector3 = netDxf.Vector3;
using Cad_Point_Manager.DrawingObjects;
using Cad_Point_Manager.Models;

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

        public static CadManager GetLayers(DxfDocument dxfDocument)
        {
            CadManager layerManager = new();

            foreach (var dxfLayer in dxfDocument.Layers)
            {
                layerManager.GetLayer(dxfLayer);
            }

            return layerManager;
        }

        public static int LoadEntityObject(EntityObject e, CadManager layerManager)
        {
            ObjectLayer layer = layerManager.GetLayer(e.Layer);
            DrawingObject drawingObject = e switch
            {
                Line line => new DrawingLine(line, layer),
                Arc arc => new DrawingArc(arc, layer),
                Polyline2D polyline2D => new DrawingPolyline2D(polyline2D, layer),
                Polyline3D polyline3D => new DrawingPolyline3D(polyline3D, layer),
                Circle circle => new DrawingCircle(circle, layer),
                Insert block => new DrawingBlock(block, layer),
                netDxf.Entities.Ellipse ellipse => new DrawingEllipse(ellipse, layer),
                MText mtext => new DrawingMtext(mtext, layer),
                _ => null
            };

            if (drawingObject != null)
            {
                layer.DrawingObjects.Add(drawingObject);
                return drawingObject.EntityCount;
            }

            return 0;
        }

        public static int LoadDrawingObjects(DxfDocument dxfDocument, CadManager layerManager, Factory1 factory,
            DeviceContext1 deviceContext, ResourceCache resCache)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            int count = dxfDocument.Entities.All.Sum(e => LoadEntityObject(e, layerManager));

            stopwatch.Stop();
            Debug.WriteLine($"LoadDrawingObjects: {stopwatch.ElapsedMilliseconds} ms");

            stopwatch.Restart();

            foreach (var layer in layerManager.Layers.Values)
            {
                foreach (var obj in layer.DrawingObjects)
                {
                    obj.UpdateGeometry();
                }
            }

            stopwatch.Stop();
            Debug.WriteLine($"Load Geometries: {stopwatch.ElapsedMilliseconds} ms");

            return count;
        }

        public static DrawingObject GetDrawingObject(EntityObject entity, ObjectLayer layer, DrawingBlock block = null)
        {
            return entity switch
            {
                Line line => new DrawingLine(line, layer, block),
                Arc arc => new DrawingArc(arc, layer, block),
                Polyline2D polyline2D => new DrawingPolyline2D(polyline2D, layer, block),
                Polyline3D polyline3D => new DrawingPolyline3D(polyline3D, layer, block),
                Circle circle => new DrawingCircle(circle, layer, block),
                netDxf.Entities.Ellipse ellipse => new DrawingEllipse(ellipse, layer, block),
                Insert insert => new DrawingBlock(insert, layer, block),
                MText mtext => new DrawingMtext(mtext, layer, block),
                _ => null
            };
        }

        public static DrawingSegment GetDrawingSegment(EntityObject entity, ObjectLayer layer, DrawingBlock block = null)
        {
            return entity switch
            {
                Line line => new DrawingLine(line, layer, block),
                Arc arc => new DrawingArc(arc, layer, block),
                _ => null
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