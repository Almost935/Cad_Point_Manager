namespace Cad_Point_Manager.Models.Importing
{
    public enum CogoFieldType
    {
        None,
        PointNumber,
        Northing,
        Easting,
        Elevation,
        Description,
        PointGroup
    }

    public class ColumnMapping
    {
        public int ColumnIndex { get; set; }
        public CogoFieldType AssignedField { get; set; }
    }
}
