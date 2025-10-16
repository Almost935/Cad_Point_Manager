using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using netDxf.Entities;
using SharpDX.Direct2D1;
using System.Windows;
using Vector3 = SharpDX.Vector3;

namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public class DrawingBlock3D : DrawingObject3D
    {
        #region Fields
        private List<DrawingObject3D> _drawingObjects = [];
        private List<LineVertex> _geometryVertices = [];
        private List<TextVertex> _textVertices = [];
        #endregion

        #region Properties
        public Insert DxfInsert { get; set; }
        public List<DrawingObject3D> DrawingObjects
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
        private DrawingBlock3D() { Type = DrawingObject3dType.DrawingLine3D; }

        public DrawingBlock3D(Insert insert, ObjectLayer3D layer, bool isPartOfBlock = false, DrawingBlock3D block = null)
        {
            Type = DrawingObject3dType.DrawingBlock3D;
            DxfInsert = insert;

            EntityObject = insert;
            Layer = layer;
            IsPartOfBlock = isPartOfBlock;
            DrawingBlock3D = block;
           
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

        public override void DrawToD2dDeviceContext(DeviceContext1 deviceContext, Factory2 factory, Brush brush, float thickness, StrokeStyle1 strokeStyle)
        {
            foreach (var obj in DrawingObjects)
            {
                obj.DrawToD2dDeviceContext(deviceContext, factory, brush, thickness, strokeStyle);
            }
        }


        private void UpdateDrawingObjects(EntityObject entity)
        {
            if (entity is Insert insert)
            {
                var objs = insert.Explode();

                foreach (var e in objs)
                {
                    var obj = DxfHelpers.GetDrawingObject3D(e, Layer);
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
                if (obj is DrawingBlock3D block)
                {
                    block.UpdateGeometryVertices(layerId, objectId);
                    LineVertices.AddRange(block.LineVertices);
                }
                if (obj is DrawingGeometry3D geometry)
                {
                    geometry.UpdateVertices(layerId, objectId);
                    LineVertices.AddRange(geometry.Vertices);
                }
            }
        }
        public void UpdateTextVertices(ResCache resCache, uint layerId, uint objectId)
        {
            TextVertices.Clear();

            foreach (var obj in DrawingObjects)
            {
                if (obj is DrawingBlock3D block)
                {
                    block.UpdateTextVertices(resCache, layerId, objectId);
                    TextVertices.AddRange(block.TextVertices);
                }
                if (obj is DrawingSText3D text)
                {
                    text.UpdateTextVertices(resCache, layerId, objectId);
                    TextVertices.AddRange(text.TextVertices);
                }
                if (obj is DrawingMtext3D mtext)
                {
                    mtext.UpdateTextVertices(resCache, layerId, objectId);

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
