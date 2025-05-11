using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using SharpDX;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using System.Windows;

using FontStretch = SharpDX.DirectWrite.FontStretch;
using FontStyle = SharpDX.DirectWrite.FontStyle;
using FontWeight = SharpDX.DirectWrite.FontWeight;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class DrawingMtextSegment3D : IDisposable
    {
        #region Fields
        private const float _flatteningTolerance = 0.001f;

        private int _fontRenderingMinimumSize;
        private TextFormat _textFormat = null;
        private FontFace _fontFace = null;
        #endregion

        #region Properties
        public DrawingMtext3D DrawingMtext3D { get; set; }
        public string Text { get; set; }
        public Vector4 Color { get; set; }
        public Vector3 Position { get; set; }
        public float Rotation { get; set; } = 0;
        public float TextHeight { get; set; }
        public string FontFamilyName { get; set; }
        public System.Windows.Media.Matrix Transform { get; set; }
        public TextLayout TextLayout { get; set; }
        public TextVertex[] TextVertices { get; set; } = [];
        public float MaxWidth { get; set; }
        public Rect Bounds { get; set; }
        public bool IsItalic { get; set; } = false;
        public bool IsBold { get; set; } = false;
        public bool IsUnderlined { get; set; } = false;
        public bool IsStrikeThroughed { get; set; } = false;
        public bool IsNewLine { get; set; } = false;
        public Enums.TextAlignment TextAlignment { get; set; }
        public float SpaceWidth { get; set; }
        public float RowXOffset { get; set; } = 0;
        public float GlowOffset { get; set; }
        #endregion

        #region Constructors
        public DrawingMtextSegment3D(DrawingMtext3D drawingMtext3D, string text, Vector4 color, Vector3 position, float rotation,
            float fontHeight, string fontFamilyName, bool isItalic, bool isBold, bool isUnderlined, bool isStrikethroughed,
            bool isNewLine, int fontRenderingMinimumSize, float maxWidth, Enums.TextAlignment textAlignment = Enums.TextAlignment.Left)
        {
            DrawingMtext3D = drawingMtext3D;
            Text = text;
            Color = color;
            Position = position;
            Rotation = rotation;
            TextHeight = fontHeight;
            FontFamilyName = fontFamilyName;
            IsItalic = isItalic;
            IsBold = isBold;
            IsUnderlined = isUnderlined;
            IsStrikeThroughed = isStrikethroughed;
            IsNewLine = isNewLine;
            _fontRenderingMinimumSize = fontRenderingMinimumSize;
            MaxWidth = maxWidth;
            TextAlignment = textAlignment;
            GlowOffset = TextHeight * GlobalHelperProperties._textHeightToGlowOffsetFactor;

            UpdateTransform();
        }
        #endregion

        #region Methods
        public void GetTextLayout(SharpDX.DirectWrite.Factory1 factory)
        {
            FontWeight fontWeight;
            if (IsBold) { fontWeight = FontWeight.Bold; } else { fontWeight = FontWeight.Normal; }

            FontStyle fontStyle;
            if (IsItalic) { fontStyle = FontStyle.Italic; } else { fontStyle = FontStyle.Normal; }

            _textFormat = new(factory, FontFamilyName, fontWeight, fontStyle, TextHeight);
            TextLayout = new(factory, Text, _textFormat, (float)Bounds.Width, (float)Bounds.Height, 96, true);

            SpaceWidth = TextHeight * GlobalHelperProperties._textHeightToSpaceWidthFactor;
        }

        public void Tesselate(D3dResCache resCache)
        {
            UpdateFontFace(resCache);

            (List<Vector2> vertices, RawRectangleF bounds) = TextRenderingHelpers.TesselateTextLayout(resCache, TextLayout, Text, TextHeight, _fontFace);
            UpdateBounds(bounds);
            TextVertices = GetVertices(vertices);
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

        public TextVertex[] GetVertices(List<Vector2> vertices)
        {
            List<TextVertex> textVertices = [];

            for (int i = 0; i < vertices.Count; i += 3)
            {
                var v1 = vertices[i];
                var v2 = vertices[i + 1];
                var v3 = vertices[i + 2];

                TextVertex textVertex1 = new(new Vector3(v1.X, v1.Y, 0), Color, isVisible: 1, isMouseOver: 0, isSelected: 0);
                TextVertex textVertex2 = new(new Vector3(v2.X, v2.Y, 0), Color, isVisible: 1, isMouseOver: 0, isSelected: 0);
                TextVertex textVertex3 = new(new Vector3(v3.X, v3.Y, 0), Color, isVisible: 1, isMouseOver: 0, isSelected: 0);

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

        private void UpdateFontFace(D3dResCache resCache)
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
