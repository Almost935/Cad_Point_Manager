using Cad_Point_Manager.Models.PointRendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.Filtering
{
    public sealed class NorthingRangeFilter : PointFilterBase
    {
        public double? Min { get; }
        public double? Max { get; }

        public NorthingRangeFilter(double? min, double? max) { Min = min; Max = max; }

        public override string DisplayText =>
            $"N {(Min?.ToString("N3") ?? "…")} to {(Max?.ToString("N3") ?? "…")}";

        public override bool IsMatch(CogoPoint p)
        {
            if (Min.HasValue && p.Northing < Min.Value) { return false; }
            if (Max.HasValue && p.Northing > Max.Value) { return false; }
            return true;
        }
    }
}
