using Cad_Point_Manager.Models.PointRendering;

namespace Cad_Point_Manager.Models.Filtering
{
    public sealed class PointGroupFilter : PointFilterBase
    {
        public PointGroup Group { get; }

        public PointGroupFilter(PointGroup group) => Group = group;

        public override string DisplayText => $"Group: {Group?.Name ?? "<none>"}";

        public override bool IsMatch(CogoPoint p)
            => Group == null ? true : ReferenceEquals(p.PointGroup, Group);
    }
}
