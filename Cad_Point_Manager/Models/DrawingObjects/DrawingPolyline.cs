using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Msdf;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.DrawingObjects.HelperClasses;
using Cad_Point_Manager.Services.Exporting;
using netDxf.Entities;
using PdfSharpCore.Drawing;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System.Diagnostics;
using System.Windows;

using Vector3 = SharpDX.Vector3;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingPolyline : DrawingGeometry
    {
        #region Properties
        public double Length { get; set; }
        public bool IsClosed { get; set; }
        public List<DrawingSegment> DrawingSegments { get; set; } = [];
        public int NumberOfSegments => DrawingSegments.Count;
        #endregion

        #region Constructors
        public DrawingPolyline(Polyline2D polyline2D, ObjectLayer layer, Vector4 objectColor,
            ColorType colorType, LineType lineType, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingPolyline;
            Layer = layer;
            ObjectColor = objectColor;
            ColorType = colorType;
            LineType = lineType;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock = block;
            EntityObject = polyline2D;

            UpdateColor();
            UpdateData();
        }
        public DrawingPolyline(Polyline3D polyline3D, ObjectLayer layer, Vector4 objectColor,
            ColorType colorType, LineType lineType, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingPolyline;
            Layer = layer;
            ObjectColor = objectColor;
            ColorType = colorType;
            LineType = lineType;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock = block;
            EntityObject = polyline3D;

            UpdateColor();
            UpdateData();
        }
        #endregion

        #region Methods
        public override void UpdateData()
        {
            bool isDash = LineType.Name == "Dash";

            if (EntityObject is Polyline2D polyline2d)
            {
                IsClosed = polyline2d.IsClosed;
                Length = 0;
                foreach (var segment in DrawingSegments)
                {
                    Length += segment.Length;
                }
            }
            else if (EntityObject is Polyline3D polyline3d)
            {
                IsClosed = polyline3d.IsClosed;
                Length = 0;
                foreach (var segment in DrawingSegments)
                {
                    Length += segment.Length;
                }
            }
            else
            {
                throw new ArgumentException("entity must be of type Polyline2D or Polyline3D");
            }
        }

        public override void DrawToPdf(PdfRenderContext context, XPen pen)
        {
            var segments = DrawingSegments.ToArray();

            foreach (var segment in segments)
            {
                segment.DrawToPdf(context, pen);
            }
        }

        public override void UpdateBounds()
        {
            Bounds = Rect.Empty;

            foreach (var segment in DrawingSegments)
            {
                Bounds = Rect.Union(Bounds, segment.Bounds);
            }
        }

        public override double DistanceToPoint(Vector2d p, MsdfAtlas atlas = null)
        {
            double distance = double.MaxValue;

            foreach (var segment in DrawingSegments)
            {
                double potD = segment.DistanceToPoint(p);

                if (potD < distance)
                {
                    distance = potD;
                }
            }
            return distance;
        }

        public override bool GeometryInRect(Rect rect)
        {
            if (Bounds.IsEmpty || rect.IsEmpty)
            {
                return false;
            }

            foreach (var geometry in DrawingSegments)
            {
                if (!geometry.BoundsInRect(rect))
                {
                    break;
                }
            }

            return false;
        }


        public override void UpdateVertices(
            ResCache resCache, uint layerId, uint objectId, 
            uint lineTypeId, Vector2d renderOrigin)
        {
            DrawingSegments.Clear();
            LineInstances = [];

            if (EntityObject is Polyline2D polyline2D)
            {
                IsClosed = polyline2D.IsClosed;

                var vertices = polyline2D.Vertexes;

                if (vertices.Count == 0)
                    return;

                Start = vertices.First().Position.ToVector2d();
                End = vertices.Last().Position.ToVector2d();

                var entities = polyline2D.Explode();

                foreach (var e in entities)
                {
                    var colorType = DxfHelpers.GetColorType(e);
                    Vector4 color = DxfHelpers.GetEntityObjectColor(e, DrawingBlock?.DxfInsert);

                    var obj = DxfHelpers.GetDrawingSegment(
                        e, Layer, color, colorType, LineType, DrawingBlock);

                    if (obj is null)
                        continue;

                    obj.UpdateVertices(resCache, layerId, objectId, lineTypeId, renderOrigin);
                    DrawingSegments.Add(obj);
                }

                for (int i = 0; i < DrawingSegments.Count; i++)
                {
                    if (DrawingSegments[i] is not DrawingArc arc)
                        continue;

                    if (i >= vertices.Count)
                        break;

                    Vector2d vertexStart = vertices[i].Position.ToVector2d();
                    OrientArc(arc, vertexStart);
                }
            }
            else if (EntityObject is Polyline3D polyline3D)
            {
                IsClosed = polyline3D.IsClosed;

                var vertices = polyline3D.Vertexes;

                if (vertices.Count == 0)
                    return;

                Start = vertices.First().ToVector2d();
                End = vertices.Last().ToVector2d();

                var entities = polyline3D.Explode();

                foreach (var e in entities)
                {
                    var colorType = DxfHelpers.GetColorType(e);

                    Vector4 color = DxfHelpers.GetEntityObjectColor(e, DrawingBlock?.DxfInsert);
                    var obj = DxfHelpers.GetDrawingSegment(e, Layer, color, colorType, LineType, DrawingBlock);

                    if (obj is null)
                        continue;

                    obj.UpdateVertices(resCache, layerId, objectId, lineTypeId, renderOrigin);
                    DrawingSegments.Add(obj);
                }

                // Same orientation handling as Polyline2D.
                for (int i = 0; i < DrawingSegments.Count; i++)
                {
                    if (DrawingSegments[i] is not DrawingArc arc)
                        continue;

                    if (i >= vertices.Count)
                        break;

                    Vector2d vertexStart = vertices[i].ToVector2d();
                    OrientArc(arc, vertexStart);
                }
            }
            else
            {
                throw new ArgumentException("Entity must be of type Polyline2D or Polyline3D");
            }

            // Recalculate distances after all arc orientations
            // have been corrected.
            PrepareLineTypeDistances();

            // Combine the prepared geometry for Direct3D.
            LineInstances = DrawingSegments
                .SelectMany(s => s.LineInstances)
                .ToArray();

            Length = DrawingSegments.Sum(s => s.Length);

            UpdateBounds();

            // Local helper: orient an arc using world coordinates.
            void OrientArc(DrawingArc arc, Vector2d vertexStart)
            {
                if (arc.LineInstances is null || arc.LineInstances.Length == 0)
                {
                    return;
                }

                double startDistance = Vector2d.DistanceTo(arc.Start, vertexStart);
                double endDistance = Vector2d.DistanceTo(arc.End, vertexStart);

                if (endDistance >= startDistance)
                    return;

                // Reverse the instance sequence.
                Array.Reverse(arc.LineInstances);

                // Reverse the direction of every instance.
                for (int j = 0; j < arc.LineInstances.Length; j++)
                {
                    var instance = arc.LineInstances[j];

                    (instance.Start, instance.End) = (instance.End, instance.Start);

                    instance.StartDistance = 0;
                    arc.LineInstances[j] = instance;
                }
            }
        }


        private void PrepareLineTypeDistances()
        {
            foreach (var segment in DrawingSegments)
            {
                if (segment.LineInstances is null || segment.LineInstances.Length == 0)
                {
                    continue;
                }

                float distance = 0.0f;
                float parentLength = 0.0f;

                for (int i = 0; i < segment.LineInstances.Length; i++)
                {
                    var instance = segment.LineInstances[i];
                    parentLength += Vector2.Distance(instance.Start, instance.End);
                }

                for (int i = 0; i < segment.LineInstances.Length; i++)
                {
                    var instance = segment.LineInstances[i];
                    instance.StartDistance = distance;
                    instance.ParentSegmentLength = parentLength;

                    distance += Vector2.Distance(instance.Start, instance.End);

                    segment.LineInstances[i] = instance;
                }
            }
        }
        #endregion
    }
}
