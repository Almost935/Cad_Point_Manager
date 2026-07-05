using netDxf;

namespace Cad_Point_Manager.Models.DxfImport
{
    public class DxfImportResult
    {
        public DxfDocument DxfDocument { get; init; }

        public List<ParsedMLeader> MLeaders { get; init; } = [];

        public Dictionary<string, ParsedMLeaderStyle> MLeaderStyles { get; init; } = [];
    }
}
