using System.Runtime.InteropServices;

namespace Cad_Point_Manager.Controls.D2DControl
{
    public static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = false)]
        public static extern IntPtr GetDesktopWindow();
    }
}
