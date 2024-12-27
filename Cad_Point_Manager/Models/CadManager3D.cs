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

            DrawingObjectTree3D = new(this, Extents.ToRect(), 4);

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

            Parallel.ForEach(node.DrawingObjects, obj =>
            {
                if (drawingObject3D is null)
                {
                    if (obj.Bounds.Contains(p))
                    {
                        if (obj.HitTest(p, tolerance))
                        {
                            drawingObject3D = obj;
                        }
                    }
                }
            });

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
            //foreach (var layer in Layers.Values)
            //{
            //    Vertices.AddRange(layer.Vertices);
            //}

            List<DrawingObject3D> drawingObjects = [];
            foreach (var layer in Layers.Values)
            {
                drawingObjects.AddRange(layer.DrawingObject3Ds);
            }

            foreach (var obj in drawingObjects)
            {
                int startIndex = Vertices.Count;
                Vertices.AddRange(obj.Vertices);
                obj.StartVertexIndex = startIndex;
                obj.EndVertexIndex = Vertices.Count;
            }
        }
    }
}
