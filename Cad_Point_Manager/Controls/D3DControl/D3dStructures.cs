using SharpDX;
using System.Runtime.InteropServices;
using System.Windows;
using Matrix = SharpDX.Matrix;

namespace Cad_Point_Manager.Controls.D3DControl
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LineVertex(Vector3 position, Vector4 color, float isVisible = 1.0f, float isMouseOver = 0, float isSelected = 0)
    {
        public Vector3 Position = position;
        public Vector4 Color = color;

        /// <summary>
        /// float value indicating whether the vertex is visible or not. 1.0f is visible, 0.0f is not visible.
        /// </summary>
        public float IsVisible = isVisible;

        /// <summary>
        /// float value indicating whether the mouse is currently over the text object. 1.0f is true, 0.0f is false.
        /// </summary>
        public float IsMouseOver = isMouseOver;

        /// <summary>
        /// float value indicating whether the line is currently selected. 1.0f is true, 0.0f is false.
        /// </summary>
        public float IsSelected = isSelected;

        public void SetIsMouseOver(bool isMouseOver)
        {
            IsMouseOver = isMouseOver ? 1 : 0;
        }

        public void SetIsSelected(bool isSelected)
        {
            IsSelected = isSelected ? 1 : 0;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LineSettingsBuffer
    {
        public Vector4 SelectedColor;
        public Vector4 SelectedMouseOverColor;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LineGlowSettingsBuffer
    {
        public float GlowOffset;
        public float GlowTransparency;
        private Vector2 Padding;
        public Vector4 SelectedColor;
        public Vector4 SelectedMouseOverColor;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TextSettingsBuffer
    {
        public Vector4 SelectedColor;
        public Vector4 SelectedMouseOverColor;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TextGlowSettingsBuffer
    {
        public float GlowOffset;
        public float GlowTransparency;
        private Vector2 Padding; // Padding to ensure the structure is 16-byte aligned
        public Vector4 SelectedColor;
        public Vector4 SelectedMouseOverColor;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct TextVertex(Vector3 position, Vector4 color, float isVisible = 1.0f, float isMouseOver = 0, float isSelected = 0)
    {
        public Vector3 Position = position;
        public Vector4 Color = color;

        /// <summary>
        /// float value indicating whether the vertex is visible or not. 1.0f is visible, 0.0f is not visible.
        /// </summary>
        public float IsVisible = isVisible;

        /// <summary>
        /// float value indicating whether the mouse is currently over the text object. 1.0f is true, 0.0f is false.
        /// </summary>
        public float IsMouseOver = isMouseOver;

        /// <summary>
        /// float value indicating whether the text is currently selected. 1.0f is true, 0.0f is false.
        /// </summary>
        public float IsSelected = isSelected;

        public readonly TextVertex Translate(Vector3 offset)
        {
            return new TextVertex(Position + offset, Color, isVisible: IsVisible,
                isMouseOver: IsMouseOver, isSelected: IsSelected);
        }
        public readonly TextVertex Translate(Vector2 offset)
        {
            return new TextVertex(new Vector3(Position.X + offset.X, Position.Y + offset.Y, Position.Z), Color, isVisible: IsVisible,
                isMouseOver: IsMouseOver, isSelected: IsSelected);
        }
        public readonly TextVertex Translate(float x, float y, float z)
        {
            return new TextVertex(new Vector3(Position.X + x, Position.Y + y, Position.Z + z), Color, isVisible: IsVisible,
                isMouseOver: IsMouseOver, isSelected: IsSelected);
        }

        public static TextVertex RotateAroundPoint(TextVertex textVertex, Vector2 basePoint, float radians)
        {
            float cos = MathF.Cos(radians);
            float sin = MathF.Sin(radians);

            float dx = textVertex.Position.X - basePoint.X;
            float dy = textVertex.Position.Y - basePoint.Y;

            float rotatedX = cos * dx - sin * dy + basePoint.X;
            float rotatedY = sin * dx + cos * dy + basePoint.Y;

            return new TextVertex(new Vector3(rotatedX, rotatedY, textVertex.Position.Z), textVertex.Color, isVisible: textVertex.IsVisible, 
                isMouseOver: textVertex.IsMouseOver, isSelected: textVertex.IsSelected);
        }

        public void SetIsMouseOver(bool isMouseOver)
        {
            IsMouseOver = isMouseOver ? 1 : 0;
        }

        public void SetIsSelected(bool isSelected)
        {
            IsSelected = isSelected ? 1 : 0;
        }

        public static implicit operator System.Windows.Point(TextVertex v)
        {
            return new System.Windows.Point(v.Position.X, v.Position.Y);
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
