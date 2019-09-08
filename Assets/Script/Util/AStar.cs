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

    public static Vec2I[] FindPath(Map map, Vec2I start, Vec2I end)
        => FindPath(map, start, end, v => v == end);

    public static Vec2I[] FindPath(Map map, Vec2I start, Vec2I endHint, Func<Vec2I, bool> dstFunc)
    {
        var open = new FastPriorityQueue<Node>(map.w * map.h);
        var opened = new Dictionary<Vec2I, Node>();
        var closed = new Dictionary<Vec2I, Node>();

        var nodeStart = new Node(null, start, 0, 0);
        open.Enqueue(nodeStart, nodeStart.f);
        opened.Add(start, nodeStart);
        while (open.Count > 0)
        {
            Node n = open.Dequeue();
            opened.Remove(n.pos);
            closed.Add(n.pos, n);

            foreach (Vec2I dir in directions)
            {
                Vec2I pos = n.pos + dir;
                if (!map.ValidTile(pos) || !map.Tile(pos).passable || closed.ContainsKey(pos))
                    continue;

                if (dir.x != 0 && dir.y != 0)
                {
                    if (!map.Tile(n.pos + new Vec2I(dir.x, 0)).passable ||
                        !map.Tile(n.pos + new Vec2I(0, dir.y)).passable)
                        continue;
                }

                if (dstFunc(pos))
                {
                    Stack<Vec2I> path = new Stack<Vec2I>();
                    path.Push(pos);
                    while (n != null)
                    {
                        path.Push(n.pos);
                        n = n.parent;
                    }
                    return path.ToArray();
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
                else
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
