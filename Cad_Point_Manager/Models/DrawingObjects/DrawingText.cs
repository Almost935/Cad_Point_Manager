using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Helpers;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Models.DrawingObjects.HelperClasses;
using SharpDX;
using Matrix = System.Windows.Media.Matrix;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public abstract class DrawingText : DrawingObject
    {
        #region Properties
        protected const double UnderlineFactor = 0.3;
        protected const double OverlineFactor = 1.25;
        protected const double StrikethroughFactor = 0.50;

        public List<TextVertex> TextVertices { get; set; } = [];
        public List<LineInstance> LineInstances { get; set; } = [];
        public TextRenderStyle TextRenderStyle { get; set; } = TextRenderStyle.Triangle;

        public string Text { get; set; }
        public double TextHeight { get; set; }
        public double MaxWidth { get; set; }
        public int StartLineVertexIndex { get; set; }
        public int EndLineVertexIndex { get; set; }
        public int StartTextVertexIndex { get; set; }
        public int EndTextVertexIndex { get; set; }
        public Vector2d Position { get; set; }
        public double Rotation { get; set; } = 0;
        public Vector2d AttachmentOffset { get; set; } = Vector2d.Zero;
        public double TextHeightScaleFactor { get; set; } = 1.0f;
        public double TextWidthScaleFactor { get; set; } = 1.0;
        public bool IsUnderlined { get; set; } = false;
        public bool IsStrikeOut { get; set; } = false;
        public bool IsOverStrike { get; set; } = false;

        public Matrix LocalTranslationTransform => new Matrix(1, 0, 0, 1, AttachmentOffset.X, AttachmentOffset.Y);
        public Matrix WorldTranslationTransform => new(1, 0, 0, 1, Position.X, Position.Y);
        public Matrix RotationTransform
        {
            get
            {
                Matrix m = Matrix.Identity;
                m.Rotate(Rotation);
                return m;
            }
        }
        public Matrix ScaleTransform => new(
            TextHeightScaleFactor * TextWidthScaleFactor, 0,
            0, TextHeightScaleFactor,
            0, 0);
        public Matrix Transform =>
            LocalTranslationTransform * ScaleTransform * RotationTransform * WorldTranslationTransform;
        public Matrix UnrotatedTransform =>
            LocalTranslationTransform * ScaleTransform * WorldTranslationTransform;
        #endregion

        #region Methods
        public abstract void UpdateVertices(
            ResCache d3DResCache, uint layerId, uint lineTypeId, SceneIdMap sceneIdMap,
            D3dStateBuffers stateBuffers, Vector2d renderOrigin);


        protected void ApplyRotation(
            Vector2d renderOrigin,
            IEnumerable<DrawingText> targets)
        {
            if (Rotation == 0)
                return;

            Vector2d center = Position - renderOrigin;

            Matrix transform =
                new(1, 0, 0, 1, -center.X, -center.Y);

            transform *= RotationTransform;
            transform.Translate(center.X, center.Y);

            foreach (var target in targets)
            {
                for (int i = 0; i < target.TextVertices.Count; i++)
                {
                    var vertex = target.TextVertices[i];

                    vertex.Position = Vector2d.TransformCoordinate(
                        vertex.Position.ToVector2d(),
                        transform).ToSharpDXVector2();

                    target.TextVertices[i] = vertex;
                }

                for (int i = 0; i < target.LineInstances.Count; i++)
                {
                    var line = target.LineInstances[i];

                    line.Start = Vector2d.TransformCoordinate(
                        line.Start.ToVector2d(),
                        transform).ToSharpDXVector2();

                    line.End = Vector2d.TransformCoordinate(
                        line.End.ToVector2d(),
                        transform).ToSharpDXVector2();

                    target.LineInstances[i] = line;
                }
            }
        }

        protected void AddTextDecorations(
            double startX,
            double baselineY,
            double width,
            double height,
            bool underline,
            bool overline,
            bool strikethrough, uint
            layerId,
            uint objectId,
            Vector2d renderOrigin)
        {
            if (width <= 0 || height <= 0)
                return;

            if (underline)
                AddLine(baselineY - height * 0.10);

            if (overline)
                AddLine(baselineY + height * 0.90);

            if (strikethrough)
                AddLine(baselineY + height * 0.35);

            void AddLine(double y)
            {
                AddDecorationLine(
                    new Vector2d(startX, y),
                    new Vector2d(startX + width, y),
                    layerId,
                    objectId,
                    renderOrigin);
            }
        }

        protected void AddDecorationLine(
            Vector2d start,
            Vector2d end,
            uint layerId,
            uint objectId,
            Vector2d renderOrigin)
        {
            LineInstances.Add(new LineInstance(
                start.ToGpuRelative(renderOrigin),
                end.ToGpuRelative(renderOrigin),
                layerId,
                objectId,
                0,
                (uint)(LineInstanceFlags.ForceStartVisible |
                       LineInstanceFlags.ForceEndVisible),
                0));
        }

        protected System.Windows.Rect GetGeometryBounds(Vector2d renderOrigin)
        {
            System.Windows.Rect bounds = System.Windows.Rect.Empty;

            foreach (var vertex in TextVertices)
            {
                var p = vertex.Position;

                bounds.Union(new System.Windows.Point(
                    p.X + renderOrigin.X,
                    p.Y + renderOrigin.Y));
            }

            foreach (var line in LineInstances)
            {
                bounds.Union(new System.Windows.Point(
                    line.Start.X + renderOrigin.X,
                    line.Start.Y + renderOrigin.Y));

                bounds.Union(new System.Windows.Point(
                    line.End.X + renderOrigin.X,
                    line.End.Y + renderOrigin.Y));
            }

            return bounds;
        }
        #endregion
    }

    public class TextSegmentInformation
    {
        #region Properties
        public string Text { get; set; }
        public Vector4 ObjectColor { get; set; }
        public ColorType ColorType { get; set; }
        public string Font { get; set; }
        public double TextHeight { get; set; }
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public bool IsOverstriked { get; set; }
        public bool IsStrikethrough { get; set; }
        public bool IsUnderlined { get; set; }
        public bool IsNewLine { get; set; }
        public MTextAlignment TextAlignment { get; set; }

        public bool HasValue => !string.IsNullOrEmpty(Text);
        #endregion

        #region Constructors
        public TextSegmentInformation(string text = "", Vector4? objectColor = null, ColorType colorType = ColorType.ByObject, string font = "Arial",
            double textHeight = 0, bool isBold = false, bool isItalic = false, bool isUnderlined = false, bool isOverstrike = false,
            bool isStrikethrough = false, bool isNewLine = false, MTextAlignment textAlignment = MTextAlignment.Left)
        {
            Text = text;
            ObjectColor = objectColor ?? new Vector4(0, 0, 0, 1);
            ColorType = colorType;
            Font = font;
            TextHeight = textHeight;
            IsBold = isBold;
            IsItalic = isItalic;
            IsUnderlined = isUnderlined;
            IsOverstriked = isOverstrike;
            IsStrikethrough = isStrikethrough;
            IsNewLine = isNewLine;
            TextAlignment = textAlignment;
        }
        #endregion

        #region Methods
        public static TextSegmentInformation GetLineBreak()
        {
            return new TextSegmentInformation("\n");
        }
        #endregion
    }
}
