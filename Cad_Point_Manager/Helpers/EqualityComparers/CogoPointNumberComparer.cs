using Cad_Point_Manager.Models.PointRendering;

namespace Cad_Point_Manager.Helpers.EqualityComparers
{
    public sealed class CogoPointNumberComparer : IEqualityComparer<CogoPoint>
    {
        public bool Equals(CogoPoint x, CogoPoint y)
            => ReferenceEquals(x, y) || (x is not null && y is not null && x.PointNumber == y.PointNumber);

        public int GetHashCode(CogoPoint obj) => obj?.PointNumber ?? 0;
    }
}
