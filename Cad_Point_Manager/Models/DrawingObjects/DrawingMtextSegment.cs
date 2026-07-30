using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.LffFontRendering;
using Cad_Point_Manager.Services.Exporting;
using PdfSharpCore.Drawing;
using SharpDX;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using System.Diagnostics;
using System.Windows;

using FontStretch = SharpDX.DirectWrite.FontStretch;
using FontStyle = SharpDX.DirectWrite.FontStyle;
using FontWeight = SharpDX.DirectWrite.FontWeight;
using TextAlignment = Cad_Point_Manager.Common.TextAlignment;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingMtextSegment : DrawingText, IDisposable
    {
        #region Fields
        private TextFormat _textFormat = null;
        private FontFace _fontFace = null;
        #endregion

        #region Properties
        public DrawingMtext DrawingMtext { get; set; }
        public string FontFamilyName { get; set; }
        public TextLayout TextLayout { get; set; }
        public bool IsItalic { get; set; } = false;
        public bool IsBold { get; set; } = false;
        public bool IsUnderlined { get; set; } = false;
        public bool IsStrikeOut { get; set; } = false;
        public bool IsOverStrike { get; set; } = false;
        public bool IsNewLine { get; set; } = false;
        public TextAlignment TextAlignment { get; set; }
        public float SpaceWidth { get; set; }
        public float XOffset { get; set; } = 0;
        public float YOffset { get; set; } = 0;
        public float GlowOffset { get; set; }
        public float FontSizeFactor { get; set; } = 1;
        public float AdvanceWidth { get; set; }
        public LffFont LffFont { get; set; }
        #endregion

        #region Constructors
        public DrawingMtextSegment(DrawingMtext drawingMtext, ObjectLayer layer, string text, Vector4 objectColor, ColorType colorType, Vector3 position,
            float rotation, float fontHeight, string fontFamilyName, bool isItalic, bool isBold, bool isUnderlined, bool isStrikeOut,
            bool isOverStrike, bool isNewLine, float maxWidth, TextRenderStyle textRenderStyle, TextAlignment textAlignment = TextAlignment.Left,
            bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingMtextSegment;
            DrawingMtext = drawingMtext;
            Layer = layer;
            EntityObject = drawingMtext.EntityObject;
            Text = text;
            ObjectColor = objectColor;
            ColorType = colorType;
            Position = position;
            Rotation = rotation;
            TextHeight = fontHeight;
            FontFamilyName = fontFamilyName;
            IsItalic = isItalic;
            IsBold = isBold;
            IsUnderlined = isUnderlined;
            IsStrikeOut = isStrikeOut;
            IsOverStrike = isOverStrike;
            IsNewLine = isNewLine;
            MaxWidth = maxWidth;
            TextRenderStyle = textRenderStyle;
            IsPartOfBlock = isPartOfBlock;
            TextAlignment = textAlignment;
            DrawingBlock = block;
            GlowOffset = TextHeight * GlobalHelperProperties.TextHeightToGlowOffsetFactor;

            UpdateColor();
        }
        #endregion

        #region Methods
        public override void UpdateData() { }

        public override void DrawToD2dDeviceContext(SharpDX.Direct2D1.DeviceContext1 deviceContext, SharpDX.Direct2D1.Factory2 factory,
            SharpDX.Direct2D1.Brush brush, float thickness, SharpDX.Direct2D1.StrokeStyle1 strokeStyle)
        {

        }
        public override void DrawToPdf(XGraphics gfx, System.Windows.Media.Matrix worldToPdf, XPen pen)
        {
            if (string.IsNullOrWhiteSpace(Text)) { return; }

            if (TextRenderStyle is TextRenderStyle.Stroke)
            {
                for (int i = 0; i < LineVertices.Count; i += 2)
                {
                    var v1 = LineVertices[i];
                    var v2 = LineVertices[i + 1];
                    var p1Pdf = PdfDrawingHelpers.WorldToPdf(new Vector2(v1.Position.X, v1.Position.Y), worldToPdf);
                    var p2Pdf = PdfDrawingHelpers.WorldToPdf(new Vector2(v2.Position.X, v2.Position.Y), worldToPdf);

                    gfx.DrawLine(pen, new XPoint(p1Pdf.X, p1Pdf.Y), new XPoint(p2Pdf.X, p2Pdf.Y));
                }
            }
            else
            {
                var fontFamily = PdfDrawingHelpers.GetFontFamily(FontFamilyName);

                XFontStyle style = XFontStyle.Regular;
                if (IsBold) { style |= XFontStyle.Bold; }
                if (IsItalic) { style |= XFontStyle.Italic; }

                var fontSizePts = TextHeight * FontSizeFactor * worldToPdf.M11;
                var font = new XFont(fontFamily, fontSizePts, style);
                var brush = new XSolidBrush(PdfTransform.ToXColor(GetColor().ToVector4()));

                var pPdf = PdfDrawingHelpers.WorldToPdf(new Vector2(Position.X, Position.Y), worldToPdf);
                var size = gfx.MeasureString(Text, font);

                double x = pPdf.X;
                double y = pPdf.Y;

                gfx.DrawString(Text, font, brush, new XPoint(x, y));

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
                        gfx.DrawLine(linePen, x, underlineY, x + size.Width, underlineY);
                    }
                    if (IsStrikeOut)
                    {
                        gfx.DrawLine(linePen, x, strikeY, x + size.Width, strikeY);
                    }
                    if (IsOverStrike)
                    {
                        gfx.DrawLine(linePen, x, overstrikeY, x + size.Width, overstrikeY);
                    }
                }
            }
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

        public override double DistanceToPoint(System.Windows.Point p)
        {
            return 0;
        }
        public override void UpdateBounds()
        {
            if (TextRenderStyle == TextRenderStyle.Stroke)
            {
                if (LineVertices.Count == 0)
                {
                    Bounds = Rect.Empty;
                    return;
                }

                float minX = LineVertices.Min(v => v.Position.X);
                float maxX = LineVertices.Max(v => v.Position.X);
                float minY = LineVertices.Min(v => v.Position.Y);
                float maxY = LineVertices.Max(v => v.Position.Y);

                Bounds = new Rect(minX, minY, maxX - minX, maxY - minY);
            }
            else
            {
                for (int i = 0; i < TextVertices.Count; i++)
                {
                    Bounds = Rect.Union(Bounds, (System.Windows.Point)TextVertices[i]);
                }
            }
        }

        public void GetTextLayout(Factory1 factory)
        {
            FontWeight fontWeight;
            if (IsBold) { fontWeight = FontWeight.Bold; } else { fontWeight = FontWeight.Normal; }

            FontStyle fontStyle;
            if (IsItalic) { fontStyle = FontStyle.Italic; } else { fontStyle = FontStyle.Normal; }

            _textFormat = new(factory, FontFamilyName, fontWeight, fontStyle, TextHeight);
            TextLayout = new(factory, Text, _textFormat, float.MaxValue, float.MaxValue, 96, true);

            SpaceWidth = TextRenderingHelpers.GetSpaceWidth(
                factory,
                FontFamilyName,
                TextHeight);
        }

        public override void UpdateVertices(ResCache resCache, uint layerId, SceneIdMap sceneIdMap, D3dStateBuffers stateBuffers)
        {
            var objectId = sceneIdMap.GetOrAddObjectId(this, out var isNewObj);
            if (isNewObj) { stateBuffers.InitializeObjectState(sceneIdMap.MaxObjectId, this, objectId); }

            if (TextRenderStyle == TextRenderStyle.Stroke)
            {
                List<Vector2> vertices = GetLffVertices();
                LineVertices = GetLineVertices(vertices, layerId, objectId);

                UpdateBounds();
                SpaceWidth = LffFont.WordSpacing * TextHeightScaleFactor;
                AdvanceWidth = SpaceWidth + Bounds.Width.ToFloat();

                TextVertices = [];
            }
            else
            {
                GetTextLayout(resCache.WriteFactory);
                UpdateFontFace(resCache);
                FontSizeFactor = TextRenderingHelpers.GetFontSizeFactor(resCache, TextLayout, _fontFace);
                AdvanceWidth = TextLayout.Metrics.WidthIncludingTrailingWhitespace * FontSizeFactor;

                (List<Vector2> vertices, RawRectangleF bounds)
                    = TextRenderingHelpers.TesselateTextLayout(resCache, TextLayout, Text, _fontFace);
                UpdateBounds();
                TextVertices = GetTextVertices(vertices, layerId, objectId);

                LineVertices = [];
            }
        }

        private List<TextVertex> GetTextVertices(List<Vector2> vertices, uint layerId, uint objectId)
        {
            List<TextVertex> textVertices = [];

            for (int i = 0; i < vertices.Count; i += 3)
            {
                var v1 = vertices[i];
                var v2 = vertices[i + 1];
                var v3 = vertices[i + 2];

                TextVertex textVertex1 = new(new Vector3(v1.X, v1.Y, 0), layerId, objectId);
                TextVertex textVertex2 = new(new Vector3(v2.X, v2.Y, 0), layerId, objectId);
                TextVertex textVertex3 = new(new Vector3(v3.X, v3.Y, 0), layerId, objectId);

                textVertices.AddRange([textVertex1, textVertex2, textVertex3]);
            }

            return textVertices;
        }
        private List<LineVertex> GetLineVertices(List<Vector2> vertices, uint layerId, uint objectId)
        {
            List<LineVertex> lineVertices = [];

            if (TextRenderStyle == TextRenderStyle.Triangle) { return lineVertices; }

            if (LffFont is null)
            {
                throw new Exception("LffFont is null.");
            }

            TextHeightScaleFactor = TextHeight / LffFont.DesignHeight;

            var transform = Transform;

            for (int i = 0; i < vertices.Count; i++)
            {
                var v = vertices[i];
                var scaledVector = Vector2.TransformCoordinate(v, transform);
                LineVertex lineVertex = new(new Vector3(scaledVector.X, scaledVector.Y, 0), layerId, objectId);
                lineVertices.Add(lineVertex);
            }

            return lineVertices;
        }

        public void ApplyTranslate(Vector3 rowTransform)
        {
            Vector3 transform = new(rowTransform.X + XOffset, rowTransform.Y, rowTransform.Z);
            Translate(transform);
        }
        private void Translate(Vector3 offset)
        {
            Position += offset;
            for (int i = 0; i < TextVertices.Count; i++)
            {
                TextVertices[i] = TextVertices[i].Translate(offset);
            }
            for (int i = 0; i < LineVertices.Count; i++)
            {
                LineVertices[i] = LineVertices[i].Translate(offset);
            }
        }

        private void UpdateFontFace(ResCache resCache)
        {
            FontWeight fontWeight = IsBold ? FontWeight.Bold : FontWeight.Normal;
            FontStyle fontStyle = IsItalic ? FontStyle.Italic : FontStyle.Normal;
            _fontFace = resCache.GetFontFace(FontFamilyName, fontWeight, FontStretch.Normal, fontStyle);
        }

        private List<Vector2> GetLffVertices()
        {
            LffFont = LffFontManager.GetFont(FontFamilyName);

            if (LffFont == null)
            {
                throw new Exception($"Font '{FontFamilyName}' not found in LffFontManager.");
            }

            List<Vector2> vertices = [];

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
                            vertices.Add(new Vector2(pts[i - 1].X + penX, pts[i - 1].Y));
                            vertices.Add(new Vector2(pts[i].X + penX, pts[i].Y));
                        }
                    }
                }

                penX += glyph.AdvanceWidth;
            }

            return vertices;
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
