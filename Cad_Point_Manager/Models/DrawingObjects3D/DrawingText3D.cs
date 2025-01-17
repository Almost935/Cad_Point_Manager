using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D2DControl;
using Cad_Point_Manager.Models.DrawingObjects;
using Cad_Point_Manager.Models.SerializableObjects;
using netDxf;
using netDxf.Entities;
using SharpDX.Direct2D1;
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
        private MText _dxfMtext;

        private TextFormat _textFormat;
        private TextLayout _textLayout;
        #endregion

        #region Properties
        public MText DxfMtext
        {
            get { return _dxfMtext; }
            set
            {
                _dxfMtext = value;
                OnPropertyChanged(nameof(DxfMtext));
            }
        }

        public string Text { get; set; }
        public Point Position { get; set; }
        public float Rotation { get; set; } = 0;
        public float FontSize { get; set; }
        public string FontFamilyName { get; set; }
        public Matrix Transform { get; set; }
        public Enums.TextAttachmentPoint AttachmentPoint { get; set; }
        public bool TextFormatCreated => _textFormat != null;
        public bool TextLayoutCreated => _textLayout != null;
        #endregion

        #region Constructor
        public DrawingText3D(MText mtext, ObjectLayer3D layer, bool isPartOfBlock = false, DrawingBlock3D block = null)
        {
            EntityObject = mtext;
            DxfMtext = mtext;
            Layer = layer;
            DrawingBlock3D = block;

            UpdateColor();
            UpdateData(mtext);
        }
        #endregion

        #region Methods
        public override void DrawToD2D(DeviceContext1 deviceContext, SharpDX.Direct2D1.Factory2 factory, Brush brush, float thickness, StrokeStyle1 strokeStyle)
        {
            deviceContext.DrawTextLayout(new RawVector2((float)DxfMtext.Position.X, -(float)DxfMtext.Position.Y), _textLayout, brush);
        }
        public override void UpdateData(EntityObject entity)
        {
            if (entity is MText mText)
            {
                Text = mText.PlainText();
                Bounds = new(DxfMtext.Position.X, DxfMtext.Position.Y, DxfMtext.RectangleWidth * 2, DxfMtext.Height * 2);
                Rotation = (float)mText.Rotation;
                AttachmentPoint = GetAttachmentPoint(mText.AttachmentPoint);
                Position = GetTextOrigin(AttachmentPoint, Bounds, new Point(mText.Position.X, mText.Position.Y));
                FontSize = (float)(DxfMtext.Height * 1.25);
                FontFamilyName = DxfMtext.Style.FontFamilyName;
                Transform = GetTransform(DxfMtext.Position);
            }
            else if (entity is Text)
            {

            }
            else
            {
                throw new ArgumentException("EntityObject must be of type MText");
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


        public void GetTextFormat(Factory1 factory)
        {
            _textFormat = new(factory, FontFamilyName, FontSize);
        }
        public Matrix GetTransform(Vector3 dxfPos)
        {
            Matrix matrix = new();
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

        /// <summary>
        /// Gets the upper left point of the MText.
        /// </summary>
        /// <param name="mText"></param>
        /// <param name="rect"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public Point GetTextOrigin(Enums.TextAttachmentPoint attachmentPoint, Rect rect, Point position)
        {
            Point adjustedPos = new();

            switch (attachmentPoint)
            {
                case Enums.TextAttachmentPoint.TopLeft:
                    adjustedPos = position;
                    break;

                case Enums.TextAttachmentPoint.TopCenter:
                    adjustedPos = new Point(position.X - (rect.Width) / 2,
                        position.Y);
                    break;

                case Enums.TextAttachmentPoint.TopRight:
                    adjustedPos = new Point(position.X - (rect.Width),
                        position.Y);
                    break;

                case Enums.TextAttachmentPoint.MiddleLeft:
                    adjustedPos = new Point(position.X,
                        position.Y - (rect.Height / 2));
                    break;

                case Enums.TextAttachmentPoint.MiddleCenter:
                    adjustedPos = new Point(position.X - (rect.Width) / 2,
                        position.Y - (rect.Height / 2));
                    break;

                case Enums.TextAttachmentPoint.MiddleRight:
                    adjustedPos = new Point(position.X - (rect.Width),
                        position.Y - (rect.Height / 2));
                    break;

                case Enums.TextAttachmentPoint.BottomLeft:
                    adjustedPos = new Point(position.X,
                        position.Y - (rect.Height));
                    break;

                case Enums.TextAttachmentPoint.BottomCenter:
                    adjustedPos = new Point(position.X - (rect.Width) / 2,
                        position.Y - (rect.Height));
                    break;

                case Enums.TextAttachmentPoint.BottomRight:
                    adjustedPos = new Point(position.X - (rect.Width),
                        position.Y - (rect.Height));
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
