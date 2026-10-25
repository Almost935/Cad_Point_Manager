using SharpDX;
using System;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Cad_Point_Manager.Common
{
    public readonly record struct Vector2d(double X, double Y)
    {
        public static Vector2d Zero => default;

        public static Vector2d operator +(Vector2d a, Vector2d b) => new(a.X + b.X, a.Y + b.Y);
        public static Vector2d operator -(Vector2d a, Vector2d b) => new(a.X - b.X, a.Y - b.Y);
        public static Vector2d operator -(Vector2d v) => new(-v.X, -v.Y);
        public static Vector2d operator *(Vector2d v, double scalar) => new(v.X * scalar, v.Y * scalar);
        public static Vector2d operator *(double scalar, Vector2d v) => v * scalar;
        public static double operator *(Vector2d a, Vector2d b) => Dot(a, b);
        public static Vector2d operator /(Vector2d v, double scalar) => new(v.X / scalar, v.Y / scalar);

        public static double Dot(Vector2d a, Vector2d b) => a.X * b.X + a.Y * b.Y;
        public static double Multiply(Vector2d a, Vector2d b) => Dot(a, b);
        public static double Cross(Vector2d a, Vector2d b) => a.X * b.Y - a.Y * b.X;
        public static double Distance(Vector2d a, Vector2d b) => a.DistanceTo(b);
        public static Vector2d Normalize(Vector2d v) => v.Normalize();
        public Vector2d Perpendicular() => new(-Y, X);
        public double LengthSquared() => X * X + Y * Y;
        public double Length() => Math.Sqrt(LengthSquared());
        public double DistanceSquaredTo(Vector2d other) => (this - other).LengthSquared();
        public double DistanceTo(Vector2d other) => Math.Sqrt(DistanceSquaredTo(other));
        public static double DistanceTo(Vector2d a, Vector2d b) => a.DistanceTo(b);
        public static double DistanceSquared(Vector2d a, Vector2d b) => a.DistanceSquaredTo(b);
        public Vector2d Normalize()
        {
            double lengthSquared = LengthSquared();
            return lengthSquared > 0 ? this / Math.Sqrt(lengthSquared) : Zero;
        }
        public static Vector2d Average(Vector2d a, Vector2d b) => a + (b - a) * 0.5;

        public static double PointToPointDistance(Vector2d a, Vector2d b) => a.DistanceTo(b);
        public static Vector2d GetClosestPointOnLine(Vector2d start, Vector2d end, Vector2d point)
        {
            Vector2d d = end - start;
            double lengthSquared = d.LengthSquared();

            if (lengthSquared == 0)
                return start;

            double t = Math.Clamp(Dot(point - start, d) / lengthSquared, 0.0, 1.0);

            return start + d * t;
        }
        public static double PointToLineDistanceSquared(Vector2d point, Vector2d start, Vector2d end)
            => point.DistanceSquaredTo(GetClosestPointOnLine(start, end, point));
        public static double PointToLineDistance(Vector2d point, Vector2d start, Vector2d end)
            => Math.Sqrt(PointToLineDistanceSquared(point, start, end));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2d TransformCoordinate(
            Vector2d position, System.Windows.Media.Matrix matrix) => new(
                position.X * matrix.M11 + position.Y * matrix.M21 + matrix.OffsetX,
                position.X * matrix.M12 + position.Y * matrix.M22 + matrix.OffsetY);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2d TransformCoordinate(System.Windows.Media.Matrix matrix)
            => TransformCoordinate(this, matrix);

        public bool IsWithin(Rect rect) => !rect.IsEmpty &&
            X >= rect.Left && X <= rect.Right && Y >= rect.Top && Y <= rect.Bottom;
        public Vector2d Transform(System.Windows.Media.Matrix matrix)
            => new(X * matrix.M11 + Y * matrix.M21 + matrix.OffsetX,
                X * matrix.M12 + Y * matrix.M22 + matrix.OffsetY);
        public System.Windows.Point ToPoint() => new(X, Y);
        public Vector2 ToSharpDXVector2() => new((float)X, (float)Y);
        public Vector2 ToGpuRelative(Vector2d origin) => new((float)(X - origin.X), (float)(Y - origin.Y));
    }
}
