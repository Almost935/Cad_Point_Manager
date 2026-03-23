using Cad_Point_Manager.Models.DrawingObjects;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Cad_Point_Manager.Models.Printing
{
    public class Layout : INotifyPropertyChanged
    {
        #region Fields
        private string _name = "Layout 1";
        private LayoutViewport _viewport;
        private double _pageWidth = 36;
        private double _pageHeight = 24;
        private TitleblockAttributes _attributes = new TitleblockAttributes();
        #endregion

        #region Properties
        public string Name
        {
            get => _name; 
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }

        }
        public LayoutViewport Viewport
        {
            get => _viewport;
            set
            {
                if (_viewport != value)
                {
                    _viewport = value;
                    OnPropertyChanged(nameof(Viewport));
                }
            }
        }
        public double PageWidth
        {
            get => _pageWidth;
            set
            {
                if (value != _pageWidth)
                {
                    _pageWidth = value;
                    OnPropertyChanged(nameof(PageWidth));
                }
            }
        }   
        public double PageHeight
        {
            get => _pageHeight;
            set
            {
                if (value != _pageHeight)
                {
                    _pageHeight = value;
                    OnPropertyChanged(nameof(PageHeight));
                }
            }
        }
        public TitleblockAttributes Attributes
        {
            get => _attributes;
            set
            {
                if (_attributes != value)
                {
                    _attributes = value;
                    OnPropertyChanged(nameof(Attributes));
                }
            }
        }

        public ObservableCollection<DrawingObject> DrawingObjects { get; set; } = [];
        public FontFamily FontFamily { get; set; } = new FontFamily("Arial");
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
