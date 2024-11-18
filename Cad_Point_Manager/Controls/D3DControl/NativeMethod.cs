using System.Runtime.InteropServices;

namespace Cad_Point_Manager.Controls.D3DControl
{
    public static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = false)]
        public static extern IntPtr GetDesktopWindow();
    }
}
