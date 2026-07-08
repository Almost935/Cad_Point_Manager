
using SharpDX;
using System.Collections.Immutable;

namespace Cad_Point_Manager.Models.LffFontRendering
{
    public sealed class LffStroke
    {
        public List<LffPathSegment> Segments { get; } = [];

        public LffStroke Clone()
        {
            LffStroke clone = new();

            foreach (var seg in Segments)
            {
                clone.Segments.Add(new LffPathSegment
                {
                    Start = seg.Start,
                    End = seg.End,
                    Bulge = seg.Bulge
                });
            }

            return clone;
        }
    }
}
