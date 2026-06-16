using Cad_Point_Manager.Models.DrawingObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.DxfImport
{
    public static class AutoCadFontResolver
    {
        private static readonly Dictionary<string, string> _fontMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // SHX fonts
                ["txt"] = "Monotxt",
                ["txt.shx"] = "Monotxt",

                ["simplex"] = "Monotxt",
                ["simplex.shx"] = "Monotxt",

                ["romans"] = "Times New Roman",
                ["romans.shx"] = "Times New Roman",

                ["romand"] = "Times New Roman",
                ["romand.shx"] = "Times New Roman",

                ["italic"] = "Times New Roman",
                ["italic.shx"] = "Times New Roman",

                ["iso"] = "Arial",
                ["iso.shx"] = "Arial",

                ["isocp"] = "Arial",
                ["isocp.shx"] = "Arial",

                ["gothic"] = "Arial",
                ["gothic.shx"] = "Arial",

                ["ltypeshp"] = "Arial",
                ["ltypeshp.shx"] = "Arial",

                // Common TTF passthroughs
                ["arial.ttf"] = "Arial",
                ["arial"] = "Arial",

                ["calibri.ttf"] = "Calibri",
                ["calibri"] = "Calibri",

                ["times.ttf"] = "Times New Roman",
                ["times"] = "Times New Roman",

                ["monotxt"] = "Monotxt"
            };

        public static string Resolve(string? fontName)
        {
            if (string.IsNullOrWhiteSpace(fontName))
            {
                return "Arial";
            }

            fontName = fontName.Trim();

            if (_fontMap.TryGetValue(fontName, out var mapped))
            {
                return mapped;
            }

            // Remove extension and try again
            string noExtension =
                Path.GetFileNameWithoutExtension(fontName);

            if (_fontMap.TryGetValue(noExtension, out mapped))
            {
                return mapped;
            }

            // Let DirectWrite try it directly
            return noExtension;
        }
    }
}
