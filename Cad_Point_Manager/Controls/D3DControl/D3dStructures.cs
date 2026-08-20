using Cad_Point_Manager.Models.PointRendering;
using SharpDX;
using System.Runtime.InteropServices;
using Matrix = SharpDX.Matrix;

namespace Cad_Point_Manager.Controls.D3DControl
{
    #region Enums
    [Flags]
    public enum LineInstanceFlags : uint
    {
        None = 0,
        ForceStartVisible = 1u << 0,
        ForceEndVisible = 1u << 1
    }
    #endregion

    #region Structures
    [StructLayout(LayoutKind.Sequential)]
    public struct DrawingSettingsBuffer
    {
        // View
        public Vector2 ViewportSize;
        private Vector2 _pad1;

        // Line rendering
        public float LineHalfWidthPixels;
        public float GlobalLineTypeScale;
        public float AnnotationScale;
        public float GlowPixelOffset;

        // Colors
        public Vector4 SelectedColor;
        public Vector4 SelectedMouseOverColor;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct SolidVertex(Vector3 pos, uint layerId, uint objectId)
    {
        public Vector3 Position = pos;
        public uint LayerId = layerId;
        public uint ObjectId = objectId;

        public readonly SolidVertex Transform(Matrix transform)
        {
            Vector3 transformedPosition = Vector3.TransformCoordinate(Position, transform);
            return new SolidVertex(transformedPosition, LayerId, ObjectId);
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct SignificantPointVertex
    {
        public Vector3 Position;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct SignificantPointSettingsBuffer
    {
        public Vector4 Color;
        public Vector2 ViewPortSize;
        public float RadiusPx;
        private float _pad;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct OverlayQuadVertex
    {
        public Vector2 Local;      // -1..1
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct OverlayVertex
    {
        public Vector3 Position;
        public Vector4 Color;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct OverlayOutlineSettings
    {
        public Vector2 RectMinWorld;   // 0..7
        public Vector2 RectMaxWorld;   // 8..15
        public float ThicknessPx;    // 16..19
        public float FeatherPx;      // 20..23
        private Vector2 _pad0;         // 24..31  <-- moves BorderColor to 32
        public Vector4 BorderColor;    // 32..47
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
    public struct MsdfVertex
    {
        public Vector2 Corner;
        public MsdfVertex(float x, float y)
        {
            Corner = new Vector2(x, y);
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct MsdfGlyphInstance
    {
        // Existing data
        public float EmToWorld;
        public float PenX;
        public float YSign;

        public uint LabelId;
        public uint PointId;

        // Glyph rectangle in EM space
        public Vector2 PlaneOrigin;
        public Vector2 PlaneSize;

        // Atlas rectangle
        public Vector2 UvOrigin;
        public Vector2 UvSize;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct GlyphMetricGpu
    {
        public Vector2 PlaneMin;

        public Vector2 PlaneMax;

        public Vector2 UvMin;

        public Vector2 UvMax;

        public float Advance;

        private Vector3 _padding;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct MsdfSettingsBuffer
    {
        public float AtlasWidth;
        public float AtlasHeight;
        public float DistanceRange;
        public float CameraZoom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct GlyphVertexQuad
    {
        public Vector2 PosDU; // design-unit vertex (triangle-list)
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct GlyphVertex
    {
        public Vector2 Corner;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct GlyphVertexDU
    {
        public Vector2 PosDU; // design-unit vertex (triangle-list)
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct GlyphInstance
    {
        public float DuToWorld;
        public float PenDU;
        public float YSign;

        public uint GlyphIndex;      // NEW

        public uint LabelId;
        public uint PointId;
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
        public uint Flags;  // bit0: visible
        public float Pad;    // keep 16B stride
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PointState
    {
        public Vector2 Offset;
        public Vector2 PointInfoOffset; // Offset of the point info text from the point position
        public uint GroupId;   // PointGroup index
        public uint Flags; // bit0: visible bit1: selected, bit2: mouseOver, bit3: hasLeaderLine, bit4: mouseOverAnchor, bit5: anchorPressed, bit6: isFlippedY, bit7: isFlippedX
        public Vector2 _pad;   // 16B stride
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct GroupState
    {
        public Vector4 Color; // rgba
        public float Scale; // point-scale
        public uint Flags; // bit0: visible
        public float TextInfoBaseXoffset; // world offset from point to base of text (before any drag)
        public float _pad;   // 16B stride
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PointMarkerInstance
    {
        public Vector3 Position;
        public float Radius;
        public uint LabelId;
        public uint PointId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LeaderLineSettings
    {
        public Vector2 InvViewport;
        public float PixelThickness;
        public float _pad;
        public Vector4 SelectedColor;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct LeaderLineInstance
    {
        public Vector2 Start;   // world: ellipse center
        public Vector2 End;     // world: text base *before* drag; shader adds LabelSRV.Offset
        public uint PointId;    // Point index
        public uint GroupId;    // PointGroup index
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LeaderLineGlowSettings
    {
        public Vector2 InvViewport;
        public float PixelThickness;
        public float _pad;
        public Vector4 HoverColor;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ToggleAnchorInstance
    {
        public Vector2 Center;
        public uint PointId;
        public uint GroupId;        // TEXCOORD9
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct ToggleAnchorSettingsBuffer
    {
        public Vector4 BaseColor; // rgba
        public Vector4 SelectedColor; // rgba
        public Vector4 MouseOverColor; // rgba
        public float DesiredHalf; // world units
        public float CornerFracOfHalf; // e.g., 0.35f
        public float Feather; // world units
        public float MaxHalfBase; // world units (pre-scale)
    }
    [StructLayout(LayoutKind.Sequential)]
    readonly struct AnchorDraw
    {
        public readonly Vector2 Center;
        public readonly Vector2 Half;
        public readonly CogoPoint Point;
        public AnchorDraw(Vector2 c, Vector2 h, CogoPoint p) { Center = c; Half = h; Point = p; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LayerState
    {
        public Vector4 Color; // rgba
        public uint Flags; // bit0: visible 
        public Vector3 Pad;   // 16B stride
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct ObjectState
    {
        public uint Flags; // bit0: visible bit1: selected, bit2: mouseOver, bit3: colorByLayer
        public uint LineTypeId; // LineType index
        public Vector2 Pad;   // 16B stride
        public Vector4 Color;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct CircleHoverVertex(Vector3 position, float radius, float isSelected = 0)
    {
        public Vector3 Position = position;
        public float PointMarkerRadiusWorld = radius;
        public float IsSelected = isSelected;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct CogoPointGlowSettingsBuffer
    {
        public float GlowRadiusPixels;
        private Vector3 padding;
        public Vector2 ViewportSize;
        private Vector2 padding2;
        public Vector4 HoverColor;
        public Vector4 SelectedColor;
        public Vector4 SelectedMouseOverColor;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LineVertex(Vector3 position, uint layerId, uint objectId)
    {
        public Vector3 Position = position;
        public uint LayerId = layerId;   // Layer index
        public uint ObjectId = objectId;  // Object index
        public uint LinetypeId; // Line type index
        private Vector2 _padding; // Padding to ensure the structure is 16-byte aligned

        public readonly LineVertex Translate(Vector3 offset)
        {
            return new LineVertex(Position + offset, LayerId, ObjectId);
        }

        public readonly LineVertex Transform(Matrix transform)
        {
            Vector3 transformedPosition = Vector3.TransformCoordinate(Position, transform);
            return new LineVertex(transformedPosition, LayerId, ObjectId);
        }

        public static implicit operator System.Windows.Point(LineVertex v)
        {
            return new System.Windows.Point(v.Position.X, v.Position.Y);
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct LineCornerVertex(float side, float along)
    {
        public Vector2 Corner = new(side, along);
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct LineInstance(Vector2 start, Vector2 end, uint layerId, uint objectId, float startDistance, uint flags)
    {
        public Vector2 Start = start;
        public Vector2 End = end;

        public uint LayerId = layerId;
        public uint ObjectId = objectId;

        public float StartDistance = startDistance;

        public uint Flags = flags;

        public readonly LineInstance Translate(Vector2 offset)
        {
            return new LineInstance(
                Start + offset, 
                End + offset, 
                LayerId, 
                ObjectId, 
                StartDistance, 
                Flags);
        }

        public readonly LineInstance Transform(Matrix transform)
        {
            return new LineInstance(
                Vector2.TransformCoordinate(Start, transform),
                Vector2.TransformCoordinate(End, transform),
                LayerId, 
                ObjectId,
                StartDistance,
                Flags);
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct LineGlowInstance(Vector2 start, Vector2 end, uint layerId, uint lineTypeId, float startDistance, uint flags)
    {
        public Vector2 Start = start;
        public Vector2 End = end;

        public uint LayerId = layerId;
        public uint LineTypeId = lineTypeId;

        public float StartDistance = startDistance;
        public uint Flags = flags;

        public readonly LineGlowInstance Translate(Vector2 offset)
        {
            return new LineGlowInstance(Start + offset, End + offset, LayerId, LineTypeId, StartDistance, Flags);
        }
        public readonly LineGlowInstance Transform(Matrix transform)
        {
            return new LineGlowInstance(
                Vector2.TransformCoordinate(Start, transform),
                Vector2.TransformCoordinate(End, transform),
                LayerId, LineTypeId, StartDistance, Flags);
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct LineTypeInfo
    {
        public uint FirstPatternIndex;
        public uint PatternCount;

        public float PatternLength;
        public float Padding;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct GlowCompositeVertex
    {
        public Vector2 Position;
        public Vector2 TexCoord;

        public GlowCompositeVertex(Vector2 position, Vector2 texCoord)
        {
            Position = position;
            TexCoord = texCoord;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DxfObjectSettingsBuffer
    {
        public Vector4 SelectedColor;
        public Vector4 SelectedMouseOverColor;
        public float HalfWidth;
        private Vector3 _padding; // Padding to ensure the structure is 16-byte aligned
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct LineRenderModeBuffer
    {
        public uint RenderSelectedOnly;
        public uint RenderGlowPass;
        private Vector2 _padding;
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
    public struct TextVertex(Vector3 position, uint layerId, uint objectId)
    {
        public Vector3 Position { get; set; } = position;
        public uint LayerId { get; set; } = layerId;   // Layer index
        public uint ObjectId { get; set; } = objectId; // Object index
        private Vector3 _padding; // Padding to ensure the structure is 16-byte aligned

        public readonly TextVertex Translate(Vector3 offset)
        {
            return new TextVertex(Position + offset, LayerId, ObjectId);
        }
        public readonly TextVertex Transform(Matrix transform)
        {
            Vector3 transformedPosition = Vector3.TransformCoordinate(Position, transform);
            return new TextVertex(transformedPosition, LayerId, ObjectId);
        }

        public static TextVertex RotateAroundPoint(TextVertex textVertex, Vector2 basePoint, float radians)
        {
            float cos = MathF.Cos(radians);
            float sin = MathF.Sin(radians);

            float dx = textVertex.Position.X - basePoint.X;
            float dy = textVertex.Position.Y - basePoint.Y;

            float rotatedX = cos * dx - sin * dy + basePoint.X;
            float rotatedY = sin * dx + cos * dy + basePoint.Y;

            return new TextVertex(new Vector3(rotatedX, rotatedY, textVertex.Position.Z), layerId: textVertex.LayerId,
                objectId: textVertex.ObjectId);
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

    [StructLayout(LayoutKind.Sequential)]
    public struct PanVertex
    {
        public Vector2 Position;
        public Vector2 TexCoord;

        public PanVertex(Vector2 position, Vector2 texCoord)
        {
            Position = position;
            TexCoord = texCoord;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PanSettings
    {
        public Vector2 OffsetUv;
        public Vector2 Padding;
    }
    #endregion
}
