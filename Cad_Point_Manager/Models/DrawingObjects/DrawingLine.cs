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
using System.Windows;
using Vector3 = SharpDX.Vector3;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingLine : DrawingSegment
    {
        #region Properties
        public Vector2d MidPoint { get; set; }
        #endregion

        #region Constructors
        public DrawingLine(Line line, ObjectLayer layer, Vector4 objectColor, ColorType colorType,
            LineType lineType, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingLine;
            Layer = layer;
            ObjectColor = objectColor;
            ColorType = colorType;
            LineType = lineType;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock = block;
            EntityObject = line;

            UpdateColor();
            UpdateData();
        }
        #endregion

        #region Methods
        public override void UpdateData()
        {
            if (EntityObject is not null && EntityObject is Line line)
            {
                Start = line.StartPoint.ToVector2d();
                End = line.EndPoint.ToVector2d();
                Length = Vector2d.DistanceTo(Start, End);
                MidPoint = Vector2d.Average(Start, End);

                UpdateBounds();
            }
            else
            {
                throw new ArgumentException("entity must be of type Line");
            }
        }
        public override void DrawToPdf(
            XGraphics gfx, System.Windows.Media.Matrix worldToPdf, XPen pen)
        {
            var p0 = PdfTransform.WorldToPdf(Start, worldToPdf);
            var p1 = PdfTransform.WorldToPdf(End, worldToPdf);
            gfx.DrawLine(pen, p0, p1);
        }

        public override void UpdateVertices(
            ResCache resCache, uint layerId, uint objectId, uint lineTypeId, Vector2d renderOrigin)
        {
            LineInstances = new[]
            {
                new LineInstance(
                    Start.ToGpuRelative(renderOrigin),
                    End.ToGpuRelative(renderOrigin),
                    layerId,
                    objectId,
                    0,
                    (uint)LineInstanceFlags.None,
                    (float)Length)
            };
        }

        public override void UpdateBounds()
        {
            Bounds = Rect.Empty;
            Bounds = Rect.Union(Bounds, new System.Windows.Point(Start.X, Start.Y));
            Bounds = Rect.Union(Bounds, new System.Windows.Point(End.X, End.Y));
            Bounds.Inflate(
                Bounds.Width * GlobalHelperProperties.GeometryBoundsInflationFactor,
                Bounds.Height * GlobalHelperProperties.GeometryBoundsInflationFactor);
        }

        public override double DistanceToPoint(Vector2d point, MsdfAtlas atlas = null)
        {
            return (float)MathHelpers.PointToLineDistance(point, Start, End);
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
