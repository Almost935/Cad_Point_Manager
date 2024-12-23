using Cad_Point_Manager.Controls.D3DControl;
using netDxf;
using netDxf.Entities;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Vector3 = SharpDX.Vector3;
using Vector4 = SharpDX.Vector4;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class DrawingLine3D : DrawingSegment3D
    {
        private DrawingLine3D() { Type = DrawingObject3dType.DrawingLine3D; }

        public DrawingLine3D(Line line)
        {
            Type = DrawingObject3dType.DrawingLine3D;

            var r = line.Layer.Color.R / 255;
            var g = line.Layer.Color.R / 255;
            var b = line.Layer.Color.R / 255;
            var a = line.Layer.Color.R / 255;
            LayerColor = new(line.Layer.Color.R / 255.0f, line.Layer.Color.G / 255.0f, line.Layer.Color.B / 255.0f, 1);
            if (line.Color.IsByLayer) { Color = LayerColor; }
            else { Color = new(line.Color.R / 255.0f, line.Color.G / 255.0f, line.Color.B / 255.0f, 1); }

            if (LayerColor == new Vector4(1, 1, 1, 1)) { Color = new(0, 0, 0, 1); }
            if (Color == new Vector4(1, 1, 1, 1)) { Color = new(0, 0, 0, 1); }

            StartVertex = new(new Vector3((float)line.StartPoint.X, (float)line.StartPoint.Y, 0), Color);
            EndVertex = new(new Vector3((float)line.EndPoint.X, (float)line.EndPoint.Y, 0), Color);
        }
    }
}
