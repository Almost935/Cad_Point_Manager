using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.LffFontRendering
{
    public sealed class LffFont
    {
        public string Name { get; set; }
        public float LetterSpacing { get; set; }
        public float WordSpacing { get; set; }
        public float DesignHeight { get; set; }
        public Dictionary<char, LffGlyph> Glyphs { get; } = [];
    }
}
