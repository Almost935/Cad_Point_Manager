using Cad_Point_Manager.Models.DrawingObjects;
using Cad_Point_Manager.Services;
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
        private string _dxfFilePath;
        private DxfDocument _dxfDoc;
        private ObservableCollection<ObjectLayer> _layers = new();
        private Rect _extents = new();
        #endregion

        #region Properties
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
        public ObservableCollection<ObjectLayer> Layers 
        {
            get { return _layers; }
            set
            {
                _layers = value;
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

        DxfService DxfService { get; set; } = new();
        #endregion

        #region Constructors
        public JobFile()
        {

        }
        #endregion

        #region Methods
        public void LoadDxf(string dxfFilePath)
        {
            var dxfDoc = DxfDocument.Load(dxfFilePath);
            if (dxfDoc is not null) 
            { 
                DxfDoc = dxfDoc; 
                bool extentsFound = DxfService.TryGetExtentsFromDxfDoc(dxfDoc, out Rect extents);
                Layers = DxfService.LoadLayers(dxfDoc, extents);
            }
        }
        #endregion
    }
}
