using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.LffFontRendering
{
    public static class LffFontManager
    {
        private static readonly Dictionary<string, LffFont> _cache = new();

        public static LffFont GetFont(string filename)
        {
            filename = filename.ToLowerInvariant();

            if (_cache.TryGetValue(filename, out var font)) { return font; }

            string path = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Resources",
                "Fonts",
                filename);

            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Unable to locate LFF font '{filename}'. Expected location: {path}");
            }

            font = LffParser.Load(path);

            _cache[filename] = font;

            return font;
        }
    }
}
