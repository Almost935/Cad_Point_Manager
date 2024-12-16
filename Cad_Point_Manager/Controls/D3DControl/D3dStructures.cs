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

        public readonly override string ToString()
        {
            return $"Left: {Left}, Right: {Right}, Bottom: {Bottom}, Top: {Top})";
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

        public static Bounds ScaleToCenter(Bounds overallBounds, float overallScale, float screenWidth, float screenHeight)
        {
            //float addX = -screenWidth / 8;
            //float addY = -screenHeight / 8;
            float addX = 0;
            float addY = 0;

            Bounds newBounds = new Bounds((overallBounds.Left / overallScale) + addX, (overallBounds.Right / overallScale) + addX, (overallBounds.Bottom / overallScale) + addY, (overallBounds.Top / overallScale) + addY);
            
            return newBounds;
        }

        //public static Bounds ScaleTo(Bounds bounds, float scale, Vector2 pivot)
        //{
        //    // Calculate the current width and height of the box
        //    float width = bounds.Right - bounds.Left;
        //    float height = bounds.Top - bounds.Bottom;

        //    // Calculate the new width and height after scaling
        //    float newWidth = width * scale;
        //    float newHeight = height * scale;

        //    // Calculate the fixed offsets to maintain the point's position
        //    float offsetX = (pivot.X - bounds.Left) * (1 - scale);
        //    float offsetY = (pivot.Y - bounds.Bottom) * (1 - scale);

        //    // Adjust the box edges
        //    float newLeft = bounds.Left + offsetX;
        //    float newRight = newLeft + newWidth;
        //    float newBottom = bounds.Bottom + offsetY;
        //    float newTop = newBottom + newHeight;

        //    return new Bounds(newLeft, newRight, newBottom, newTop);
        //}

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

        //public static Bounds ScaleTo(Bounds bounds, float scale, Vector2 pivot)
        //{
        //    Debug.WriteLine($"bounds: {bounds.Width} {bounds.Height}");

        //    float newLeft = pivot.X + ((bounds.Left - pivot.X)) / scale;
        //    float newRight = pivot.X + ((bounds.Right - pivot.X)) / scale;
        //    //float newBottom = pivot.Y + ((bounds.Bottom - pivot.Y)) / scale;
        //    //float newTop = pivot.Y + (bounds.Top - pivot.Y) / scale;

        //    //float newLeft = bounds.Left / scale;
        //    //float newRight = bounds.Right / scale;
        //    float newBottom = bounds.Bottom / scale;
        //    float newTop = bounds.Top / scale;

        //    return new Bounds(newLeft, newRight, newBottom, newTop);
        //}
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
