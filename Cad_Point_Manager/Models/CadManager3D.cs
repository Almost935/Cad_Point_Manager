using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.DrawingObjects3D;
using netDxf;
using netDxf.Entities;
using netDxf.Tables;
using SharpDX;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;

using Point = System.Windows.Point;
using Vector2 = SharpDX.Vector2;

namespace Cad_Point_Manager.Models
{
    public class CadManager3D : INotifyPropertyChanged
    {
        private bool _dxfDirty = true;
        private bool _dxfNeedsLoad = true;
        private Bounds _extents;
        private List<Vertex> _vertices = [];

        public bool DxfDirty
        {
            get => _dxfDirty;
            set
            {
                _dxfDirty = value;
                OnPropertyChanged();
            }
        }
        public bool DxfNeedsReload
        {
            get => _dxfNeedsLoad;
            set
            {
                _dxfNeedsLoad = value;
                OnPropertyChanged();
            }
        }
        public Bounds Extents
        {
            get => _extents;
            set
            {
                _extents = value;
                OnPropertyChanged(nameof(Extents));
            }
        }
        public List<Vertex> Vertices
        {
            get => _vertices;
            set
            {
                _vertices = value;
                OnPropertyChanged(nameof(Vertices));
            }
        }

        public DxfDocument DxfDocument { get; set; }
        public SortedDictionary<string, ObjectLayer3D> Layers { get; set; } = [];
        public DrawingObjectTree3D DrawingObjectTree3D { get; set; }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void LoadDxf(DxfDocument dxfDocument)
        {
            DxfDocument = dxfDocument;
            Extents = DxfHelpers.GetBoundsFromHeader(DxfDocument);

            foreach (var e in DxfDocument.Entities.All)
            {
                var layer = GetLayer(e.Layer);
                var drawingObj3d = DxfHelpers.GetDrawingObject3D(e, layer);
                if (layer is not null && drawingObj3d is not null)
                {
                    layer.AddDrawingObject(drawingObj3d);
                }
            }

            DrawingObjectTree3D = new(this, Extents.ToRect(), 5);

            GetVerticesList();

            DxfDirty = true;
            DxfNeedsReload = true;
        }

        public DrawingObject3D HitTestPoint(Point p, float tolerance)
        {
            if (DrawingObjectTree3D is null) { return null; }

            DrawingObjectNode3D node = DrawingObjectTree3D.GetIntersectingNode(p);

            if (node is null) { return null; }

            DrawingObject3D drawingObject3D = null;

            foreach (var obj in node.DrawingObjects)
            {
                //Debug.WriteLine($"\nobj.GetType(): {obj.GetType()}" +
                //    $"\nRect.Inflate(obj.Bounds, 2, 2).Contains(p): {Rect.Inflate(obj.Bounds, 2, 2).Contains(p)}");

                if (Rect.Inflate(obj.Bounds, 5, 5).Contains(p))
                {
                    //Debug.WriteLine($"obj.HitTest(p, tolerance): {obj.HitTest(p, tolerance)}");

                    if (obj.HitTest(p, tolerance))
                    {
                        drawingObject3D = obj;
                    }
                }
            }

            return drawingObject3D;
        }

        public void ClearDxf()
        {
            DxfDocument = null;

            Layers.Clear();
            Vertices.Clear();

            DxfDirty = true;
        }

        public ObjectLayer3D GetLayer(Layer dxfLayer)
        {
            var layerExists = Layers.TryGetValue(dxfLayer.Name, out ObjectLayer3D layer);

            if (layerExists) { return layer; }
            else
            {
                layer = new(dxfLayer);
                Layers.Add(dxfLayer.Name, layer);

                return layer;
            }
        }

        public void GetVerticesList()
        {
            foreach (var layer in Layers.Values)
            {
                Vertices.AddRange(layer.Vertices);
            }

            // Testing for Quadtree bounds
            SharpDX.Vector4 color = new(0, 1, 0, 1);
            foreach (var node in DrawingObjectTree3D.BaseLevelNodes)
            {
                Vertex tl = new(new SharpDX.Vector3((float)node.Extents.TopLeft.X, (float)node.Extents.TopLeft.Y, 0), color);
                Vertex tr = new(new SharpDX.Vector3((float)node.Extents.TopRight.X, (float)node.Extents.TopRight.Y, 0), color);
                Vertex bl = new(new SharpDX.Vector3((float)node.Extents.BottomLeft.X, (float)node.Extents.BottomLeft.Y, 0), color);
                Vertex br = new(new SharpDX.Vector3((float)node.Extents.BottomRight.X, (float)node.Extents.BottomRight.Y, 0), color);

                Vertices.Add(tl); Vertices.Add(tr);
                Vertices.Add(tr); Vertices.Add(br);
                Vertices.Add(br); Vertices.Add(bl);
                Vertices.Add(bl); Vertices.Add(tl);
            }
        }
    }
}
