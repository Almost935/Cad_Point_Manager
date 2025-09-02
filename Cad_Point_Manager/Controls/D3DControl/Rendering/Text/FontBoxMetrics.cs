using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Text
{
    public readonly struct FontBoxMetrics
    {
        public readonly int UnitsPerEm;
        public readonly int AscentDU;
        public readonly int DescentDU;
        public readonly int LineGapDU;

        public FontBoxMetrics(SharpDX.DirectWrite.FontFace face)
        {
            var m = face.Metrics;
            UnitsPerEm = m.DesignUnitsPerEm;
            AscentDU = m.Ascent;
            DescentDU = m.Descent;         // positive DU magnitude
            LineGapDU = m.LineGap;
        }

        public float AscentWorld(float duToWorld) => AscentDU * duToWorld;
        public float DescentWorld(float duToWorld) => -DescentDU * duToWorld; // Y up in world
    }

}
