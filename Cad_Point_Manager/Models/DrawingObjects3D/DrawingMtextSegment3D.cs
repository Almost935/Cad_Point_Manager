using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using netDxf.Entities;
using SharpDX;
using SharpDX.DirectWrite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using FontStyle = SharpDX.DirectWrite.FontStyle;
using FontWeight = SharpDX.DirectWrite.FontWeight;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class DrawingMtextSegment3D
    {
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
        public TextGeometryVertex[] TextGeometryVertices { get; set; } = [];
        public float DxfTextHeight { get; set; }
        public float Width { get; set; }
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
            int fontSize, string fontFamilyName, float dxfTextHeight, float width, bool isItalic, bool isBold, bool isUnderlined, bool isStrikethroughed)
        {
            DrawingMtext3D = drawingMtext3D;
            Text = text;
            Color = color;
            Position = position;
            Rotation = rotation;
            FontSize = fontSize;
            FontFamilyName = fontFamilyName;
            DxfTextHeight = dxfTextHeight;
            Width = width;
            Bounds = new(Position.X, Position.Y, Width, DxfTextHeight * 1.2f);
            IsItalic = isItalic; 
            IsBold = isBold; 
            IsUnderlined = isUnderlined;
            IsStrikeThroughed = isStrikethroughed;

            Transform = GetTransform(new netDxf.Vector3(Position.X, Position.Y, Position.Z));
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
            var geometry = TextRenderingHelpers.CreateTextGeometry(factory, Text, TextLayout);
            var vertices = TextRenderingHelpers.TessellateGeometry(geometry);
            TextGeometryVertices = GetVertices(vertices);

            geometry.Dispose();
        }

        private protected System.Windows.Media.Matrix GetTransform(netDxf.Vector3 dxfPos)
        {
            System.Windows.Media.Matrix matrix = new();
            //matrix.ScaleAt(1, 1, dxfPos.X, dxfPos.Y);
            matrix.Translate(dxfPos.X, dxfPos.Y);
            return matrix;
        }

        public TextGeometryVertex[] GetVertices(List<Vector2> vertices)
        {
            List<TextGeometryVertex> textGeometries = [];
            Matrix transform = Matrix.Translation((float)Transform.OffsetX, (float)Transform.OffsetY, 0);

            foreach (var vertex in vertices)
            {
                var translatedVector = Vector2.TransformCoordinate(vertex, transform);

                TextGeometryVertex textGeometryVertex = new(new Vector3(translatedVector.X, translatedVector.Y, 0), Color);
                textGeometries.Add(textGeometryVertex);
            }

            return textGeometries.ToArray();
        }
        #endregion
    }
}
