using Cad_Point_Manager.Controls;
using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.DrawingObjects;
using Cad_Point_Manager.Models.DrawingObjects3D;
using netDxf;
using netDxf.Entities;
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
        private Rect _extents;

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
        public Rect Extents
        {
            get => _extents;
            set
            {
                _extents = value;
                OnPropertyChanged(nameof(Extents));
            }
        }

        public DxfDocument DxfDocument { get; set; }
        public LayerManager LayerManager { get; set; } = new();

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
                if (e is Polyline2D polyline2d)
                {
                    LayerManager.AddObjectToLayer(polyline2d.Layer.Name, new DrawingPolyline3D(polyline2d));
                }
                if (e is Polyline3D polyline3d)
                {
                    LayerManager.AddObjectToLayer(polyline3d.Layer.Name, new DrawingPolyline3D(polyline3d));
                }
            }
            DxfDirty = true;
            DxfNeedsReload = true;
        }

        public System.Windows.Media.Matrix GetExtentsMatrix(float width, float height)
        {
            if (Extents == Rect.Empty)
            {
                return new System.Windows.Media.Matrix();
            }
            else
            {
                System.Windows.Media.Matrix matrix = new();

                float scaleX = width / (float)Extents.Width;
                float scaleY = height / (float)Extents.Height;

                float centerX = (float)Extents.Left - (width - (float)Extents.Width) * 0.5f;
                float centerY = (float)Extents.Top - (height - (float)Extents.Height) * 0.5f;
                matrix.Translate(-centerX, -centerY);

                if (scaleX < scaleY)
                {
                    matrix.ScaleAt(scaleX, -scaleX, width / 2, height / 2);
                }
                else
                {
                    matrix.ScaleAt(scaleY, -scaleY, width / 2, height / 2);
                }

                return matrix;
            }
        }

        public void ClearDxf()
        {
            DxfDocument = null;
            LayerManager.ClearAllLayers();
            DxfDirty = true;
        }
    }
}
