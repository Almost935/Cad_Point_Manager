using SharpDX;

namespace Cad_Point_Manager.Extensions
{
    public static class WindowsMatrixExtensions
    {
        public static Matrix3x2 ToWindowsMatrix(this System.Windows.Media.Matrix matrix)
        {
            return new Matrix3x2((float)matrix.M11, (float)matrix.M12, (float)matrix.M21, (float)matrix.M22, (float)matrix.OffsetX, (float)matrix.OffsetY);
        }
    }
}
