using SharpDX;
using System.Windows;
using Point = System.Windows.Point;

namespace Cad_Point_Manager.Extensions
{
    public static class Matrix3x2Extensions
    {
        public static System.Windows.Media.Matrix ToWindowsMatrix(this Matrix3x2 matrix)
        {
            return new System.Windows.Media.Matrix(matrix.M11, matrix.M12, matrix.M21, matrix.M22, matrix.M31, matrix.M32);
        }
    }
}
