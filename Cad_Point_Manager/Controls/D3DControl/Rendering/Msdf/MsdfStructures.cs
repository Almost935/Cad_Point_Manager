using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Msdf
{
    public struct MsdfGlyphHitRegion
    {
        public Rect Bounds;
        public Vector2 UvMin;
        public Vector2 UvMax;
    }
}
