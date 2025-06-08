using SharpDX;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Cad_Point_Manager.Helpers
{
    public class VectorEqualityComparer : IEqualityComparer<Vector>
    {
        private readonly float _epsilon;

        public VectorEqualityComparer(float epsilon = 1e-5f)
        {
            _epsilon = epsilon;
        }

        public bool Equals(Vector v1, Vector v2)
        {
            return Math.Abs(v1.X - v2.X) < _epsilon && Math.Abs(v1.Y - v2.Y) < _epsilon;
        }

        public int GetHashCode(Vector v)
        {
            // Round components to the nearest multiple of epsilon to reduce hash collisions
            int xHash = (int)(Math.Round(v.X / _epsilon));
            int yHash = (int)(Math.Round(v.Y / _epsilon));

            return xHash * 397 ^ yHash;
        }
    }
}
