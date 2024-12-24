using Cad_Point_Manager.Controls;
using Cad_Point_Manager.Controls.D3DControl;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.DrawingObjects;
using Cad_Point_Manager.Models.DrawingObjects3D;
using netDxf;
using netDxf.Entities;
using netDxf.Tables;
using SharpDX;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Vector3 = SharpDX.Vector3;

namespace Cad_Point_Manager.Models
{
    public class CadManager3D : INotifyPropertyChanged
    {
        private bool _dxfDirty = true;
        private bool _dxfNeedsLoad = true;
        private Bounds _extents;

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

        public DxfDocument DxfDocument { get; set; }
        public SortedDictionary<string, ObjectLayer3D> Layers { get; set; } = [];

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
                    layer.DrawingObject3Ds.Add(drawingObj3d);
                }
            }
            DxfDirty = true;
            DxfNeedsReload = true;
        }

        public void ClearDxf()
        {
            DxfDocument = null;
            Layers.Clear();
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


        public Vertex[] GetVerticesList()
        {

        }
    }
}
