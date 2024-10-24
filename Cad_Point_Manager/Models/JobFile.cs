using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models
{
    public class JobFile : BaseModel
    {
        #region Fields
        private string _dxfFilePath;
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
        #endregion
    }
}
