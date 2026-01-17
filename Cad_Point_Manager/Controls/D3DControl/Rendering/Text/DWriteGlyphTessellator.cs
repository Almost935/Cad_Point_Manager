using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using static Cad_Point_Manager.Controls.D3DControl.Rendering.Text.GlyphMeshCache;
using Factory = SharpDX.Direct2D1.Factory;

namespace Cad_Point_Manager.Controls.D3DControl.Rendering.Text
{
    // <summary>
    /// Uses DirectWrite to get the glyph outline (in DESIGN UNITS) and your
    /// TessellateGeometry(...) to generate triangles. Returns a GlyphMesh.
    /// </summary>
    public sealed class DWriteGlyphTessellator : IGlyphTessellator, IDisposable
    {
        private readonly Factory _d2dFactory;
        private readonly float _flatteningTolerance;
        private bool _disposed;

        /// <param name="d2dFactory">Direct2D factory (to create PathGeometry)</param>
        /// <param name="flatteningTolerance">Passed to Geometry.Tessellate</param>
        public DWriteGlyphTessellator(Factory d2dFactory, float flatteningTolerance = 0.25f)
        {
            _d2dFactory = d2dFactory ?? throw new ArgumentNullException(nameof(d2dFactory));
            _flatteningTolerance = flatteningTolerance;
        }

        public GlyphMesh Build(short glyphIndex, FontFace fontFace)
        {
            if (glyphIndex == 0 || fontFace == null) { return GlyphMesh.Empty; }

            // 1) Build a PathGeometry for this glyph in DESIGN UNITS.
            //    Trick: pass emSize = DesignUnitsPerEm so the outline coords come out in DU.
            var duPerEm = fontFace.Metrics.DesignUnitsPerEm;

            using (var path = new PathGeometry(_d2dFactory))
            using (var sink = path.Open())
            {
                // One-glyph "run" — advances/offsets not needed for a single glyph outline
                fontFace.GetGlyphRunOutline(
                    emSize: duPerEm,                 // -> output coords = design units
                    glyphIndices: new short[] { glyphIndex },
                    glyphAdvances: null,
                    glyphOffsets: null,
                    glyphCount: 1,
                    isSideways: false,
                    isRightToLeft: false,
                    geometrySink: sink);

                sink.Close();

                // 2) Tessellate with your code
                //    Your current sink returns vertices as a triangle list (v0,v1,v2, v3,v4,v5, ...).
                var vertices = TessellateGeometry(path, _flatteningTolerance, tesselationFactor: 1f);

                if (vertices.Count == 0) { return GlyphMesh.Empty; }

                // 3) Build a simple index array (0..N-1), 3 per triangle.
                var indices = BuildSequentialTriangleIndices(vertices.Count);

                // 4) (optional) compute DU bounds
                var bounds = ComputeBounds(vertices);

                return new GlyphMesh
                {
                    PositionsDU = vertices.ToArray(),
                    BoundsDU = bounds
                };
            }
        }

        private static int[] BuildSequentialTriangleIndices(int vertexCount)
        {
            // vertexCount should be multiple of 3
            var idx = new int[vertexCount];
            for (int i = 0; i < vertexCount; i++) { idx[i] = i; }
            return idx;
        }

        private static RectangleF ComputeBounds(List<Vector2> v)
        {
            float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
            for (int i = 0; i < v.Count; i++)
            {
                var p = v[i];
                if (p.X < minX) minX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
            }
            if (minX == float.PositiveInfinity) return RectangleF.Empty;
            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        public static List<Vector2> TessellateGeometry(Geometry geometry, float flatteningTolerance = 0.001f, float tesselationFactor = 1)
        {
            var vertices = new List<Vector2>();

            using (var sink = new CustomTessellationSink())
            {
                geometry.Tessellate(flatteningTolerance, sink);
                vertices.AddRange(sink.Vertices);
            }

            var scale = Matrix.Scaling(tesselationFactor, tesselationFactor, 1);
            for (int i = 0; i < vertices.Count; i++)
            {
                var vertex = vertices[i];
                vertices[i] = Vector2.TransformCoordinate(vertex, scale);
            }

            return vertices;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            // nothing to dispose here besides the factory which you don't own
        }
    }
}
