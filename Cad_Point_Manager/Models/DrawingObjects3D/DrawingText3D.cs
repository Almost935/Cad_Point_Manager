using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D2DControl;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Models.DrawingObjects;
using Cad_Point_Manager.Models.SerializableObjects;
using netDxf.Entities;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D9;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using System.Windows;
using System.Windows.Media;

using Brush = SharpDX.Direct2D1.Brush;
using Factory1 = SharpDX.DirectWrite.Factory1;
using Point = System.Windows.Point;


namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class DrawingText3D : DrawingObject3D
    {
        #region Fields
        private TextFormat _textFormat;
        private TextLayout _textLayout;
        #endregion

        #region Properties
        public string Text { get; set; }
        public Vector3 Position { get; set; }
        public int StartVertexIndex { get; set; }
        public int EndVertexIndex { get; set; }
        public List<TextVertex> TextVertices { get; set; } = [];
        public float Rotation { get; set; } = 0;
        public float FontSize { get; set; }
        public string FontFamilyName { get; set; }
        public float WidthFactor { get; set; } = 1.0f;
        public System.Windows.Media.Matrix Transform { get; set; }
        public Enums.TextAttachmentPoint AttachmentPoint { get; set; }
        public bool TextFormatCreated => _textFormat != null;
        public bool TextLayoutCreated => _textLayout != null;
        #endregion

        #region Constructor
        public DrawingText3D(MText mtext, ObjectLayer3D layer, bool isPartOfBlock = false, DrawingBlock3D block = null)
        {
            EntityObject = mtext;
            Layer = layer;
            DrawingBlock3D = block;

            UpdateColor();
            UpdateData(mtext);
        }
        #endregion

        #region Methods
        public override void DrawToD2D(DeviceContext1 deviceContext, SharpDX.Direct2D1.Factory2 factory, Brush brush, float thickness, StrokeStyle1 strokeStyle)
        {
            deviceContext.DrawTextLayout(new RawVector2((float)Position.X, -(float)Position.Y), _textLayout, brush);
        }
        public override void UpdateData(EntityObject entity)
        {
            if (entity is MText mText)
            {
                Text = mText.PlainText();
                Bounds = new(mText.Position.X, mText.Position.Y, mText.RectangleWidth * 2, mText.Height * 2);
                Rotation = (float)mText.Rotation;
                AttachmentPoint = GetAttachmentPoint(mText.AttachmentPoint);
                Position = GetTextOrigin(AttachmentPoint, new RectangleF((float)Bounds.Left, (float)Bounds.Top, (float)Bounds.Width, (float)Bounds.Height), new Vector3((float)mText.Position.X, (float)mText.Position.Y, 0));
                FontSize = (float)(mText.Height * 1.25);
                FontFamilyName = mText.Style.FontFamilyName;
                Transform = GetTransform(mText.Position);
                
                UpdateTextVertices(mText);
            }
            else if (entity is Text text)
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
                FontSize = (float)(text.Height * 1.25);
                FontFamilyName = text.Style.FontFamilyName;
                Transform = GetTransform(text.Position);

                UpdateTextVertices(text);
            }
            else
            {
                throw new ArgumentException("EntityObject must be of type MText or Text");
            }
        }
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


        public void UpdateTextVertices(MText mtext)
        {
            float xOffset = 0;  // Starting position for each line of text
            float lineHeight = 1.0f;  // Adjust based on your font size

            var lines = mtext.Value.Split('\n');
            foreach (var line in lines)  // Split by newline for multiline text
            {
                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];
                    TextVertex vertex = CreateTextVertex(Position, c, xOffset, lineHeight, Color);
                    TextVertices.Add(vertex);
                    xOffset += lineHeight;  
                }
                lineHeight *= 1.2f;  // Increase line height for next line (optional)
            }
        }
        public void UpdateTextVertices(Text text)
        {
            float xOffset = 0;  // Starting position for each line of text
            float lineHeight = 1.0f;  // Adjust based on your font size

            var lines = text.Value.Split('\n');
            foreach (var line in lines)  // Split by newline for multiline text
            {
                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];
                    TextVertex vertex = CreateTextVertex(Position, c, xOffset, lineHeight, Color);
                    TextVertices.Add(vertex);
                    xOffset += lineHeight;
                }
                lineHeight *= 1.2f;  // Increase line height for next line (optional)
            }
        }
        // Method to create a TextVertex for each character
        private TextVertex CreateTextVertex(Vector3 position, char c, float xOffset, float lineHeight, Vector4 color)
        {
            Vector3 vertexPosition = new Vector3(position.X + xOffset, position.Y, 0);
            Vector3 texCoord = GetTextureCoordinatesForChar(c); // You'll need a font texture atlas
            float isVisible = 1.0f;  // Set this based on visibility rules
            
            return new TextVertex(vertexPosition, color, texCoord, isVisible);
        }

        // Method to get texture coordinates for each character (using a font texture atlas)
        private Vector3 GetTextureCoordinatesForChar(char c)
        {
            // Assuming a font texture atlas where characters are placed in a grid
            // You need a way to map each character to its coordinates in the texture
            // For example, 'A' might be mapped to (0.0f, 0.0f), 'B' to (0.1f, 0.0f), etc.
            // This will vary depending on your font texture atlas setup.
            return new Vector3(c % 16 * 0.0625f, c / 16 * 0.0625f, 0); // Example assuming 16x16 grid of characters
        }

        public void GetTextFormat(Factory1 factory)
        {
            _textFormat = new(factory, FontFamilyName, FontSize);
        }
        public System.Windows.Media.Matrix GetTransform(netDxf.Vector3 dxfPos)
        {
            System.Windows.Media.Matrix matrix = new();
            matrix.ScaleAt(-1, -1, dxfPos.X, dxfPos.Y);
            return matrix;
        }
        public void GetTextLayout(Factory1 factory)
        {
            //RawMatrix3x2 transform = new((float)Transform.M11, (float)Transform.M12, (float)Transform.M21, (float)Transform.M22, (float)Transform.OffsetX, (float)Transform.OffsetY);
            RawMatrix3x2 transform = new(-1, 0, 0, -1, 0, 0);
            _textLayout = new(factory, Text, _textFormat, (float)Bounds.Width, (float)Bounds.Height, 96, transform, true);
        }
        private Enums.TextAttachmentPoint GetAttachmentPoint(MTextAttachmentPoint mTextAttachment)
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
        private Enums.TextAttachmentPoint GetAttachmentPoint(netDxf.Entities.TextAlignment mTextAttachment)
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
        #endregion
    }
}
