using Cad_Point_Manager.Models.PointRendering;
using SharpDX;
using System.Runtime.InteropServices;
using Matrix = SharpDX.Matrix;

namespace Cad_Point_Manager.Controls.D3DControl
{
    [StructLayout(LayoutKind.Sequential)]
    struct OverlayQuadVertex
    {
        public Vector2 Local;      // -1..1
    }
    [StructLayout(LayoutKind.Sequential)]
    struct RoundedHoverRectInstance
    {
        public Vector2 Center;       // world
        public Vector2 HalfSize;     // world
        public Vector2 RadiusFeather;// x=radius(world), y=feather(world)
        public Vector4 Color;        // rgba
    }
    [StructLayout(LayoutKind.Sequential)]
    struct CircleHoverVertex(Vector3 position, float radius, float isSelected = 0)
    {
        public Vector3 Position = position;
        public float Radius = radius;
        public float IsSelected = isSelected;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct CircleHoverSettingsBuffer
    {
        public float GlowOffset;
        public float GlowTransparency;
        private Vector2 Padding;
        public Vector4 SelectedColor;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct GlyphVertexDU
    {
        public Vector2 PosDU; // design-unit vertex (triangle-list)
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct GlyphInstance
    {
        public Vector2  Origin;     // world position (baseline origin) of the string/line
        public float    DuToWorld;    // scale: DU -> world
        public float    PenDU;        // accumulated advance in DU for this glyph
        public float    YSign;        // typically -1 when world Y is up and font Y is down
        public uint     LabelId;   // stable per text line: PN/Elev/Desc for a cogo point
        public uint     GroupId;   // PointGroup index
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct GlyphSettingsBuffer
    {
        public Vector4 SelectedColor;
    }
    public struct GlyphRange
    {
        public int StartVertex;   // into the packed glyph vertex buffer
        public int VertexCount;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct LabelState
    {
        public Vector2 Offset; // world-space drag delta
        public uint    Flags;  // bit0: visible, bit1: selected, bit2: mouseOver
        public float   Pad;    // keep 16B stride
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct GroupState
    {
        public Vector4 Color; // rgba
        public float   Scale; // point-scale
        public uint    Flags; // bit0: visible
        public Vector2 Pad;   // 16B stride
    }

    public struct PointMarkerInstance
    {
        public Vector3 Position;
        public float   Radius;
        public uint    LabelId;
        public uint    GroupId;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct LeaderLineSettings
    {
       
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct LeaderLineInstance
    {
        public Vector2 Start;        // world: ellipse center
        public Vector2 End;    // world: text base *before* drag; shader adds LabelSRV.Offset
        public uint LabelId;  // same scheme you use for glyphs/circles (per-CogoPoint label id)
        public uint GroupId;  // PointGroup index
    }

    [StructLayout(LayoutKind.Sequential)]
    struct ToggleAnchorInstance
    {
        public Vector2 Center;
        public Vector2 Size;           // world half-width/height
        public Vector2 RadiusFeather;
        public Vector4 BaseColor;
        public Vector4 HoverColor;
        public Vector4 PressedColor;
        public float On;
        public uint State; // 0=normal,1=hover,2=pressed
        private uint _pad0, _pad1;
    }
    [StructLayout(LayoutKind.Sequential)]
    readonly struct AnchorDraw   // for CPU hit-test & mapping
    {
        public readonly Vector2 Center;
        public readonly Vector2 Half;
        public readonly CogoPoint Point;
        public AnchorDraw(Vector2 c, Vector2 h, CogoPoint p) { Center = c; Half = h; Point = p; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OverlayVertex
    {
        public Vector3 Position;
        public Vector4 Color;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CircleVertex(Vector3 position, Vector4 color, float radius, float isVisible = 1.0f, float isMouseOver = 0, float isSelected = 0)
    {
        public Vector3 Position = position;
        public Vector4 Color = color;
        public float Radius = radius;

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
    public struct CircleSettingsBuffer
    {
        public Vector4 SelectedColor;
        public Vector4 SelectedMouseOverColor;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct CircleGlowSettingsBuffer
    {
        public float GlowOffset;
        public float GlowTransparency;
        private Vector2 Padding;
        public Vector4 SelectedColor;
        public Vector4 SelectedMouseOverColor;
    }

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
    public struct TextVertex(Vector3 position, Vector4 color, float isVisible = 1.0f, float isMouseOver = 0, float isSelected = 0)
    {
        public Vector3 Position { get; set; } = position;
        public Vector4 Color { get; set; } = color;

        /// <summary>
        /// float value indicating whether the vertex is visible or not. 1.0f is visible, 0.0f is not visible.
        /// </summary>
        public float IsVisible = isVisible;

        /// <summary>
        /// float value indicating whether the mouse is currently over the text object. 1.0f is true, 0.0f is false.
        /// </summary>
        public float IsMouseOver { get; set; } = isMouseOver;

        /// <summary>
        /// float value indicating whether the text is currently selected. 1.0f is true, 0.0f is false.
        /// </summary>
        public float IsSelected { get; set; } = isSelected;

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
        public void Transform(Matrix transform)
        {
            Position = Vector3.TransformCoordinate(Position, transform);
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
    public struct TextGlowSettingsBuffer
    {
        public float GlowOffset;
        public float GlowTransparency;
        private Vector2 Padding; // Padding to ensure the structure is 16-byte aligned
        public Vector4 SelectedColor;
        public Vector4 SelectedMouseOverColor;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TransformationBuffer
    {
        public Matrix WorldViewProjection;  // This is the matrix you send to the shader
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ViewportBuffer
    {
        public Vector2 ViewportSize;
        public Vector2 Padding;
    }
}
