using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.LffFontRendering;
using netDxf;
using netDxf.Entities;
using PdfSharpCore.Drawing;
using SharpDX.Direct2D1;
using System.Text.RegularExpressions;
using System.Windows;

using Brush = SharpDX.Direct2D1.Brush;
using FontStyle = netDxf.Tables.FontStyle;
using Point = System.Windows.Point;
using Vector2 = SharpDX.Vector2;
using Vector3 = SharpDX.Vector3;
using Vector4 = SharpDX.Vector4;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingMtext : DrawingText
    {
        #region Properties
        public MText DxfMtext { get; set; }
        public string FontFamilyName { get; set; }
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public DrawingMtextBlock MtextBlock { get; set; }
        public bool AllowsWrapping { get; set; }

        public IEnumerable<DrawingMtextSegment> Segments =>
            MtextBlock?.Rows.SelectMany(r => r.Segments) ?? Enumerable.Empty<DrawingMtextSegment>();
        #endregion

        #region Constructor
        public DrawingMtext(MText mtext, ObjectLayer layer, Vector4 objectColor, ColorType colorType, bool allowsWrapping, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingMtext;
            DxfMtext = mtext;
            EntityObject = mtext;
            Layer = layer;
            ObjectColor = objectColor;
            ColorType = colorType;
            AllowsWrapping = allowsWrapping;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock = block;
            AttachmentPoint = TextRenderingHelpers.GetAttachmentPoint(DxfMtext.AttachmentPoint);

            UpdateColor();
            UpdateData();
        }
        #endregion

        #region Methods
        public override void UpdateVertices(ResCache resCache, uint layerId, SceneIdMap sceneIdMap, D3dStateBuffers stateBuffers)
        {
            if (DxfMtext is null) { return; }

            UpdateMtextBlock(resCache, layerId, sceneIdMap, stateBuffers);
            MtextBlock.SetTextPositions();
            MtextBlock.GetTextBox(MtextBlock.Height);
            SetRotation();
            UpdateBounds();

            TextVertices = Segments.SelectMany(s => s.TextVertices).ToList();
            LineVertices = Segments.SelectMany(s => s.LineVertices).ToList();
        }
        public override void MouseEnter()
        {
            this.IsMouseOver = true;
        }
        public override void MouseLeave()
        {
            this.IsMouseOver = false;
        }

        public override void Select()
        {
            this.IsSelected = true;
        }
        public override void Deselect()
        {
            this.IsSelected = false;
        }

        public override double DistanceToPoint(Point p)
        {
            if (MtextBlock is null || MtextBlock.Rows.Count == 0) { return double.MaxValue; }

            var vertices = MtextBlock.Rows
                .SelectMany(row => row.Segments
                .SelectMany(segment => segment.TextVertices)).ToList();

            if (vertices.Count < 3) return double.MaxValue;

            Vector2 testPoint = new((float)p.X, (float)p.Y);
            double minDistance = double.MaxValue;
            bool pointInside = false;
            object locker = new();

            Parallel.For(0, vertices.Count / 3, (i, state) =>
            {
                if (pointInside) return;

                Vector2 v0 = vertices[i * 3 + 0].Position.ToSharpDXVector2();
                Vector2 v1 = vertices[i * 3 + 1].Position.ToSharpDXVector2();
                Vector2 v2 = vertices[i * 3 + 2].Position.ToSharpDXVector2();

                if (MathHelpers.IsPointInTriangle(testPoint, v0, v1, v2))
                {
                    lock (locker)
                    {
                        pointInside = true;
                        minDistance = 0.0;
                    }

                    state.Stop();
                }
                else
                {
                    double dist = MathHelpers.DistanceToTriangle(testPoint, v0, v1, v2);

                    lock (locker)
                    {
                        if (dist < minDistance)
                            minDistance = dist;
                    }
                }
            });

            return minDistance;
        }

        public override void UpdateData()
        {
            if (EntityObject is MText mText)
            {
                DxfMtext = mText;
                Text = mText.Value;
                MaxWidth = (float)mText.RectangleWidth;
                Position = new((float)mText.Position.X, (float)mText.Position.Y, 0);
                Rotation = (float)mText.Rotation;
                UpdateBounds();
                IsBold = mText.Style.FontStyle == FontStyle.Bold;
                IsItalic = mText.Style.FontStyle == FontStyle.Italic;
                TextHeight = (float)mText.Height;

                var dxfFontFamilyName = mText.Style.FontFamilyName;
                if (string.IsNullOrWhiteSpace(dxfFontFamilyName))
                {
                    dxfFontFamilyName = mText.Style.FontFile;
                }

                (FontFamilyName, TextRenderStyle) = AutoCadFontResolver.Resolve(dxfFontFamilyName);
            }
            else
            {
                throw new ArgumentException("EntityObject must be of type MText or Text");
            }
        }
        public override void DrawToD2dDeviceContext(DeviceContext1 deviceContext, Factory2 factory, Brush brush, float thickness, StrokeStyle1 strokeStyle)
        {
            //deviceContext.DrawTextLayout(new RawVector2((float)Position.X, -(float)Position.Y), TextLayout, brush);
        }
        public override void DrawToPdf(XGraphics gfx, System.Windows.Media.Matrix worldToPdf, XPen pen)
        {
            if (MtextBlock is null || MtextBlock.Rows.Count == 0) { return; }

            double ptsPerWorld = PdfDrawingHelpers.WorldToPdfScale(worldToPdf);
            if (ptsPerWorld <= 0.000001) { return; }

            double rotDeg = -Rotation;
            var basePdf = PdfDrawingHelpers.WorldToPdf(new Vector2(Position.X, Position.Y), worldToPdf);

            var state = gfx.Save();
            if (Math.Abs(rotDeg) > 0.0001) { gfx.RotateAtTransform(rotDeg, basePdf); }

            var rows = MtextBlock.Rows.ToList();

            foreach (var row in rows)
            {
                var segments = row.Segments.ToList();

                foreach (var seg in segments)
                {
                    seg.DrawToPdf(gfx, worldToPdf, pen);
                }
            }
            gfx.Restore(state);
        }

        public override void UpdateBounds()
        {
            if (MtextBlock is null) { return; } // No text to update bounds for.

            Bounds = Rect.Empty;

            foreach (var segment in Segments)
            {
                segment.UpdateBounds();
                Bounds = Rect.Union(Bounds, segment.Bounds);
            }
        }

        public void UpdateMtextBlock(ResCache resCache, uint layerId, SceneIdMap sceneIdMap, D3dStateBuffers stateBuffers)
        {
            MtextBlock?.Dispose();
            MtextBlock = new((float)MaxWidth, Position, DxfMtext.AttachmentPoint, Rotation, AllowsWrapping);

            string rawText = DxfMtext.Value;
            Vector4 baseColor = ObjectColor;

            var texts = rawText.Split(new[] { '{', '}' }, StringSplitOptions.RemoveEmptyEntries);

            // Regex patterns for DXF formatting
            string aciColorPattern = @"\\[C](\d+);";
            string trueTypeColorPattern = @"\\[c](\d+);";
            string fontPattern = @"\\[fF]([^;]+);";
            string heightPattern = @"\\H([\d.]+)x?;";
            string lineBreakPattern = @"\\P";
            string underlineStartPattern = @"\\L";
            string underlineEndPattern = @"\\l";
            string overstrikeStartPattern = @"\\O";
            string overstrikeEndPattern = @"\\o";
            string strikethroughStartPattern = @"\\K";
            string strikethroughEndPattern = @"\\k";
            string paraAlignLeftPattern = @"\\pxql;";
            string paraAlignCenterPattern = @"\\pxqc;";
            string paraAlignRightPattern = @"\\pxqr;";
            string paraAlignJustifyPattern = @"\\pxqj;";
            string paraAlignDistributedPattern = @"\\pxqd;";
            string paragraphIndentPattern = @"\\pi([\d.]+);";
            string alignmentPattern = @"\\A([012]);";
            string paragraphPropertiesPattern = @"\\pxi[-\d.]+,l[-\d.]+,t[-\d.]+;";

            string pattern = $@"((\\[LOkoK])|{aciColorPattern}|{trueTypeColorPattern}|{fontPattern}|{heightPattern}|{lineBreakPattern}|{paragraphIndentPattern}|{underlineStartPattern}|{underlineEndPattern}|{overstrikeStartPattern}|{overstrikeEndPattern}|{strikethroughStartPattern}|{strikethroughEndPattern}|{paraAlignLeftPattern}|{paraAlignCenterPattern}|{paraAlignRightPattern}|{paraAlignJustifyPattern}|{paraAlignDistributedPattern}|{alignmentPattern}|{paragraphPropertiesPattern}|[^{{}}\\]+)";

            Enums.TextAlignment baseAlignment;
            if (AttachmentPoint == Enums.TextAttachmentPoint.TopRight ||
                AttachmentPoint == Enums.TextAttachmentPoint.MiddleRight ||
                AttachmentPoint == Enums.TextAttachmentPoint.BottomRight)
            { baseAlignment = Enums.TextAlignment.Right; }
            else if (AttachmentPoint == Enums.TextAttachmentPoint.TopCenter ||
                AttachmentPoint == Enums.TextAttachmentPoint.MiddleCenter ||
                AttachmentPoint == Enums.TextAttachmentPoint.BottomCenter)
            { baseAlignment = Enums.TextAlignment.Center; }
            else { baseAlignment = Enums.TextAlignment.Left; }

            foreach (var text in texts)
            {
                TextSegmentInformation currentSegment = new("", baseColor, ColorType, FontFamilyName, DxfMtext.Height,
                    IsBold, IsItalic, false, false, false, false, baseAlignment);
                List<TextSegmentInformation> textSegments = [];
                MatchCollection matches = Regex.Matches(text, pattern);

                foreach (Match match in matches)
                {
                    string value = match.Value;

                    if (string.IsNullOrWhiteSpace(value)) { continue; }

                    if (Regex.IsMatch(value, aciColorPattern))
                    {
                        int colorI = int.Parse(Regex.Match(value, aciColorPattern).Groups[1].Value);

                        if (colorI == 0)
                        {
                            if (IsPartOfBlock && DrawingBlock is not null)
                            {
                                currentSegment.ObjectColor = DxfHelpers.GetDrawingObjectColor(DrawingBlock);

                                currentSegment.ObjectColor = DrawingBlock.ObjectColor;
                                currentSegment.ColorType = DrawingBlock.ColorType;
                            }
                            else
                            {
                                var aciColor = AciColor.Default;
                                currentSegment.ObjectColor = new(aciColor.R / 255.0f, aciColor.G / 255.0f, aciColor.B / 255.0f, 1.0f);
                            }
                        }
                        else if (colorI == 256)
                        {
                            currentSegment.ObjectColor = Layer.Color;
                            currentSegment.ColorType = ColorType.ByLayer;
                        }
                        else
                        {
                            var vector = AutoCadColorConverter.ConvertACINumberToRGBA((short)colorI);
                            currentSegment.ObjectColor = new((float)vector.X, (float)vector.Y, (float)vector.Z, (float)vector.W);
                            currentSegment.ColorType = ColorType.ByObject;
                        }
                    }
                    else if (Regex.IsMatch(value, trueTypeColorPattern))
                    {
                        int colorI = int.Parse(Regex.Match(value, trueTypeColorPattern).Groups[1].Value);
                        var trueTypeColor = AutoCadColorConverter.ConvertTrueColorToVector4(colorI);

                        currentSegment.ObjectColor = new((float)trueTypeColor.X, (float)trueTypeColor.Y, (float)trueTypeColor.Z, (float)trueTypeColor.W);
                        currentSegment.ColorType = ColorType.ByObject;
                    }
                    else if (Regex.IsMatch(value, fontPattern, RegexOptions.IgnoreCase))
                    {
                        string fontSpec = Regex.Match(value, fontPattern).Groups[1].Value;
                        var parts = fontSpec.Split('|', StringSplitOptions.RemoveEmptyEntries);
                        currentSegment.Font = parts[0];

                        for (int pi = 1; pi < parts.Length; pi++)
                        {
                            string p = parts[pi];

                            // bold
                            if (p.Length >= 2 && p[0] == 'b') { currentSegment.IsBold = p[1] == '1'; }

                            // italic
                            else if (p.Length >= 2 && p[0] == 'i') { currentSegment.IsItalic = p[1] == '1'; }
                        }
                    }
                    else if (Regex.IsMatch(value, heightPattern))
                    {
                        currentSegment.TextHeight *= double.Parse(Regex.Match(value, heightPattern).Groups[1].Value);
                    }
                    else if (Regex.IsMatch(value, lineBreakPattern))
                    {
                        currentSegment.IsNewLine = true;
                    }
                    else if (Regex.IsMatch(value, paragraphIndentPattern)) { }
                    else if (Regex.IsMatch(value, underlineStartPattern))
                    {
                        currentSegment.IsUnderlined = true;
                    }
                    else if (Regex.IsMatch(value, underlineEndPattern))
                    {
                        currentSegment.IsUnderlined = false;
                    }
                    else if (Regex.IsMatch(value, overstrikeStartPattern))
                    {
                        currentSegment.IsOverstriked = true;
                    }
                    else if (Regex.IsMatch(value, overstrikeEndPattern))
                    {
                        currentSegment.IsOverstriked = false;
                    }
                    else if (Regex.IsMatch(value, strikethroughStartPattern))
                    {
                        currentSegment.IsStrikethrough = true;
                    }
                    else if (Regex.IsMatch(value, strikethroughEndPattern))
                    {
                        currentSegment.IsStrikethrough = false;
                    }
                    else if (Regex.IsMatch(value, paraAlignLeftPattern))
                    {
                        currentSegment.TextAlignment = Enums.TextAlignment.Left;
                    }
                    else if (Regex.IsMatch(value, paraAlignCenterPattern))
                    {
                        currentSegment.TextAlignment = Enums.TextAlignment.Center;
                    }
                    else if (Regex.IsMatch(value, paraAlignRightPattern))
                    {
                        currentSegment.TextAlignment = Enums.TextAlignment.Right;
                    }
                    else if (Regex.IsMatch(value, paraAlignJustifyPattern))
                    {
                        currentSegment.TextAlignment = Enums.TextAlignment.Justified;
                    }
                    else if (Regex.IsMatch(value, paraAlignDistributedPattern))
                    {
                        currentSegment.TextAlignment = Enums.TextAlignment.Distributed;
                    }
                    else if (Regex.IsMatch(value, alignmentPattern))
                    {
                        int alignment = int.Parse(Regex.Match(value, alignmentPattern).Groups[1].Value);
                    }
                    else if (Regex.IsMatch(value, paragraphPropertiesPattern))
                    {
                    }
                    else
                    {
                        currentSegment.Text += value;

                        if (currentSegment.HasValue) { textSegments.Add(currentSegment); }

                        currentSegment = new("", currentSegment.ObjectColor, currentSegment.ColorType, currentSegment.Font, currentSegment.TextHeight,
                            currentSegment.IsBold, currentSegment.IsItalic, currentSegment.IsUnderlined, currentSegment.IsOverstriked,
                            currentSegment.IsStrikethrough, currentSegment.IsNewLine, currentSegment.TextAlignment);

                        currentSegment.IsNewLine = false;
                    }
                }

                Vector3 basePosition = Position;

                foreach (var segmentInfo in textSegments)
                {
                    string[] segmentTexts;

                    if (AllowsWrapping)
                    {
                        segmentTexts = SplitIntoWordsPreservingSpaces(segmentInfo.Text).ToArray();
                    }
                    else
                    {
                        segmentTexts = [segmentInfo.Text];
                    }

                    if (segmentTexts.Length > 1)
                    {
                        for (int i = 0; i < segmentTexts.Length; i++)
                        {
                            var segmentText = segmentTexts[i];
                            bool isNewLine = segmentInfo.IsNewLine;

                            if (i != 0) { isNewLine = false; }

                            var newSegmentInfo = new TextSegmentInformation(segmentText, segmentInfo.ObjectColor, segmentInfo.ColorType, segmentInfo.Font, segmentInfo.TextHeight,
                                segmentInfo.IsBold, segmentInfo.IsItalic, segmentInfo.IsUnderlined, segmentInfo.IsOverstriked, segmentInfo.IsStrikethrough,
                                isNewLine, segmentInfo.TextAlignment);
                            var newSegment = CreateMtextSegment(newSegmentInfo, resCache, layerId, sceneIdMap, stateBuffers);

                            MtextBlock.AddSegment(newSegment);
                        }
                    }
                    else
                    {
                        var segment = CreateMtextSegment(segmentInfo, resCache, layerId, sceneIdMap, stateBuffers);
                        MtextBlock.AddSegment(segment);
                    }
                }
            }
        }
        private void SetRotation()
        {
            foreach (var segment in Segments)
            {
                for (int i = 0; i < segment.TextVertices.Count; i++)
                {
                    segment.TextVertices[i] = TextVertex.RotateAroundPoint(segment.TextVertices[i], new Vector2(Position.X, Position.Y), (float)(MathHelper.DegToRad * Rotation));
                }
            }
        }
        private DrawingMtextSegment CreateMtextSegment(
            TextSegmentInformation segmentInfo,
            ResCache resCache,
            uint layerId,
            SceneIdMap sceneIdMap,
            D3dStateBuffers stateBuffers)
        {
            (var fontFamily, var renderStyle) = AutoCadFontResolver.Resolve(segmentInfo.Font);

            DrawingMtextSegment segment = new(this, Layer, segmentInfo.Text, segmentInfo.ObjectColor, segmentInfo.ColorType,
                Vector3.Zero, 0, (float)segmentInfo.TextHeight, fontFamily, segmentInfo.IsItalic, segmentInfo.IsBold,
                segmentInfo.IsUnderlined, segmentInfo.IsStrikethrough, segmentInfo.IsOverstriked, segmentInfo.IsNewLine,
                0, renderStyle, segmentInfo.TextAlignment, IsPartOfBlock, DrawingBlock);

            segment.UpdateVertices(resCache, layerId, sceneIdMap, stateBuffers);

            return segment;
        }

        private static IEnumerable<string> SplitIntoWordsPreservingSpaces(string text)
        {
            return Regex.Matches(text, @"\S+\s*")
                .Cast<Match>()
                .Select(m => m.Value);
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
        public Enums.TextAlignment TextAlignment { get; set; }

        public bool HasValue => !string.IsNullOrEmpty(Text);
        #endregion

        #region Constructors
        public TextSegmentInformation(string text = "", Vector4? objectColor = null, ColorType colorType = ColorType.ByObject, string font = "Arial",
            double textHeight = 0, bool isBold = false, bool isItalic = false, bool isUnderlined = false, bool isOverstrike = false,
            bool isStrikethrough = false, bool isNewLine = false, Enums.TextAlignment textAlignment = Enums.TextAlignment.Left)
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
