using UnityEngine;
using System.Collections.Generic;
using Priority_Queue;

using Vec2I = UnityEngine.Vector2Int;
using System;

public class AStar 
{
    private class Node : FastPriorityQueueNode
    {
        public Node parent;
        public readonly Vec2I pos;
        public float g;
        public readonly float h;

        public Node(Node parent, Vec2I pos, float g, float h)
        {
            this.parent = parent;
            this.pos = pos;
            this.g = g;
            this.h = h;
        }

        public float f => g + h;
    }

    public interface IQueueCache { }

    private class QueueCache : IQueueCache
    {
        public readonly FastPriorityQueue<Node> queue;
        public QueueCache(int size) =>
            this.queue = new FastPriorityQueue<Node>(size);
    }

    public static IQueueCache CreateQueueCache(int w, int h) => new QueueCache(w* h);
    public static IQueueCache K_queue = new QueueCache(64 * 64);

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
        IQueueCache queueCache,
        Vec2I size, Func<Vec2I, bool> passFn,
        Vec2I start, Vec2I endHint, Func<Vec2I, bool> dstFunc)
    {
        var open = (queueCache as QueueCache).queue;
        open.Clear();

        var opened = new Dictionary<Vec2I, Node>();
        var closed = new Dictionary<Vec2I, Node>();

        var nodeStart = new Node(null, start, 0, 0);
        open.Enqueue(nodeStart, nodeStart.f);
        opened.Add(start, nodeStart);
        while (open.Count > 0)
        {
            Node n = open.Dequeue();
            if (dstFunc(n.pos))
                return ConstructPath(n);

            opened.Remove(n.pos);
            closed.Add(n.pos, n);

            foreach (Vec2I dir in directions)
            {
                Vec2I pos = n.pos + dir;
                if (!BB.InGrid(size, pos) || !passFn(pos) || closed.ContainsKey(pos))
                    continue;

                if (dir.x != 0 && dir.y != 0)
                {
                    if (!passFn(n.pos + new Vec2I(dir.x, 0)) ||
                        !passFn(n.pos + new Vec2I(0, dir.y)))
                        continue;
                }

                float g = n.g + Vec2I.Distance(pos, n.pos);

                if (opened.TryGetValue(pos, out var nodeOpen))
                {
                    if (g < nodeOpen.g)
                    {
                        nodeOpen.parent = n;
                        nodeOpen.g = g;
                        open.UpdatePriority(nodeOpen, nodeOpen.f);
                    }
                }
                else if (!closed.ContainsKey(pos))
                {
                    Node nodeNext = new Node(n, pos, g, Vec2I.Distance(start, endHint));
                    open.Enqueue(nodeNext, nodeNext.f);
                    opened.Add(nodeNext.pos, nodeNext);
                }
            }
        }

        return null;
    }
}
