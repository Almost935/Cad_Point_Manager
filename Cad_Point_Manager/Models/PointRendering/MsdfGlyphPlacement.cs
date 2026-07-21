using Cad_Point_Manager.Controls.D3DControl.Rendering.Msdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Cad_Point_Manager.Models.PointRendering
{
    public sealed class MsdfGlyphPlacement
    {
        public required MsdfGlyph Glyph { get; init; }

        /// <summary>
        /// Horizontal pen position (em units)
        /// </summary>
        public required float PenX { get; init; }

        /// <summary>
        /// Bounds of this glyph in world coordinates.
        /// </summary>
        public required Rect Bounds { get; init; }
    }
}
