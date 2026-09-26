using Cad_Point_Manager.Models.DrawingObjects;
using Cad_Point_Manager.Models.HitTesting;
using Cad_Point_Manager.Models.PointRendering;
using System.Diagnostics;

namespace Cad_Point_Manager.Helpers
{
    public static class HitTestingHelpers
    {
        public static T? GetCycledHit<T>(IReadOnlyList<T> hits, ref int index)
        {
            if (hits.Count == 0)
            {
                index = 0;
                return default;
            }

            index %= hits.Count;

            return hits[index];
        }
    }
}
