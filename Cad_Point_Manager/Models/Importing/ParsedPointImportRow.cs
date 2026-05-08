using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.Importing
{
    public class ParsedPointImportRow
    {
        #region Properties
        public int PointNumber { get; set; }
        public double Northing { get; set; }
        public double Easting { get; set; }
        public double? Elevation { get; set; }
        public string? Description { get; set; }
        public string? PointGroup { get; set; }
        #endregion

        #region Constructors
        public ParsedPointImportRow(
            int pointNumber,
            double northing,
            double easting,
            double? elevation,
            string? description,
            string? pointGroup)
        {
            PointNumber = pointNumber;
            Northing = northing;
            Easting = easting;
            Elevation = elevation;
            Description = description;
            PointGroup = pointGroup;
        }
        #endregion
    }
}
