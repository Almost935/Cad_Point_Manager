using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using System.Diagnostics;
using System.Windows;

using FontStyle = SharpDX.DirectWrite.FontStyle;
using FontWeight = SharpDX.DirectWrite.FontWeight;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class DrawingMtextSegment3D : IDisposable
    {
        #region Fields
        private const float _flatteningTolerance = 0.001f;

        private int _fontRenderingMinimumSize;
        #endregion

        #region Properties
        public DrawingMtext3D DrawingMtext3D { get; set; }
        public string Text { get; set; }
        public Vector4 Color { get; set; }
        public Vector3 Position { get; set; }
        public float Rotation { get; set; } = 0;
        public float FontHeight { get; set; }
        public float FontSize { get; set; }
        public string FontFamilyName { get; set; }
        public System.Windows.Media.Matrix Transform { get; set; }
        public TextFormat TextFormat { get; set; }
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
        public float RowXOffset { get; set; } = 0; // This is used to offset the segment within a row for alignment purposes.
        public float GlowOffset { get; set; }

        public bool TextFormatCreated => TextFormat != null;
        public bool TextLayoutCreated => TextLayout != null;
        public bool TextVerticesCreated => TextVertices.Length > 0;
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
            FontHeight = fontHeight;
            FontSize = TextRenderingHelpers.TextHeightToFontSize(fontHeight);
            FontFamilyName = fontFamilyName;
            IsItalic = isItalic;
            IsBold = isBold;
            IsUnderlined = isUnderlined;
            IsStrikeThroughed = isStrikethroughed;
            IsNewLine = isNewLine;
            _fontRenderingMinimumSize = fontRenderingMinimumSize;
            MaxWidth = maxWidth;
            TextAlignment = textAlignment;
            GlowOffset = FontHeight * GlobalHelperProperties._textHeightToGlowOffsetFactor;

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

            TextFormat = new(factory, FontFamilyName, fontWeight, fontStyle, FontHeight);
            TextLayout = new(factory, Text, TextFormat, (float)Bounds.Width, (float)Bounds.Height, 96, true);

            SpaceWidth = FontHeight * 0.6f;
        }

        public void Tesselate(SharpDX.Direct2D1.Factory2 factory)
        {
            float fontSizeScaleFactor = _fontRenderingMinimumSize / FontHeight;

            (TransformedGeometry geometry, RawRectangleF bounds) = TextRenderingHelpers.CreateTextGeometry(factory, Text, TextLayout,
                fontSizeScaleFactor, FontHeight, _flatteningTolerance);

            var vertices = TextRenderingHelpers.TessellateGeometry(geometry, _flatteningTolerance);

            UpdateBounds(bounds);

            TextVertices = GetVertices(vertices, 1.00f / fontSizeScaleFactor);

            geometry.Dispose();
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

        public TextVertex[] GetVertices(List<Vector2> vertices, float scaleFactor = 1)
        {
            List<TextVertex> textVertices = [];
            Matrix scaleTransform = Matrix.Scaling(scaleFactor, scaleFactor, 1);

            for (int i = 0; i < vertices.Count; i += 3)
            {
                var v1 = vertices[i];
                var v2 = vertices[i + 1];
                var v3 = vertices[i + 2];
                Vector2 centroid = (v1 + v2 + v3) / 3;

                Vector2 direction1 = Vector2.Normalize(v1 - centroid);
                Vector2 direction2 = Vector2.Normalize(v2 - centroid);
                Vector2 direction3 = Vector2.Normalize(v3 - centroid);

                var scaledVector1 = Vector2.TransformCoordinate(v1, scaleTransform);
                TextVertex textVertex1 = new(new Vector3(scaledVector1.X, scaledVector1.Y, 0), Color, direction1, 1, 0, 1, GlowOffset);
                var scaledVector2 = Vector2.TransformCoordinate(v2, scaleTransform);
                TextVertex textVertex2 = new(new Vector3(scaledVector2.X, scaledVector2.Y, 0), Color, direction2, 1, 0, 1, GlowOffset);
                var scaledVector3 = Vector2.TransformCoordinate(v3, scaleTransform);
                TextVertex textVertex3 = new(new Vector3(scaledVector3.X, scaledVector3.Y, 0), Color, direction3, 1, 0, 1, GlowOffset);

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
        #endregion

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    TextFormat?.Dispose();
                    TextLayout?.Dispose();
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
