using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;
using Cad_Point_Manager.Extensions;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Services.LayoutExporting;
using netDxf.Entities;
using PdfSharpCore.Drawing;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using Point = System.Windows.Point;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingSText : DrawingText
    {
        #region Fields
        private const float _flatteningTolerance = 0.001f;
        private const int _fontRenderingMinimumSize = 50;

        private protected TextFormat _textFormat;
        private protected FontFace _fontFace;
        #endregion

        #region Properties
        public override List<TextVertex> TextVertices { get; set; } = [];

        public Text DxfText { get; set; }
        public float Rotation { get; set; } = 0;
        public float TextHeight { get; set; }
        public string FontFamilyName { get; set; }
        public float WidthFactor { get; set; } = 1.0f;
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public System.Windows.Media.Matrix Transform { get; set; }
        public Enums.TextAttachmentPoint AttachmentPoint { get; set; }
        public TextLayout TextLayout { get; set; }
        public float FontSizeFactor { get; set; } = 1;

        public bool TextFormatCreated => _textFormat != null;
        public bool TextLayoutCreated => TextLayout != null;
        #endregion

        #region Constructor
        public DrawingSText(Text text, ObjectLayer layer, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingSText;
            EntityObject = text;
            DxfText = text;
            Layer = layer;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock3D = block;
            ColorByLayer = EntityObject.Color.IsByLayer;

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
                AttachmentPoint = GetAttachmentPoint(text.Alignment);
                Position = GetTextOrigin(AttachmentPoint, new RectangleF((float)Bounds.Left, (float)Bounds.Top, (float)Bounds.Width, (float)Bounds.Height),
                    new Vector3((float)text.Position.X, (float)text.Position.Y, 0));
                TextHeight = (float)text.Height;
                FontFamilyName = text.Style.FontFamilyName;
                Transform = GetTransform(text.Position);
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
        public override void DrawToPdf(
           XGraphics gfx,
           System.Windows.Media.Matrix worldToPdf,
           XPen pen)
        {
            var verts = TextVertices;
            if (verts == null || verts.Count < 3) { return; }

            var brush = new XSolidBrush(PdfTransform.ToXColor(Color.ToVector4()));
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

        /// <summary>
        /// Gets the upper left point of the MText.
        /// </summary>
        /// <param name="mText"></param>
        /// <param name="rect"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public Vector3 GetTextOrigin(Enums.TextAttachmentPoint attachmentPoint, RectangleF rect, Vector3 position)
        {
            Vector3 adjustedPos = Vector3.Zero;

            switch (attachmentPoint)
            {
                case Enums.TextAttachmentPoint.TopLeft:
                    adjustedPos = position;
                    break;

                case Enums.TextAttachmentPoint.TopCenter:
                    adjustedPos = new Vector3(position.X - (rect.Width) / 2,
                        position.Y, 0);
                    break;

                case Enums.TextAttachmentPoint.TopRight:
                    adjustedPos = new Vector3(position.X - (rect.Width),
                        position.Y, 0);
                    break;

                case Enums.TextAttachmentPoint.MiddleLeft:
                    adjustedPos = new Vector3(position.X,
                        position.Y - (rect.Height / 2), 0);
                    break;

                case Enums.TextAttachmentPoint.MiddleCenter:
                    adjustedPos = new Vector3(position.X - (rect.Width) / 2,
                        position.Y - (rect.Height / 2), 0);
                    break;

                case Enums.TextAttachmentPoint.MiddleRight:
                    adjustedPos = new Vector3(position.X - (rect.Width),
                        position.Y - (rect.Height / 2), 0);
                    break;

                case Enums.TextAttachmentPoint.BottomLeft:
                    adjustedPos = new Vector3(position.X,
                        position.Y - (rect.Height), 0);
                    break;

                case Enums.TextAttachmentPoint.BottomCenter:
                    adjustedPos = new Vector3(position.X - (rect.Width) / 2,
                        position.Y - (rect.Height), 0);
                    break;

                case Enums.TextAttachmentPoint.BottomRight:
                    adjustedPos = new Vector3(position.X - (rect.Width),
                        position.Y - (rect.Height), 0);
                    break;

                default:
                    adjustedPos = position;
                    break;
            }

            return adjustedPos;
        }

        public override void UpdateTextVertices(ResCache resCache, uint layerId, SceneIdMap sceneIdMap, D3dStateBuffers stateBuffers)
        {
            var objectId = sceneIdMap.GetOrAddObjectId(this, out var isNewObject);
            if (isNewObject) { stateBuffers.InitializeObjectState(sceneIdMap.ObjectCount, this, objectId); }
            GetTextFormat(resCache.WriteFactory);
            GetTextLayout(resCache.WriteFactory);
            Tesselate(resCache, layerId, objectId);
        }
        public override void MouseEnter()
        {
            throw new NotImplementedException();
        }
        public override void MouseLeave()
        {
            throw new NotImplementedException();
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
            for (int i = 0; i < TextVertices.Count; i++)
            {
                TextVertices[i].SetIsSelected(isSelected);
            }
        }

        public override double DistanceToPoint(Point p)
        {
            return 1000;
        }
        public override void UpdateBounds()
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
            _textFormat = new(factory, FontFamilyName, TextHeight);
        }

        public void GetTextLayout(SharpDX.DirectWrite.Factory1 factory)
        {
            TextLayout = new(factory, Text, _textFormat, (float)Bounds.Width, (float)Bounds.Height, 96, true);
        }

        public void Tesselate(ResCache resCache, uint layerId, uint objectId)
        {
            UpdateFontFace(resCache);

            FontSizeFactor = TextRenderingHelpers.GetFontSizeFactor(resCache, TextLayout, _fontFace);
            (List<Vector2> vertices, RawRectangleF bounds) = TextRenderingHelpers.TesselateTextLayout(resCache, TextLayout, Text, _fontFace);

            TextVertices = GetVertices(vertices, layerId, objectId);
            UpdateBounds();
        }

        public List<TextVertex> GetVertices(List<Vector2> vertices, uint layerId, uint objectId)
        {
            List<TextVertex> textVertices = [];
            Matrix translationTransform = Matrix.Translation((float)Transform.OffsetX, (float)Transform.OffsetY, 0);

            for (int i = 0; i < vertices.Count; i += 3)
            {
                var v1 = vertices[i];
                var v2 = vertices[i + 1];
                var v3 = vertices[i + 2];

                var scaledVector1 = Vector2.TransformCoordinate(v1, translationTransform);
                TextVertex textVertex1 = new(new Vector3(scaledVector1.X, scaledVector1.Y, 0), layerId, objectId, isMouseOver: 0, isSelected: 0);
                var scaledVector2 = Vector2.TransformCoordinate(v2, translationTransform);
                TextVertex textVertex2 = new(new Vector3(scaledVector2.X, scaledVector2.Y, 0), layerId, objectId, isMouseOver: 0, isSelected: 0);
                var scaledVector3 = Vector2.TransformCoordinate(v3, translationTransform);
                TextVertex textVertex3 = new(new Vector3(scaledVector3.X, scaledVector3.Y, 0), layerId, objectId, isMouseOver: 0, isSelected: 0);

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
