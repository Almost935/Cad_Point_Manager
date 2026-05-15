using Cad_Point_Manager.Models.DrawingObjects;

namespace Cad_Point_Manager.Services.GeometrySelection
{
    public sealed class SelectionConnectivityService : ISelectionConnectivityService
    {
        public List<ChainPath> BuildChainsFromSelection(IEnumerable<DrawingObject> selected, double eps)
        {
            if (selected is null) return new List<ChainPath>();

            // Per-call locals (no fields => no retention across calls)
            var raw = new List<EdgeInput>(1024);
            var circleChains = new List<ChainPath>();

            foreach (var g in selected)
            {
                switch (g)
                {
                    case DrawingLine ln:
                        {
                            // Lines: use Start/End (Vector3) from your model
                            var a = new Pt(ln.Start.X, ln.Start.Y);
                            var b = new Pt(ln.End.X, ln.End.Y);
                            raw.Add(new EdgeInput(a, b, SegmentKind.Line, null));
                            break;
                        }

                    case DrawingArc arc:
                        {
                            // Arcs: center = RadiusPoint, angles in degrees (StartAngle/EndAngle)
                            var cx = arc.RadiusPoint.X; var cy = arc.RadiusPoint.Y;
                            var r = arc.Radius;
                            var start = arc.StartAngle;
                            var end = arc.EndAngle;

                            var a = AtAngle(cx, cy, r, start);
                            var b = AtAngle(cx, cy, r, end);

                            var ad = new ArcData(new Pt(cx, cy), r, start, end - start); // sweep may be +/-; normalized in key
                            raw.Add(new EdgeInput(a, b, SegmentKind.Arc, ad));
                            break;
                        }

                    case DrawingCircle c:
                        {
                            // Full circle -> stand-alone chain with single EdgeUse
                            var center = new Pt(c.RadiusPoint.X, c.RadiusPoint.Y);
                            var ad = new ArcData(center, c.Radius, 0.0, 360.0);
                            var circEdge = new EdgeInput(center, center, SegmentKind.Circle, ad);
                            circleChains.Add(new ChainPath(new List<Pt>(), new List<EdgeUse> { new EdgeUse(circEdge, true) }));
                            break;
                        }

                    case DrawingPolyline pl:
                        {
                            // Exploded segments already available
                            foreach (var seg in pl.DrawingSegments)
                            {
                                if (seg is DrawingLine lseg)
                                {
                                    var a = new Pt(lseg.Start.X, lseg.Start.Y);
                                    var b = new Pt(lseg.End.X, lseg.End.Y);
                                    raw.Add(new EdgeInput(a, b, SegmentKind.Line, null));
                                }
                                else if (seg is DrawingArc aseg)
                                {
                                    var cx = aseg.RadiusPoint.X; var cy = aseg.RadiusPoint.Y;
                                    var r = aseg.Radius;
                                    var start = aseg.StartAngle;
                                    var end = aseg.EndAngle;

                                    var a = AtAngle(cx, cy, r, start);
                                    var b = AtAngle(cx, cy, r, end);

                                    var ad = new ArcData(new Pt(cx, cy), r, start, end - start);
                                    raw.Add(new EdgeInput(a, b, SegmentKind.Arc, ad));
                                }
                            }
                            break;
                        }
                }
            }

            // Dedupe within this build so the same geometric edge only appears once
            var seen = new HashSet<EdgeKey>();
            var deduped = new List<EdgeInput>(raw.Count);
            foreach (var e in raw)
                if (seen.Add(EdgeKey.FromEdge(e)))
                    deduped.Add(e);

            // Build chains from deduped inputs
            var chains = ChainBuilder.BuildChainsDetailed(deduped, eps);

            // Add circle-only chains
            chains.AddRange(circleChains);

            return chains; // fresh lists; no shared internal buffers
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
