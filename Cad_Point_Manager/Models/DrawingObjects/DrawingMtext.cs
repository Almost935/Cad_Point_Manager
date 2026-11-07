using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Helpers;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Msdf;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.DrawingObjects.HelperClasses;
using Cad_Point_Manager.Models.LffFontRendering;
using Cad_Point_Manager.Services.Exporting;
using netDxf;
using netDxf.Entities;
using PdfSharpCore.Drawing;
using SharpDX.Direct2D1;
using System.Text.RegularExpressions;
using System.Windows;
using Brush = SharpDX.Direct2D1.Brush;
using FontStyle = netDxf.Tables.FontStyle;
using MTextAlignment = Cad_Point_Manager.Common.MTextAlignment;
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
        public MTextAttachment AttachmentPoint { get; set; }
        public MTextAlignment TextAlignment { get; set; } = MTextAlignment.Left;

        public IEnumerable<DrawingMtextSegment> Segments =>
            MtextBlock?.Rows.SelectMany(r => r.Segments) ?? Enumerable.Empty<DrawingMtextSegment>();
        #endregion

        #region Constructor
        public DrawingMtext(MText mtext, ObjectLayer layer, Vector4 objectColor, ColorType colorType, LineType lineType,
            bool allowsWrapping, Common.MTextAttachment attachmentPoint, MTextAlignment textAlignment = MTextAlignment.Left,
            bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingMtext;
            DxfMtext = mtext;
            EntityObject = mtext;
            Layer = layer;
            ObjectColor = objectColor;
            ColorType = colorType;
            LineType = lineType;
            AllowsWrapping = allowsWrapping;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock = block;
            AttachmentPoint = attachmentPoint;
            TextAlignment = textAlignment;

            UpdateColor();
            UpdateData();
        }
        #endregion

        #region Methods
        public override void UpdateVertices(
            ResCache resCache, uint layerId, uint lineTypeId,
            SceneIdMap sceneIdMap, D3dStateBuffers stateBuffers,
            Vector2d renderOrigin)
        {
            if (DxfMtext is null)
                return;

            UpdateMtextBlock(resCache, layerId, lineTypeId,
                sceneIdMap, stateBuffers, renderOrigin);

            MtextBlock.SetTextPositions();
            MtextBlock.GetTextBox(MtextBlock.Height);

            foreach (var segment in Segments)
            {
                uint objectId = sceneIdMap.GetOrAddObjectId(
                    segment, out _);

                segment.GenerateDecorations(
                    layerId, objectId, renderOrigin);
            }

            ApplyRotation(renderOrigin, Segments);

            UpdateBounds(renderOrigin);

            TextVertices = Segments.SelectMany(s => s.TextVertices).ToList();
            LineInstances = Segments.SelectMany(s => s.LineInstances).ToList();
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

        public override double DistanceToPoint(Vector2d p, MsdfAtlas atlas = null)
        {
            if (MtextBlock is null || MtextBlock.Rows.Count == 0) { return double.MaxValue; }

            var vertices = MtextBlock.Rows
                .SelectMany(row => row.Segments
                .SelectMany(segment => segment.TextVertices)).ToList();

            if (vertices.Count < 3) return double.MaxValue;

            double minDistance = double.MaxValue;
            bool pointInside = false;
            object locker = new();

            Parallel.For(0, vertices.Count / 3, (i, state) =>
            {
                if (pointInside) return;

                Vector2d v0 = vertices[i * 3 + 0].Position.ToVector2d();
                Vector2d v1 = vertices[i * 3 + 1].Position.ToVector2d();
                Vector2d v2 = vertices[i * 3 + 2].Position.ToVector2d();

                if (MathHelpers.IsPointInTriangle(p, v0, v1, v2))
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
                    double dist = MathHelpers.DistanceToTriangle(p, v0, v1, v2);

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
                Position = mText.Position.ToVector2d();
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

        public override void DrawToPdf(PdfRenderContext context, XPen pen)
        {
            if (MtextBlock is null || MtextBlock.Rows.Count == 0) { return; }

            double ptsPerWorld = PdfDrawingHelpers.WorldToPdfScale(context.WorldToPdf);
            if (ptsPerWorld <= 0.000001) { return; }

            double rotDeg = -Rotation;
            var basePdf = PdfDrawingHelpers.WorldToPdf(Position, context.WorldToPdf);

            var state = context.Graphics.Save();
            if (Math.Abs(rotDeg) > 0.0001) { context.Graphics.RotateAtTransform(rotDeg, basePdf); }

            var rows = MtextBlock.Rows.ToList();

            foreach (var row in rows)
            {
                var segments = row.Segments.ToList();

                foreach (var seg in segments)
                {
                    seg.DrawToPdf(context, pen);
                }
            }
            context.Graphics.Restore(state);
        }

        public override void UpdateBounds()
        {
            Rect bounds = Rect.Empty;

            foreach (var segment in Segments)
            {
                bounds.Union(segment.Bounds);
            }

            Bounds = bounds;
        }
        public void UpdateBounds(Vector2d renderOrigin)
        {
            Rect bounds = Rect.Empty;

            foreach (var segment in Segments)
            {
                bounds.Union(segment.GetMtextBounds(renderOrigin));
            }

            Bounds = bounds;
        }

        public void UpdateMtextBlock(
            ResCache resCache, uint layerId, uint lineTypeId, SceneIdMap sceneIdMap,
            D3dStateBuffers stateBuffers, Vector2d renderOrigin)
        {
            MtextBlock?.Dispose();
            MtextBlock = new(MaxWidth, Position, DxfMtext.AttachmentPoint, Rotation, AllowsWrapping);

            string rawText = DecodeSpecialCharacters(DxfMtext.Value);
            Vector4 baseColor = ObjectColor;

            var texts = rawText.Split(
                new[] { '{', '}' },
                StringSplitOptions.RemoveEmptyEntries);

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

            MTextAlignment baseAlignment;
            if (AttachmentPoint == Common.MTextAttachment.TopRight ||
                AttachmentPoint == Common.MTextAttachment.MiddleRight ||
                AttachmentPoint == Common.MTextAttachment.BottomRight)
            { baseAlignment = MTextAlignment.Right; }
            else if (AttachmentPoint == Common.MTextAttachment.TopCenter ||

                AttachmentPoint == Common.MTextAttachment.MiddleCenter ||
                AttachmentPoint == Common.MTextAttachment.BottomCenter)
            { baseAlignment = MTextAlignment.Center; }
            else { baseAlignment = MTextAlignment.Left; }

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
                        currentSegment.TextAlignment = MTextAlignment.Left;
                    }
                    else if (Regex.IsMatch(value, paraAlignCenterPattern))
                    {
                        currentSegment.TextAlignment = MTextAlignment.Center;
                    }
                    else if (Regex.IsMatch(value, paraAlignRightPattern))
                    {
                        currentSegment.TextAlignment = MTextAlignment.Right;
                    }
                    else if (Regex.IsMatch(value, paraAlignJustifyPattern))
                    {
                        currentSegment.TextAlignment = MTextAlignment.Justified;
                    }
                    else if (Regex.IsMatch(value, paraAlignDistributedPattern))
                    {
                        currentSegment.TextAlignment = MTextAlignment.Distributed;
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
                            currentSegment.IsStrikethrough, currentSegment.IsNewLine, currentSegment.TextAlignment)
                        {
                            IsNewLine = false
                        };
                    }
                }

                Vector2d basePosition = Position;

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
                            var newSegment = CreateMtextSegment(
                                newSegmentInfo, resCache, layerId, lineTypeId, sceneIdMap, stateBuffers, renderOrigin);

                            MtextBlock.AddSegment(newSegment);
                        }
                    }
                    else
                    {
                        var segment = CreateMtextSegment(
                            segmentInfo, resCache, layerId, lineTypeId, sceneIdMap, stateBuffers, renderOrigin);
                        MtextBlock.AddSegment(segment);
                    }
                }
            }
        }

        private DrawingMtextSegment CreateMtextSegment(
            TextSegmentInformation segmentInfo,
            ResCache resCache,
            uint layerId,
            uint lineTypeId,
            SceneIdMap sceneIdMap,
            D3dStateBuffers stateBuffers,
            Vector2d renderOrigin)
        {
            (var fontFamily, var renderStyle) = AutoCadFontResolver.Resolve(segmentInfo.Font);

            DrawingMtextSegment segment = new(this, Layer, segmentInfo.Text, segmentInfo.ObjectColor, segmentInfo.ColorType,
                LineType, Vector2d.Zero, 0, (float)segmentInfo.TextHeight, fontFamily, segmentInfo.IsItalic, segmentInfo.IsBold,
                segmentInfo.IsUnderlined, segmentInfo.IsStrikethrough, segmentInfo.IsOverstriked, segmentInfo.IsNewLine,
                0, renderStyle, segmentInfo.TextAlignment, IsPartOfBlock, DrawingBlock);

            segment.UpdateVertices(resCache, layerId, lineTypeId, sceneIdMap, stateBuffers, renderOrigin);

            return segment;
        }

        private static IEnumerable<string> SplitIntoWordsPreservingSpaces(string text)
        {
            return Regex.Matches(text, @"\S+\s*")
                .Cast<Match>()
                .Select(m => m.Value);
        }

        private static string DecodeSpecialCharacters(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Unicode escape sequences (\U+XXXX)
            text = Regex.Replace(
                text,
                @"\\U\+([0-9A-Fa-f]{4})",
                match =>
                {
                    int codePoint = Convert.ToInt32(
                        match.Groups[1].Value, 16);

                    return char.ConvertFromUtf32(codePoint);
                });

            // Legacy AutoCAD symbols
            text = Regex.Replace(
                text,
                @"%%([dpc%])",
                match => match.Groups[1].Value.ToLowerInvariant() switch
                {
                    "d" => "\u00B0",  // Degree
                    "p" => "\u00B1",  // Plus/minus
                    "c" => "\u2205",  // Diameter
                    "%" => "%",       // Literal percent
                    _ => match.Value
                },
                RegexOptions.IgnoreCase);

            return text;
        }
        #endregion
    }
}
