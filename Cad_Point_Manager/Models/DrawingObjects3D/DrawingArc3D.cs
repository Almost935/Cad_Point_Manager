using Cad_Point_Manager.Controls.D3DControl;
using netDxf;
using netDxf.Entities;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Vector3 = SharpDX.Vector3;
using Vector4 = SharpDX.Vector4;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class DrawingArc3D : DrawingCurve3D
    {
        #region Properties
        public bool IsLargeArc { get; set; }
        #endregion

        #region Constructor
        private DrawingArc3D() { Type = DrawingObject3dType.DrawingLine3D; }

        public DrawingArc3D(Arc arc, ObjectLayer3D layer, bool isPartOfBlock = false, DrawingBlock3D block = null)
        {
            Type = DrawingObject3dType.DrawingArc3D;
            Layer = layer;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock3D = block;
            EntityObject = arc;

            UpdateColor();
            UpdateData(arc);
            UpdateVertices();

            //var verteces = arc.ToPolyline2D(500).Vertexes;

            //for (int i = 0; i < verteces.Count; i++)
            //{
            //    if (i == verteces.Count - 1) { break; }

            //    Vertex s = new(
            //        new Vector3((float)verteces[i].Position.X, (float)verteces[i].Position.Y, 0),
            //        Color
            //        );
            //    Vertex e = new(
            //        new Vector3((float)verteces[i + 1].Position.X, (float)verteces[i + 1].Position.Y, 0),
            //        Color
            //        );

            //    Vertices.Add(s);
            //    Vertices.Add(e);
            //}

            //StartVertex = Vertices.First();
            //EndVertex = Vertices.Last();
        }
        #endregion

        #region Methods
        public override void UpdateData(EntityObject entity)
        {
            if (entity is Arc arc)
            {
                Radius = (float)arc.Radius;
                StartAngle = (float)arc.StartAngle;
                EndAngle = (float)arc.EndAngle;
                Center = new Vector3((float)arc.Center.X, (float)arc.Center.Y, (float)arc.Center.Z);
                
                Sweep = EndAngle - StartAngle;
                if (Sweep < 0) { Sweep += 360; }
                IsLargeArc = Sweep >= 180;

                Length = (float)((Sweep / 360) * (2 * Math.PI * Radius));
            }
            else
            {
                throw new ArgumentException("entity must be of type Arc");
            }
        }

        public override void UpdateVertices()
        {
            Vertices.Clear();

            NumberOfSegments = (int)Math.Ceiling(Math.Abs(Sweep) / ToleranceAngle);

            for (int i = 0; i <= NumberOfSegments; i++)
            {
                double theta = StartAngle + i * Sweep / NumberOfSegments;
                float x = Center.X + (float)(Radius * Math.Cos(theta));
                float y = Center.Y + (float)(Radius * Math.Sin(theta));

                Vertices.Add();
            }
        }
        #endregion
    }
}
