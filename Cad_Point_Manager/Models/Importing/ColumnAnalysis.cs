
namespace Cad_Point_Manager.Models.Importing
{
    public enum ColumnDataType
    {
        Unknown,
        Integer,
        Double,
        String
    }

    public class ColumnAnalysis
    {
        public int Index { get; set; }
        public string Header { get; set; }

        public ColumnDataType DetectedType { get; set; }

        public bool IsAllIntegers { get; set; }
        public bool IsAllDoubles { get; set; }

        public List<string> SampleValues { get; set; } = new();

        public ColumnMapping Mapping { get; set; }
    }
}
