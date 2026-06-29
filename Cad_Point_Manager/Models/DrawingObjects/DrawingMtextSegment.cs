using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Services.Exporting;
using netDxf.Entities;
using PdfSharpCore.Drawing;
using SharpDX;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using System.Diagnostics;
using System.Windows;

using FontStretch = SharpDX.DirectWrite.FontStretch;
using FontStyle = SharpDX.DirectWrite.FontStyle;
using FontWeight = SharpDX.DirectWrite.FontWeight;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingMtextSegment : DrawingObject, IDisposable
    {
        #region Fields
        private TextFormat _textFormat = null;
        private FontFace _fontFace = null;
        #endregion

        #region Properties
        public DrawingMtext DrawingMtext { get; set; }
        public string Text { get; set; }
        public Vector3 Position { get; set; }
        public float Rotation { get; set; } = 0;
        public float TextHeight { get; set; }
        public string FontFamilyName { get; set; }
        public System.Windows.Media.Matrix Transform { get; set; }
        public TextLayout TextLayout { get; set; }
        public TextVertex[] TextVertices { get; set; } = [];
        public LineVertex[] LineVertices { get; set; } = [];
        public float MaxWidth { get; set; }
        public bool IsItalic { get; set; } = false;
        public bool IsBold { get; set; } = false;
        public bool IsUnderlined { get; set; } = false;
        public bool IsStrikeOut { get; set; } = false;
        public bool IsOverStrike { get; set; } = false;
        public bool IsNewLine { get; set; } = false;
        public Enums.TextAlignment TextAlignment { get; set; }
        public float SpaceWidth { get; set; }
        public float RowXOffset { get; set; } = 0;
        public float GlowOffset { get; set; }
        public float FontSizeFactor { get; set; } = 1;
        public TextRenderStyle TextRenderStyle { get; set; }
        public float AdvanceWidth { get; set; }
        #endregion

        #region Constructors
        public DrawingMtextSegment(DrawingMtext drawingMtext, ObjectLayer layer, string text, Vector4 objectColor, ColorType colorType, Vector3 position,
            float rotation, float fontHeight, string fontFamilyName, bool isItalic, bool isBold, bool isUnderlined, bool isStrikeOut,
            bool isOverStrike, bool isNewLine, float maxWidth, TextRenderStyle textRenderStyle, Enums.TextAlignment textAlignment = Enums.TextAlignment.Left,
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
            TextAlignment = textAlignment;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock = block;
            GlowOffset = TextHeight * GlobalHelperProperties.TextHeightToGlowOffsetFactor;

            UpdateColor();
            UpdateTransform();
        }
        #endregion

        #region Methods
        public override void UpdateData() { }

        public override void DrawToD2dDeviceContext(SharpDX.Direct2D1.DeviceContext1 deviceContext, SharpDX.Direct2D1.Factory2 factory,
            SharpDX.Direct2D1.Brush brush, float thickness, SharpDX.Direct2D1.StrokeStyle1 strokeStyle)
        {

        }
        public override void DrawToPdf(
            XGraphics gfx,
            System.Windows.Media.Matrix worldToPdf,
            XPen pen)
        {
            if (string.IsNullOrWhiteSpace(Text)) { return; }

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
                for (int i = 0; i < LineVertices.Length; i++)
                {
                    Bounds = Rect.Union(Bounds, (System.Windows.Point)LineVertices[i]);
                }
            }
            else
            {
                for (int i = 0; i < TextVertices.Length; i++)
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

        public void UpdateVertices(ResCache resCache, uint layerId, uint objectId)
        {
            UpdateFontFace(resCache);
            FontSizeFactor = TextRenderingHelpers.GetFontSizeFactor(resCache, TextLayout, _fontFace);

            AdvanceWidth = TextLayout.Metrics.WidthIncludingTrailingWhitespace * FontSizeFactor;

            if (TextRenderStyle == TextRenderStyle.Stroke)
            {
                (List<Vector2> vertices,
                 RawRectangleF bounds)
                    = TextRenderingHelpers
                        .GetLineRepresentationOfTextLayout(
                            resCache,
                            TextLayout,
                            Text,
                            _fontFace);

                UpdateBounds(bounds);

                LineVertices =
                    GetLineVertices(
                        vertices,
                        layerId,
                        objectId);

                TextVertices = [];
            }
            else
            {
                (List<Vector2> vertices,
                 RawRectangleF bounds)
                    = TextRenderingHelpers
                        .TesselateTextLayout(
                            resCache,
                            TextLayout,
                            Text,
                            _fontFace);

                UpdateBounds(bounds);

                TextVertices =
                    GetTextVertices(
                        vertices,
                        layerId,
                        objectId);

                LineVertices = [];
            }
        }

        private void UpdateBounds(RawRectangleF textGeometryBounds)
        {
            if (Text.EndsWith(" "))
            {
                var clusterMetrics = TextLayout.GetClusterMetrics();
                foreach (var cluster in clusterMetrics)
                {
                    if (cluster.Length == 1 && cluster.IsWhitespace)
                    {
                        Bounds = new(
                            textGeometryBounds.Left,
                            textGeometryBounds.Top,
                            textGeometryBounds.Right - textGeometryBounds.Left + cluster.Width,
                            textGeometryBounds.Bottom - textGeometryBounds.Top);
                    }
                }
            }
            else
            {
                Bounds = new Rect(
                    textGeometryBounds.Left,
                    textGeometryBounds.Top,
                    textGeometryBounds.Right - textGeometryBounds.Left,
                    textGeometryBounds.Bottom - textGeometryBounds.Top);
            }
        }

        public void UpdateTransform()
        {
            Transform = GetTransform(new netDxf.Vector3(Position.X, Position.Y, Position.Z));
        }
        private System.Windows.Media.Matrix GetTransform(netDxf.Vector3 dxfPos)
        {
            System.Windows.Media.Matrix matrix = new();
            matrix.Translate(dxfPos.X, dxfPos.Y);
            return matrix;
        }

        public TextVertex[] GetTextVertices(List<Vector2> vertices, uint layerId, uint objectId)
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

            return textVertices.ToArray();
        }
        private LineVertex[] GetLineVertices(
            List<Vector2> vertices,
            uint layerId,
            uint objectId)
        {
            var result = new LineVertex[vertices.Count];

            for (int i = 0; i < vertices.Count; i++)
            {
                Vector2 v = vertices[i];

                result[i] = new LineVertex(
                    new Vector3(
                        v.X,
                        v.Y,
                        Position.Z),
                    layerId,
                    objectId);
            }

            return result;
        }

        public void ApplyTranslate(Vector3 rowTransform)
        {
            Vector3 transform = new(rowTransform.X + RowXOffset, rowTransform.Y, rowTransform.Z);
            Translate(transform);
        }
        private void Translate(Vector3 offset)
        {
            Position += offset;
            for (int i = 0; i < TextVertices.Length; i++)
            {
                TextVertices[i] = TextVertices[i].Translate(offset);
            }
            for (int i = 0; i < LineVertices.Length; i++)
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
