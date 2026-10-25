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

        // CogoPoint Rendering
        public float CogoPointCircleRadiusPixels;
        private Vector3 _pad2;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct SolidVertex(Vector2 pos, uint layerId, uint objectId)
    {
        public Vector2 Position = pos;
        public uint LayerId = layerId;
        public uint ObjectId = objectId;

        public readonly SolidVertex Transform(Matrix transform)
        {
            Vector2 transformedPosition = Vector2.TransformCoordinate(Position, transform);
            return new SolidVertex(transformedPosition, LayerId, ObjectId);
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct SignificantPointVertex
    {
        public Vector2 Position;
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
        public Vector2 Local;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct OverlayVertex
    {
        public Vector2 Position;
        public Vector4 Color;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct DragOverlayVertex(float x, float y)
    {
        public Vector2 Local = new(x, y);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DragOverlaySettings
    {
        // Rectangle in render-target pixels
        public Vector2 RectMinPx;
        public Vector2 RectMaxPx;

        // Render target size in pixels
        public Vector2 ViewportSize;

        // Border settings
        public float ThicknessPx;
        public float FeatherPx;

        // Colors
        public Vector4 FillColor;
        public Vector4 BorderColor;
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
    public struct MsdfVertex(float x, float y)
    {
        public Vector2 Corner = new(x, y);
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
        /// <summary>
        /// Represents the offset of a specific label (CogoPoint.PointNumberOffset, CogoPoint.PointDescriptionOffset, CogoPoint.PointElevationOffset) 
        /// from the base point of the CogoPoint (CogoPoint.Position)
        /// </summary>
        public Vector2 Offset;
        public uint Flags;  // bit0: visible
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PointState
    {
        /// <summary>
        /// Vector representing the base point offset of the CogoPoint (CogoPoint.Position)
        /// </summary>
        public Vector2 Offset;

        /// <summary>
        /// Represents the current distance between the CogoPoint and the base point. Changes as the CogoPoint is dragged by the user (CogoPoint.TextInfoOffset)
        /// </summary>
        public Vector2 PointInfoOffset;
        public uint GroupId;

        public uint Flags;

        /// <summary>
        /// Represents the quadrant that the CogoPoint's labels are in (CogoPoint.LabelQuadrant)
        /// </summary>
        public uint LabelQuadrant;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct GroupState
    {
        public Vector4 Color; // rgba

        /// <summary>
        /// Represents the current scale of the CogoPoint. Is defined by the PointGroup (PointGroup.PointScale)
        /// </summary>
        public float Scale;
        public uint Flags; // bit0: visible

        /// <summary>
        /// Represents the base distance between the CogoPoint and the point info text. Property is contained in the CogoPoint's PointGroup (PointGroup.PointInfoBaseXoffset)
        /// </summary>
        public float TextInfoBaseXoffset; // world offset from point to base of text (before any drag)
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PointMarkerInstance
    {
        public uint PointId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LeaderLineInstance
    {
        public uint PointId;    // Point index
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ToggleAnchorInstance
    {
        public Vector2 Center;
        public uint PointId;
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
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct ObjectState
    {
        public uint Flags; // bit0: visible bit1: selected, bit2: mouseOver, bit3: colorByLayer
        public uint LineTypeId; // LineType index
        public Vector4 Color;
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
    public struct LineVertex(Vector2 position, uint layerId, uint objectId)
    {
        public Vector2 Position = position;
        public uint LayerId = layerId;   // Layer index
        public uint ObjectId = objectId;  // Object index
        public uint LinetypeId; // Line type index

        public readonly LineVertex Translate(Vector2 offset)
        {
            return new LineVertex(Position + offset, LayerId, ObjectId);
        }

        public readonly LineVertex Transform(Matrix transform)
        {
            Vector2 transformedPosition = Vector2.TransformCoordinate(Position, transform);
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
    public struct LineInstance(
        Vector2 start, Vector2 end, uint layerId, uint objectId, float startDistance, uint flags, float parentSegmentLength)
    {
        public Vector2 Start = start;
        public Vector2 End = end;
        public uint LayerId = layerId;
        public uint ObjectId = objectId;
        public float StartDistance = startDistance;
        public uint Flags = flags;
        public float ParentSegmentLength = parentSegmentLength;

        public readonly LineInstance Translate(Vector2 offset)
        {
            return new LineInstance(
                Start + offset,
                End + offset,
                LayerId,
                ObjectId,
                StartDistance,
                Flags,
                ParentSegmentLength);
        }
        public readonly LineInstance Transform(Matrix transform)
        {
            return new LineInstance(
                Vector2.TransformCoordinate(Start, transform),
                Vector2.TransformCoordinate(End, transform),
                LayerId,
                ObjectId,
                StartDistance,
                Flags,
                ParentSegmentLength);
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct LineTypeInfo(uint firstPatternIndex, uint patternCount, float patternLength, float dashLength)
    {
        public uint FirstPatternIndex = firstPatternIndex;
        public uint PatternCount = patternCount;

        public float PatternLength = patternLength;
        public float DashLength = dashLength;
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
    public struct TextVertex(Vector2 position, uint layerId, uint objectId)
    {
        public Vector2 Position { get; set; } = position;
        public uint LayerId { get; set; } = layerId;   // Layer index
        public uint ObjectId { get; set; } = objectId; // Object index

        public readonly TextVertex Translate(Vector2 offset)
        {
            return new TextVertex(Position + offset, LayerId, ObjectId);
        }
        public readonly TextVertex Transform(Matrix transform)
        {
            Vector2 transformedPosition = Vector2.TransformCoordinate(Position, transform);
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

            return new TextVertex(new Vector2(rotatedX, rotatedY), layerId: textVertex.LayerId,
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
    public struct PanVertex(Vector2 position, Vector2 texCoord)
    {
        public Vector2 Position = position;
        public Vector2 TexCoord = texCoord;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PanSettings
    {
        public Vector2 OffsetUv;
        public float PanCacheFactorX;
        public float PanCacheFactorY;
    }
    #endregion
}
