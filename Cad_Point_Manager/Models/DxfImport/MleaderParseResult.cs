using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.DxfImport
{
    public class MLeaderParseResult
    {
        public Dictionary<string, List<DxfTag>> ObjectsByHandle { get; } = [];
        public List<ParsedMLeader> MLeaders { get; } = [];
        public Dictionary<string, ParsedMLeaderStyle> MLeaderStyles { get; } = [];
        public Dictionary<string, ParsedBlockRecord> BlockRecords { get; } = [];
        public Dictionary<string, ParsedDictionary> Dictionaries { get; } = [];

        // Testing
        public void DumpObject(string handle)
        {
            if (!ObjectsByHandle.TryGetValue(
                    handle,
                    out var tags))
            {
                return;
            }

            Debug.WriteLine(
                $"---- HANDLE {handle} ----");

            foreach (var tag in tags)
            {
                Debug.WriteLine(
                    $"{tag.Code}: {tag.Value}");
            }
        }
    }
}
