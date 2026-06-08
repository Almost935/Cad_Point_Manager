using Cad_Point_Manager.Models.DrawingObjects;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.DxfImport
{
    public class ParsedMLeader
    {
        public string Text = string.Empty;

        public Vector3 TextLocation;

        public float TextHeight;

        public List<List<Vector3>> LeaderLines = [];

        public float ArrowSize;

        public string LayerName = string.Empty;

        public ColorType ColorType;

        public Vector4 Color;
    }
}
