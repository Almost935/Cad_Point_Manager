using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.DxfImport
{
    public static class MleaderParser
    {
        public static List<ParsedMLeader> Read(string dxfPath)
        {
            var tags = DxfTagReader.ReadTags(dxfPath);

            var leaders = new List<ParsedMLeader>();

            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i].Code == 0 &&
                    tags[i].Value == "MULTILEADER")
                {
                    int start = i;

                    i++;

                    while (i < tags.Count &&
                           tags[i].Code != 0)
                    {
                        i++;
                    }

                    leaders.Add(
                        ParseEntity(
                            tags.GetRange(
                                start,
                                i - start)));
                }
            }

            return leaders;
        }

        private static ParsedMLeader ParseEntity(List<DxfTag> tags)
        {
            var leader = new ParsedMLeader();

            ParseText(tags, leader);
            ParseTextLocation(tags, leader);
            ParseLeaderVertices(tags, leader);

            return leader;
        }

        private static void ParseText(List<DxfTag> tags, ParsedMLeader leader)
        {
            foreach (var tag in tags)
            {
                if (tag.Code == 304)
                {
                    leader.Text = tag.Value;
                    return;
                }
            }
        }

        private static void ParseTextLocation(List<DxfTag> tags, ParsedMLeader leader)
        {
            float x = 0;
            float y = 0;
            float z = 0;

            bool foundX = false;
            bool foundY = false;
            bool foundZ = false;

            foreach (var tag in tags)
            {
                if (tag.Code == 12)
                {
                    x = float.Parse(tag.Value);
                    foundX = true;
                }

                if (tag.Code == 22)
                {
                    y = float.Parse(tag.Value);
                    foundY = true;
                }

                if (tag.Code == 32)
                {
                    z = float.Parse(tag.Value);
                    foundZ = true;
                }
            }

            if (foundX && foundY)
            {
                leader.TextLocation =
                    new Vector3(x, y, z);
            }
        }

        private static void ParseLeaderVertices(List<DxfTag> tags, ParsedMLeader leader)
        {
            List<Vector3> line = [];

            float? x = null;
            float? y = null;

            foreach (var tag in tags)
            {
                if (tag.Code == 10)
                {
                    x = float.Parse(tag.Value);
                }
                else if (tag.Code == 20)
                {
                    y = float.Parse(tag.Value);
                }
                else if (tag.Code == 30 &&
                         x.HasValue &&
                         y.HasValue)
                {
                    line.Add(
                        new Vector3(
                            x.Value,
                            y.Value,
                            float.Parse(tag.Value)));
                }
            }

            if (line.Count > 1)
            {
                leader.LeaderLines.Add(line);
            }
        }
    }
}
