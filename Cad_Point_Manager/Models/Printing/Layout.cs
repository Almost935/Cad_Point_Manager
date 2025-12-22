using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

        public Guid LayoutId { get; init; } = Guid.NewGuid();
        public string Name { get; set; } = "Layout 1";
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
