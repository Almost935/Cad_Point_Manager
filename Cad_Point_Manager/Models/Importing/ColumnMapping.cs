using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.Importing
{
    public enum CogoFieldType
    {
        None,
        PointNumber,
        Northing,
        Easting,
        Elevation,
        Description
    }

    public class ColumnMapping
    {
        public int ColumnIndex { get; set; }
        public CogoFieldType AssignedField { get; set; }
    }
}
