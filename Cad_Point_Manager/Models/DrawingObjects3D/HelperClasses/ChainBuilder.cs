namespace Cad_Point_Manager.Models.DrawingObjects3D
{
    public readonly record struct Pt(double X, double Y);
    public readonly record struct EdgeInput(Pt A, Pt B, SegmentKind Kind, ArcData? Arc = null);
    public readonly record struct ArcData(Pt Center, double Radius, double StartAngle, double SweepAngle);
    public readonly record struct EdgeUse(EdgeInput Edge, bool Forward); // Forward=true means A->B as stored in Edge

    public enum SegmentKind { Line, Arc, Circle }

    public sealed class ChainPath
    {
        public List<Pt> Nodes { get; }             // snapped vertex positions (start..end; loops will repeat the start at the end)
        public List<EdgeUse> Steps { get; }        // each segment in order, with direction
        public ChainPath(List<Pt> nodes, List<EdgeUse> steps) { Nodes = nodes; Steps = steps; }
    }

    public static class ChainBuilder
    {
        private readonly record struct Key(long X, long Y);
        private static Key K(Pt p, double eps)
        {
            long qx = (long)Math.Round(p.X / eps);
            long qy = (long)Math.Round(p.Y / eps);
            return new Key(qx, qy);
        }

        // ---- 1) Connectivity + payload → directed chains ----
        public static List<ChainPath> BuildChainsDetailed(IEnumerable<EdgeInput> edges, double eps)
        {
            // snap vertices
            var keyToNode = new Dictionary<Key, int>();
            var nodes = new List<Pt>();
            int NodeFor(Pt p)
            {
                var k = K(p, eps);
                if (keyToNode.TryGetValue(k, out int id)) return id;
                id = nodes.Count;
                keyToNode[k] = id;
                nodes.Add(p);
                return id;
            }

            // adjacency + edge list (carry payload)
            var adj = new List<List<(int v, int e)>>();
            var elist = new List<(int u, int v, EdgeInput payload)>();

            void EnsureAdjSize(int n) { while (adj.Count <= n) adj.Add([]); }

            foreach (var e in edges)
            {
                int u = NodeFor(e.A);
                int v = NodeFor(e.B);
                if (u == v) continue;
                int ei = elist.Count;
                elist.Add((u, v, e));
                EnsureAdjSize(Math.Max(u, v));
                adj[u].Add((v, ei));
                adj[v].Add((u, ei));
            }

            var used = new bool[elist.Count];
            var result = new List<ChainPath>();

            bool HasUnvisited(int u) => u < adj.Count && adj[u].Any(ae => !used[ae.e]);
            IEnumerable<int> OddStarts() => Enumerable.Range(0, adj.Count).Where(u => adj[u].Count % 2 == 1 && HasUnvisited(u));
            IEnumerable<int> AnyStarts() => Enumerable.Range(0, adj.Count).Where(HasUnvisited);

            void WalkFrom(int start)
            {
                var nodeIds = new List<int> { start };
                var steps = new List<EdgeUse>();
                int u = start;

                while (true)
                {
                    int next = -1, nextEdge = -1;
                    foreach (var (v, e) in adj[u])
                    {
                        if (!used[e])
                        {
                            next = v;
                            nextEdge = e;
                            break;
                        }
                    }
                    if (next == -1) { break; }

                    used[nextEdge] = true;

                    // determine direction
                    var (eu, ev, payload) = elist[nextEdge];
                    bool forward = (eu == u && ev == next) || !(ev == u && eu == next) && (eu == u); // simple check
                    if (eu == u && ev == next) forward = true;
                    else if (ev == u && eu == next) forward = false;

                    steps.Add(new EdgeUse(payload, forward));
                    nodeIds.Add(next);
                    u = next;
                }

                if (steps.Count > 0)
                {
                    result.Add(new ChainPath(nodeIds.Select(id => nodes[id]).ToList(), steps));
                }
            }

            foreach (var s in OddStarts()) WalkFrom(s); // open chains first
            foreach (var s in AnyStarts()) WalkFrom(s); // then loops

            return result;
        }

        public static List<List<Pt>> BuildChains(IEnumerable<EdgeInput> edges, double eps)
            => BuildChainsDetailed(edges, eps).Select(c => c.Nodes).ToList();

        public static List<Pt> ExpandChainPoints(ChainPath chain, int intermediatesPerSegment)
        {
            var points = new List<Pt>();
            bool isClosed = chain.Nodes.Count > 1 && chain.Nodes.First().Equals(chain.Nodes.Last());

            for (int i = 0; i < chain.Steps.Count; i++)
            {
                var step = chain.Steps[i];

                // pick oriented endpoints
                var a = step.Forward ? step.Edge.A : step.Edge.B;
                var b = step.Forward ? step.Edge.B : step.Edge.A;

                if (i == 0)
                {
                    points.Add(a);
                }

                if (step.Edge.Kind == SegmentKind.Line)
                {
                    for (int k = 1; k <= intermediatesPerSegment; k++)
                    {
                        double t = (double)k / (intermediatesPerSegment + 1);
                        points.Add(new Pt(
                            a.X + (b.X - a.X) * t,
                            a.Y + (b.Y - a.Y) * t));
                    }
                }
                else
                {
                    var arc = step.Edge.Arc!.Value;

                    double start = arc.StartAngle;
                    double sweep = arc.SweepAngle;
                    if (!step.Forward) { start = start + sweep; sweep = -sweep; }

                    for (int k = 1; k <= intermediatesPerSegment; k++)
                    {
                        double t = (double)k / (intermediatesPerSegment + 1);
                        double ang = start + sweep * t;
                        points.Add(new Pt(
                            arc.Center.X + arc.Radius * Math.Cos(ang),
                            arc.Center.Y + arc.Radius * Math.Sin(ang)));
                    }
                }

                // Add the segment end, except for the last edge of a closed loop (avoid duplicating the first point)
                bool isLastEdge = (i == chain.Steps.Count - 1);
                if (!(isLastEdge && isClosed))
                    points.Add(b);
            }

            return points;
        }
    }
}
