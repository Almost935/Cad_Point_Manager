using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Cad_Point_Manager.Models.PointRendering
{
    public sealed class CogoPointBoundsSnapshot
    {
        public Rect Name { get; init; }
        public Rect Elevation { get; init; }
        public Rect Description { get; init; }
        public Rect Ellipse { get; init; }
        public Rect Union => Rect.Union(Rect.Union(Name, Elevation), Rect.Union(Description, Ellipse));
    }
}
