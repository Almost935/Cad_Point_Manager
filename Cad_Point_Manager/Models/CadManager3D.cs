using Cad_Point_Manager.Controls;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.DrawingObjects;
using Cad_Point_Manager.Models.DrawingObjects3D;
using netDxf;
using netDxf.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Cad_Point_Manager.Models
{
    public class CadManager3D : INotifyPropertyChanged
    {
        private bool _dxfDirty = true;

        public bool DxfDirty
        {
            get => _dxfDirty;
            set
            {
                _dxfDirty = value;
                OnPropertyChanged();
            }
        }

        public DxfDocument DxfDocument { get; set; }
        public LayerManager LayerManager { get; set; } = new();
        public Rect Extents = new();

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public void LoadDxf(DxfDocument dxfDocument)
        {
            DxfDocument = dxfDocument;
            Extents = DxfHelpers.GetExtentsFromHeader(DxfDocument);

            foreach (var e in DxfDocument.Entities.All)
            {
                if (e is Line line)
                {
                    LayerManager.AddObjectToLayer(line.Layer.Name, new DrawingLine3D(line));
                }
                if (e is Arc arc)
                {
                    LayerManager.AddObjectToLayer(arc.Layer.Name, new DrawingArc3D(arc));
                }
            }
            DxfDirty = true;
        }

        public void ClearDxf()
        {
            DxfDocument = null;
            LayerManager.ClearAllLayers();
        }
    }
}
