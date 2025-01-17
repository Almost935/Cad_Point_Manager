using SharpDX;
using SharpDX.Direct2D1.Effects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Matrix = SharpDX.Matrix;

namespace Cad_Point_Manager.Controls.D3DControl
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex(Vector3 position, Vector4 color, float isVisible = 1.0f)
    {
        public Vector3 Position = position;
        public Vector4 Color = color;

        /// <summary>
        /// float value indicating whether the vertex is visible or not. 1.0f is visible, 0.0f is not visible.
        /// </summary>
        public float IsVisible = isVisible;

        public float GetDistanceTo(Vertex vertex)
        {
            return Vector3.Distance(Position, vertex.Position);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TextVertex(Vector3 position, Vector4 color, Vector2 textCoord)
    {
        public Vector3 Position = position; // Position of the character on the screen
        public Vector4 Color = color;    // Color of the text
        public Vector2 TextCoord = textCoord; // Texture coordinate on the font texture
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

        public readonly Vector2 Center => new((Left + Right) / 2, (Top + Bottom) / 2);
        public readonly Vector2 TopLeft => new(Left, Top);
        public readonly Vector2 TopRight => new(Right, Top);
        public readonly Vector2 BottomLeft => new(Left, Bottom);
        public readonly Vector2 BottomRight => new(Right, Bottom);
        public readonly float MaxDimension => Math.Max(Width, Height);
        public readonly float MinimumDimension => Math.Min(Width, Height);

        public Bounds(float left, float right, float bottom, float top)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;

            Width = right - left;
            Height = top - bottom;
        }

        public readonly override string ToString()
        {
            return $"Left: {Left}, Right: {Right}, Bottom: {Bottom}, Top: {Top})";
        }

        public Rect ToRect()
        {
            Rect rect = new Rect(this.Left, this.Bottom, this.Width, this.Height);
            return rect;
        }

        public static Bounds Empty => new(0, 0, 0, 0);

        public static Bounds Translate(Bounds bounds, float x, float y)
        {
            return new Bounds(bounds.Left + x, bounds.Right + x, bounds.Bottom + y, bounds.Top + y);
        }

        public static Bounds Scale(Bounds bounds, float scale)
        {
            return new Bounds(bounds.Left / scale, bounds.Right / scale, bounds.Bottom / scale, bounds.Top / scale);
        }

        public static Bounds ScaleToCenter(Bounds bounds, float scale)
        {
            Bounds scaledBounds = Bounds.Scale(bounds, scale);
            Vector2 centerOffset = new((bounds.Center.X - scaledBounds.Center.X), (bounds.Center.Y - scaledBounds.Center.Y));
            scaledBounds = Bounds.Translate(scaledBounds, centerOffset.X, centerOffset.Y);

            return scaledBounds;
        }

        public static Bounds ScaleTo(Bounds bounds, float scale, Vector2 pivot)
        {
            // Calculate the box center
            float boxCenterX = (bounds.Left + bounds.Right) / 2f;
            float boxCenterY = (bounds.Bottom + bounds.Top) / 2f;

            // Calculate scaling offsets
            float deltaX = (pivot.X - boxCenterX) * (1 - scale);
            float deltaY = (pivot.Y - boxCenterY) * (1 - scale);

            // Calculate the new box edges
            float newLeft = bounds.Left + deltaX;
            float newRight = bounds.Right + deltaX;
            float newBottom = bounds.Bottom + deltaY;
            float newTop = bounds.Top + deltaY;

            // Scale the box dimensions
            float width = (newRight - newLeft) * (1 / scale);
            float height = (newTop - newBottom) * (1 / scale);

            // Adjust the box edges based on the scaled dimensions
            newLeft = pivot.X - (pivot.X - newLeft) * (1 / scale);
            newRight = newLeft + width;
            newBottom = pivot.Y - (pivot.Y - newBottom) * (1 / scale);
            newTop = newBottom + height;

            return new Bounds(newLeft, newRight, newBottom, newTop);
        }

        public static Rect ToRect(Bounds bounds)
        {
            Rect rect = new Rect(bounds.Left, bounds.Bottom, bounds.Width, bounds.Height);
            return rect;
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
