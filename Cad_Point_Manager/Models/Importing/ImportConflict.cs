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
        public List<(int num, double n, double e, double? elev, string? desc, string? pg)> Row { get; set; }

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

        public string Reason { get; set; }

        public ImportConflict(List<(int num, double n, double e, double? elev, string? desc, string? pg)> row, int existingPointNumber, string reason)
        {
            Row = row;
            ExistingPointNumber = existingPointNumber;
            Reason = reason;
        }   
    }
}
