using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Services.Exporting;
using netDxf;
using netDxf.Entities;
using PdfSharpCore.Drawing;
using SharpDX.Direct2D1;
using System.Collections.Generic;
using System.Diagnostics;
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
        #region Fields
        private const int _fontRenderingMinimumSize = 50;
        #endregion

        #region Properties
        public MText DxfMtext { get; set; }
        public float Rotation { get; set; } = 0;
        public float FontHeight { get; set; }
        public string FontFamilyName { get; set; }
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public Enums.TextAttachmentPoint AttachmentPoint { get; set; }
        public DrawingMtextBlock MtextBlock { get; set; }
        public Vector3 TextAttachmentOffset { get; set; } = Vector3.Zero;

        public override List<TextVertex> TextVertices { get; set; } = [];
        #endregion

        #region Constructor
        public DrawingMtext(MText mtext, ObjectLayer layer, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingMtext;
            DxfMtext = mtext;
            EntityObject = mtext;
            Layer = layer;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock3D = block;
            ColorByLayer = EntityObject.Color.IsByLayer;
            AttachmentPoint = TextRenderingHelpers.GetAttachmentPoint(DxfMtext.AttachmentPoint);

            UpdateColor();
            UpdateData();
        }
        #endregion

        #region Methods
        public override void UpdateTextVertices(ResCache resCache, uint layerId, SceneIdMap sceneIdMap, D3dStateBuffers stateBuffers)
        {
            if (DxfMtext is null) { return; }

            UpdateMtextBlock(resCache, layerId, sceneIdMap, stateBuffers);
            MtextBlock.SetTextPositions();
            MtextBlock.GetTextBox(MtextBlock.Height);
            SetRotation();
            UpdateBounds();
            TextVertices = MtextBlock.Rows.SelectMany(r => r.Segments).SelectMany(s => s.TextVertices).ToList();
        }
        public override void MouseEnter()
        {
            this.IsMouseOver = true;
            SetMouseOver(true);
        }
        public override void MouseLeave()
        {
            this.IsMouseOver = false;
            SetMouseOver(false);
        }
        private void SetMouseOver(bool isMouseOver)
        {
            foreach (var row in MtextBlock.Rows)
            {
                foreach (var segment in row.Segments)
                {
                    Span<TextVertex> vertexSpan = segment.TextVertices.AsSpan();

                    for (int i = 0; i < vertexSpan.Length; i++)
                    {
                        vertexSpan[i].SetIsMouseOver(isMouseOver);
                    }
                }
            }
        }
        public override void Select()
        {
            this.IsSelected = true;
            SetIsSelected(true);
        }
        public override void Deselect()
        {
            this.IsSelected = false;
            SetIsSelected(false);
        }

        private void SetIsSelected(bool isSelected)
        {
            foreach (var row in MtextBlock.Rows)
            {
                foreach (var segment in row.Segments)
                {
                    Span<TextVertex> vertexSpan = segment.TextVertices.AsSpan();

                    for (int i = 0; i < vertexSpan.Length; i++)
                    {
                        vertexSpan[i].SetIsSelected(isSelected);
                    }
                }
            }
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
                FontHeight = (float)mText.Height;
                FontFamilyName = mText.Style.FontFamilyName;
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
                    if (string.IsNullOrWhiteSpace(seg.Text)) { continue; }

                    var fontFamily = PdfDrawingHelpers.GetFontFamily(seg.FontFamilyName);

                    XFontStyle style = XFontStyle.Regular;
                    if (seg.IsBold) { style |= XFontStyle.Bold; }
                    if (seg.IsItalic) { style |= XFontStyle.Italic; }

                    var fontSizePts = seg.TextHeight * seg.FontSizeFactor * worldToPdf.M11;
                    var font = new XFont(fontFamily, fontSizePts, style);
                    var brush = new XSolidBrush(PdfTransform.ToXColor(seg.Color.ToVector4()));

                    var pPdf = PdfDrawingHelpers.WorldToPdf(new Vector2(seg.Position.X, seg.Position.Y), worldToPdf);
                    var size = gfx.MeasureString(seg.Text, font);

                    double x = pPdf.X;
                    double y = pPdf.Y;

                    gfx.DrawString(seg.Text, font, brush, new XPoint(x, y));

                    if (seg.IsUnderlined || seg.IsStrikeOut || seg.IsOverStrike)
                    {
                        var linePen = new XPen(brush.Color, fontSizePts * 0.01);

                        var m = font.Metrics;
                        double em = m.UnitsPerEm;

                        // Convert design-units metrics to points
                        double ascentPt = fontSizePts * (m.Ascent / em);
                        double descentPt = fontSizePts * (m.Descent / em);

                        // If your y is baseline: (common in PDF libs)
                        double baselineY = y;

                        // Typical positions (tweak constants to match AutoCAD)
                        double underlineY = baselineY + descentPt * 0.95;      // slightly below baseline
                        double strikeY = baselineY - ascentPt * 0.35;      // around x-height-ish
                        double overstrikeY = baselineY - ascentPt * 0.95;      // near top

                        if (seg.IsUnderlined)
                        {
                            gfx.DrawLine(linePen, x, underlineY, x + size.Width, underlineY);
                        }
                        if (seg.IsStrikeOut)
                        {
                            gfx.DrawLine(linePen, x, strikeY, x + size.Width, strikeY);
                        }
                        if (seg.IsOverStrike)
                        {
                            gfx.DrawLine(linePen, x, overstrikeY, x + size.Width, overstrikeY);
                        }
                    }
                }
            }
            gfx.Restore(state);
        }

        public override void UpdateBounds()
        {
            if (MtextBlock is null) { return; } // No text to update bounds for.

            Bounds = Rect.Empty;
            foreach (var row in MtextBlock.Rows)
            {
                foreach (var segment in row.Segments)
                {
                    for (int i = 0; i < segment.TextVertices.Length; i++)
                    {
                        Bounds = Rect.Union(Bounds, (Point)segment.TextVertices[i]);
                    }
                }
            }
        }

        public List<TextVertex> GetTextVertices()
        {
            List<TextVertex> textVertices = [];
            foreach (var row in MtextBlock.Rows)
            {
                foreach (var segment in row.Segments)
                {
                    textVertices.AddRange(segment.TextVertices);
                }
            }

            return textVertices;
        }
        public void UpdateMtextBlock(ResCache resCache, uint layerId, SceneIdMap sceneIdMap, D3dStateBuffers stateBuffers)
        {
            MtextBlock?.Dispose();
            MtextBlock = new((float)MaxWidth, Position, DxfMtext.AttachmentPoint, Rotation);

            string rawText = DxfMtext.Value;

            if (!rawText.Contains('\\'))
            {
                var segmentTexts = rawText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var text in segmentTexts)
                {
                    Enums.TextAlignment alignment;
                    if (AttachmentPoint == Enums.TextAttachmentPoint.TopRight ||
                        AttachmentPoint == Enums.TextAttachmentPoint.MiddleRight ||
                        AttachmentPoint == Enums.TextAttachmentPoint.BottomRight)
                    { alignment = Enums.TextAlignment.Right; }
                    else if (AttachmentPoint == Enums.TextAttachmentPoint.TopCenter ||
                        AttachmentPoint == Enums.TextAttachmentPoint.MiddleCenter ||
                        AttachmentPoint == Enums.TextAttachmentPoint.BottomCenter)
                    { alignment = Enums.TextAlignment.Center; }
                    else { alignment = Enums.TextAlignment.Left; }

                    TextSegmentInformation segmentInfo = new(text, Color, FontFamilyName, DxfMtext.Height,
                        IsBold, IsItalic, false, false, false, false, alignment);
                    var textSegment = CreateMtextSegment(segmentInfo, resCache, layerId, sceneIdMap, stateBuffers);
                    MtextBlock.AddSegment(textSegment);
                }
                return;
            }

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
            string alignLeftPattern = @"\\pxql;";
            string alignCenterPattern = @"\\pxqc;";
            string alignRightPattern = @"\\pxqr;";
            string alignJustifyPattern = @"\\pxqj;";
            string alignDistributedPattern = @"\\pxqd;";
            string paragraphIndentPattern = @"\\pi([\d.]+);";

            string pattern = $@"((\\[LOkoK])|{aciColorPattern}|{trueTypeColorPattern}|{fontPattern}|{heightPattern}|{lineBreakPattern}|{paragraphIndentPattern}|{underlineStartPattern}|{underlineEndPattern}|{overstrikeStartPattern}|{overstrikeEndPattern}|{strikethroughStartPattern}|{strikethroughEndPattern}|{alignLeftPattern}|{alignCenterPattern}|{alignRightPattern}|{alignJustifyPattern}|{alignDistributedPattern}|[^{{}}\\]+)";

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

            TextSegmentInformation currentSegment = new("", Color, FontFamilyName, DxfMtext.Height, IsBold, IsItalic, false, false, false, false, baseAlignment);

            foreach (var text in texts)
            {
                List<TextSegmentInformation> textSegments = [];
                MatchCollection matches = Regex.Matches(text, pattern);
                currentSegment.Color = Color;

                foreach (Match match in matches)
                {
                    string value = match.Value;

                    if (string.IsNullOrWhiteSpace(value)) { continue; }

                    if (Regex.IsMatch(value, aciColorPattern))
                    {
                        int colorI = int.Parse(Regex.Match(value, aciColorPattern).Groups[1].Value);

                        if (colorI == 0)
                        {
                            if (IsPartOfBlock)
                            {
                                currentSegment.Color = DrawingBlock3D.Color;
                            }
                            else
                            {
                                var aciColor = AciColor.Default;
                                currentSegment.Color = new(aciColor.R / 255.0f, aciColor.G / 255.0f, aciColor.B / 255.0f, 1.0f);
                            }
                        }
                        else if (colorI == 256)
                        {
                            currentSegment.Color = Layer.Color;
                        }
                        else
                        {
                            var vector = AutoCadColorConverter.ConvertACINumberToRGBA((short)colorI);
                            currentSegment.Color = new((float)vector.X, (float)vector.Y, (float)vector.Z, (float)vector.W);
                        }
                    }
                    else if (Regex.IsMatch(value, trueTypeColorPattern))
                    {
                        int colorI = int.Parse(Regex.Match(value, trueTypeColorPattern).Groups[1].Value);
                        var trueTypeColor = AutoCadColorConverter.ConvertTrueColorToVector4(colorI);

                        currentSegment.Color = new((float)trueTypeColor.X, (float)trueTypeColor.Y, (float)trueTypeColor.Z, (float)trueTypeColor.W);
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
                    else if (Regex.IsMatch(value, alignLeftPattern))
                    {
                        currentSegment.TextAlignment = Enums.TextAlignment.Left;
                    }
                    else if (Regex.IsMatch(value, alignCenterPattern))
                    {
                        currentSegment.TextAlignment = Enums.TextAlignment.Center;
                    }
                    else if (Regex.IsMatch(value, alignRightPattern))
                    {
                        currentSegment.TextAlignment = Enums.TextAlignment.Right;
                    }
                    else if (Regex.IsMatch(value, alignJustifyPattern))
                    {
                        currentSegment.TextAlignment = Enums.TextAlignment.Justified;
                    }
                    else if (Regex.IsMatch(value, alignDistributedPattern))
                    {
                        currentSegment.TextAlignment = Enums.TextAlignment.Distributed;
                    }
                    else
                    {
                        currentSegment.Text += value;
                        currentSegment.Text = currentSegment.Text.TrimEnd();
                        if (currentSegment.HasValue) { textSegments.Add(currentSegment); }

                        currentSegment = new("", currentSegment.Color, currentSegment.Font, currentSegment.TextHeight, currentSegment.IsBold,
                            currentSegment.IsItalic, currentSegment.IsUnderlined, currentSegment.IsOverstriked, currentSegment.IsStrikethrough,
                            currentSegment.IsNewLine, currentSegment.TextAlignment);

                        currentSegment.IsNewLine = false;
                    }
                }

                Vector3 basePosition = Position;

                foreach (var segmentInfo in textSegments)
                {
                    var segmentTexts = segmentInfo.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (segmentTexts.Length > 1)
                    {
                        for (int i = 0; i < segmentTexts.Length; i++)
                        {
                            var segmentText = segmentTexts[i];
                            bool isNewLine = segmentInfo.IsNewLine;

                            if (i != 0) { isNewLine = false; }

                            var newSegmentInfo = new TextSegmentInformation(segmentText, segmentInfo.Color, segmentInfo.Font, segmentInfo.TextHeight,
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
            foreach (var row in MtextBlock.Rows)
            {
                foreach (var segment in row.Segments)
                {
                    for (int i = 0; i < segment.TextVertices.Length; i++)
                    {
                        segment.TextVertices[i] = TextVertex.RotateAroundPoint(segment.TextVertices[i], new Vector2(Position.X, Position.Y), (float)(MathHelper.DegToRad * Rotation));
                    }
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
            DrawingMtextSegment segment = new(this, segmentInfo.Text, segmentInfo.Color, Vector3.Zero, 0,
                (float)segmentInfo.TextHeight, segmentInfo.Font, segmentInfo.IsItalic, segmentInfo.IsBold,
                segmentInfo.IsUnderlined, segmentInfo.IsStrikethrough, segmentInfo.IsOverstriked, segmentInfo.IsNewLine,
                _fontRenderingMinimumSize, 0, segmentInfo.TextAlignment);

            var objectId = sceneIdMap.GetOrAddObjectId(segment, out bool isNewObj);

            if (isNewObj) { stateBuffers.InitializeObjectState(sceneIdMap.MaxObjectId, segment, objectId); }
            segment.GetTextLayout(resCache.WriteFactory);
            segment.Tesselate(resCache, layerId, objectId);

            return segment;
        }
        #endregion
    }

    public class TextSegmentInformation
    {
        #region Properties
        public string Text { get; set; }
        public Vector4 Color { get; set; }
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
        public TextSegmentInformation(string text = "", Vector4? color = null, string font = "Arial", double textHeight = 0, bool isBold = false,
            bool isItalic = false, bool isUnderlined = false, bool isOverstrike = false, bool isStrikethrough = false, bool isNewLine = false,
            Enums.TextAlignment textAlignment = Enums.TextAlignment.Left)
        {
            Text = text;
            Color = color ?? new Vector4(0, 0, 0, 1);
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
