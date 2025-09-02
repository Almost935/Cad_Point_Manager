using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DirectWrite;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Text
{
    public sealed class GlyphAtlas : IDisposable
    {
        public Buffer VertexBuffer { get; }
        public int VertexStride => Utilities.SizeOf<GlyphVertexDU>();
        public IReadOnlyDictionary<short, GlyphRange> Ranges { get; }

        public GlyphAtlas(Device device,
                          FontFace fontFace,
                          IGlyphTessellator tessellator,
                          IEnumerable<short> glyphIds)
        {
            var vertices = new List<GlyphVertexDU>();
            var ranges = new Dictionary<short, GlyphRange>();

            foreach (var gid in glyphIds.Distinct())
            {
                var mesh = tessellator.Build(gid, fontFace); // returns Vector2[] in DU
                int start = vertices.Count;
                for (int i = 0; i < mesh.PositionsDU.Length; i++)
                    vertices.Add(new GlyphVertexDU { PosDU = mesh.PositionsDU[i] });
                int count = mesh.PositionsDU.Length;

                ranges[gid] = new GlyphRange { StartVertex = start, VertexCount = count };
            }

            VertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, vertices.ToArray());
            Ranges = ranges;
        }

        public static GlyphAtlas CreateForAscii(Device device,
                                            FontFace fontFace,
                                            IGlyphTessellator tessellator)
        {
            var glyphIds = GlyphSets.Ascii32To126(fontFace);
            return new GlyphAtlas(device, fontFace, tessellator, glyphIds);
        }

        public void Dispose() => VertexBuffer?.Dispose();
    }
}
