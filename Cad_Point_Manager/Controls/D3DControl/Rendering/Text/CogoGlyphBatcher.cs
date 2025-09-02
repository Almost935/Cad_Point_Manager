using Cad_Point_Manager.Models.PointRendering;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Text
{
    public static class CogoGlyphBatcher
    {
        public static Dictionary<short, List<GlyphInstance>> BuildBatches(
            IEnumerable<PointGroup> pointGroups,
            SharpDX.DirectWrite.FontFace fontFace,
            AdvanceWidthCache advCache,
            float fontBaseSizeWorld, // same as PointGroup.FontBaseSize
            float ySign = +1f        // use -1 if your DU->world needs Y flip
        )
        {
            var byGlyph = new Dictionary<short, List<GlyphInstance>>();

            var duPerEm = fontFace.Metrics.DesignUnitsPerEm;

            foreach (var pg in pointGroups)
            {
                if (!pg.IsVisible) continue;

                // per-group scale : world units per design unit
                float heightWorld = (float)(pg.FontBaseSize * pg.PointScale); // your base text height in world units
                float duToWorld = heightWorld / duPerEm;

                // group color/flags
                var color = pg.Color;
                float isVisible = 1f;

                foreach (var p in pg.Points)
                {
                    // Three stacked labels (you already compute these positions)
                    var basePos = new Vector2((float)p.Position.X, (float)p.Position.Y);
                    float lineH = (float)(pg.FontBaseSize * pg.PointScale); // line spacing multiplier as you need

                    var posPoint = new Vector2(basePos.X + 0.5f * heightWorld, basePos.Y + 0 * lineH);
                    var posElev = new Vector2(basePos.X + 0.5f * heightWorld, basePos.Y + 1 * lineH);
                    var posDesc = new Vector2(basePos.X + 0.5f * heightWorld, basePos.Y + 2 * lineH);

                    AddStringInstances(p.PointNumber.ToString(), posPoint);
                    AddStringInstances(p.Elevation.ToString("F3"), posElev);
                    AddStringInstances(p.Description ?? string.Empty, posDesc);

                    void AddStringInstances(string s, Vector2 origin)
                    {
                        if (string.IsNullOrEmpty(s)) return;

                        // map to glyph ids
                        var cps = new int[s.Length];
                        for (int i = 0; i < s.Length; i++) cps[i] = s[i];
                        var glyphIds = fontFace.GetGlyphIndices(cps);

                        float penDU = 0f;
                        for (int i = 0; i < glyphIds.Length; i++)
                        {
                            short gid = glyphIds[i];

                            var inst = new GlyphInstance
                            {
                                Origin = origin,
                                DuToWorld = duToWorld,
                                PenDU = penDU,
                                Color = color,
                                IsVisible = isVisible,
                                IsMouseOver = p.IsMouseOver ? 1f : 0f,
                                IsSelected = p.IsSelected ? 1f : 0f,
                                YSign = ySign
                            };

                            if (!byGlyph.TryGetValue(gid, out var list))
                                byGlyph[gid] = list = new List<GlyphInstance>();
                            list.Add(inst);

                            penDU += advCache[gid]; // advance in DU (no kerning here)
                        }
                    }
                }
            }

            return byGlyph;
        }
    }

}
