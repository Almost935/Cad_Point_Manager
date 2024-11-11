using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Cad_Point_Manager.Models
{
    public class JobFileData
    {
        #region Properties
        public string JobName { get; set; }
        public string JobFileLocation { get; set; }
        public string JobFilePath { get; set; }
        public string DxfFilePath { get; set; }
        public Rect Extents { get; set; }
        #endregion

        #region Constructors
        public JobFileData(string jobName, string jobFileLocation)
        {
        }
        #endregion

        #region Methods
        #endregion
    }
    public class DrawingLineData : DrawingSegmentData
    {

    }
}
