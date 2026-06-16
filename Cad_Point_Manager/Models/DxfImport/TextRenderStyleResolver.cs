using Cad_Point_Manager.Models.DrawingObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.DxfImport
{
    public static class TextRenderStyleResolver
    {
        private static readonly Dictionary<string, TextRenderStyle> _fontMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["monotxt"] = (TextRenderStyle.Triangle),
            };

        public static TextRenderStyle Resolve(string? fontName)
        {
            if (string.IsNullOrWhiteSpace(fontName))
            {
                return TextRenderStyle.Triangle;
            }

            fontName = fontName.Trim();

            if (_fontMap.TryGetValue(fontName, out var renderStyle))
            {
                return renderStyle;
            }

            return TextRenderStyle.Triangle;
        }
    }
}
