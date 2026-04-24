using Cad_Point_Manager.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.Importing
{
    public class ImportConflict : BaseViewModel
    {
        public List<string> Row { get; set; }

        public int ExistingPointNumber { get; set; }

        private int? _newPointNumber;
        public int? NewPointNumber
        {
            get => _newPointNumber;
            set
            {
                _newPointNumber = value;
                OnPropertyChanged();
            }
        }

        public string Reason { get; set; } = "Point number already exists";

        public ImportConflict(List<string> row, int existingPointNumber)
        {
            Row = row;
            ExistingPointNumber = existingPointNumber;
        }   
    }
}
