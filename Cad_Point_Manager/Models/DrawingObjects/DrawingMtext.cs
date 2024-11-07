using Cad_Point_Manager.Controls.D2DControl;
using netDxf.Entities;
using netDxf.Units;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using System.Windows;
using System.Windows.Media;
using Brush = SharpDX.Direct2D1.Brush;
using DeviceContext1 = SharpDX.Direct2D1.DeviceContext1;
using Factory1 = SharpDX.DirectWrite.Factory1;
using Point = System.Windows.Point;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingMtext : DrawingObject
    {
        #region Fields
        private MText _dxfMtext;

        private Factory1 _factoryWrite;
        private TextFormat _textFormat;
        private Matrix _transform;
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
        #endregion

        #region Constructor
        public DrawingMtext(MText dxfMtext, ObjectLayer layer)
        {
            DxfMtext = dxfMtext;
            Entity = dxfMtext;
            Layer = layer;
            EntityCount = 1;
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

        public override void UpdateDxfProperties()
        {

        }
        public override void UpdateGeometry()
        {
            Bounds = new(DxfMtext.Position.X, DxfMtext.Position.Y, DxfMtext.RectangleWidth * 2, DxfMtext.Height * 2);
            GetTransform();
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
            _textFormat = new(_factoryWrite, DxfMtext.Style.FontFamilyName, (float)(DxfMtext.Height * 1.25));
        }
        public void GetTransform()
        {
            _transform = new();
            _transform.ScaleAt(-1, -1, DxfMtext.Position.X, DxfMtext.Position.Y);
        }
        public void GetTextLayout()
        {
            _adjustedPos = GetTextOrigin(DxfMtext, Bounds, new Point(DxfMtext.Position.X, DxfMtext.Position.Y));
            RawMatrix3x2 transform = new((float)_transform.M11, (float)_transform.M12, (float)_transform.M21, (float)_transform.M22, (float)_transform.OffsetX, (float)_transform.OffsetY);
            _textLayout = new(_factoryWrite, DxfMtext.PlainText(), _textFormat, (float)Bounds.Width, (float)Bounds.Height, 96, transform, true);
        }
        public override bool Hittest(RawVector2 p, float thickness)
        {
            return Bounds.Contains(p.X, p.Y);
        }

        /// <summary>
        /// Gets the upper left point of the MText.
        /// </summary>
        /// <param name="mText"></param>
        /// <param name="rect"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public Point GetTextOrigin(MText mText, Rect rect, Point position)
        {
            Point adjustedPos = new();

            switch (mText.AttachmentPoint)
            {
                case MTextAttachmentPoint.TopLeft:
                    adjustedPos = position;
                    break;

                case MTextAttachmentPoint.TopCenter:
                    adjustedPos = new Point(position.X - rect.Width / 2,
                        position.Y);
                    break;

                case MTextAttachmentPoint.TopRight:
                    adjustedPos = new Point(position.X - rect.Width,
                        position.Y);
                    break;

                case MTextAttachmentPoint.MiddleLeft:
                    adjustedPos = new Point(position.X,
                        position.Y - rect.Height / 2);
                    break;

                case MTextAttachmentPoint.MiddleCenter:
                    adjustedPos = new Point(position.X - rect.Width / 2,
                        position.Y - rect.Height / 2);
                    break;

                case MTextAttachmentPoint.MiddleRight:
                    adjustedPos = new Point(position.X - rect.Width,
                        position.Y - rect.Height / 2);
                    break;

                case MTextAttachmentPoint.BottomLeft:
                    adjustedPos = new Point(position.X,
                        position.Y - rect.Height);
                    break;

                case MTextAttachmentPoint.BottomCenter:
                    adjustedPos = new Point(position.X - rect.Width / 2,
                        position.Y - rect.Height);
                    break;

                case MTextAttachmentPoint.BottomRight:
                    adjustedPos = new Point(position.X - rect.Width,
                        position.Y - rect.Height);
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
