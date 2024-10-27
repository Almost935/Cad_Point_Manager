using Direct2DDXFViewer.DrawingObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        #region Fields
        private ObjectLayerManager _layerManager;
        #endregion

        #region Properties
        public ObjectLayerManager LayerManager
        {
            get { return _layerManager; }
            set
            {
                _layerManager = value;
                OnPropertyChanged(nameof(LayerManager));
            }
        }
        #endregion

        #region Constructors
        #endregion

        #region Methods
        #endregion
    }
}
