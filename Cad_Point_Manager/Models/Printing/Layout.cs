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
        public Guid LayoutId { get; init; } = Guid.NewGuid();
        public string Name { get; set; } = "Layout 1";

        public double PageWidthIn { get; set; } = 36;
        public double PageHeightIn { get; set; } = 24;
        public ObservableCollection<LayoutViewport> Viewports { get; } = [];

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
