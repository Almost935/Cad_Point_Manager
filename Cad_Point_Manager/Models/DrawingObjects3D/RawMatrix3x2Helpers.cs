using SharpDX.Mathematics.Interop;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public static class RawMatrix3x2Helpers
    {
        // Extract scale from a matrix
        public static (float scaleX, float scaleY) ExtractScale(RawMatrix3x2 matrix)
        {
            float scaleX = (float)Math.Sqrt(matrix.M11 * matrix.M11 + matrix.M12 * matrix.M12);
            float scaleY = (float)Math.Sqrt(matrix.M21 * matrix.M21 + matrix.M22 * matrix.M22);
            return (scaleX, scaleY);
        }

        // Create a pure rotation matrix around a point (px, py)
        public static RawMatrix3x2 CreateRotationMatrix(float angleInDegrees, float px, float py)
        {
            float angleInRadians = MathUtil.DegreesToRadians(angleInDegrees);
            float cosTheta = (float)Math.Cos(angleInRadians);
            float sinTheta = (float)Math.Sin(angleInRadians);

            return new RawMatrix3x2
            {
                M11 = cosTheta,
                M12 = sinTheta,
                M21 = -sinTheta,
                M22 = cosTheta,
                M31 = px - (px * cosTheta) + (py * sinTheta),
                M32 = py - (px * sinTheta) - (py * cosTheta)
            };
        }

        // Multiply two RawMatrix3x2 matrices manually
        public static RawMatrix3x2 Multiply(RawMatrix3x2 a, RawMatrix3x2 b)
        {
            return new RawMatrix3x2
            {
                M11 = a.M11 * b.M11 + a.M12 * b.M21,
                M12 = a.M11 * b.M12 + a.M12 * b.M22,
                M21 = a.M21 * b.M11 + a.M22 * b.M21,
                M22 = a.M21 * b.M12 + a.M22 * b.M22,
                M31 = a.M31 * b.M11 + a.M32 * b.M21 + b.M31,
                M32 = a.M31 * b.M12 + a.M32 * b.M22 + b.M32
            };
        }

        // Rotate matrix around a point while preserving scale
        public static RawMatrix3x2 RotateMatrixPreserveScale(RawMatrix3x2 originalMatrix, float angleInDegrees, float px, float py)
        {
            // Extract the original scale
            var (scaleX, scaleY) = ExtractScale(originalMatrix);

            // Create a pure rotation matrix around a point
            var rotationMatrix = CreateRotationMatrix(angleInDegrees, px, py);

            // Apply the original scale back to the rotation matrix
            var scaledRotationMatrix = new RawMatrix3x2
            {
                M11 = rotationMatrix.M11 * scaleX,
                M12 = rotationMatrix.M12 * scaleX,
                M21 = rotationMatrix.M21 * scaleY,
                M22 = rotationMatrix.M22 * scaleY,
                M31 = rotationMatrix.M31 + originalMatrix.M31,
                M32 = rotationMatrix.M32 + originalMatrix.M32
            };

            return scaledRotationMatrix;
        }
    }
}
