using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Cad_Point_Manager.Models.Printing
{
    public class LayoutViewport : INotifyPropertyChanged
    {
        #region Fields
        private Scene _scene;
        #endregion

        #region Properties
        public Guid Id { get; init; } = Guid.NewGuid();
        public int SceneIndex { get; set; } = 0;
        public Rect LocalRectIn { get; set; }
        public bool ShowBorder { get; set; } = true;
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
