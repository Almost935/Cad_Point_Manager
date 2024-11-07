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
        private string _dxfFileName;
        private CadManager _cadManager;
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
        public string DxfFileName
        {
            get { return _dxfFileName; }
            set
            {
                _dxfFileName = value;
                OnPropertyChanged();
            }
        }
        public CadManager CadManager 
        {
            get { return _cadManager; }
            set
            {
                _cadManager = value;
                OnPropertyChanged();
            }
        }

        public bool DxfLoaded { get; set; } = false;
        #endregion

        #region Constructors
        public JobFile() 
        { 
            CadManager = new(); 
        }
        #endregion

        #region Methods
        public void LoadDxf(string filepath)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            DxfLoaded = false;

            DxfDoc = DxfDocument.Load(filepath);
            if (DxfDoc is not null)
            {
                DxfFilePath = filepath;
                DxfFileName = DxfDoc.Name;
                CadManager.LoadDxfDocument(DxfDoc);
                DxfLoaded = true;
            }           

            stopwatch.Stop();
            Debug.WriteLine($"LoadDxfDocument: {stopwatch.ElapsedMilliseconds} ms");
        }

        public void SaveToFile()
        {

        }
        #endregion
    }
}
