using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Helpers;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Msdf;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.DrawingObjects.HelperClasses;
using Cad_Point_Manager.Models.LffFontRendering;
using Cad_Point_Manager.Services.Exporting;
using netDxf.Entities;
using PdfSharpCore.Drawing;
using SharpDX;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using System.Diagnostics;
using System.Text;
using System.Windows;

using FontStretch = SharpDX.DirectWrite.FontStretch;
using FontStyle = SharpDX.DirectWrite.FontStyle;
using FontWeight = SharpDX.DirectWrite.FontWeight;
using Point = System.Windows.Point;
using TextAlignment = Cad_Point_Manager.Common.TextAlignment;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingSText : DrawingText
    {
        #region Fields
        private protected TextFormat _textFormat;
        private protected FontFace _fontFace;
        #endregion

        #region Properties
        public Text DxfText { get; set; }
        public string FontFamilyName { get; set; }
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public TextLayout TextLayout { get; set; }
        public double FontSizeFactor { get; set; } = 1;
        public LffFont LffFont { get; set; }
        public List<TextSegmentInformation> TextSegments { get; private set; } = [];
        public TextAlignment TextAlignment { get; private set; } = TextAlignment.BottomLeft;

        public bool TextFormatCreated => _textFormat != null;
        public bool TextLayoutCreated => TextLayout != null;
        #endregion

        #region Constructor
        public DrawingSText(Text text, ObjectLayer layer, Vector4 objectColor, ColorType colorType,
            LineType lineType, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingSText;
            EntityObject = text;
            DxfText = text;
            Layer = layer;
            ObjectColor = objectColor;
            ColorType = colorType;
            LineType = lineType;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock = block;

            UpdateColor();
            UpdateData();
        }
        #endregion

        #region Methods
        public override void UpdateData()
        {
            if (EntityObject is Text text)
            {
                TextWidthScaleFactor = text.WidthFactor;
                Rotation = (float)text.Rotation;
                TextAlignment = TextRenderingHelpers.GetTextAlignment(text.Alignment);
                Position = text.Position.ToVector2d();
                TextHeight = (float)text.Height;
                FontFamilyName = text.Style.FontFamilyName;

                var dxfFontFamilyName = text.Style.FontFamilyName;

                if (string.IsNullOrWhiteSpace(dxfFontFamilyName))
                {
                    dxfFontFamilyName = text.Style.FontFile;
                }

                (FontFamilyName, TextRenderStyle) = AutoCadFontResolver.Resolve(dxfFontFamilyName);

                TextSegments = ParseFormattingCodes(text.Value);
                Text = string.Concat(TextSegments.Select(s => s.Text));

                double widthPerCharacter = 0.6 * text.Height;
                double textWidth = widthPerCharacter * Text.Length * TextWidthScaleFactor;

                Bounds = new(text.Position.X, text.Position.Y, textWidth * 2, text.Height * 2);

                if (TextRenderStyle == TextRenderStyle.Stroke)
                {
                    LffFont = LffFontManager.GetFont(FontFamilyName);
                }
            }
            else
            {
                throw new ArgumentException("EntityObject must be of type MText or Text");
            }
        }
        public override void DrawToPdf(PdfRenderContext context, XPen pen)
        {
            XFontStyle style = XFontStyle.Regular;
            if (IsBold) { style |= XFontStyle.Bold; }
            if (IsItalic) { style |= XFontStyle.Italic; }

            var brush = new XSolidBrush(PdfTransform.ToXColor(GetColor().ToVector4()));

            var fontFamily = PdfDrawingHelpers.GetFontFamily(FontFamilyName);
            var fontSizePts = TextHeight * FontSizeFactor * context.WorldToPdf.M11;
            var font = new XFont(fontFamily, fontSizePts, style);

            var pPdf = PdfDrawingHelpers.WorldToPdf(Position, context.WorldToPdf);
            var size = context.Graphics.MeasureString(Text, font);

            double x = pPdf.X;
            double y = pPdf.Y;

            if (TextRenderStyle == TextRenderStyle.Stroke)
            {
                foreach (var line in LineInstances)
                {
                    Vector2d start = new(
                        line.Start.X + context.RenderOrigin.X,
                        line.Start.Y + context.RenderOrigin.Y);
                    Vector2d end = new(
                        line.End.X + context.RenderOrigin.X,
                        line.End.Y + context.RenderOrigin.Y);

                    XPoint p1 = PdfTransform.WorldToPdf(
                        start, context.WorldToPdf);
                    XPoint p2 = PdfTransform.WorldToPdf(
                        end, context.WorldToPdf);

                    context.Graphics.DrawLine(pen, p1, p2);
                }
            }
            else
            {
                //context.Graphics.DrawString(Text, font, brush, new XPoint(x, y));
                var stringFormat = TextRenderingHelpers.TextAlignmentToStringFormat(TextAlignment);
                context.Graphics.DrawString(Text, font, brush, new XPoint(pPdf.X, pPdf.Y), stringFormat);
            }

            if (IsUnderlined || IsStrikeOut || IsOverStrike)
            {
                var linePen = new XPen(brush.Color, fontSizePts * 0.01);

                var m = font.Metrics;
                double em = m.UnitsPerEm;

                double ascentPt = fontSizePts * (m.Ascent / em);
                double descentPt = fontSizePts * (m.Descent / em);
                double baselineY = y;

                double underlineY = baselineY + descentPt * 0.95;
                double strikeY = baselineY - ascentPt * 0.35;
                double overstrikeY = baselineY - ascentPt * 0.95;

                if (IsUnderlined)
                {
                    context.Graphics.DrawLine(linePen, x, underlineY, x + size.Width, underlineY);
                }
                if (IsStrikeOut)
                {
                    context.Graphics.DrawLine(linePen, x, strikeY, x + size.Width, strikeY);
                }
                if (IsOverStrike)
                {
                    context.Graphics.DrawLine(linePen, x, overstrikeY, x + size.Width, overstrikeY);
                }
            }
        }

        public override void UpdateVertices(
            ResCache resCache, uint layerId, uint lineTypeId, SceneIdMap sceneIdMap,
            D3dStateBuffers stateBuffers, Vector2d renderOrigin)
        {
            GetTextFormat(resCache.WriteFactory);
            GetTextLayout(resCache.WriteFactory);
            UpdateFontFace(resCache);

            var objectId = sceneIdMap.GetOrAddObjectId(this, out var isNewObj);
            if (isNewObj) { stateBuffers.InitializeObjectState(sceneIdMap.MaxObjectId, this, objectId); }


            if (TextRenderStyle == TextRenderStyle.Stroke)
            {
                List<Vector2d> vertices = GetLffVertices();

                LineInstances = GetLineVertices(
                    vertices, layerId, objectId, renderOrigin);

                TextVertices = [];
            }
            else
            {
                FontSizeFactor = TextRenderingHelpers.GetFontSizeFactor(
                    resCache, TextLayout, _fontFace);

                (List<Vector2d> vertices, RawRectangleF bounds) =
                    TextRenderingHelpers.TesselateTextLayout(resCache, TextLayout, Text, _fontFace);

                Bounds = bounds.ToRect();

                TextVertices = GetTextVertices(
                    vertices, layerId, objectId, renderOrigin);

                LineInstances = [];
            }

            GenerateDecorations(layerId, objectId, renderOrigin);
            ApplyRotation(renderOrigin, [this]);
            GetTextBounds(renderOrigin);

        }
        public List<LineInstance> GetLineVertices(
            List<Vector2d> vertices, uint layerId, uint objectId, Vector2d renderOrigin)
        {
            List<LineInstance> lineInstances = [];

            if (TextRenderStyle == TextRenderStyle.Triangle) { return lineInstances; }

            if (LffFont is null)
            {
                throw new Exception("LffFont is null.");
            }

            Bounds = MathHelpers.GetLocalBounds(vertices);
            TextHeightScaleFactor = TextHeight / LffFont.DesignHeight;
            AttachmentOffset = GetAlignmentOffset();

            var transform = UnrotatedTransform;

            for (int i = 0; i < vertices.Count; i += 2)
            {
                var s = Vector2d.TransformCoordinate(vertices[i], transform);
                var scaledStart = s.ToGpuRelative(renderOrigin);

                var e = Vector2d.TransformCoordinate(vertices[i + 1], transform);
                var scaledEnd = e.ToGpuRelative(renderOrigin);

                LineInstance lineInstance = new(
                    scaledStart, scaledEnd, layerId, objectId, 0, (uint)LineInstanceFlags.None, 0);

                lineInstances.Add(lineInstance);
            }

            return lineInstances;
        }
        public List<TextVertex> GetTextVertices(
            List<Vector2d> vertices, uint layerId, uint objectId, Vector2d renderOrigin)
        {
            List<TextVertex> textVertices = [];

            if (TextRenderStyle == TextRenderStyle.Stroke) { return textVertices; }

            AttachmentOffset = GetAlignmentOffset();

            TextHeightScaleFactor = 1;

            var transform = UnrotatedTransform;

            for (int i = 0; i < vertices.Count; i += 3)
            {
                var v1 = Vector2d.TransformCoordinate(vertices[i], transform);
                var scaledVector1 = v1.ToGpuRelative(renderOrigin);
                TextVertex textVertex1 = new(scaledVector1, layerId, objectId);

                var v2 = Vector2d.TransformCoordinate(vertices[i + 1], transform);
                var scaledVector2 = v2.ToGpuRelative(renderOrigin);
                TextVertex textVertex2 = new(scaledVector2, layerId, objectId);

                var v3 = Vector2d.TransformCoordinate(vertices[i + 2], transform);
                var scaledVector3 = v3.ToGpuRelative(renderOrigin);
                TextVertex textVertex3 = new(scaledVector3, layerId, objectId);

                textVertices.AddRange([textVertex1, textVertex2, textVertex3]);
            }

            return textVertices;
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
            return double.MaxValue;
        }
        public override void UpdateBounds()
        {
            //if (TextRenderStyle == TextRenderStyle.Stroke)
            //{
            //    if (LineInstances.Count == 0)
            //    {
            //        Bounds = System.Windows.Rect.Empty;
            //        return;
            //    }

            //    float minX = Math.Min(LineInstances.Min(v => v.Start.X), LineInstances.Min(v => v.End.X));
            //    float maxX = Math.Max(LineInstances.Max(v => v.Start.X), LineInstances.Max(v => v.End.X));
            //    float minY = Math.Min(LineInstances.Min(v => v.Start.Y), LineInstances.Min(v => v.End.Y));
            //    float maxY = Math.Max(LineInstances.Max(v => v.Start.Y), LineInstances.Max(v => v.End.Y));

            //    Bounds = new System.Windows.Rect(minX, minY, maxX - minX, maxY - minY);
            //}
            //else
            //{
            //    if (TextVertices.Count == 0)
            //    {
            //        Bounds = System.Windows.Rect.Empty;
            //        return;
            //    }

            //    float minX = TextVertices.Min(v => v.Position.X);
            //    float maxX = TextVertices.Max(v => v.Position.X);
            //    float minY = TextVertices.Min(v => v.Position.Y);
            //    float maxY = TextVertices.Max(v => v.Position.Y);

            //    Bounds = new System.Windows.Rect(minX, minY, maxX - minX, maxY - minY);
            //}
        }

        public Rect GetTextBounds(Vector2d renderOrigin)
        {
            Bounds = GetGeometryBounds(renderOrigin);
            return Bounds;
        }

        public void GetTextFormat(Factory1 factory)
        {
            _textFormat = new(factory, FontFamilyName, (float)TextHeight);
        }
        public void GetTextLayout(Factory1 factory)
        {
            double widthPerCharacter = 0.6 * DxfText.Height;
            double textWidth = widthPerCharacter * Text.Length * TextWidthScaleFactor;

            Rect bounds = new(
                DxfText.Position.X, DxfText.Position.Y, textWidth * 2, DxfText.Height * 2);

            TextLayout = new(
                factory, Text, _textFormat, (float)bounds.Width, (float)bounds.Height, 96, true);
        }

        private void UpdateFontFace(ResCache resCache)
        {
            FontWeight fontWeight = IsBold ? FontWeight.Bold : FontWeight.Normal;
            FontStyle fontStyle = IsItalic ? FontStyle.Italic : FontStyle.Normal;
            _fontFace = resCache.GetFontFace(FontFamilyName, fontWeight, FontStretch.Normal, fontStyle);
        }

        private List<Vector2d> GetLffVertices()
        {
            if (LffFont == null)
            {
                throw new Exception($"Font '{FontFamilyName}' not found in LffFontManager.");
            }

            List<Vector2d> vertices = [];

            float penX = 0;

            foreach (char c in Text)
            {
                if (!LffFont.Glyphs.TryGetValue(c, out var glyph))
                {
                    penX += LffFont.WordSpacing;
                    continue;
                }

                foreach (var stroke in glyph.Strokes)
                {
                    foreach (var segment in stroke.Segments)
                    {
                        var pts = MathHelpers.TessellateBulge(segment.Start, segment.End, segment.Bulge);

                        for (int i = 1; i < pts.Count; i++)
                        {
                            vertices.Add(new Vector2d(pts[i - 1].X + penX, pts[i - 1].Y));
                            vertices.Add(new Vector2d(pts[i].X + penX, pts[i].Y));
                        }
                    }
                }

                penX += glyph.AdvanceWidth;
            }

            return vertices;
        }

        private Vector2d GetAlignmentOffset()
        {
            double left = Bounds.Left;
            double centerX = Bounds.Left + Bounds.Width / 2;
            double right = Bounds.Right;

            double bottom = Bounds.Top;
            double centerY = Bounds.Top + Bounds.Height / 2;
            double top = Bounds.Bottom;

            double nominalHeight = TextHeight / TextHeightScaleFactor;
            double nominalMiddleY = nominalHeight / 2; // Baseline center is not actual center of text bounds

            Vector2d anchor = TextAlignment switch
            {
                TextAlignment.TopLeft =>
                    new(left, top),

                TextAlignment.TopCenter =>
                    new(centerX, top),

                TextAlignment.TopRight =>
                    new(right, top),

                TextAlignment.MiddleLeft =>
                    new(left, nominalMiddleY),

                TextAlignment.MiddleCenter =>
                    new(centerX, nominalMiddleY),

                TextAlignment.MiddleRight =>
                    new(right, nominalMiddleY),

                TextAlignment.BottomLeft =>
                    new(left, bottom),

                TextAlignment.BottomCenter =>
                    new(centerX, bottom),

                TextAlignment.BottomRight =>
                    new(right, bottom),

                TextAlignment.BaselineLeft =>
                    new(left, 0),

                TextAlignment.BaselineCenter =>
                    new(centerX, 0),

                TextAlignment.BaselineRight =>
                    new(right, 0),

                TextAlignment.Aligned =>
                    new(left, 0),

                TextAlignment.Middle =>
                    new(centerX, centerY),

                TextAlignment.Fit =>
                    new(left, 0),

                _ => Vector2d.Zero
            };

            return -anchor;
        }

        private List<TextSegmentInformation> ParseFormattingCodes(string rawText)
        {
            List<TextSegmentInformation> segments = [];
            StringBuilder currentText = new();

            bool isUnderlined = false;
            bool isOverstriked = false;
            bool isStrikethrough = false;

            void AddSegment()
            {
                if (currentText.Length == 0) { return; }

                segments.Add(new TextSegmentInformation(
                    currentText.ToString(),
                    ObjectColor,
                    ColorType,
                    FontFamilyName,
                    TextHeight,
                    IsBold,
                    IsItalic,
                    isUnderlined,
                    isOverstriked,
                    isStrikethrough));

                currentText.Clear();
            }

            for (int i = 0; i < rawText.Length; i++)
            {
                if (i + 2 >= rawText.Length ||
                    rawText[i] != '%' ||
                    rawText[i + 1] != '%')
                {
                    currentText.Append(rawText[i]);
                    continue;
                }

                char code = char.ToLowerInvariant(rawText[i + 2]);

                switch (code)
                {
                    case 'u':
                        AddSegment();
                        isUnderlined = !isUnderlined;
                        i += 2;
                        break;

                    case 'o':
                        AddSegment();
                        isOverstriked = !isOverstriked;
                        i += 2;
                        break;

                    case 'k':
                        AddSegment();
                        isStrikethrough = !isStrikethrough;
                        i += 2;
                        break;

                    case 'd':
                        currentText.Append('°');
                        i += 2;
                        break;

                    case 'p':
                        currentText.Append('±');
                        i += 2;
                        break;

                    case 'c':
                        currentText.Append('⌀');
                        i += 2;
                        break;

                    case '%':
                        currentText.Append('%');
                        i += 2;
                        break;

                    default:
                        if (char.IsAsciiDigit(code))
                        {
                            int start = i + 2;
                            int count = 0;

                            while (start + count < rawText.Length &&
                                   count < 3 &&
                                   char.IsAsciiDigit(rawText[start + count]))
                            {
                                count++;
                            }

                            int characterCode = int.Parse(
                                rawText.Substring(start, count));

                            currentText.Append((char)characterCode);
                            i = start + count - 1;
                        }
                        else
                        {
                            // Preserve unrecognized codes.
                            currentText.Append("%%");
                            currentText.Append(rawText[i + 2]);
                            i += 2;
                        }
                        break;
                }
            }

            AddSegment();

            return segments;
        }

        private void GenerateDecorations(
            uint layerId, uint objectId, Vector2d renderOrigin)
        {
            if (TextRenderStyle == TextRenderStyle.Stroke)
            {
                GenerateLffDecorations(layerId, objectId, renderOrigin);
                return;
            }

            if (TextLayout is null || _fontFace is null || TextSegments.Count == 0)
                return;

            var metrics = _fontFace.Metrics;

            double fontScale = TextLayout.FontSize * FontSizeFactor / metrics.DesignUnitsPerEm;

            double capHeight = metrics.CapHeight * fontScale;

            double underlineY = -capHeight * UnderlineFactor;
            double overlineY = capHeight * OverlineFactor;
            double strikeY = capHeight * StrikethroughFactor;

            var transform = UnrotatedTransform;
            int startIndex = 0;

            foreach (var segment in TextSegments)
            {
                int length = segment.Text.Length;

                if (length == 0)
                    continue;

                if (!segment.IsUnderlined &&
                    !segment.IsOverstriked &&
                    !segment.IsStrikethrough)
                {
                    startIndex += length;
                    continue;
                }

                var ranges = TextLayout.HitTestTextRange(
                    startIndex, length, 0, 0);

                foreach (var range in ranges)
                {
                    double x1 = range.Left * FontSizeFactor;
                    double x2 = (range.Left + range.Width) * FontSizeFactor;

                    if (x2 <= x1)
                        continue;

                    void AddLine(double y)
                    {
                        var start = Vector2d.TransformCoordinate(
                            new Vector2d(x1, y), transform);

                        var end = Vector2d.TransformCoordinate(
                            new Vector2d(x2, y), transform);

                        AddDecorationLine(
                            start, end,
                            layerId, objectId, renderOrigin);
                    }

                    if (segment.IsUnderlined)
                        AddLine(underlineY);

                    if (segment.IsOverstriked)
                        AddLine(overlineY);

                    if (segment.IsStrikethrough)
                        AddLine(strikeY);
                }

                startIndex += length;
            }
        }

        private void GenerateLffDecorations(
            uint layerId, uint objectId, Vector2d renderOrigin)
        {
            if (LffFont is null)
                return;

            var transform = UnrotatedTransform;
            double penX = 0;

            foreach (var segment in TextSegments)
            {
                double startX = penX;

                foreach (char c in segment.Text)
                {
                    penX += LffFont.Glyphs.TryGetValue(c, out var glyph)
                        ? glyph.AdvanceWidth
                        : LffFont.WordSpacing;
                }

                if (!segment.IsUnderlined &&
                    !segment.IsOverstriked &&
                    !segment.IsStrikethrough)
                    continue;

                void AddLine(double y)
                {
                    double xOffset = Bounds.Left;

                    var start = Vector2d.TransformCoordinate(
                        new Vector2d(startX + xOffset, y), transform);

                    var end = Vector2d.TransformCoordinate(
                        new Vector2d(penX + xOffset, y), transform);

                    AddDecorationLine(
                        start, end, layerId, objectId, renderOrigin);
                }

                double height = LffFont.DesignHeight;

                if (segment.IsUnderlined)
                    AddLine(-height * UnderlineFactor);

                if (segment.IsOverstriked)
                    AddLine(height * OverlineFactor);

                if (segment.IsStrikethrough)
                    AddLine(height * StrikethroughFactor);
            }
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _textFormat?.Dispose();
                    _textFormat = null;
                    TextLayout?.Dispose();
                    TextLayout = null;
                    _fontFace?.Dispose();
                    _fontFace = null;
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
