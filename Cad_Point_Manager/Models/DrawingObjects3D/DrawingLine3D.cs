using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using netDxf.Entities;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System.Windows;
using Vector3 = SharpDX.Vector3;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class DrawingLine3D : DrawingSegment3D
    {
        #region Properties
        public Vector3 MidPoint { get; set; }
        #endregion

        #region Constructors
        private DrawingLine3D() { Type = DrawingObject3dType.DrawingLine3D; }

        public DrawingLine3D(Line line, ObjectLayer3D layer, bool isPartOfBlock = false, DrawingBlock3D block = null)
        {
            Type = DrawingObject3dType.DrawingLine3D;
            Layer = layer;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock3D = block;
            EntityObject = line;

            UpdateColor();
            UpdateData(line);
        }
        #endregion

        #region Methods
        public override void UpdateData(EntityObject entity)
        {
            if (entity is Line line)
            {
                Start = new Vector3((float)line.StartPoint.X, (float)line.StartPoint.Y, 0);
                LineVertex startVertex = new(Start, Color);
                End = new Vector3((float)line.EndPoint.X, (float)line.EndPoint.Y, 0);
                LineVertex endVertex = new(End, Color);
                Vertices = new[] { startVertex, endVertex };
                Length = Vector3.Distance(Start, End); 
                MidPoint = (Start + End) / 2;

                UpdateBounds();
            }
            else
            {
                throw new ArgumentException("entity must be of type Line");
            }
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

        public override void DrawToD2dDeviceContext(DeviceContext1 deviceContext, Factory2 factory, Brush brush, float thickness, StrokeStyle1 strokeStyle)
        {
            deviceContext.DrawLine(new RawVector2(Start.X, Start.Y), new RawVector2(End.X, End.Y), brush, thickness, strokeStyle);
        }
        #endregion
    }
}
