using Cad_Point_Manager.Controls.D2DControl;
using Cad_Point_Manager.Models.SerializableObjects;
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

        public Matrix TextTransform { get; set; } = Matrix.Identity;
        public Point InitialPosition { get; set; }
        public Enums.TextAttachmentPoint AttachmentPoint { get; set; }
        public Point AdjustedPosition { get; set; }
        public string Text { get; set; }
        public string FontFamilyName { get; set; }
        public double FontSize { get; set; }
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
            DeviceContext?.DrawTextLayout(new RawVector2((float)InitialPosition.X, (float)InitialPosition.Y), _textLayout, brush);
        }
        public override void DrawToDeviceContext(float thickness, Brush brush, StrokeStyle1 strokeStyle)
        {
            DeviceContext?.DrawTextLayout(new RawVector2((float)InitialPosition.X, (float)InitialPosition.Y), _textLayout, brush);
        }
        public override bool DrawingObjectIsInRect(Rect rect)
        {
            return Bounds.IntersectsWith(rect) || Bounds.Contains(rect);
        }

        public override void UpdateDxfProperties()
        {
            InitialPosition = new(DxfMtext.Position.X, DxfMtext.Position.Y);
            Text = DxfMtext.PlainText();
            Bounds = new(InitialPosition.X, InitialPosition.Y, DxfMtext.RectangleWidth * 2, DxfMtext.Height * 2);
            TextTransform = new();
            TextTransform.ScaleAt(-1, -1, InitialPosition.X, InitialPosition.Y);
            FontFamilyName = DxfMtext.Style.FontFamilyName;
            FontSize = DxfMtext.Height * 1.25;

            switch (DxfMtext.AttachmentPoint)
            {
                case MTextAttachmentPoint.TopLeft:
                    AttachmentPoint = Enums.TextAttachmentPoint.TopLeft;
                    AdjustedPosition = InitialPosition;
                    break;

                case MTextAttachmentPoint.TopCenter:
                    AttachmentPoint = Enums.TextAttachmentPoint.TopCenter;
                    AdjustedPosition = new Point(InitialPosition.X - (Bounds.Width) / 2,
                        InitialPosition.Y);
                    break;

                case MTextAttachmentPoint.TopRight:
                    AttachmentPoint = Enums.TextAttachmentPoint.TopRight;
                    AdjustedPosition = new Point(InitialPosition.X - (Bounds.Width),
                        InitialPosition.Y);
                    break;

                case MTextAttachmentPoint.MiddleLeft:
                    AttachmentPoint = Enums.TextAttachmentPoint.MiddleLeft;
                    AdjustedPosition = new Point(InitialPosition.X,
                        InitialPosition.Y - (Bounds.Height / 2));
                    break;

                case MTextAttachmentPoint.MiddleCenter:
                    AttachmentPoint = Enums.TextAttachmentPoint.MiddleCenter;
                    AdjustedPosition = new Point(InitialPosition.X - (Bounds.Width) / 2,
                        InitialPosition.Y - (Bounds.Height / 2));
                    break;

                case MTextAttachmentPoint.MiddleRight:
                    AttachmentPoint = Enums.TextAttachmentPoint.MiddleRight;
                    AdjustedPosition = new Point(InitialPosition.X - (Bounds.Width),
                        InitialPosition.Y - (Bounds.Height / 2));
                    break;

                case MTextAttachmentPoint.BottomLeft:
                    AttachmentPoint = Enums.TextAttachmentPoint.BottomLeft;
                    AdjustedPosition = new Point(InitialPosition.X,
                        InitialPosition.Y - (Bounds.Height));
                    break;

                case MTextAttachmentPoint.BottomCenter:
                    AttachmentPoint = Enums.TextAttachmentPoint.BottomCenter;
                    AdjustedPosition = new Point(InitialPosition.X - (Bounds.Width) / 2,
                        InitialPosition.Y - (Bounds.Height));
                    break;

                case MTextAttachmentPoint.BottomRight:
                    AttachmentPoint = Enums.TextAttachmentPoint.BottomRight;
                    AdjustedPosition = new Point(InitialPosition.X - (Bounds.Width),
                        InitialPosition.Y - (Bounds.Height));
                    break;

                default:
                    AttachmentPoint = Enums.TextAttachmentPoint.TopLeft;
                    AdjustedPosition = InitialPosition;
                    break;
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
            _textFormat = new(_factoryWrite, FontFamilyName, (float)FontSize);
        }
        public void GetTextLayout()
        {
            RawMatrix3x2 transform = new((float)TextTransform.M11, (float)TextTransform.M12, (float)TextTransform.M21, (float)TextTransform.M22, (float)TextTransform.OffsetX, (float)TextTransform.OffsetY);
            _textLayout = new(_factoryWrite, Text, _textFormat, (float)Bounds.Width, (float)Bounds.Height, 96, transform, true);
        }
        public override bool Hittest(RawVector2 p, float thickness)
        {
            return Bounds.Contains(p.X, p.Y);
        }
        #endregion
    }

    public class DrawingMtextData : DrawingObjectData
    {
        public SerializableMatrix TextTransform { get; set; } 
        public SerializablePoint InitialPosition { get; set; }
        public Enums.TextAttachmentPoint AttachmentPoint { get; set; }
        public SerializablePoint AdjustedPosition { get; set; }
        public string Text { get; set; }
        public string FontFamilyName { get; set; }
        public double FontSize { get; set; }
    }
}
