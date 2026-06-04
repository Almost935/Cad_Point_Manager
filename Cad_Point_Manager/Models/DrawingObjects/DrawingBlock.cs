using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Controls.D3DControl.Rendering.Text;
using Cad_Point_Manager.Helpers;
using netDxf.Entities;
using PdfSharpCore.Drawing;
using SharpDX;
using SharpDX.Direct2D1;
using System.Windows;
using static netDxf.Entities.HatchBoundaryPath;
using Vector3 = SharpDX.Vector3;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingBlock : DrawingObject
    {
        #region Fields
        private List<DrawingObject> _drawingObjects = [];
        private List<LineVertex> _geometryVertices = [];
        private List<TextVertex> _textVertices = [];
        #endregion

        #region Properties
        public Insert DxfInsert { get; set; }
        public List<DrawingObject> DrawingObjects
        {
            get => _drawingObjects;
            set
            {
                _drawingObjects = value;
                OnPropertyChanged(nameof(DrawingObjects));
            }
        }
        public List<LineVertex> LineVertices
        {
            get => _geometryVertices;
            set
            {
                _geometryVertices = value;
                OnPropertyChanged(nameof(LineVertices));
            }
        }
        public List<TextVertex> TextVertices
        {
            get => _textVertices;
            set
            {
                _textVertices = value;
                OnPropertyChanged(nameof(TextVertices));
            }
        }

        public Vector3 InsertionPoint { get; set; }
        public int StartLineVertexIndex { get; set; }
        public int EndLineVertexIndex { get; set; }
        public int StartTextVertexIndex { get; set; }
        public int EndTextVertexIndex { get; set; }

        public int NumberOfDrawingObjects => DrawingObjects.Count;

        public bool TextVerticesCreated = false;
        #endregion

        #region Constructors
        public DrawingBlock(Insert insert, ObjectLayer layer, Vector4 objectColor, ColorType colorType, bool isPartOfBlock = false, DrawingBlock block = null)
        {
            Type = DrawingObjectType.DrawingBlock;
            DxfInsert = insert;
            EntityObject = insert;
            Layer = layer;
            ObjectColor = objectColor;
            ColorType = colorType;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock = block;

            UpdateColor();
            UpdateData();
        }
        #endregion

        #region Methods
        public override void UpdateData()
        {
            if (EntityObject is Insert insert)
            {
                InsertionPoint = new((float)insert.Position.X, (float)insert.Position.Y, (float)insert.Position.Z);

                UpdateDrawingObjects(insert);
                UpdateBounds();
            }
            else
            {
                throw new ArgumentException("entity must be of type Insert");
            }
        }
        public override void DrawToD2dDeviceContext(DeviceContext1 deviceContext, Factory2 factory, Brush brush, float thickness, StrokeStyle1 strokeStyle)
        {
            foreach (var obj in DrawingObjects)
            {
                obj.DrawToD2dDeviceContext(deviceContext, factory, brush, thickness, strokeStyle);
            }
        }
        public override void DrawToPdf(
            XGraphics gfx,
            System.Windows.Media.Matrix worldToPdf,
            XPen pen)
        {
            foreach (var obj in DrawingObjects)
            {
                obj.DrawToPdf(gfx, worldToPdf, pen);
            }
        }

        public override void UpdateBounds()
        {
            Bounds = Rect.Empty;

            foreach (var drawingObj in DrawingObjects)
            {
                Bounds = Rect.Union(Bounds, drawingObj.Bounds);
            }
        }
        public override double DistanceToPoint(System.Windows.Point point)
        {
            double distance = double.MaxValue;

            Parallel.ForEach(DrawingObjects, obj =>
            {
                var d = obj.DistanceToPoint(point);
                if (d < distance)
                {
                    distance = d;
                }
            });

            return distance;
        }

        public override void MouseEnter()
        {
            this.IsMouseOver = true;
        }
        public override void MouseLeave()
        {
            this.IsMouseOver = false;
        }

        public override void Select()
        {
            this.IsSelected = true;
        }
        public override void Deselect()
        {
            this.IsSelected = false;
        }

        private void UpdateDrawingObjects(EntityObject entity)
        {
            if (entity is Insert insert)
            {
                var objs = insert.Explode();

                foreach (var e in objs)
                {
                    var obj = DxfHelpers.GetDrawingObject(e, Layer);
                    if (obj is not null) { DrawingObjects.Add(obj); }
                }
            }
            else
            {
                throw new ArgumentException("entity must be of type Insert");
            }
        }

        public void UpdateGeometryVertices(uint layerId, uint objectId)
        {
            LineVertices.Clear();

            foreach (var obj in DrawingObjects)
            {
                if (obj is DrawingBlock block)
                {
                    block.UpdateGeometryVertices(layerId, objectId);
                    LineVertices.AddRange(block.LineVertices);
                }
                if (obj is DrawingGeometry geometry)
                {
                    geometry.UpdateVertices(layerId, objectId);
                    LineVertices.AddRange(geometry.Vertices);
                }
            }
        }
        public void UpdateTextVertices(ResCache resCache, uint layerId, SceneIdMap sceneIdMap, D3dStateBuffers stateBuffers)
        {
            TextVertices.Clear();

            foreach (var obj in DrawingObjects)
            {
                if (obj is DrawingBlock block)
                {
                    block.UpdateTextVertices(resCache, layerId, sceneIdMap, stateBuffers);
                    TextVertices.AddRange(block.TextVertices);
                }
                if (obj is DrawingSText text)
                {
                    text.UpdateTextVertices(resCache, layerId, sceneIdMap, stateBuffers);
                    TextVertices.AddRange(text.TextVertices);
                }
                if (obj is DrawingMtext mtext)
                {
                    mtext.UpdateTextVertices(resCache, layerId, sceneIdMap, stateBuffers);

                    foreach (var row in mtext.MtextBlock.Rows)
                    {
                        foreach (var segment in row.Segments)
                        {
                            TextVertices.AddRange(segment.TextVertices);
                        }
                    }
                }
            }
        }
        #endregion
    }
}
