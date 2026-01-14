using SharpDX;
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
        private Rect _localRectIn;
        private Scene _scene;
        #endregion

        #region Properties
        public Rect LocalRectIn
        {
            get => _localRectIn;
            set
            {
                if (_localRectIn != value)
                {
                    _localRectIn = value;
                    OnPropertyChanged(nameof(LocalRectIn));
                }
            }
        }
        public Scene Scene
        {
            get => _scene;
            set
            {
                if (_scene != value)
                {
                    _scene = value;
                    OnPropertyChanged();
                }
            }
        }

        public Guid Id { get; init; } = Guid.NewGuid();
        public bool ShowBorder { get; set; } = true;
        public Matrix SceneMatrix { get; set; } = Matrix.Identity; 
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
