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
        public double WidthFactor { get; set; } = 1.0;
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public TextLayout TextLayout { get; set; }
        public double FontSizeFactor { get; set; } = 1;
        public LffFont LffFont { get; set; }

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
                Text = text.Value;
                WidthFactor = (float)text.WidthFactor;

                double widthPerCharacter = 0.6 * text.Height;
                double textWidth = widthPerCharacter * Text.Length * WidthFactor;

                Bounds = new(text.Position.X, text.Position.Y, textWidth * 2, text.Height * 2);
                Rotation = (float)text.Rotation;
                AttachmentPoint = TextRenderingHelpers.GetAttachmentPoint(text.Alignment);
                Position = text.Position.ToVector2d();
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
        public override void DrawToPdf(XGraphics gfx, System.Windows.Media.Matrix worldToPdf, XPen pen)
        {
            if (TextRenderStyle is TextRenderStyle.Stroke)
            {
                for (int i = 0; i < LineInstances.Count; i++)
                {
                    var v = LineInstances[i];
                    var p1Pdf = PdfDrawingHelpers.WorldToPdf(new Vector2d(v.Start.X, v.Start.Y), worldToPdf);
                    var p2Pdf = PdfDrawingHelpers.WorldToPdf(new Vector2d(v.End.X, v.End.Y), worldToPdf);

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
                    var p0 = PdfTransform.WorldToPdf(verts[i].Position.ToVector2d(), worldToPdf);
                    var p1 = PdfTransform.WorldToPdf(verts[i + 1].Position.ToVector2d(), worldToPdf);
                    var p2 = PdfTransform.WorldToPdf(verts[i + 2].Position.ToVector2d(), worldToPdf);
                    path.AddPolygon(new[] { p0, p1, p2 });
                }

                gfx.DrawPath(brush, path);
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
                LineInstances = GetLineVertices(vertices, layerId, objectId, lineTypeId, renderOrigin);
                TextVertices = [];
            }
            else
            {
                FontSizeFactor = TextRenderingHelpers.GetFontSizeFactor(resCache, TextLayout, _fontFace);
                (List<Vector2d> vertices, RawRectangleF bounds) = 
                    TextRenderingHelpers.TesselateTextLayout(resCache, TextLayout, Text, _fontFace);
                Bounds = bounds.ToRect();
                TextVertices = GetTextVertices(vertices, layerId, objectId, renderOrigin);

                LineInstances = [];
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

        public override double DistanceToPoint(Vector2d p, MsdfAtlas atlas = null)
        {
            return double.MaxValue;
        }
        public override void UpdateBounds()
        {
            if (TextRenderStyle == TextRenderStyle.Stroke)
            {
                if (LineInstances.Count == 0)
                {
                    Bounds = System.Windows.Rect.Empty;
                    return;
                }

                float minX = Math.Min(LineInstances.Min(v => v.Start.X), LineInstances.Min(v => v.End.X));
                float maxX = Math.Max(LineInstances.Max(v => v.Start.X), LineInstances.Max(v => v.End.X));
                float minY = Math.Min(LineInstances.Min(v => v.Start.Y), LineInstances.Min(v => v.End.Y));
                float maxY = Math.Max(LineInstances.Max(v => v.Start.Y), LineInstances.Max(v => v.End.Y));

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
            _textFormat = new(factory, FontFamilyName, (float)TextHeight);
        }
        public void GetTextLayout(SharpDX.DirectWrite.Factory1 factory)
        {
            TextLayout = new(factory, Text, _textFormat, (float)Bounds.Width, (float)Bounds.Height, 96, true);
        }

        public List<LineInstance> GetLineVertices(
            List<Vector2d> vertices, uint layerId, uint objectId, uint lineTypeId, Vector2d renderOrigin)
        {
            List<LineInstance> lineInstances = [];

            if (TextRenderStyle == TextRenderStyle.Triangle) { return lineInstances; }

            if (LffFont is null)
            {
                throw new Exception("LffFont is null.");
            }

            Bounds = MathHelpers.GetLocalBounds(vertices);
            TextHeightScaleFactor = TextHeight / LffFont.DesignHeight;
            AttachmentOffset = UpdateAttachmentOffset();

            var transform = Transform.ToSharpDxMatrix();

            for (int i = 0; i < vertices.Count; i += 2)
            {
                var s = vertices[i].ToGpuRelative(renderOrigin);
                var e = vertices[i + 1].ToGpuRelative(renderOrigin);

                LineInstance lineInstance = new(
                    Vector2.TransformCoordinate(s, transform),
                    Vector2.TransformCoordinate(e, transform),
                    layerId, objectId, 0, (uint)LineInstanceFlags.None, 0);

                lineInstances.Add(lineInstance);
            }
            return lineInstances;
        }
        public List<TextVertex> GetTextVertices(
            List<Vector2d> vertices, uint layerId, uint objectId, Vector2d renderOrigin)
        {
            List<TextVertex> textVertices = [];

            if (TextRenderStyle == TextRenderStyle.Stroke) { return textVertices; }

            AttachmentOffset = TextRenderingHelpers.GetAttachmentOffset(Bounds.ToRectangeF(), AttachmentPoint).ToVector2d();
            TextHeightScaleFactor = 1;

            var transform = Transform.ToSharpDxMatrix();

            for (int i = 0; i < vertices.Count; i += 3)
            {
                var v1 = vertices[i].ToGpuRelative(renderOrigin);
                var v2 = vertices[i + 1].ToGpuRelative(renderOrigin);
                var v3 = vertices[i + 2].ToGpuRelative(renderOrigin);

                var scaledVector1 = Vector2.TransformCoordinate(v1, transform);
                TextVertex textVertex1 = new(v1, layerId, objectId);
                var scaledVector2 = Vector2.TransformCoordinate(v2, transform);
                TextVertex textVertex2 = new(v2, layerId, objectId);
                var scaledVector3 = Vector2.TransformCoordinate(v3, transform);
                TextVertex textVertex3 = new(v3, layerId, objectId);

                textVertices.AddRange([textVertex1, textVertex2, textVertex3]);
            }

            return textVertices;
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

        private Vector2d UpdateAttachmentOffset()
        {
            var xOffset = Bounds.Width;
            var yOffset = TextHeight / TextHeightScaleFactor;
            var bottomOffset = Bounds.Height - yOffset;

            return AttachmentPoint switch
            {
                TextAttachmentPoint.TopLeft =>
                    new Vector2d(0, -yOffset),

                TextAttachmentPoint.TopCenter =>
                    new Vector2d(-(xOffset / 2), -yOffset),

                TextAttachmentPoint.TopRight =>
                    new Vector2d(-xOffset, -yOffset),

                TextAttachmentPoint.MiddleLeft =>
                    new Vector2d(0, -yOffset / 2),

                TextAttachmentPoint.MiddleCenter =>
                    new Vector2d(-(xOffset / 2), -yOffset / 2),

                TextAttachmentPoint.MiddleRight =>
                    new Vector2d(-xOffset, -yOffset / 2),

                TextAttachmentPoint.BottomLeft =>
                    new Vector2d(0, bottomOffset),

                TextAttachmentPoint.BottomCenter =>
                    new Vector2d(-(xOffset / 2), bottomOffset),

                TextAttachmentPoint.BottomRight =>
                    new Vector2d(-xOffset, bottomOffset),

                _ => Vector2d.Zero
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
