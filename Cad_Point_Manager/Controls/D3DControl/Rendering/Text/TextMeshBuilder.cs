using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Cad_Point_Manager.Controls.D3DControl.Rendering.Text.GlyphMeshCache;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Text
{
    /// <summary>
    /// Builds final TextVertex triangles from (layout cache + glyph mesh cache).
    /// </summary>
    /// Expands a string into your TextVertex triangles using caches.
    public sealed class TextMeshBuilder
    {
        private readonly TextLayoutCache _layout;
        private readonly GlyphMeshCache _glyphs;

        public TextMeshBuilder(TextLayoutCache layoutCache, GlyphMeshCache glyphMeshCache)
        {
            _layout = layoutCache;
            _glyphs = glyphMeshCache;
        }

        /// <param name="originWorld">baseline origin in world coords</param>
        /// <param name="duToWorld">world units per design unit</param>
        /// <param name="color">vertex color (RGBA)</param>
        /// <param name="flags">(isVisible, isMouseOver, isSelected)</param>
        /// <param name="yUpSign">+1 for Y-up world, -1 for Y-down</param>
        public List<TextVertex> Build(
            string text,
            Vector2 originWorld,
            float duToWorld,
            Vector4 color,
            (float isVisible, float isMouseOver, float isSelected) flags,
            float yUpSign = +1f)
        {
            var outVerts = new List<TextVertex>();
            var layout = _layout.Get(text);
            if (layout.Count == 0) return outVerts;

            float penDU = 0f; // advance in DU

            for (int i = 0; i < layout.Count; i++)
            {
                var gi = layout.GlyphIndices[i];
                var advDU = layout.AdvanceDU[i];

                GlyphMesh glyph = _glyphs.Get(gi);
                if (!glyph.IsEmpty)
                {
                    // Each 3 positions in DU is a triangle
                    for (int v = 0; v < glyph.PositionsDU.Length; v += 3)
                    {
                        AppendTri(outVerts, glyph.PositionsDU, v, originWorld, penDU, duToWorld, yUpSign, color, flags);
                    }
                }
                penDU += advDU;
            }

            return outVerts;
        }

        private static void AppendTri(
            List<TextVertex> dst,
            Vector2[] srcDU, int start,
            Vector2 originWorld, float penDU, float duToWorld, float yUpSign,
            Vector4 color, (float vis, float mo, float sel) f)
        {
            dst.Add(MakeVertex(srcDU[start + 0], originWorld, penDU, duToWorld, yUpSign, color, f));
            dst.Add(MakeVertex(srcDU[start + 1], originWorld, penDU, duToWorld, yUpSign, color, f));
            dst.Add(MakeVertex(srcDU[start + 2], originWorld, penDU, duToWorld, yUpSign, color, f));
        }

        private static TextVertex MakeVertex(
            Vector2 posDU, Vector2 originWorld, float penDU, float duToWorld, float yUpSign,
            Vector4 color, (float vis, float mo, float sel) f)
        {
            float xw = originWorld.X + (penDU + posDU.X) * duToWorld;
            float yw = originWorld.Y + (posDU.Y * yUpSign) * duToWorld;
            return new TextVertex(
                new Vector3(xw, yw, 0f),
                color,
                isVisible: f.vis,
                isMouseOver: f.mo,
                isSelected: f.sel
            );
        }
    }
}
