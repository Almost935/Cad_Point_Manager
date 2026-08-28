using System.Diagnostics;

namespace Cad_Point_Manager.Models.DxfImport
{
    public class MLeaderParseResult
    {
        public Dictionary<string, List<DxfTag>> ObjectsByHandle { get; } = [];
        public List<ParsedMLeader> MLeaders { get; } = [];
        public Dictionary<string, ParsedMLeaderStyle> MLeaderStyles { get; } = [];
        public Dictionary<string, ParsedBlockRecord> BlockRecords { get; } = [];
        public Dictionary<string, ParsedDictionary> Dictionaries { get; } = [];
    }
}
