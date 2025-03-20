using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using SharpDX;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
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
        public int FontSize { get; set; }
        public string FontFamilyName { get; set; }
        public System.Windows.Media.Matrix Transform { get; set; }
        public TextFormat TextFormat { get; set; }
        public TextLayout TextLayout { get; set; }
        public TextVertex[] TextGeometryVertices { get; set; } = [];
        public float DxfTextHeight { get; set; }
        public float MaxWidth { get;set; }
        public Rect Bounds { get; set; }
        public bool IsItalic { get; set; } = false;
        public bool IsBold { get; set; } = false;
        public bool IsUnderlined { get;set; } = false;
        public bool IsStrikeThroughed { get; set; } = false;

        public bool TextFormatCreated => TextFormat != null;
        public bool TextLayoutCreated => TextLayout != null;
        public bool TextVerticesCreated => TextGeometryVertices.Length > 0;
        #endregion

        #region Constructors
        public DrawingMtextSegment3D(DrawingMtext3D drawingMtext3D, string text, Vector4 color, Vector3 position, float rotation, 
            int fontSize, string fontFamilyName, float dxfTextHeight, bool isItalic, bool isBold, bool isUnderlined, bool isStrikethroughed, 
            int fontRenderingMinimumSize, float maxWidth)
        {
            DrawingMtext3D = drawingMtext3D;
            Text = text;
            Color = color;
            Position = position;
            Rotation = rotation;
            FontSize = fontSize;
            FontFamilyName = fontFamilyName;
            DxfTextHeight = dxfTextHeight;
            IsItalic = isItalic; 
            IsBold = isBold; 
            IsUnderlined = isUnderlined;
            IsStrikeThroughed = isStrikethroughed;
            _fontRenderingMinimumSize = fontRenderingMinimumSize;
            MaxWidth = maxWidth;

            UpdateTransform();
        }
        #endregion

        #region Methods
        public void GetTextFormat(Factory1 factory)
        {
            FontWeight fontWeight;
            if (IsBold) { fontWeight = FontWeight.Bold; } else { fontWeight = FontWeight.Normal; }

            FontStyle fontStyle; 
            if (IsItalic) { fontStyle = FontStyle.Italic; } else { fontStyle = FontStyle.Normal; }

            TextFormat = new(factory, FontFamilyName, fontWeight, fontStyle, FontSize);
        }

        public void GetTextLayout(Factory1 factory)
        {
            TextLayout = new(factory, Text, TextFormat, (float)Bounds.Width, (float)Bounds.Height, 96, true);
        }

        public void Tesselate(SharpDX.Direct2D1.Factory2 factory)
        {
            float fontSizeScaleFactor = _fontRenderingMinimumSize / (float)FontSize;

            (SharpDX.Direct2D1.TransformedGeometry geometry, RawRectangleF bounds) = TextRenderingHelpers.CreateTextGeometry(factory, Text, TextLayout, fontSizeScaleFactor, _flatteningTolerance);
            var vertices = TextRenderingHelpers.TessellateGeometry(geometry, _flatteningTolerance);

            UpdateBounds(bounds);
            
            TextGeometryVertices = GetVertices(vertices, 1.00f / fontSizeScaleFactor);

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
            List<TextVertex> textGeometries = [];
            Matrix scaleTransform = Matrix.Scaling(scaleFactor, scaleFactor, 1);
            Matrix translationTransform = Matrix.Translation((float)Transform.OffsetX, (float)Transform.OffsetY, 0);

            foreach (var vector in vertices)
            {
                // Apply the scale transform
                var scaledVector = Vector2.TransformCoordinate(vector, scaleTransform);
                var translatedVector = Vector2.TransformCoordinate(scaledVector, translationTransform);

                TextVertex textGeometryVertex = new(new Vector3(translatedVector.X, translatedVector.Y, 0), Color);
                textGeometries.Add(textGeometryVertex);
            }

            return textGeometries.ToArray();
        }

        public void Translate(Vector2 offset)
        {
            for (int i = 0; i < TextGeometryVertices.Length; i++)
            {
                TextGeometryVertices[i] = TextGeometryVertices[i].Translate(offset);
            }
        }

        public void Translate(Vector3 offset)
        {
            for (int i = 0; i < TextGeometryVertices.Length; i++)
            {
                TextGeometryVertices[i] = TextGeometryVertices[i].Translate(offset);
            }
        }

        public void Split(int index)
        {
            //var firstSegment = new DrawingMtextSegment3D(DrawingMtext3D, Text.Substring(0, index), Color, Position, Rotation, FontSize, FontFamilyName, DxfTextHeight, IsItalic, IsBold, IsUnderlined, IsStrikeThroughed, _fontRenderingMinimumSize, MaxWidth);
            //var secondSegment = new DrawingMtextSegment3D(DrawingMtext3D, Text.Substring(index), Color, Position, Rotation, FontSize, FontFamilyName, DxfTextHeight, IsItalic, IsBold, Is
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
