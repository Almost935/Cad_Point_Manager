using Cad_Point_Manager.DrawingObjects;
using Direct2DDXFViewer.DrawingObjects;
using netDxf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Cad_Point_Manager.Models
{
    public class JobFile : BaseModel
    {
        #region Fields
        private string _jobName;
        private string _jobFilePath;
        private string _dxfFilePath;
        private DxfDocument _dxfDoc;
        private ObjectLayerManager _layerManager;
        private Rect _extents = new();
        #endregion

        #region Properties
        public string JobName
        {
            get { return _jobName; }
            set
            {
                _jobName = value;
                OnPropertyChanged();
            }
        }
        public string JobFilePath
        {
            get { return _jobFilePath; }
            set
            {
                _jobFilePath = value;
                OnPropertyChanged();
            }
        }
        public string DxfFilePath
        {
            get { return _dxfFilePath; }
            set
            {
                _dxfFilePath = value;
                OnPropertyChanged();
            }
        }
        public DxfDocument DxfDoc
        {
            get { return  _dxfDoc; }
            set
            {
                _dxfDoc = value;
                OnPropertyChanged();
            }
        }
        public ObjectLayerManager LayerManager 
        {
            get { return _layerManager; }
            set
            {
                _layerManager = value;
                OnPropertyChanged();
            }
        }
        public Rect Extents
        {
           get { return _extents; }
            set
            {
                _extents = value;
                OnPropertyChanged();
            }
        }

        public bool DxfLoaded { get { return LayerManager is not null; } }
        #endregion

        #region Constructors
        public JobFile()
        {

        }
        #endregion

        #region Methods
        public void LoadDxf(DxfDocument dxfDoc)
        {
            
        }
        #endregion
    }
}
