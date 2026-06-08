using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.DxfImport
{
    public class DxfTagReader
    {
        public static List<DxfTag> ReadTags(string path)
        {
            var tags = new List<DxfTag>();

            string[] lines = File.ReadAllLines(path);

            for (int i = 0; i < lines.Length - 1; i += 2)
            {
                tags.Add(new DxfTag(
                    int.Parse(lines[i].Trim()),
                    lines[i + 1].Trim()));
            }

            return tags;
        }
    }
}
