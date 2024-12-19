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
    public class DrawingArc3D : DrawingSegment3D
    {
        public List<Vertex> IntermediateVertices { get; set; } = [];
        public bool IsLargeArc { get; set; }
        public float Radius { get; set; }
        public float StartAngle { get; set; }
        public float EndAngle { get; set; }
        public float Sweep { get; set; }

        private DrawingArc3D() { Type = DrawingObject3dType.DrawingLine3D; }

        public DrawingArc3D(Arc arc)
        {
            Type = DrawingObject3dType.DrawingArc3D;
            
            LayerColor = new(arc.Layer.Color.R / 255, arc.Layer.Color.G / 255, arc.Layer.Color.B / 255, 1);
            if (arc.Color == AciColor.ByLayer) { Color = LayerColor; }
            else { Color = new(arc.Color.R / 255, arc.Color.G / 255, arc.Color.B / 255, 1); }

            if (LayerColor == new Vector4(1, 1, 1, 1)) { Color = new(0, 0, 0, 1); }
            if (Color == new Vector4(1, 1, 1, 1)) { Color = new(0, 0, 0, 1); }

            var verteces = arc.ToPolyline2D(500).Vertexes;

            for (int i = 0; i < verteces.Count; i++)
            {
                if (i == verteces.Count - 1) { break; }

                Vertex s = new(
                    new Vector3((float)verteces[i].Position.X, (float)verteces[i].Position.Y, 0),
                    Color
                    );
                Vertex e = new(
                    new Vector3((float)verteces[i + 1].Position.X, (float)verteces[i + 1].Position.Y, 0),
                    Color
                    );

                IntermediateVertices.Add(s);
                IntermediateVertices.Add(e);
            }

            StartVertex = IntermediateVertices.First();
            EndVertex = IntermediateVertices.Last();

            Radius = (float)arc.Radius;
            StartAngle = (float)arc.StartAngle;
            EndAngle = (float)arc.EndAngle;
            Sweep = EndAngle - StartAngle;
            if (Sweep < 0) { Sweep += 360; }
            IsLargeArc = Sweep >= 180;
        }
    }
}
