using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.TextRendering;
using netDxf.Entities;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Point = System.Windows.Point;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public abstract class DrawingText3D : DrawingObject3D
    {
        #region Fields
        protected const double _textHeightToFontSizeFactor = 0.75;

        private protected TextFormat _textFormat;
        #endregion

        #region Properties
        public string Text { get; set; }
        public Vector3 Position { get; set; }
        public int StartVertexIndex { get; set; }
        public int EndVertexIndex { get; set; }
        public float Rotation { get; set; } = 0;
        public int FontSize { get; set; }
        public string FontFamilyName { get; set; }
        public float WidthFactor { get; set; } = 1.0f;
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public System.Windows.Media.Matrix Transform { get; set; }
        public Enums.TextAttachmentPoint AttachmentPoint { get; set; }
        public TextLayout TextLayout { get; set; }
        public TextGeometryVertex[] TextGeometryVertices { get; set; } = [];

        public bool TextFormatCreated => _textFormat != null;
        public bool TextLayoutCreated => TextLayout != null;
        public bool TextVerticesCreated => TextGeometryVertices.Length > 0;
        #endregion

        #region Methods
        public override void Select()
        {
            throw new NotImplementedException();
        }
        public override void Deselect()
        {
            throw new NotImplementedException();
        }
        public override double DistanceToPoint(Point p)
        {
            return 1000;
        }
        public override bool HitTest(Point point, float tolerance)
        {
            return false;
        }
        public override void UpdateBounds()
        {
            throw new NotImplementedException();
        }
        public override void DrawToD2dDeviceContext(DeviceContext1 deviceContext, SharpDX.Direct2D1.Factory2 factory, Brush brush, float thickness, StrokeStyle1 strokeStyle)
        {
            //deviceContext.DrawTextLayout(new RawVector2((float)Position.X, -(float)Position.Y), TextLayout, brush);
        }



        private protected Enums.TextAttachmentPoint GetAttachmentPoint(MTextAttachmentPoint mTextAttachment)
        {
            return mTextAttachment switch
            {
                MTextAttachmentPoint.TopLeft => Enums.TextAttachmentPoint.TopLeft,
                MTextAttachmentPoint.TopCenter => Enums.TextAttachmentPoint.TopCenter,
                MTextAttachmentPoint.TopRight => Enums.TextAttachmentPoint.TopRight,
                MTextAttachmentPoint.MiddleLeft => Enums.TextAttachmentPoint.MiddleLeft,
                MTextAttachmentPoint.MiddleCenter => Enums.TextAttachmentPoint.MiddleCenter,
                MTextAttachmentPoint.MiddleRight => Enums.TextAttachmentPoint.MiddleRight,
                MTextAttachmentPoint.BottomLeft => Enums.TextAttachmentPoint.BottomLeft,
                MTextAttachmentPoint.BottomCenter => Enums.TextAttachmentPoint.BottomCenter,
                MTextAttachmentPoint.BottomRight => Enums.TextAttachmentPoint.BottomRight,
                _ => Enums.TextAttachmentPoint.MiddleCenter,
            };
        }
        private protected Enums.TextAttachmentPoint GetAttachmentPoint(netDxf.Entities.TextAlignment mTextAttachment)
        {
            return mTextAttachment switch
            {
                netDxf.Entities.TextAlignment.TopLeft => Enums.TextAttachmentPoint.TopLeft,
                netDxf.Entities.TextAlignment.TopCenter => Enums.TextAttachmentPoint.TopCenter,
                netDxf.Entities.TextAlignment.TopRight => Enums.TextAttachmentPoint.TopRight,
                netDxf.Entities.TextAlignment.MiddleLeft => Enums.TextAttachmentPoint.MiddleLeft,
                netDxf.Entities.TextAlignment.MiddleCenter => Enums.TextAttachmentPoint.MiddleCenter,
                netDxf.Entities.TextAlignment.MiddleRight => Enums.TextAttachmentPoint.MiddleRight,
                netDxf.Entities.TextAlignment.BottomLeft => Enums.TextAttachmentPoint.BottomLeft,
                netDxf.Entities.TextAlignment.BottomCenter => Enums.TextAttachmentPoint.BottomCenter,
                netDxf.Entities.TextAlignment.BottomRight => Enums.TextAttachmentPoint.BottomRight,
                _ => Enums.TextAttachmentPoint.MiddleCenter,
            };
        }

        private protected System.Windows.Media.Matrix GetTransform(netDxf.Vector3 dxfPos)
        {
            System.Windows.Media.Matrix matrix = new();
            //matrix.ScaleAt(1, 1, dxfPos.X, dxfPos.Y);
            matrix.Translate(dxfPos.X, dxfPos.Y);
            return matrix;
        }

        //private protected TextQuadVertex CreateTextVertex(Vector3 position, char c, float xOffset, float lineHeight, Vector4 color)
        //{
        //    Vector3 vertexPosition = new(position.X + xOffset, position.Y, 0);
        //    Vector3 texCoord = GetTextureCoordinatesForChar(c); // You'll need a font texture atlas
        //    float isVisible = 1.0f;  // Set this based on visibility rules
        //    Matrix rotation = Matrix.RotationZ(Rotation);

        //    return new TextQuadVertex(vertexPosition, color, texCoord, isVisible, rotation);
        //}

        public void GetTextFormat(SharpDX.DirectWrite.Factory1 factory)
        {
            _textFormat = new(factory, FontFamilyName, FontSize);
        }

        public void GetTextLayout(SharpDX.DirectWrite.Factory1 factory)
        {
            TextLayout = new(factory, Text, _textFormat, (float)Bounds.Width, (float)Bounds.Height, 96, true);
        }

        public void Tesselate(SharpDX.Direct2D1.Factory2 factory, RenderTarget renderTarget)
        {
            var geometry = TextRenderingHelpers.CreateTextGeometry(factory, Text, TextLayout);
            var vertices = TextRenderingHelpers.TessellateGeometry(geometry);
            
            TextGeometryVertices = GetVertices(vertices);

            geometry.Dispose();
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

        public static float ConvertDxfHeightToFontSize(float height)
        {
            //// Convert millimeters to inches (1 inch = 25.4 mm)
            //float heightInInches = dxfHeightInMm / 25.4f;

            //// Convert inches to points (1 inch = 72 points)
            //float fontSizeInPoints = heightInInches * 72f;

            return height;
        }
        #endregion
    }
}
