using System.Windows.Input;

namespace Cad_Point_Manager.Helpers
{
    public static class AppCursors
    {
        private static Cursor? _darkCrosshair;
        public static Cursor DarkCrosshairCursor => _darkCrosshair ??= CustomCursorFactory.CreateCrosshairWithSquareCenterCursor(100, 0.65, 14, 10, 12, (255, 0, 0, 0));

        private static Cursor? _lightCrosshair;
        public static Cursor LightCrosshairCursor => _lightCrosshair ??= CustomCursorFactory.CreateCrosshairWithSquareCenterCursor(100, 0.65, 14, 10, 12, (255, 255, 255, 255));
    }
}
