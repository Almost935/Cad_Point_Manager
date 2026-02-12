using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using PdfSharpCore.Drawing;
using SharpDX;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using System.Windows;

using FontStretch = SharpDX.DirectWrite.FontStretch;
using FontStyle = SharpDX.DirectWrite.FontStyle;
using FontWeight = SharpDX.DirectWrite.FontWeight;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingMtextSegment : DrawingObject, IDisposable
    {
        #region Fields
        private const float _flatteningTolerance = 0.001f;

        private int _fontRenderingMinimumSize;
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
        #endregion

        #region Constructors
        public DrawingMtextSegment(DrawingMtext drawingMtext, string text, Vector4 color, Vector3 position, float rotation,
            float fontHeight, string fontFamilyName, bool isItalic, bool isBold, bool isUnderlined, bool isStrikeOut, bool isOverStrike,
            bool isNewLine, int fontRenderingMinimumSize, float maxWidth, Enums.TextAlignment textAlignment = Enums.TextAlignment.Left)
        {
            Type = DrawingObjectType.DrawingMtextSegment;
            DrawingMtext = drawingMtext;
            EntityObject = drawingMtext.EntityObject;
            ColorByLayer = EntityObject.Color.IsByLayer;
            Text = text;
            Color = color;
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
            _fontRenderingMinimumSize = fontRenderingMinimumSize;
            MaxWidth = maxWidth;
            TextAlignment = textAlignment;
            GlowOffset = TextHeight * GlobalHelperProperties.TextHeightToGlowOffsetFactor;

            UpdateTransform();
        }
        #endregion

        #region Methods
        public override void UpdateData()
        {

        }
        public override void DrawToD2dDeviceContext(SharpDX.Direct2D1.DeviceContext1 deviceContext, SharpDX.Direct2D1.Factory2 factory,
            SharpDX.Direct2D1.Brush brush, float thickness, SharpDX.Direct2D1.StrokeStyle1 strokeStyle)
        {

        }
        public override void DrawToPdf(
            XGraphics gfx,
            System.Windows.Media.Matrix worldToPdf,
            XPen pen)
        { }

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
            Span<TextVertex> vertexSpan = TextVertices.AsSpan();

            for (int i = 0; i < vertexSpan.Length; i++)
            {
                vertexSpan[i].SetIsMouseOver(isMouseOver);
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
            Span<TextVertex> vertexSpan = TextVertices.AsSpan();

            for (int i = 0; i < vertexSpan.Length; i++)
            {
                vertexSpan[i].SetIsSelected(isSelected);
            }
        }
        public override double DistanceToPoint(System.Windows.Point p)
        {
            return 0;
        }
        public override void UpdateBounds()
        {

        }

        public void GetTextLayout(Factory1 factory)
        {
            FontWeight fontWeight;
            if (IsBold) { fontWeight = FontWeight.Bold; } else { fontWeight = FontWeight.Normal; }

            FontStyle fontStyle;
            if (IsItalic) { fontStyle = FontStyle.Italic; } else { fontStyle = FontStyle.Normal; }

            _textFormat = new(factory, FontFamilyName, fontWeight, fontStyle, TextHeight);
            TextLayout = new(factory, Text, _textFormat, float.MaxValue, float.MaxValue, 96, true);

            SpaceWidth = TextHeight * GlobalHelperProperties.TextHeightToSpaceWidthFactor;
        }

        public void Tesselate(ResCache resCache, uint layerId, uint objectId)
        {
            UpdateFontFace(resCache);
            FontSizeFactor = TextRenderingHelpers.GetFontSizeFactor(resCache, TextLayout, _fontFace);
            (List<Vector2> vertices, RawRectangleF bounds) = TextRenderingHelpers.TesselateTextLayout(resCache, TextLayout, Text, _fontFace);
            UpdateBounds(bounds);
            TextVertices = GetVertices(vertices, layerId, objectId);
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

        public TextVertex[] GetVertices(List<Vector2> vertices, uint layerId, uint objectId)
        {
            List<TextVertex> textVertices = [];

            for (int i = 0; i < vertices.Count; i += 3)
            {
                var v1 = vertices[i];
                var v2 = vertices[i + 1];
                var v3 = vertices[i + 2];

                TextVertex textVertex1 = new(new Vector3(v1.X, v1.Y, 0), layerId, objectId, isMouseOver: 0, isSelected: 0);
                TextVertex textVertex2 = new(new Vector3(v2.X, v2.Y, 0), layerId, objectId, isMouseOver: 0, isSelected: 0);
                TextVertex textVertex3 = new(new Vector3(v3.X, v3.Y, 0), layerId, objectId, isMouseOver: 0, isSelected: 0);

                textVertices.AddRange([textVertex1, textVertex2, textVertex3]);
            }

            return textVertices.ToArray();
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
