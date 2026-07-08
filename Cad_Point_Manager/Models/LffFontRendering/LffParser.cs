using SharpDX;
using SixLabors.Fonts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Models.LffFontRendering
{
    public static class LffParser
    {
        private static List<string> _debugHits = new List<string>() { "F", "e", "x", "1" };

        public static LffFont Load(string filename)
        {
            using var reader = new StreamReader(filename);

            LffFont font = new();
            LffGlyph currentGlyph = null;

            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(line)) { continue; }

                if (line.StartsWith("#"))
                {
                    ParseHeader(font, line);
                    continue;
                }

                if (line.StartsWith("["))
                {
                    currentGlyph = ParseGlyphHeader(font, line);
                    continue;
                }

                if (line.StartsWith("C"))
                {
                    CopyGlyph(font, currentGlyph, line);
                    continue;
                }

                ParsePolyline(currentGlyph, line);
            }

            ComputeGlyphMetrics(font);

            return font;
        }

        private static void ParseHeader(LffFont font, string line)
        {
            line = line.Substring(1).Trim();

            int colon = line.IndexOf(':');

            if (colon < 0) { return; }

            string key = line[..colon].Trim();
            string value = line[(colon + 1)..].Trim();

            switch (key)
            {
                case "Name":
                    font.Name = value;
                    break;

                case "LetterSpacing":
                    font.LetterSpacing = float.Parse(value, CultureInfo.InvariantCulture);
                    break;

                case "WordSpacing":
                    font.WordSpacing = float.Parse(value, CultureInfo.InvariantCulture);
                    break;
            }
        }

        private static LffGlyph ParseGlyphHeader(LffFont font, string line)
        {
            int closeBracket = line.IndexOf(']');

            if (closeBracket < 0)
            {
                throw new FormatException($"Invalid LFF glyph header: '{line}'");
            }

            string hex = line.Substring(1, closeBracket - 1);

            char c = (char)Convert.ToUInt16(hex, 16);

            LffGlyph glyph = new()
            {
                Character = c
            };

            font.Glyphs.Add(c, glyph);

            return glyph;
        }

        private static void ParsePolyline(LffGlyph glyph, string line)
        {
            LffStroke stroke = new();

            Vector2? previousPoint = null;

            foreach (string token in line.Split(';'))
            {
                string piece = token.Trim();

                string[] parts = piece.Split(',');

                if (parts.Length < 2) { continue; }

                Vector2 point = new(
                    float.Parse(parts[0], CultureInfo.InvariantCulture),
                    float.Parse(parts[1], CultureInfo.InvariantCulture));

                float bulge = 0;

                if (parts.Length >= 3 && parts[2].StartsWith("A", StringComparison.OrdinalIgnoreCase))
                {
                    bulge = float.Parse(parts[2].Substring(1), CultureInfo.InvariantCulture);
                }

                if (previousPoint != null)
                {
                    stroke.Segments.Add(
                        new LffPathSegment
                        {
                            Start = previousPoint.Value,
                            End = point,
                            Bulge = bulge
                        });
                }

                previousPoint = point;
            }

            if (stroke.Segments.Count > 0) { glyph.Strokes.Add(stroke); }
        }

        private static void ComputeGlyphMetrics(LffFont font)
        {
            foreach (var glyph in font.Glyphs.Values)
            {
                bool hasPoint = false;

                float minX = float.MaxValue;
                float maxX = float.MinValue;
                float minY = float.MaxValue;
                float maxY = float.MinValue;

                foreach (var stroke in glyph.Strokes)
                {
                    foreach (var seg in stroke.Segments)
                    {
                        hasPoint = true;

                        minX = Math.Min(minX, seg.Start.X);
                        minY = Math.Min(minY, seg.Start.Y);

                        maxX = Math.Max(maxX, seg.Start.X);
                        maxY = Math.Max(maxY, seg.Start.Y);

                        minX = Math.Min(minX, seg.End.X);
                        minY = Math.Min(minY, seg.End.Y);

                        maxX = Math.Max(maxX, seg.End.X);
                        maxY = Math.Max(maxY, seg.End.Y);
                    }
                }

                if (!hasPoint) { continue; }

                glyph.Bounds = new RectangleF(minX, minY, maxX - minX, maxY - minY);

                //glyph.AdvanceWidth = glyph.Bounds.Width + font.LetterSpacing;
                glyph.AdvanceWidth = maxX + font.LetterSpacing;

                //Debug.WriteLineIf(_debugHits.Contains(glyph.Character.ToString()), $"{glyph.Character} minX={minX} maxX={maxX} width={maxX - minX}");
            }

            // Get the overall design height of the font by finding the min and max Y values across all glyphs.

            if (font.Glyphs.TryGetValue('I', out var testGlyph))
            {
                font.DesignHeight = testGlyph.Bounds.Height;
            }

            if (font.Glyphs.TryGetValue(' ', out var space)) { space.AdvanceWidth = font.WordSpacing; }
        }

        private static void CopyGlyph(LffFont font, LffGlyph glyph, string line)
        {
            ushort code = Convert.ToUInt16(line.Substring(1), 16);

            char sourceChar = (char)code;

            if (!font.Glyphs.TryGetValue(sourceChar, out var sourceGlyph))
            {
                throw new Exception($"Referenced glyph '{sourceChar}' has not been parsed.");
            }

            foreach (var stroke in sourceGlyph.Strokes)
            {
                glyph.Strokes.Add(stroke.Clone());
            }
        }
    }
}
