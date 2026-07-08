using Cad_Point_Manager.Models.DrawingObjects;
using System.IO;

namespace Cad_Point_Manager.Models.LffFontRendering
{
    public static class AutoCadFontResolver
    {
        private static readonly Dictionary<string, (string fontName, TextRenderStyle textRenderStyle)> _fontMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // SHX fonts
                ["txt"] = ("simplex.lff", TextRenderStyle.Stroke),
                ["txt.shx"] = ("simplex.lff", TextRenderStyle.Stroke),

                ["simplex"] = ("simplex.lff", TextRenderStyle.Stroke),
                ["simplex.shx"] = ("simplex.lff", TextRenderStyle.Stroke),

                ["romans"] = ("romans.lff", TextRenderStyle.Stroke),
                ["romans.shx"] = ("romans.lff", TextRenderStyle.Stroke),

                ["romand"] = ("romand.lff", TextRenderStyle.Stroke),
                ["romand.shx"] = ("romand.lff", TextRenderStyle.Stroke),

                ["italic"] = ("italicc.lff", TextRenderStyle.Stroke),
                ["italic.shx"] = ("italicc.lff", TextRenderStyle.Stroke),

                ["iso"] = ("iso.lff", TextRenderStyle.Stroke),
                ["iso.shx"] = ("iso.lff", TextRenderStyle.Stroke),

                ["isocp"] = ("isocp.lff", TextRenderStyle.Stroke),
                ["isocp.shx"] = ("isocp.lff", TextRenderStyle.Stroke),

                ["gothic"] = ("gothic.lff", TextRenderStyle.Stroke),
                ["gothic.shx"] = ("gothic.lff", TextRenderStyle.Stroke),

                ["ltypeshp"] = ("Arial", TextRenderStyle.Stroke),
                ["ltypeshp.shx"] = ("Arial", TextRenderStyle.Stroke),

                ["arial.ttf"] = ("Arial", TextRenderStyle.Triangle),
                ["arial"] = ("Arial", TextRenderStyle.Triangle),

                ["calibri.ttf"] = ("Calibri", TextRenderStyle.Triangle),
                ["calibri"] = ("Calibri", TextRenderStyle.Triangle),

                ["times.ttf"] = ("Times New Roman", TextRenderStyle.Triangle),
                ["times"] = ("Times New Roman", TextRenderStyle.Triangle),
            };

        public static (string fontName, TextRenderStyle textRenderStyle) Resolve(string? fontName)
        {
            if (string.IsNullOrWhiteSpace(fontName))
            {
                return ("Arial", TextRenderStyle.Triangle);
            }

            fontName = fontName.Trim();

            if (_fontMap.TryGetValue(fontName, out var mapped))
            {
                return mapped;
            }

            // Remove extension and try again
            string noExtension = Path.GetFileNameWithoutExtension(fontName);

            if (_fontMap.TryGetValue(noExtension, out mapped))
            {
                return mapped;
            }

            // Let DirectWrite try it directly
            return (noExtension, TextRenderStyle.Triangle);
        }
    }
}
