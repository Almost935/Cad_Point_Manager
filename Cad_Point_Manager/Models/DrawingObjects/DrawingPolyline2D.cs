using Cad_Point_Manager.Helpers;
using netDxf.Entities;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Cad_Point_Manager.Models.DrawingObjects
{
    public class DrawingPolyline2D : DrawingPolyline
    {
        #region Fields
        private Polyline2D _dxfPolyline2D;
        #endregion

        #region Properties
        public Polyline2D DxfPolyline2D
        {
            get { return _dxfPolyline2D; }
            set
            {
                _dxfPolyline2D = value;
                OnPropertyChanged(nameof(DxfPolyline2D));
            }
        }
        #endregion

        #region Constructor
        public DrawingPolyline2D(Polyline2D dxfPolyline2D, ObjectLayer layer)
        {
            DxfPolyline2D = dxfPolyline2D;
            Entity = dxfPolyline2D;
            Layer = layer;

            LoadFromDxfEntity(DxfPolyline2D);
        }
        #endregion

        #region Methods

        public ObservableCollection<DrawingObject> GetDrawingSegments(Polyline2D pline)
        {
            foreach (var e in pline.Explode())
            {
                var obj = DxfHelpers.GetDrawingSegment(e, Layer);
                obj.IsPartOfPolyline = true;
                obj.DrawingPolyline = this;
                obj.LoadFromDxfEntity(e);

                if (obj is not null)
                {
                    EntityCount += obj.EntityCount;
                    DrawingSegments.Add(obj);
                }
            }
        }

        public override void LoadFromDxfEntity(EntityObject e)
        {
            if (e is Polyline2D pline)
            {
                StartPoint = pline.;
                GetDrawingSegments(pline);
            }
            else
            {
                throw new ArgumentException("EntityObject must be of type DrawingPolyline2D");
            }
        }
        public override void LoadFromData(DrawingObjectData drawingObjectData)
        {
            throw new NotImplementedException();
        }

        public override void UpdateGeometry()
        {
            Parallel.ForEach(DrawingSegments, segment =>
            {
                segment.UpdateGeometry();

                if (Bounds.IsEmpty)
                {
                    Bounds = segment.Bounds;
                }
                else
                {
                    Bounds = Rect.Union(Bounds, segment.Bounds);
                }
            });
        }
        #endregion
    }

    public class DrawingPolyline2DData : DrawingPolylineData
    {
        
    }
}
