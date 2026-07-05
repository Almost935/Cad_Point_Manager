using Cad_Point_Manager.Models.PointRendering;

namespace Cad_Point_Manager.Models.Filtering
{
    public abstract class PointFilterBase : IPointFilter
    {
        public abstract string DisplayText { get; }
        public abstract bool IsMatch(CogoPoint p);
        public override string ToString() => DisplayText;
    }
}
