using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Cad_Point_Manager.Models.PointRendering
{
    public sealed class MsdfTextLayout
    {
        public Rect Bounds { get; set; } = Rect.Empty;
        public List<MsdfGlyphPlacement> Glyphs { get; } = [];
    }
}
