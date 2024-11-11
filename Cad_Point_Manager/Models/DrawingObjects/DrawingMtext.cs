using Cad_Point_Manager.Common;
using Cad_Point_Manager.Controls.D2DControl;
using Cad_Point_Manager.Models.SerializableObjects;
using netDxf;
using netDxf.Entities;
using netDxf.Units;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using System.Security.AccessControl;
using System.Windows;
using System.Windows.Media;
using Brush = SharpDX.Direct2D1.Brush;
using DeviceContext1 = SharpDX.Direct2D1.DeviceContext1;
using Factory1 = SharpDX.DirectWrite.Factory1;
using Point = System.Windows.Point;

namespace Cad_Point_Manager.DrawingObjects
{
    public class DrawingMtext : DrawingObject
    {
        #region Fields
        private MText _dxfMtext;

        private Factory1 _factoryWrite;
        private TextFormat _textFormat;
        private TextLayout _textLayout;
        private Point _adjustedPos;
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
        public float FontSize { get; set; }
        public string FontFamilyName { get; set; }
        public Matrix Transform { get; set; }
        public Enums.TextAttachmentPoint AttachmentPoint { get; set; }
        #endregion

        #region Constructor
        public DrawingMtext(MText dxfMtext, ObjectLayer layer)
        {
            DxfMtext = dxfMtext;
            Entity = dxfMtext;
            Layer = layer;
            EntityCount = 1;

            LoadFromDxfEntity(dxfMtext);
        }
        #endregion

        #region Methods
        public override void DrawToDeviceContext(float thickness, Brush brush)
        {
            DeviceContext?.DrawTextLayout(new RawVector2((float)DxfMtext.Position.X, (float)DxfMtext.Position.Y), _textLayout, brush);
        }
        public override void DrawToDeviceContext(float thickness, Brush brush, StrokeStyle1 strokeStyle)
        {
            DeviceContext?.DrawTextLayout(new RawVector2((float)DxfMtext.Position.X, (float)DxfMtext.Position.Y), _textLayout, brush);
            //deviceContext.DrawText(DxfMtext.PlainText(), _textFormat, new RawRectangleF((float)Bounds.Left, (float)Bounds.Top, (float)Bounds.Right, (float)Bounds.Bottom), Brush);
        }
        public override bool DrawingObjectIsInRect(Rect rect)
        {
            return Bounds.IntersectsWith(rect) || Bounds.Contains(rect);
        }

        public override void LoadFromDxfEntity(EntityObject e)
        {
            if (e is MText mText)
            {
                Text = mText.PlainText();
                Bounds = new(DxfMtext.Position.X, DxfMtext.Position.Y, DxfMtext.RectangleWidth * 2, DxfMtext.Height * 2);
                AttachmentPoint = GetAttachmentPoint(mText.AttachmentPoint);
                Position = GetTextOrigin(AttachmentPoint, Bounds, new Point(mText.Position.X, mText.Position.Y));
                FontSize = (float)(DxfMtext.Height * 1.25);
                FontFamilyName = DxfMtext.Style.FontFamilyName;
                Transform = GetTransform(DxfMtext.Position);
            }
            else
            {
                throw new ArgumentException("EntityObject must be of type MText");
            }
        }
        public override void LoadFromData(DrawingObjectData drawingObjectData)
        {
            if (drawingObjectData is DrawingMtextData mTextData)
            {
                Text = mTextData.Text;
                Bounds = mTextData.Bounds;
                Position = new Point(mTextData.Position.X, mTextData.Position.Y);
                AttachmentPoint = mTextData.AttachmentPoint;
                FontSize = mTextData.FontSize;
                FontFamilyName = mTextData.FontFamilyName;
                Transform = new Matrix(mTextData.Transform.M11, mTextData.Transform.M12, mTextData.Transform.M21, mTextData.Transform.M22, mTextData.Transform.OffsetX, mTextData.Transform.OffsetY);
            }
            else
            {
                throw new ArgumentException("DrawingObjectData must be of type DrawingMtextData");
            }
        }

        public override void UpdateGeometry()
        {
            GetTextFormat();
            GetTextLayout();
        }

        public override void InitializeResources(ResourceCache resCache)
        {
            ResCache = resCache;
            DeviceContext = resCache.DeviceContext;
            Factory = resCache.Factory;
            _factoryWrite = resCache.FactoryWrite;

            UpdateBrush();
            GetStrokeStyle();
            UpdateGeometry();
        }
        public override void UpdateDeviceDependentResources(ResourceCache resCache)
        {
            ResCache = resCache;
            DeviceContext = resCache.DeviceContext;

            UpdateBrush();
        }
        public override void UpdateDeviceIndependentResources(ResourceCache resCache)
        {
            ResCache = resCache;
            Factory = resCache.Factory;
            _factoryWrite = resCache.FactoryWrite;

            GetStrokeStyle();
            UpdateGeometry();
        }

        public void GetTextFormat()
        {
            _textFormat = new(_factoryWrite, FontFamilyName, FontSize);
        }
        public Matrix GetTransform(Vector3 dxfPos)
        {
            Matrix matrix = new();
            matrix.ScaleAt(-1, -1, dxfPos.X, dxfPos.Y);
            return matrix;
        }
        public void GetTextLayout()
        {
            RawMatrix3x2 transform = new((float)Transform.M11, (float)Transform.M12, (float)Transform.M21, (float)Transform.M22, (float)Transform.OffsetX, (float)Transform.OffsetY);
            _textLayout = new(_factoryWrite, Text, _textFormat, (float)Bounds.Width, (float)Bounds.Height, 96, transform, true);
        }
        public override bool Hittest(RawVector2 p, float thickness)
        {
            return Bounds.Contains(p.X, p.Y);
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

    public class DrawingMtextData : DrawingObjectData
    {
        public string Text { get; set; }
        public SerializablePoint Position { get; set; }
        public int FontSize { get; set; }
        public string FontFamilyName { get; set; }
        public Enums.TextAttachmentPoint AttachmentPoint { get; set; }
        public SerializableMatrix Transform { get; set; }
    }
}
