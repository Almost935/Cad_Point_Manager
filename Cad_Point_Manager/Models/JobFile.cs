using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.DrawingObjects;
using netDxf;
using netDxf.Collections;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace Cad_Point_Manager.Models
{
    public class JobFile : BaseModel
    {
        #region Fields
        private string _jobName;
        private string _jobFilePath;
        private string _dxfFilePath;
        private DxfDocument _dxfDoc;
        private CadManager _layerManager;
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
        public CadManager LayerManager 
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

        public bool DxfLoaded { get; set; } = false;
        #endregion

        #region Constructors
        public JobFile() { }
        #endregion

        #region Methods
        public void LoadDxf(DxfDocument dxfDoc)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            DxfLoaded = false;

            DxfDoc = dxfDoc;
            Layers.Clear();
            Extents = new();

            Extents = DxfHelpers.GetExtentsFromHeader(DxfDocument);

            foreach (var e in _dxfDocument.Entities.All)
            {
                var layer = GetLayer(e.Layer);
                var obj = DxfHelpers.GetDrawingObject(e, layer);
                if (obj is not null)
                {
                    layer.DrawingObjects.Add(obj);
                }
            }
            DxfLoaded = true;

            stopwatch.Stop();
            Debug.WriteLine($"LoadDxfDocument: {stopwatch.ElapsedMilliseconds} ms");
        }

        public void SaveToFile()
        {

        }
        #endregion
    }
}
