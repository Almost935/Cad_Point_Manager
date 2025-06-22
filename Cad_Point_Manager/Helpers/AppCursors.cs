using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Cad_Point_Manager.Helpers
{
    public static class AppCursors
    {
        private static Cursor? _crosshair;
        public static Cursor CrosshairCursor => _crosshair ??= CustomCursorFactory.CreateCrosshairWithSquareCenterCursor(100, 0.65, 14, 10, 12, (255, 0, 0, 0));
    }

}
