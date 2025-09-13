using System.Windows;

namespace Cad_Point_Manager.Helpers.EqualityComparers
{
    public class PointEqualityComparer : IEqualityComparer<Point>
    {
        private readonly float _epsilon;

        public PointEqualityComparer(float epsilon = 1e-5f)
        {
            _epsilon = epsilon;
        }

        public bool Equals(Point v1, Point v2)
        {
            return Math.Abs(v1.X - v2.X) < _epsilon && Math.Abs(v1.Y - v2.Y) < _epsilon;
        }

        public int GetHashCode(Point v)
        {
            // Round components to the nearest multiple of epsilon to reduce hash collisions
            int xHash = (int)Math.Round(v.X / _epsilon);
            int yHash = (int)Math.Round(v.Y / _epsilon);

            return xHash * 397 ^ yHash;
        }
    }
}
