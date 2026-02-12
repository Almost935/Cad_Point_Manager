using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Services.LayoutExporting;
using netDxf.Entities;
using PdfSharpCore.Drawing;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System.Windows;
using Vector3 = SharpDX.Vector3;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingLine : DrawingSegment
    {
        #region Properties
        public Vector3 MidPoint { get; set; }
        #endregion

        #region Constructors
        //private DrawingLine() { Type = DrawingObject3dType.DrawingLine; }

        public DrawingLine(Line line, ObjectLayer layer, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingLine;
            Layer = layer;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock3D = block;
            EntityObject = line;
            ColorByLayer = EntityObject.Color.IsByLayer;

            UpdateData();
            UpdateColor();
        }
        #endregion

        #region Methods
        public override void UpdateData()
        {
            if (EntityObject is not null && EntityObject is Line line)
            {
                Start = new Vector3((float)line.StartPoint.X, (float)line.StartPoint.Y, 0);
                End = new Vector3((float)line.EndPoint.X, (float)line.EndPoint.Y, 0);
                Length = Vector3.Distance(Start, End);
                MidPoint = (Start + End) / 2;

                UpdateBounds();
            }
            else
            {
                throw new ArgumentException("entity must be of type Line");
            }
        }
        public override void DrawToD2dDeviceContext(DeviceContext1 deviceContext, Factory2 factory, Brush brush, float thickness, StrokeStyle1 strokeStyle)
        {
            deviceContext.DrawLine(new RawVector2(Start.X, Start.Y), new RawVector2(End.X, End.Y), brush, thickness, strokeStyle);
        }
        public override void DrawToPdf(
           XGraphics gfx,
           System.Windows.Media.Matrix worldToPdf,
           XPen pen)
        {
            var p0 = PdfTransform.WorldToPdf(Start.ToVector3(), worldToPdf);
            var p1 = PdfTransform.WorldToPdf(End.ToVector3(), worldToPdf);
            gfx.DrawLine(pen, p0, p1);
        }

        public override void UpdateVertices(uint layerId, uint objectId)
        {
            LineVertex startVertex = new(Start, layerId, objectId);
            LineVertex endVertex = new(End, layerId, objectId);
            Vertices = new[] { startVertex, endVertex };
        }

        public override void UpdateBounds()
        {
            Bounds = Rect.Empty;
            Bounds = Rect.Union(Bounds, new System.Windows.Point(Start.X, Start.Y));
            Bounds = Rect.Union(Bounds, new System.Windows.Point(End.X, End.Y));
        }

        public override double DistanceToPoint(System.Windows.Point point)
        {
            return (float)MathHelpers.PointToLineDistance(point, new System.Windows.Point(Start.X, Start.Y), new System.Windows.Point(End.X, End.Y));
        }

        public override bool GeometryInRect(Rect rect)
        {
            if (rect.Contains(Start.ToPoint()) && rect.Contains(End.ToPoint()))
            {
                return true;
            }
            return false;
        }
        #endregion
    }
}
