using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Cad_Point_Manager.Models.LffFontRendering
{
    public sealed class LffGlyph
    {
        public char Character { get; init; }

        public List<LffStroke> Strokes { get; } = [];

        public RectangleF Bounds { get; set; }

        public float AdvanceWidth { get; set; }
    }
}
