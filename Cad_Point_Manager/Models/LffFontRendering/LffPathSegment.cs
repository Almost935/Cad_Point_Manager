using Cad_Point_Manager.Common;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.LffFontRendering
{
    public class LffPathSegment
    {
        public Vector2d Start { get; init; }

        public Vector2d End { get; init; }

        /// <summary>
        /// 0 = straight line
        /// Non-zero = bulge
        /// </summary>
        public double Bulge { get; init; }

    }
}
