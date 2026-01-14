using Cad_Point_Manager.Models.DrawingObjects3D;
using netDxf;
using netDxf.Entities;
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

namespace Cad_Point_Manager.Models.Printing
{
    public class Layout : INotifyPropertyChanged
    {
        #region Fields
        private LayoutViewport _viewport;
        private PageSize _pageSize = PageSize.Get36x24;
        #endregion

        #region Properties
        public LayoutViewport Viewport
        {
            get { return _viewport; }
            set
            {
                if (_viewport != value)
                {
                    _viewport = value;
                    OnPropertyChanged(nameof(Viewport));
                }
            }
        }
        public PageSize PageSize
        {
            get { return _pageSize; }
            set
            {
                if (value != _pageSize)
                {
                    _pageSize = value;
                    OnPropertyChanged(nameof(PageSize));
                }
            }
        }

        public string Name { get; set; } = "Layout 1";
        public ObservableCollection<DrawingObject> DrawingObjects { get; set; } = [];
        #endregion

        #region Constructors
        //public Layout(netDxf.Objects.Layout layout)
        //{
        //    Debug.WriteLine($"Layout Name: {layout.Name}");

        //    var viewport = layout.Viewport;
        //    var associatedBlock = layout.AssociatedBlock;

        //    List<EntityObject> entities = [];
        //    foreach (var potE in associatedBlock.Entities)
        //    {
        //        if (potE is Insert insert)
        //        {
        //            entities.AddRange(insert.Explode());
        //        }
        //        else
        //        {
        //            entities.Add(potE);
        //        }
        //    }
        //    foreach (var entity in entities)
        //    {
        //        if (entity is Line line)
        //        {
        //            Debug.WriteLine($"Line from {line.StartPoint} to {line.EndPoint}");
        //        }
        //        else if (entity is Circle circle)
        //        {
        //            Debug.WriteLine($"Circle at {circle.Center} with radius {circle.Radius}");
        //        }
        //        if (entity is Polyline2D pline)
        //        {
        //            pline.Explode().ForEach(explodedEntity =>
        //            {
        //                if (explodedEntity is Line explodedLine)
        //                {
        //                    Debug.WriteLine($"Polyline Line from {explodedLine.StartPoint} to {explodedLine.EndPoint}");
        //                }
        //            });
        //        }
        //    }
        //}
        #endregion

        #region NotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
