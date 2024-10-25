using Cad_Point_Manager.Models.DrawingObjects;
using Direct2DDxfViewer.Direct2DControl;
using netDxf.Entities;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ellipse = netDxf.Entities.Ellipse;

namespace Cad_Point_Manager.Helpers
{
    public static class DxfHelper
    {
        public static DrawingObject LoadObject(EntityObject entity, ObjectLayer layer, Factory1 factory, DeviceContext1 deviceContext, ResourceCache resCache)
        {
            return entity switch
            {
                Line line => new DrawingLine(line, factory, deviceContext, resCache, layer),
                Arc arc => new DrawingArc(arc, factory, deviceContext, resCache, layer),
                Circle circle => new DrawingCircle(circle, factory, deviceContext, resCache, layer),
                Ellipse ellipse => new DrawingEllipse(ellipse, factory, deviceContext, resCache, layer),
                Polyline2D polyline2D => new DrawingPolyline2D(polyline2D, factory, deviceContext, resCache, layer),
                Polyline3D polyline3D => new DrawingPolyline3D(polyline3D, factory, deviceContext, resCache, layer),
                MText mText => new DrawingMtext(mText, factory, deviceContext, resCache, layer, resCache.FactoryWrite),
                Insert block => new DrawingBlock(block, factory, deviceContext, resCache, layer),
                _ => null,
            };
        }


    }
}
