using Cad_Point_Manager.Models.PointRendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.Filtering
{
    public abstract class PointFilterBase : IPointFilter
    {
        public abstract string DisplayText { get; }
        public abstract bool IsMatch(CogoPoint p);
        public override string ToString() => DisplayText;
    }
}
