using Cad_Point_Manager.Models.DrawingObjects3D;
using Cad_Point_Manager.Models.HitTesting;
using Cad_Point_Manager.Models.PointRendering;
using System.Windows;

namespace Cad_Point_Manager.Helpers
{
    public static class HitTestingHelpers
    {
        public static bool TryGetNextHitTestablePoint(int currentIndex, List<(double distance, HitTestablePoint hitTestablePoint)> pointTups, out (double distance, HitTestablePoint hitTestablePoint) hitTestablePointTup)
        {
            hitTestablePointTup = default;
            if (currentIndex >= pointTups.Count) { return false; }

            hitTestablePointTup = pointTups[currentIndex];

            if (hitTestablePointTup.hitTestablePoint is null) { return false; }

            return true;
        }

        public static bool TryGetNextDrawingGeometry(int currentIndex, List<(double distance, DrawingGeometry3D geometry)> geometryTups, out (double distance, DrawingGeometry3D geometry) geometryTup)
        {
            geometryTup = default;
            if (currentIndex > geometryTups.Count) { return false; }

            geometryTup = geometryTups[currentIndex];

            if (geometryTup.geometry is null)
            {
                return false;
            }

            return true;
        }

        public static bool TryGetNextCogoPoint(int currentIndex, List<(double distance, CogoPoint point)> cogoPointsTups, out (double distance, CogoPoint point) cogoPointsTup)
        {
            cogoPointsTup = default;
            if (currentIndex > cogoPointsTups.Count) { return false; }

            cogoPointsTup = cogoPointsTups[currentIndex];

            if (cogoPointsTup.point is null)
            {
                return false;
            }

            return true;
        }

        public static bool TryGetNextHitTestableObject(int currentIndex, List<(double distance, HitTestableObject hitTestableObject)> hitTestableObjectTups, out (double distance, HitTestableObject hitTestableObject) hitTestableObjectTup)
        {
            hitTestableObjectTup = default;
            if (currentIndex > hitTestableObjectTups.Count) { return false; }

            hitTestableObjectTup = hitTestableObjectTups[currentIndex];

            if (hitTestableObjectTup.hitTestableObject is null)
            {
                return false;
            }

            return true;
        }
    }
}
