using Cad_Point_Manager.Models.DrawingObjects3D;

namespace Cad_Point_Manager.Services
{
    /// <summary>
    /// Minimal adapter from common DXF entities to EdgeInput, then uses ChainBuilder.
    /// Add cases for your own geometry types as needed.
    /// </summary>
    public sealed class SelectionConnectivityService : ISelectionConnectivityService
    {
        public List<ChainPath> BuildChainsFromSelection(IEnumerable<DrawingObject> selected, double eps)
        {
            var inputs = new List<EdgeInput>();
            var circleChains = new List<ChainPath>();

            foreach (var obj in selected)
            {
                switch (obj)
                {
                    case DrawingLine ln:
                        {
                            inputs.Add(new EdgeInput(ToPt(ln.Start), ToPt(ln.End), SegmentKind.Line));
                            break;
                        }

                    case DrawingArc arc:
                        {
                            var a = ToPt(arc.Start);
                            var b = ToPt(arc.End);
                            var c = ToPt(arc.RadiusPoint);
                            double r = arc.Radius;

                            // compute start angle from center to start/end
                            double thS = Math.Atan2(a.Y - c.Y, a.X - c.X);
                            double thE = Math.Atan2(b.Y - c.Y, b.X - c.X);

                            //// Determine sweep with orientation. If you have arc.IsCounterClockwise, use it.
                            //bool ccw = arc.IsCounterClockwise;   // <-- adapt if your type differs
                            //double sweep = ccw ? AngleCCW(thS, thE) : -AngleCCW(thE, thS);
                            double sweep = AngleCCW(thS, thE);

                            var ad = new ArcData(c, r, thS, sweep);
                            inputs.Add(new EdgeInput(a, b, SegmentKind.Arc, ad));
                            break;
                        }

                    case DrawingCircle circle:
                        {
                            var c = ToPt(circle.RadiusPoint);
                            double r = circle.Radius;

                            double start = 0.0;
                            var a = new Pt(c.X + r, c.Y);

                            var arcData = new ArcData(c, r, start, 2.0 * Math.PI);
                            var edge = new EdgeInput(a, a, SegmentKind.Arc, arcData);

                            var nodes = new List<Pt> { a, a };
                            var steps = new List<EdgeUse> { new(edge, true) };
                            circleChains.Add(new ChainPath(nodes, steps));
                            break;
                        }

                    case DrawingPolyline polyline:
                        {
                            foreach (var seg in polyline.DrawingSegments)
                            {
                                switch (seg)
                                {
                                    case DrawingLine lineSeg:
                                        {
                                            inputs.Add(new EdgeInput(ToPt(lineSeg.Start), ToPt(lineSeg.End), SegmentKind.Line));
                                            break;
                                        }

                                    case DrawingArc arcSeg:
                                        {
                                            var a = ToPt(arcSeg.Start);
                                            var b = ToPt(arcSeg.End);
                                            var c = ToPt(arcSeg.RadiusPoint);
                                            double r = arcSeg.Radius;

                                            // compute start angle from center to start/end
                                            double thS = Math.Atan2(a.Y - c.Y, a.X - c.X);
                                            double thE = Math.Atan2(b.Y - c.Y, b.X - c.X);

                                            //// Determine sweep with orientation. If you have arc.IsCounterClockwise, use it.
                                            //bool ccw = arc.IsCounterClockwise;   // <-- adapt if your type differs
                                            //double sweep = ccw ? AngleCCW(thS, thE) : -AngleCCW(thE, thS);
                                            double sweep = AngleCCW(thS, thE);

                                            var ad = new ArcData(c, r, thS, sweep);
                                            inputs.Add(new EdgeInput(a, b, SegmentKind.Arc, ad));
                                            break;
                                        }

                                    default:
                                        break; // unknown segment type
                                }
                            }

                            break;
                        }

                    default:
                        break;
                }
            }

            var chains = ChainBuilder.BuildChainsDetailed(inputs, eps);
            chains.AddRange(circleChains);

            return chains;
        }

        private static Pt ToPt(SharpDX.Vector3 v) => new Pt(v.X, v.Y);
        private static Pt ToPt(SharpDX.Vector2 v) => new Pt(v.X, v.Y);

        private static double AngleCCW(double from, double to)
        {
            double d = to - from;
            while (d < 0) d += Math.PI * 2;
            while (d >= Math.PI * 2) d -= Math.PI * 2;
            return d;
        }
    }
}
