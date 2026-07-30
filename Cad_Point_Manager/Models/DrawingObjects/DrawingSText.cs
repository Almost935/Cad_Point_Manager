using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.LffFontRendering;
using Cad_Point_Manager.Services.Exporting;
using netDxf.Entities;
using PdfSharpCore.Drawing;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using System.Diagnostics;
using Point = System.Windows.Point;

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
        public float WidthFactor { get; set; } = 1.0f;
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public TextLayout TextLayout { get; set; }
        public float FontSizeFactor { get; set; } = 1;
        public LffFont LffFont { get; set; }

        public bool TextFormatCreated => _textFormat != null;
        public bool TextLayoutCreated => TextLayout != null;
        #endregion

        #region Constructor
        public DrawingSText(Text text, ObjectLayer layer, Vector4 objectColor, ColorType colorType, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingSText;
            EntityObject = text;
            DxfText = text;
            Layer = layer;
            ObjectColor = objectColor;
            ColorType = colorType;
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
                Text = text.Value;
                WidthFactor = (float)text.WidthFactor;

                float widthPerCharacter = (float)(0.6f * text.Height);
                float textWidth = widthPerCharacter * Text.Length * WidthFactor;

                Bounds = new(text.Position.X, text.Position.Y, textWidth * 2, text.Height * 2);
                Rotation = (float)text.Rotation;
                AttachmentPoint = TextRenderingHelpers.GetAttachmentPoint(text.Alignment);
                Position = text.Position.ToSharpDXVector3();
                TextHeight = (float)text.Height;
                FontFamilyName = text.Style.FontFamilyName;

                var dxfFontFamilyName = text.Style.FontFamilyName;
                if (string.IsNullOrWhiteSpace(dxfFontFamilyName))
                {
                    dxfFontFamilyName = text.Style.FontFile;
                }

                (FontFamilyName, TextRenderStyle) = AutoCadFontResolver.Resolve(dxfFontFamilyName);
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
        public override void DrawToD2dDeviceContext(DeviceContext1 deviceContext, SharpDX.Direct2D1.Factory2 factory, Brush brush, float thickness, StrokeStyle1 strokeStyle)
        {
            //deviceContext.DrawTextLayout(new RawVector2((float)Position.X, -(float)Position.Y), TextLayout, brush);
        }
        public override void DrawToPdf(XGraphics gfx, System.Windows.Media.Matrix worldToPdf, XPen pen)
        {
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
                var verts = TextVertices;
                if (verts == null || verts.Count < 3) { return; }

                var brush = new XSolidBrush(PdfTransform.ToXColor(ObjectColor.ToVector4()));
                var path = new XGraphicsPath();

                for (int i = 0; i + 2 < verts.Count; i += 3)
                {
                    var p0 = PdfTransform.WorldToPdf(verts[i].Position.ToVector3(), worldToPdf);
                    var p1 = PdfTransform.WorldToPdf(verts[i + 1].Position.ToVector3(), worldToPdf);
                    var p2 = PdfTransform.WorldToPdf(verts[i + 2].Position.ToVector3(), worldToPdf);
                    path.AddPolygon(new[] { p0, p1, p2 });
                }

                gfx.DrawPath(brush, path);
            }
        }

        public override void UpdateVertices(ResCache resCache, uint layerId, SceneIdMap sceneIdMap, D3dStateBuffers stateBuffers)
        {
            GetTextFormat(resCache.WriteFactory);
            GetTextLayout(resCache.WriteFactory);
            UpdateFontFace(resCache);

            var objectId = sceneIdMap.GetOrAddObjectId(this, out var isNewObj);
            if (isNewObj) { stateBuffers.InitializeObjectState(sceneIdMap.MaxObjectId, this, objectId); }

            if (TextRenderStyle == TextRenderStyle.Stroke)
            {
                List<Vector2> vertices = GetLffVertices();
                LineVertices = GetLineVertices(vertices, layerId, objectId);
                TextVertices = [];
            }
            else
            {
                FontSizeFactor = TextRenderingHelpers.GetFontSizeFactor(resCache, TextLayout, _fontFace);
                (List<Vector2> vertices, RawRectangleF bounds) = TextRenderingHelpers.TesselateTextLayout(resCache, TextLayout, Text, _fontFace);
                Bounds = bounds.ToRect();
                TextVertices = GetTextVertices(vertices, layerId, objectId);

                LineVertices = [];
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

        public override double DistanceToPoint(Point p)
        {
            return 1000;
        }
        public override void UpdateBounds()
        {
            if (TextRenderStyle == TextRenderStyle.Stroke)
            {
                if (LineVertices.Count == 0)
                {
                    Bounds = System.Windows.Rect.Empty;
                    return;
                }

                float minX = LineVertices.Min(v => v.Position.X);
                float maxX = LineVertices.Max(v => v.Position.X);
                float minY = LineVertices.Min(v => v.Position.Y);
                float maxY = LineVertices.Max(v => v.Position.Y);

                Bounds = new System.Windows.Rect(minX, minY, maxX - minX, maxY - minY);
            }
            else
            {
                if (TextVertices.Count == 0)
                {
                    Bounds = System.Windows.Rect.Empty;
                    return;
                }

                float minX = TextVertices.Min(v => v.Position.X);
                float maxX = TextVertices.Max(v => v.Position.X);
                float minY = TextVertices.Min(v => v.Position.Y);
                float maxY = TextVertices.Max(v => v.Position.Y);

                Bounds = new System.Windows.Rect(minX, minY, maxX - minX, maxY - minY);
            }
        }

        public void GetTextFormat(SharpDX.DirectWrite.Factory1 factory)
        {
            _textFormat = new(factory, FontFamilyName, TextHeight);
        }

        public void GetTextLayout(SharpDX.DirectWrite.Factory1 factory)
        {
            TextLayout = new(factory, Text, _textFormat, (float)Bounds.Width, (float)Bounds.Height, 96, true);
        }

        public List<LineVertex> GetLineVertices(List<Vector2> vertices, uint layerId, uint objectId)
        {
            List<LineVertex> lineVertices = [];

            if (TextRenderStyle == TextRenderStyle.Triangle) { return lineVertices; }

            if (LffFont is null)
            {
                throw new Exception("LffFont is null.");
            }

            Bounds = MathHelpers.GetLocalBounds(vertices);
            TextHeightScaleFactor = TextHeight / LffFont.DesignHeight;
            AttachmentOffset = UpdateAttachmentOffset();

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
        public List<TextVertex> GetTextVertices(List<Vector2> vertices, uint layerId, uint objectId)
        {
            List<TextVertex> textVertices = [];

            if (TextRenderStyle == TextRenderStyle.Stroke) { return textVertices; }

            AttachmentOffset = TextRenderingHelpers.GetAttachmentOffset(Bounds.ToRectangeF(), AttachmentPoint);
            TextHeightScaleFactor = 1;

            var transform = Transform;

            for (int i = 0; i < vertices.Count; i += 3)
            {
                var v1 = vertices[i];
                var v2 = vertices[i + 1];
                var v3 = vertices[i + 2];

                var scaledVector1 = Vector2.TransformCoordinate(v1, transform);
                TextVertex textVertex1 = new(new Vector3(scaledVector1.X, scaledVector1.Y, 0), layerId, objectId);
                var scaledVector2 = Vector2.TransformCoordinate(v2, transform);
                TextVertex textVertex2 = new(new Vector3(scaledVector2.X, scaledVector2.Y, 0), layerId, objectId);
                var scaledVector3 = Vector2.TransformCoordinate(v3, transform);
                TextVertex textVertex3 = new(new Vector3(scaledVector3.X, scaledVector3.Y, 0), layerId, objectId);

                textVertices.AddRange([textVertex1, textVertex2, textVertex3]);
            }

            return textVertices;
        }

        public static float ConvertDxfHeightToFontSize(float height)
        {
            //// Convert millimeters to inches (1 inch = 25.4 mm)
            //float heightInInches = dxfHeightInMm / 25.4f;

            //// Convert inches to points (1 inch = 72 points)
            //float fontSizeInPoints = heightInInches * 72f;

            return height;
        }

        private void UpdateFontFace(ResCache resCache)
        {
            FontWeight fontWeight = IsBold ? FontWeight.Bold : FontWeight.Normal;
            FontStyle fontStyle = IsItalic ? FontStyle.Italic : FontStyle.Normal;
            _fontFace = resCache.GetFontFace(FontFamilyName, fontWeight, FontStretch.Normal, fontStyle);
        }

        private List<Vector2> GetLffVertices()
        {
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

        private Vector2 UpdateAttachmentOffset()
        {
            var xOffset = Bounds.Width.ToFloat();
            var yOffset = TextHeight / TextHeightScaleFactor;
            var bottomOffset = Bounds.Height.ToFloat() - yOffset;

            return AttachmentPoint switch
            {
                TextAttachmentPoint.TopLeft =>
                    new Vector2(0, -yOffset),

                TextAttachmentPoint.TopCenter =>
                    new Vector2(-(xOffset / 2), -yOffset),

                TextAttachmentPoint.TopRight =>
                    new Vector2(-xOffset, -yOffset),

                TextAttachmentPoint.MiddleLeft =>
                    new Vector2(0, -yOffset / 2),

                TextAttachmentPoint.MiddleCenter =>
                    new Vector2(-(xOffset / 2), -yOffset / 2),

                TextAttachmentPoint.MiddleRight =>
                    new Vector2(-xOffset, -yOffset / 2),

                TextAttachmentPoint.BottomLeft =>
                    new Vector2(0, bottomOffset),

                TextAttachmentPoint.BottomCenter =>
                    new Vector2(-(xOffset / 2), bottomOffset),

                TextAttachmentPoint.BottomRight =>
                    new Vector2(-xOffset, bottomOffset),

                _ => Vector2.Zero
            };
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
