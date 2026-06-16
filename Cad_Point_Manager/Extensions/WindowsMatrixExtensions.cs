using SharpDX;

namespace Cad_Point_Manager.Extensions
{
    public static class WindowsMatrixExtensions
    {
        public static Matrix ToSharpDxMatrix(this System.Windows.Media.Matrix matrix)
        {
            return new Matrix(
                (float)matrix.M11, (float)matrix.M12, 0f, 0f,
                (float)matrix.M21, (float)matrix.M22, 0f, 0f,
                0f, 0f, 1f, 0f,
                (float)matrix.OffsetX, (float)matrix.OffsetY, 0f, 1f);
        }
        public static Matrix3x2 ToSharpDxMatrix3x2(this System.Windows.Media.Matrix matrix)
        {
            return new Matrix3x2((float)matrix.M11, (float)matrix.M12, (float)matrix.M21, (float)matrix.M22, (float)matrix.OffsetX, (float)matrix.OffsetY);
        }
    }
}
