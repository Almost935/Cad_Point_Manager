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
        private bool _dxfLoaded = false;
        private bool _dxfDirty = true;
        private bool _dxfNeedsLoad = true;
        private Bounds _extents;
        private List<Vertex> _vertices = [];

        public bool DxfLoaded
        {
            get => _dxfLoaded;
            set
            {
                _dxfLoaded = value;
                OnPropertyChanged();
            }
        }
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
            ClearDxf();

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
            UpdateVerticesList();

            DxfLoaded = true;
            DxfDirty = true;
            DxfNeedsReload = true;
        }

        /// <summary>
        /// Finds the closest object to the point p within the tolerance.
        /// </summary>
        /// <param name="p">The point to find the closest objects to</param>
        /// <param name="tolerance">the minimum distance the object can be from p.</param>
        /// <returns></returns>

        public (double distance, DrawingObject3D obj) HitTestPoint(Point p, float tolerance)
        {
            (double distance, DrawingObject3D obj) tup = (double.MaxValue, null);

            if (DrawingObjectTree3D is null) { return tup; }

            Rect rect = new(p.X - tolerance, p.Y - tolerance, tolerance * 2, tolerance * 2);
            List<DrawingObjectNode3D> nodes = DrawingObjectTree3D.GetIntersectingNodes(rect);

            foreach (var node in nodes)
            {
                (double distance, DrawingObject3D obj) objTup = node.HitTestNode(p);
                
                if (objTup.distance < tup.distance && objTup.distance < tolerance)
                {
                    tup = objTup;
                }
            }

            return tup;
        }

        public List<(double distance, DrawingObject3D obj)> GetNearestDrawingObjects(Point p, float tolerance) 
        {
            List<(double distance, DrawingObject3D obj)> hits = [];

            Rect rect = new(p.X - tolerance, p.Y - tolerance, tolerance * 2, tolerance * 2);
            var nodes = DrawingObjectTree3D.GetIntersectingNodes(rect);

            //Debug.WriteLine($"nodes.count: {nodes.Count}");

            foreach (var node in nodes)
            {
                hits.AddRange(node.HitTestNode(p, rect));
            }

            hits.Sort((x, y) => x.distance.CompareTo(y.distance));

            return hits;
        }

        public void ClearDxf()
        {
            DxfDocument = null;

            Layers.Clear();
            Vertices.Clear();

            DxfLoaded = false;
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

        public void UpdateVerticesList()
        {
            //Stopwatch stopwatch = Stopwatch.StartNew();

            Vertices.Clear();

            foreach (var layer in Layers.Values)
            {
                if (layer.IsVisible)
                {
                    foreach (var obj in layer.DrawingObject3Ds)
                    {
                        if (obj is DrawingGeometry3D drawingGeometry)
                        {
                            drawingGeometry.StartVertexIndex = Vertices.Count;
                            Vertices.AddRange(drawingGeometry.Vertices);
                            drawingGeometry.EndVertexIndex = Vertices.Count - 1;
                        }
                    }
                }
            }

            //stopwatch.Stop();
            //Debug.WriteLine($"UpdateVerticesList() took {stopwatch.ElapsedMilliseconds} ms");

            //// Testing for Quadtree bounds
            //SharpDX.Vector4 color = new(0, 1, 0, 1);
            //foreach (var node in DrawingObjectTree3D.BaseLevelNodes)
            //{
            //    Vertex tl = new(new SharpDX.Vector3((float)node.Extents.TopLeft.X, (float)node.Extents.TopLeft.Y, 0), color);
            //    Vertex tr = new(new SharpDX.Vector3((float)node.Extents.TopRight.X, (float)node.Extents.TopRight.Y, 0), color);
            //    Vertex bl = new(new SharpDX.Vector3((float)node.Extents.BottomLeft.X, (float)node.Extents.BottomLeft.Y, 0), color);
            //    Vertex br = new(new SharpDX.Vector3((float)node.Extents.BottomRight.X, (float)node.Extents.BottomRight.Y, 0), color);

            //    Vertices.Add(tl); Vertices.Add(tr);
            //    Vertices.Add(tr); Vertices.Add(br);
            //    Vertices.Add(br); Vertices.Add(bl);
            //    Vertices.Add(bl); Vertices.Add(tl);
            //}
        }
    }
}
