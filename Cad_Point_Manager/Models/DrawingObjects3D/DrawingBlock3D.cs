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
        public List<LineVertex> GeometryVertices
        {
            get => _geometryVertices;
            set
            {
                _geometryVertices = value;
                OnPropertyChanged(nameof(GeometryVertices));
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
        public int StartVertexIndex { get; set; }
        public int EndVertexIndex { get; set; }
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
            UpdateData(insert);
        }
        #endregion

        #region Methods
        public override void UpdateData(EntityObject entity)
        {
            if (entity is Insert insert)
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

        public override bool HitTest(System.Windows.Point point, float tolerance)
        {
            foreach (var obj in DrawingObjects)
            {
                if (obj.HitTest(point, tolerance))
                {
                    return true;
                }
            }
            return false;
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


        public override void Select()
        {
            this.IsSelected = true;
            this.IsVisible = false;

            for (int i = 0; i < GeometryVertices.Count(); i++)
            {
                var vertex = GeometryVertices[i];
                vertex.IsVisible = 0.0f;
                GeometryVertices[i] = vertex;
            }
        }
        public override void Deselect()
        {
            this.IsSelected = false;
            this.IsVisible = true;

            for (int i = 0; i < GeometryVertices.Count(); i++)
            {
                var vertex = GeometryVertices[i];
                vertex.IsVisible = 1.0f;
                GeometryVertices[i] = vertex;
            }
        }

        public override void DrawToD2dDeviceContext(DeviceContext1 deviceContext, Factory2 factory, Brush brush, float thickness, StrokeStyle1 strokeStyle)
        {
            foreach (var obj in DrawingObjects)
            {
                obj.DrawToD2dDeviceContext(deviceContext, factory, brush, thickness, strokeStyle);
            }
        }


        public void UpdateTextVertices(SharpDX.DirectWrite.Factory1 factory, Factory2 d2dFactory)
        {
            TextVertices.Clear();

            foreach (var obj in DrawingObjects)
            {
                if (obj is DrawingBlock3D block)
                {
                    block.UpdateTextVertices(factory, d2dFactory);
                    TextVertices.AddRange(block.TextVertices);
                }
                if (obj is DrawingText3D text)
                {
                    text.UpdateTextVertices(factory, d2dFactory);
                    TextVertices.AddRange(text.TextVertices);
                }
                if (obj is DrawingMtext3D mtext)
                {
                    mtext.UpdateTextVertices(factory, d2dFactory);
                    TextVertices.AddRange(mtext.TextVertices);
                }
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
                UpdateGeometryVertices();
            }
            else
            {
                throw new ArgumentException("entity must be of type Insert");
            }
        }

        private void UpdateGeometryVertices()
        {
            GeometryVertices.Clear();

            foreach (var obj in DrawingObjects)
            {
                if (obj is DrawingBlock3D block)
                {
                    GeometryVertices.AddRange(block.GeometryVertices);
                }
                if (obj is DrawingGeometry3D geometry)
                {
                    GeometryVertices.AddRange(geometry.Vertices);
                }
            }
        }
        #endregion
    }
}
