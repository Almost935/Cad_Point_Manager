using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cad_Point_Manager.Controls.D3DControl
{
    [StructLayout(LayoutKind.Sequential)]
    struct Vertex
    {
        public Vector3 Position;
        public Vector4 Color; 

        public Vertex(Vector3 position, Vector4 color)
        {
            Position = position;
            Color = color;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TransformationBuffer
    {
        public Matrix WorldViewProjection;  // This is the matrix you send to the shader
    }

    public struct Bounds
    {
        public float Left;
        public float Right;
        public float Top;
        public float Bottom;
        public float Width;
        public float Height;
        public Vector2 Center => new((Left + Right) / 2, (Top + Bottom) / 2);
        public Vector2 TopLeft => new(Left, Top);
        public Vector2 TopRight => new(Right, Top);
        public Vector2 BottomLeft => new(Left, Bottom);
        public Vector2 BottomRight => new(Right, Bottom);

        public Bounds(float left, float right, float bottom, float top)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;

            Width = right - left;
            Height = top - bottom;
        }

        public override string ToString()
        {
            return $"Left: {Left}, Right: {Right}, Top: {Top}, Bottom: {Bottom})";
        }

        public static Bounds Empty => new(0, 0, 0, 0);

        public static Bounds Translate(Bounds bounds, float x, float y)
        {
            return new Bounds(bounds.Left + x, bounds.Right + x, bounds.Bottom + y, bounds.Top + y);
        }

        public static Bounds Scale(Bounds bounds, float scale)
        {
            return Bounds.Empty;
        }

        //public static Bounds ScaleTo(Bounds bounds, float scale, Vector2 pos)
        //{
        //    float newLeft = pos.X + (bounds.Left - pos.X) / scale;
        //    float newRight = pos.X + (bounds.Right - pos.X) / scale;
        //    float newTop = pos.Y + (bounds.Top - pos.Y) / scale;
        //    float newBottom = pos.Y + (bounds.Bottom - pos.Y) / scale;

        //    return new Bounds(newLeft, newRight, newBottom, newTop);
        //}

        public static Bounds ScaleTo(Bounds bounds, float scale, Vector2 pivot)
        {
            float newLeft = pivot.X + (bounds.Left - pivot.X) / scale;
            float newRight = pivot.X + (bounds.Right - pivot.X) / scale;
            float newTop = pivot.Y + (bounds.Top - pivot.Y) / scale;
            float newBottom = pivot.Y + (bounds.Bottom - pivot.Y) / scale;

            return new Bounds(newLeft, newRight, newBottom, newTop);
        }
    }

    public struct Rotation
    {
        public float X = 0;
        public float Y = 0;
        public float Z = 0;

        public Rotation() { }
        public Rotation(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Rotation NoRotation => new Rotation(0, 0, 0);

        public void SetX(float x) { X = x; }
        public void SetY(float y) { Y = y; }
        public void SetZ(float z) { Z = z; }
    }
}
