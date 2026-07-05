using Cad_Point_Manager.Models.PointRendering;

namespace Cad_Point_Manager.Models.Filtering
{
    public sealed class PointNumberRangeFilter : PointFilterBase
    {
        public int? Min { get; }
        public int? Max { get; }

        public PointNumberRangeFilter(int? min, int? max)
        {
            Min = min;
            Max = max;
        }

        public override string DisplayText =>
            $"Point # {(Min.HasValue ? Min.Value.ToString() : "…")} to {(Max.HasValue ? Max.Value.ToString() : "…")}";

        public override bool IsMatch(CogoPoint p)
        {
            if (Min.HasValue && p.PointNumber < Min.Value) return false;
            if (Max.HasValue && p.PointNumber > Max.Value) return false;
            return true;
        }
    }
}
