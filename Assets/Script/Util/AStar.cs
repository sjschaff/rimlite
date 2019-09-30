using System.Collections.Generic;
using System;
using Priority_Queue;

using Vec2I = UnityEngine.Vector2Int;

namespace BB
{

    public class AStar
    {
        private class Node : FastPriorityQueueNode
        {
            public Node parent;
            public Vec2I pos;
            public float g;
            public float h;

            public void Init(Node parent, Vec2I pos, float g, float h)
            {
                this.parent = parent;
                this.pos = pos;
                this.g = g;
                this.h = h;
            }

            public float f => g + h;
        }

        public interface ISearchCache { }

        private class SearchCache : ISearchCache
        {
            public readonly Vec2I size;
            private readonly FastPriorityQueue<Node> open;
            private readonly Node[,] opened;
            private readonly bool[,] closed;

            private readonly List<Node> nodePool;
            private int numAllocs;

            public SearchCache(Vec2I size)
            {
                this.size = size;
                int maxNodes = size.x * size.y;
                open = new FastPriorityQueue<Node>(maxNodes);
                opened = new Node[size.x, size.y];
                closed = new bool[size.x, size.y];

                nodePool = new List<Node>(maxNodes);
                numAllocs = 0;
                for (int i = 0; i < maxNodes; ++i)
                    nodePool.Add(new Node());
            }

            public void Reset()
            {
                numAllocs = 0;
                open.Clear();
                Array.Clear(opened, 0, opened.Length);
                Array.Clear(closed, 0, closed.Length);
            }

            public void Open(Node parent, Vec2I pos, float g, float h)
            {
                Node node = nodePool[numAllocs];
                open.ResetNode(node);
                ++numAllocs;

                node.Init(parent, pos, g, h);
                open.Enqueue(node, node.f);
                opened[node.pos.x, node.pos.y] = node;
            }

            public bool hasOpen => open.Count > 0;

            public Node NextOpen()
            {
                Node node = open.Dequeue();
                opened[node.pos.x, node.pos.y] = null;
                closed[node.pos.x, node.pos.y] = true;

                return node;
            }

            public bool Closed(Vec2I pos) => closed[pos.x, pos.y];

            public Node GetOpenNode(Vec2I pos) => opened[pos.x, pos.y];

            public void UpdateOpenNode(Node node) => open.UpdatePriority(node, node.f);
        }

        public static ISearchCache CreateSearchCache(Vec2I size) => new SearchCache(size);

        private static readonly Vec2I[] directions =
        {
        new Vec2I(-1, -1),
        new Vec2I(-1, 0),
        new Vec2I(-1, 1),
        new Vec2I(0, 1),
        new Vec2I(1, 1),
        new Vec2I(1, 0),
        new Vec2I(1, -1),
        new Vec2I(0, -1)
    };

        public class Path
        {
            public readonly float g;
            public readonly Vec2I[] pts;

            public Path(float g, Vec2I[] pts)
            {
                this.g = g;
                this.pts = pts;
            }

            public Path Reversed()
            {
                Vec2I[] ptsReversed = new Vec2I[pts.Length];
                for (int i = 0; i < pts.Length; ++i)
                    ptsReversed[pts.Length - 1 - i] = pts[i];
                return new Path(g, ptsReversed);
            }
        }

        private static Path ConstructPath(Node n)
        {
            float g = n.g;

            Stack<Vec2I> path = new Stack<Vec2I>();
            while (n != null)
            {
                path.Push(n.pos);
                n = n.parent;
            }

            return new Path(g, path.ToArray());
        }

        public static Path FindPath(
            ISearchCache searchCache, Func<Vec2I, bool> passFn,
            Vec2I start, Vec2I endHint, Func<Vec2I, bool> dstFn)
        {
            var cache = (searchCache as SearchCache);
            cache.Reset();

            cache.Open(null, start, 0, 0);
            while (cache.hasOpen)
            {
                Node n = cache.NextOpen();
                if (dstFn(n.pos))
                    return ConstructPath(n);

                foreach (Vec2I dir in directions)
                {
                    Vec2I pos = n.pos + dir;
                    if (!MathExt.InGrid(cache.size, pos) || cache.Closed(pos) || !passFn(pos))
                        continue;

                    if (dir.x != 0 && dir.y != 0)
                    {
                        if (!passFn(n.pos + new Vec2I(dir.x, 0)) ||
                            !passFn(n.pos + new Vec2I(0, dir.y)))
                            continue;
                    }

                    float g = n.g + Vec2I.Distance(pos, n.pos);

                    Node openNode = cache.GetOpenNode(pos);
                    if (openNode != null)
                    {
                        if (g < openNode.g)
                        {
                            openNode.parent = n;
                            openNode.g = g;
                            cache.UpdateOpenNode(openNode);
                        }
                    }
                    else
                    {
                        cache.Open(n, pos, g, Vec2I.Distance(start, endHint));
                    }
                }
            }

            return null;
        }
    }

}