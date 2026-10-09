namespace Cad_Point_Manager.Models.DrawingObjects
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
            return new Key((long)Math.Floor(p.X / eps), (long)Math.Floor(p.Y / eps));
        }

        public static List<ChainPath> BuildChainsDetailed(IEnumerable<EdgeInput> edges, double eps)
        {
            var buckets = new Dictionary<Key, List<int>>();
            var nodes = new List<Pt>();

            int NodeFor(Pt p)
            {
                var key = K(p, eps);
                double epsSq = eps * eps;

                for (long dx = -1; dx <= 1; dx++)
                {
                    for (long dy = -1; dy <= 1; dy++)
                    {
                        var neighborKey = new Key(key.X + dx, key.Y + dy);

                        if (!buckets.TryGetValue(neighborKey, out var candidates))
                        {
                            continue;
                        }

                        foreach (int id in candidates)
                        {
                            Pt existing = nodes[id];

                            double x = existing.X - p.X;
                            double y = existing.Y - p.Y;

                            if (x * x + y * y <= epsSq)
                            {
                                return id;
                            }
                        }
                    }
                }

                int newId = nodes.Count;
                nodes.Add(p);

                if (!buckets.TryGetValue(key, out var bucket))
                {
                    bucket = [];
                    buckets[key] = bucket;
                }

                bucket.Add(newId);

                return newId;
            }

            var adj = new List<List<(int v, int e)>>();
            var elist = new List<(int u, int v, EdgeInput payload)>();

            void EnsureAdjSize(int n)
            {
                while (adj.Count <= n)
                {
                    adj.Add([]);
                }
            }

            foreach (var e in edges)
            {
                int u = NodeFor(e.A);
                int v = NodeFor(e.B);

                if (u == v)
                {
                    continue;
                }

                int ei = elist.Count;

                elist.Add((u, v, e));

                EnsureAdjSize(Math.Max(u, v));

                adj[u].Add((v, ei));
                adj[v].Add((u, ei));
            }

            var used = new bool[elist.Count];
            var result = new List<ChainPath>();

            bool HasUnvisited(int u)
            {
                return u < adj.Count && adj[u].Any(ae => !used[ae.e]);
            }

            IEnumerable<int> OddStarts()
            {
                return Enumerable.Range(0, adj.Count).Where(u => adj[u].Count % 2 == 1 && HasUnvisited(u));
            }

            IEnumerable<int> AnyStarts()
            {
                return Enumerable.Range(0, adj.Count).Where(HasUnvisited);
            }

            void WalkFrom(int start)
            {
                var nodeIds = new List<int> { start };
                var steps = new List<EdgeUse>();

                int u = start;

                while (true)
                {
                    int next = -1;
                    int nextEdge = -1;

                    foreach (var (v, e) in adj[u])
                    {
                        if (used[e])
                            continue;

                        next = v;
                        nextEdge = e;
                        break;
                    }

                    if (next == -1)
                        break;

                    used[nextEdge] = true;

                    var (eu, ev, payload) = elist[nextEdge];

                    bool forward;

                    if (eu == u && ev == next)
                        forward = true;
                    else if (ev == u && eu == next)
                        forward = false;
                    else
                        throw new InvalidOperationException("Chain traversal does not match edge endpoints.");

                    steps.Add(new EdgeUse(payload, forward));
                    nodeIds.Add(next);

                    u = next;
                }

                if (steps.Count > 0)
                    result.Add(new ChainPath(nodeIds.Select(id => nodes[id]).ToList(), steps));
            }

            foreach (int s in OddStarts())
            {
                WalkFrom(s);
            }

            foreach (int s in AnyStarts())
            {
                WalkFrom(s);
            }

            return result;
        }

        public static List<Pt> ExpandChainPoints(ChainPath chain, int intermediatesPerSegment)
        {
            List<Pt> points = [];

            if (chain.Steps.Count == 0)
                return points;

            // Circles are standalone paths and intentionally have no chain nodes.
            if (chain.Steps.Count == 1 &&
                chain.Steps[0].Edge.Kind == SegmentKind.Circle)
            {
                EdgeUse step = chain.Steps[0];

                if (step.Edge.Arc is not ArcData circle)
                {
                    throw new InvalidOperationException(
                        "Circle EdgeInput must contain ArcData.");
                }

                /*
                 * For a full circle, there is no natural start/end vertex.
                 *
                 * Use StartAngle as the starting position and distribute
                 * points around the entire circumference.
                 *
                 * intermediatesPerSegment represents how many points are
                 * requested around the circle.
                 */
                int pointCount = intermediatesPerSegment;

                if (pointCount <= 0)
                    return points;

                for (int k = 0; k < pointCount; k++)
                {
                    double t = (double)k / pointCount;

                    double angleDegrees =
                        circle.StartAngle +
                        circle.SweepAngle * t;

                    double angleRadians =
                        angleDegrees * Math.PI / 180.0;

                    Pt p = new(
                        circle.Center.X +
                        circle.Radius * Math.Cos(angleRadians),

                        circle.Center.Y +
                        circle.Radius * Math.Sin(angleRadians));

                    AddPointIfDifferent(points, p);
                }

                return points;
            }

            // Every non-circle chain must have one more node than steps.
            if (chain.Nodes.Count != chain.Steps.Count + 1)
            {
                throw new InvalidOperationException(
                    $"Invalid chain. Steps={chain.Steps.Count}, " +
                    $"Nodes={chain.Nodes.Count}. " +
                    $"A non-circle chain must contain Steps.Count + 1 nodes.");
            }

            bool isClosed =
                chain.Nodes.Count > 1 &&
                PointsEqual(
                    chain.Nodes.First(),
                    chain.Nodes.Last());

            for (int i = 0; i < chain.Steps.Count; i++)
            {
                EdgeUse step = chain.Steps[i];

                Pt a = chain.Nodes[i];
                Pt b = chain.Nodes[i + 1];

                if (i == 0)
                {
                    AddPointIfDifferent(points, a);
                }

                switch (step.Edge.Kind)
                {
                    case SegmentKind.Line:
                        {
                            for (int k = 1;
                                 k <= intermediatesPerSegment;
                                 k++)
                            {
                                double t =
                                    (double)k /
                                    (intermediatesPerSegment + 1);

                                Pt p = new(
                                    a.X + (b.X - a.X) * t,
                                    a.Y + (b.Y - a.Y) * t);

                                AddPointIfDifferent(points, p);
                            }

                            break;
                        }

                    case SegmentKind.Arc:
                        {
                            if (step.Edge.Arc is not ArcData arc)
                            {
                                throw new InvalidOperationException(
                                    "Arc EdgeInput must contain ArcData.");
                            }

                            double startDegrees;
                            double sweepDegrees;

                            if (step.Forward)
                            {
                                startDegrees =
                                    arc.StartAngle;

                                sweepDegrees =
                                    arc.SweepAngle;
                            }
                            else
                            {
                                startDegrees =
                                    arc.StartAngle +
                                    arc.SweepAngle;

                                sweepDegrees =
                                    -arc.SweepAngle;
                            }

                            for (int k = 1;
                                 k <= intermediatesPerSegment;
                                 k++)
                            {
                                double t =
                                    (double)k /
                                    (intermediatesPerSegment + 1);

                                double angleDegrees =
                                    startDegrees +
                                    sweepDegrees * t;

                                double angleRadians =
                                    angleDegrees *
                                    Math.PI / 180.0;

                                Pt p = new(
                                    arc.Center.X +
                                    arc.Radius *
                                    Math.Cos(angleRadians),

                                    arc.Center.Y +
                                    arc.Radius *
                                    Math.Sin(angleRadians));

                                AddPointIfDifferent(points, p);
                            }

                            break;
                        }

                    case SegmentKind.Circle:
                        {
                            // A circle should have been handled above.
                            throw new InvalidOperationException(
                                "Circle unexpectedly appeared inside a normal chain.");
                        }
                }

                bool isLastEdge =
                    i == chain.Steps.Count - 1;

                if (!(isLastEdge && isClosed))
                {
                    AddPointIfDifferent(points, b);
                }
            }

            return points;
        }

        private static double NormalizePositiveRadians(double angle)
        {
            double twoPi = 2.0 * Math.PI;
            angle %= twoPi;

            if (angle < 0)
                angle += twoPi;

            return angle;
        }

        private static bool PointsEqual(Pt a, Pt b, double tolerance = 1e-8)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;

            return dx * dx + dy * dy <= tolerance * tolerance;
        }

        private static void AddPointIfDifferent(List<Pt> points, Pt point, double tolerance = 1e-8)
        {
            if (points.Count == 0)
            {
                points.Add(point);
                return;
            }

            if (!PointsEqual(points[^1], point, tolerance))
                points.Add(point);
        }
    }
}
