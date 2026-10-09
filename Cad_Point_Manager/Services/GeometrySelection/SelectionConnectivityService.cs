using Cad_Point_Manager.Models.DrawingObjects;

namespace Cad_Point_Manager.Services.GeometrySelection
{
    public sealed class SelectionConnectivityService : ISelectionConnectivityService
    {
        public List<ChainPath> BuildChainsFromSelection(
            IEnumerable<DrawingObject> selected, double eps)
        {
            if (selected is null)
                return [];

            var raw = new List<EdgeInput>(1024);
            List<ChainPath> circleChains = [];

            foreach (var g in selected)
            {
                switch (g)
                {
                    case DrawingLine ln:
                        {
                            Pt a = new(ln.Start.X, ln.Start.Y);
                            Pt b = new(ln.End.X, ln.End.Y);

                            raw.Add(new EdgeInput(a, b, SegmentKind.Line));

                            break;
                        }

                    case DrawingArc arc:
                        {
                            AddArcEdge(raw, arc);
                            break;
                        }

                    case DrawingCircle circle:
                        {
                            Pt center = new(circle.RadiusPoint.X, circle.RadiusPoint.Y);
                            ArcData arcData = new(center, circle.Radius, 0.0, 360.0);

                            EdgeInput circleEdge = new(center, center, SegmentKind.Circle, arcData);

                            circleChains.Add(new ChainPath([], [new EdgeUse(circleEdge, true)]));

                            break;
                        }

                    case DrawingPolyline polyline:
                        {
                            foreach (DrawingSegment segment in polyline.DrawingSegments)
                            {
                                switch (segment)
                                {
                                    case DrawingLine lineSegment:
                                        {
                                            Pt a = new(lineSegment.Start.X, lineSegment.Start.Y);
                                            Pt b = new(lineSegment.End.X, lineSegment.End.Y);

                                            raw.Add(new EdgeInput(a, b, SegmentKind.Line));

                                            break;
                                        }

                                    case DrawingArc arcSegment:
                                        {
                                            AddArcEdge(raw, arcSegment);

                                            break;
                                        }
                                }
                            }

                            break;
                        }
                }
            }

            // Remove duplicate geometric edges.
            var seen = new HashSet<EdgeKey>();
            var deduped = new List<EdgeInput>(raw.Count);

            foreach (EdgeInput edge in raw)
            {
                if (seen.Add(EdgeKey.FromEdge(edge)))
                {
                    deduped.Add(edge);
                }
            }

            List<ChainPath> chains = ChainBuilder.BuildChainsDetailed(deduped, eps);

            chains.AddRange(circleChains);

            return chains;
        }

        private static void AddArcEdge(List<EdgeInput> edges, DrawingArc arc)
        {
            double cx = arc.RadiusPoint.X;
            double cy = arc.RadiusPoint.Y;
            double radius = arc.Radius;

            double startAngle = arc.StartAngle;
            double sweepAngle = arc.Sweep;

            Pt a = AtAngle(cx, cy, radius, startAngle);
            Pt b = AtAngle(cx, cy, radius, startAngle + sweepAngle);

            ArcData arcData = new(new Pt(cx, cy), radius, startAngle, sweepAngle);

            edges.Add(new EdgeInput(a, b, SegmentKind.Arc, arcData));
        }

        private static Pt AtAngle(double cx, double cy, double r, double deg)
        {
            var rad = deg * Math.PI / 180.0;
            return new Pt(cx + r * Math.Cos(rad), cy + r * Math.Sin(rad));
        }

        /// <summary>
        /// Direction-agnostic key for per-call dedupe.
        /// Lines: (A,B)==(B,A). Arcs: normalize sweep to non-negative and include center/radius.
        /// Circles: center+radius with canonical 360° sweep.
        /// </summary>
        private readonly struct EdgeKey : IEquatable<EdgeKey>
        {
            private readonly double Ax, Ay, Bx, By;
            private readonly int Kind;
            private readonly double Cx, Cy, R, Start, Sweep;

            private EdgeKey(double ax, double ay, double bx, double by, SegmentKind kind,
                            double cx, double cy, double r, double start, double sweep)
            {
                Ax = ax; Ay = ay; Bx = bx; By = by;
                Kind = (int)kind;
                Cx = cx; Cy = cy; R = r; Start = start; Sweep = sweep;
            }

            public static EdgeKey FromEdge(EdgeInput e)
            {
                // Canonicalize endpoints so (A,B)==(B,A)
                var (aN, bN) = Order(e.A, e.B);

                double cx = 0, cy = 0, r = 0, start = 0, sweep = 0;

                if (e.Kind == SegmentKind.Arc && e.Arc.HasValue)
                {
                    var ad = e.Arc.Value;
                    cx = ad.Center.X; cy = ad.Center.Y; r = ad.Radius;
                    start = ad.StartAngle; sweep = ad.SweepAngle;
                    if (sweep < 0) { start = start + sweep; sweep = -sweep; }
                }
                else if (e.Kind == SegmentKind.Circle && e.Arc.HasValue)
                {
                    var ad = e.Arc.Value;
                    cx = ad.Center.X; cy = ad.Center.Y; r = ad.Radius;
                    start = 0; sweep = 360;
                }

                return new EdgeKey(aN.X, aN.Y, bN.X, bN.Y, e.Kind, cx, cy, r, start, sweep);
            }

            private static (Pt a, Pt b) Order(Pt a, Pt b)
            {
                if (a.X < b.X) return (a, b);
                if (a.X > b.X) return (b, a);
                return (a.Y <= b.Y) ? (a, b) : (b, a);
            }

            public bool Equals(EdgeKey o)
            {
                return Ax.Equals(o.Ax) && Ay.Equals(o.Ay) &&
                       Bx.Equals(o.Bx) && By.Equals(o.By) &&
                       Kind == o.Kind &&
                       Cx.Equals(o.Cx) && Cy.Equals(o.Cy) &&
                       R.Equals(o.R) && Start.Equals(o.Start) && Sweep.Equals(o.Sweep);
            }
            public override bool Equals(object obj) => obj is EdgeKey ek && Equals(ek);

            public override int GetHashCode()
            {
                unchecked
                {
                    long H(double d) => BitConverter.DoubleToInt64Bits(d);
                    var h = 17;
                    h = h * 31 + H(Ax).GetHashCode();
                    h = h * 31 + H(Ay).GetHashCode();
                    h = h * 31 + H(Bx).GetHashCode();
                    h = h * 31 + H(By).GetHashCode();
                    h = h * 31 + Kind.GetHashCode();
                    h = h * 31 + H(Cx).GetHashCode();
                    h = h * 31 + H(Cy).GetHashCode();
                    h = h * 31 + H(R).GetHashCode();
                    h = h * 31 + H(Start).GetHashCode();
                    h = h * 31 + H(Sweep).GetHashCode();
                    return h;
                }
            }
        }
    }
}
