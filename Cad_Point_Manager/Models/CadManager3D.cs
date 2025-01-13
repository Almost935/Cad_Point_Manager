using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.DrawingObjects3D;
using netDxf;
using netDxf.Entities;
using netDxf.Tables;
using SharpDX;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using Point = System.Windows.Point;
using Vector2 = SharpDX.Vector2;

namespace Cad_Point_Manager.Models
{
    public class CadManager3D : INotifyPropertyChanged
    {
        private bool _dxfLoaded = false;
        private bool _dxfDirty = true;
        private bool _dxfNeedsReload = true;
        private Bounds _extents;
        private List<Vertex> _vertices = [];
        private ObservableCollection<KeyValuePair<string, ObjectLayer3D>> _layers = [];
        private ICollectionView _layesView;

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
            get => _dxfNeedsReload;
            set
            {
                Debug.WriteLine($"DxfNeedsReload Changed");
                _dxfNeedsReload = value;
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
        public ObservableCollection<KeyValuePair<string, ObjectLayer3D>> Layers
        {
            get => _layers;
            set
            {
                _layers = value;
                OnPropertyChanged(nameof(Layers));
            }
        }
        public ICollectionView LayersView
        {
            get => _layesView;
            set
            {
                _layesView = value;
                OnPropertyChanged(nameof(LayersView));
            }
        }

        public DxfDocument DxfDocument { get; set; }
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
            UpdateLayerView();
            DrawingObjectTree3D = new(this, Extents.ToRect(), 5);
            UpdateVerticesList();

            DxfLoaded = true;
            DxfDirty = true;
            DxfNeedsReload = true;
        }

        public void UpdateLayerView()
        {
            LayersView = CollectionViewSource.GetDefaultView(Layers);
            LayersView.SortDescriptions.Clear();
            LayersView.SortDescriptions.Add(new SortDescription("Key", ListSortDirection.Ascending));
        }

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
            ObjectLayer3D layer = Layers.FirstOrDefault(x => x.Value.Name == dxfLayer.Name).Value;

            if (layer is not null) { return layer; }
            else
            {
                layer = new(dxfLayer);
                Layers.Add(new KeyValuePair<string, ObjectLayer3D>(dxfLayer.Name, layer));

                return layer;
            }
        }

        public void UpdateVerticesList()
        {
            //Stopwatch stopwatch = Stopwatch.StartNew();

            Vertices.Clear();

            foreach (var keyValuePair in Layers)
            {
                var layer = keyValuePair.Value;
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

                        if (obj is DrawingBlock3D drawingBlock)
                        {
                            drawingBlock.StartVertexIndex = Vertices.Count;
                            Vertices.AddRange(drawingBlock.DrawingGeometryVerteces);
                            drawingBlock.EndVertexIndex = Vertices.Count - 1;
                        }
                    }
                }
            }
        }
    }
}
