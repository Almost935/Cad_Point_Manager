using Cad_Point_Manager.Helpers;
using Cad_Point_Manager.Models.PointRendering;
using SharpDX;
using SharpDX.DirectWrite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Text
{
    public sealed class CogoPointLabelLayout
    {
        private readonly FontBoxMetrics _metrics;
        private readonly AdvanceWidthCache _advance;

        public CogoPointLabelLayout(FontBoxMetrics metrics, AdvanceWidthCache advance)
        {
            _metrics = metrics;
            _advance = advance;
        }

        public CogoPointBoundsSnapshot BuildForPoint(
            CogoPoint p,
            FontFace face,
            float worldTextHeight,
            Vector4 color,
            float isVisible,
            float isMouseOver,
            float isSelected,
            float ySign,
            Dictionary<short, List<GlyphInstance>> outBatches,
            Camera cam)
        {
            if (p == null) return new CogoPointBoundsSnapshot();

            // DU -> world conversion for this label height
            float duToWorld = worldTextHeight / _metrics.UnitsPerEm;

            // Per-line bounds (baseline-left anchored)
            Rect name = LayoutLine(p.PointNumber.ToString(), p.PointNumberPosition, face, duToWorld, ySign);
            Rect elev = LayoutLine(p.Elevation.ToString("F3"), p.ElevationPosition, face, duToWorld, ySign);
            Rect desc = LayoutLine(p.Description, p.DescriptionPosition, face, duToWorld, ySign);

            // Emit glyph instances
            EmitLine(p.PointNumber.ToString(), p.PointNumberPosition);
            EmitLine(p.Elevation.ToString("F3"), p.ElevationPosition);
            if (!string.IsNullOrEmpty(p.Description))
                EmitLine(p.Description, p.DescriptionPosition);

            // Ellipse (pixel radius -> world)
            Rect ellipse = ComputeEllipseRectFor(p, cam);

            return new CogoPointBoundsSnapshot
            {
                Name = name,
                Elevation = elev,
                Description = desc,
                Ellipse = ellipse
            };

            // ---------- local helpers ----------

            void EmitLine(string text, Vector2 originWorld)
            {
                if (string.IsNullOrEmpty(text)) return;

                // map chars -> glyph ids once
                var cps = new int[text.Length];
                for (int i = 0; i < text.Length; i++) cps[i] = text[i];
                var gids = face.GetGlyphIndices(cps);

                float penDU = 0f;
                for (int i = 0; i < gids.Length; i++)
                {
                    short gid = (short)gids[i];
                    if (gid <= 0) continue;

                    if (!outBatches.TryGetValue(gid, out var list))
                        outBatches[gid] = list = new List<GlyphInstance>(32);

                    list.Add(new GlyphInstance
                    {
                        Origin = originWorld,
                        DuToWorld = duToWorld,
                        PenDU = penDU,
                        Color = color,
                        IsVisible = isVisible,
                        IsMouseOver = isMouseOver,
                        IsSelected = isSelected,
                        YSign = ySign
                    });

                    penDU += GetAdvanceDU(gid); // accumulate pen in DU
                }
            }

            Rect LayoutLine(string text, Vector2 originWorld, FontFace f, float duToW, float ySgn)
            {
                if (string.IsNullOrEmpty(text)) return Rect.Empty;

                // width from advances (DU)
                var cps = new int[text.Length];
                for (int i = 0; i < text.Length; i++) cps[i] = text[i];
                var gids = f.GetGlyphIndices(cps);

                float widthDU = 0f;
                for (int i = 0; i < gids.Length; i++)
                {
                    short gid = (short)gids[i];
                    if (gid > 0) widthDU += GetAdvanceDU(gid);
                }
                float w = widthDU * duToW;

                // vertical extents from ascent/descent (world)
                float ascW = _metrics.AscentDU * duToW;   // baseline -> top (magnitude)
                float descW = _metrics.DescentDU * duToW;   // baseline -> bottom (magnitude)

                // respect Y orientation: -1 for Y-down DU, +1 for Y-up
                float yTop = originWorld.Y + ySgn * ascW;
                float yBottom = originWorld.Y - ySgn * descW;

                float y = Math.Min(yTop, yBottom);
                float h = Math.Abs(yBottom - yTop);

                return new Rect(originWorld.X, y, w, h);
            }

            float GetAdvanceDU(short gid)
            {
                // Works with either an indexer OR TryGetValue on your cache
                try
                {
                    // indexer path
                    return _advance[gid];
                }
                catch
                {
                    // TryGetValue path (uncomment if your cache exposes it)
                    // if (_advance.TryGetValue(gid, out var adv)) return adv;
                    return 0f;
                }
            }
        }

        // exactly your computation
        private static Rect ComputeEllipseRectFor(CogoPoint p, Camera cam)
        {
            float wupp = cam.GetWorldUnitsPerPixel();
            float rW = (float)(GlobalHelperProperties.CogoPointCirclePixelRadius
                               * wupp * p.PointGroup.PointScale);
            var c = p.Position;
            return new Rect(c.X - rW, c.Y - rW, 2 * rW, 2 * rW);
        }
    }
}
