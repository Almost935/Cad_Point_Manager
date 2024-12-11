using SharpDX;
using SharpDX.Direct2D1.Effects;
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

        public static Bounds ScaleTo(Bounds bounds, float scale, Vector2 pivot)
        {
            Debug.WriteLine($"pivot: {pivot}");

            // Compute the distances of the bounds' edges from the pivot point
            float leftOffset = bounds.Left - pivot.X;
            float rightOffset = bounds.Right - pivot.X;
            float bottomOffset = bounds.Bottom - pivot.Y;
            float topOffset = bounds.Top - pivot.Y;

            Debug.WriteLine($"leftOffset: {leftOffset} rightOffset: {rightOffset} bottomOffset: {bottomOffset} topOffset: {topOffset}");

            // Scale these offsets
            float newLeft = pivot.X + leftOffset / scale;
            float newRight = pivot.X + rightOffset / scale;
            float newTop = pivot.Y + topOffset / scale;
            float newBottom = pivot.Y + bottomOffset / scale;

            Bounds newBounds = new(newLeft, newRight, newBottom, newTop);

            // Return the updated bounds
            return newBounds;
        }

        //public static Bounds ScaleTo(Bounds bounds, int zoomStepDelta, float zoomFactor, Vector2 scalePoint, float screenWidth, float screenHeight)
        //{
        //    Debug.WriteLine($"scalePoint: {scalePoint}");

        //    float scale = (float)Math.Pow(zoomFactor, zoomStepDelta);

        //    float addX = screenWidth / 8;
        //    float addY = screenHeight / 8;
        //    //float addX = 0;
        //    //float addY = 0;

        //    Bounds newBounds = new Bounds((bounds.Left / scale) + addX, (bounds.Right / scale) + addX, (bounds.Bottom / scale) + addY, (bounds.Top / scale) + addY);

        //    return newBounds;


        //    //// Compute the distances of the bounds' edges from the pivot point
        //    //float leftOffset = bounds.Left - pivot.X;
        //    //float rightOffset = bounds.Right - pivot.X;
        //    //float topOffset = bounds.Top - pivot.Y;
        //    //float bottomOffset = bounds.Bottom - pivot.Y;

        //    //// Scale these offsets
        //    //float newLeft = pivot.X + leftOffset / scale;
        //    //float newRight = pivot.X + rightOffset / scale;
        //    //float newTop = pivot.Y + topOffset / scale;
        //    //float newBottom = pivot.Y + bottomOffset / scale;

        //    //Bounds  newBounds = new Bounds(newLeft, newRight, newBottom, newTop);

        //    //// Return the updated bounds
        //    //return newBounds;
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
