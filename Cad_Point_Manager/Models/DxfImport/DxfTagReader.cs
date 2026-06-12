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
            using var fs = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);

            using var reader = new StreamReader(fs);

            return ReadTags(reader);
        }

        public static List<DxfTag> ReadTags(TextReader reader)
        {
            var tags = new List<DxfTag>();

            while (true)
            {
                string? codeLine = reader.ReadLine();
                if (codeLine == null)
                    break;

                string? valueLine = reader.ReadLine();
                if (valueLine == null)
                    break;

                tags.Add(
                    new DxfTag(
                        int.Parse(codeLine.Trim()),
                        valueLine.Trim()));
            }

            return tags;
        }
    }
}
