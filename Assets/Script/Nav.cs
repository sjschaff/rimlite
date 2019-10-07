using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public struct PathCfg
    {
        public readonly Func<Vec2I, bool> destinationFn;
        public readonly Func<Vec2I, float> hueristicFn;

        public PathCfg(Func<Vec2I, bool> destinationFn, Func<Vec2I, float> hueristicFn)
        {
            this.destinationFn = destinationFn;
            this.hueristicFn = hueristicFn;
        }

        public static PathCfg Point(Vec2I pos)
            => new PathCfg(pt => pt == pos, pt => Vec2.Distance(pt, pos));

        // TODO: this hueristic is probably wrong
        public static PathCfg Vacate(Vec2I pos)
            => new PathCfg(pt => pt != pos, pt => Vec2.Distance(pt, pos));

        // TODO: this hueristic is probably wrong
        public static PathCfg Vacate(RectInt area)
            => new PathCfg(pt => !area.Contains(pt), pt => Vec2.Distance(pt, area.center));

        // TODO: this hueristic is probably wrong
        public static PathCfg Adjacent(Vec2I pos)
            => new PathCfg(pt => pt.Adjacent(pos), pt => Vec2.Distance(pt, pos));

        // TODO: this hueristic is probably wrong
        public static PathCfg Adjacent(RectInt area)
            => new PathCfg(pt => area.IsAdjacent(pt), pt => Vec2.Distance(pt, area.center));

        // TODO: this hueristic is probably wrong
        public static PathCfg Area(RectInt area)
            => new PathCfg(pt => area.Contains(pt), pt => Vec2.Distance(pt, area.center));
    }

    public class Nav
    {
        private readonly AStar.ISearchCache searchCache;
        private readonly Func<Vec2I, bool> passFn;

        public Nav(Map map)
        {
            searchCache = AStar.CreateSearchCache(map.size);
            passFn = pt => map.GetTile(pt).passable;
        }

        public Vec2I[] GetPath(Vec2I start, PathCfg cfg)
            => AStar.FindPath(searchCache, start, passFn, cfg.destinationFn, cfg.hueristicFn)?.pts;
    }

    // Initial half implementation of hierarchical A* 
    public class Nav__Unfinished
    {
        private const int aDim = 8;
        private const int maxSeq = 4;

        //private
        public class Edge
        {
            public static int Down = 0;
            public static int Left = 1;
            public static int Up = 2;
            public static int Right = 3;

            public readonly Vec2I[] pts;
            public readonly AStar.Path[] pathsToEdge = new AStar.Path[4];

            public Edge(Vec2I[] pts) => this.pts = pts;
        }

        //private
        public class ATile
        {
            private struct Path
            {
                //public readonly Vec2I[] pts;
            }

            public readonly Func<Vec2I, bool> passFn;

            private readonly List<Path> paths;
            // todo: cache for easy access for a given side
            public Edge[] edges = new Edge[4]; // down -> cw

            public ATile(Func<Vec2I, bool> passFn, int xa, int ya)
            {
                int x = xa * aDim;
                int y = ya * aDim;
                this.passFn = pt => passFn(new Vec2I(x + pt.x, y + pt.y));
            }
        }

        //private readonly Vec2I size;
        private readonly Func<Vec2I, bool> passFn;

        private readonly Vec2I asize;
        //private
        public
        ATile[,] atiles;

        private readonly AStar.ISearchCache cacheInternal;
        //private readonly AStar.ISearchCache cacheAbstract;

        /* Debug Vis Stuff

        void KD_AddCircle(Vec2I pt, bool big)
        {
            GameObject child = new GameObject();
            child.transform.SetParent(transform, false);

            var color = Color.red * (big ? .8f : .4f);
            float rad = big ? .5f : .4f;

            List<Vec2> pts = new List<Vec2>();
            for (int i = 0; i < 32; ++i)
            {
                float theta = i * 2 * Mathf.PI / 32f;
                float x = Mathf.Cos(theta) * rad;
                float y = Mathf.Sin(theta) * rad;
                pts.Add(new Vec2(x + pt.x + .5f, y + pt.y + .5f));
            }


            child.AddLineRenderer("Highlight", 2001, color, 1 / 32f, true, true, pts.ToArray());
        }

        void KD_AddPath(IEnumerable<Vec2I> path)
        {
            GameObject child = new GameObject();
            child.transform.SetParent(transform, false);
            child.AddLineRenderer("Highlight", 2000, Color.black, 1 / 32f, false, true,
                path.Select(pt => pt + new Vec2(.5f, .5f)).ToArray());
        }

        void KD_AddSquare(Vec2I o)
        {
            GameObject child = new GameObject();
            child.transform.SetParent(transform, false);
            child.AddLineRenderer("Highlight", 1999, Color.white, 1 / 32f, true, true,
                new Vec2[] {o, new Vec2(o.x + 8, o.y), new Vec2(o.x+8, o.y+8), new Vec2(o.x, o.y+8)} );
        }

                // K_deubg vis
                for (int xa = 0; xa < 8; ++xa)
                {
                    for (int ya = 0; ya < 8; ++ya)
                    {
                        int x = 8 * xa;
                        int y = 8 * ya;
                        Vec2I o = new Vec2I(x, y);
                        KD_AddSquare(o);
                        var tile = nav.atiles[xa, ya];
                        for (int ea = 0; ea < 4; ++ea)
                        {
                            var edgeA = tile.edges[ea];
                            if (edgeA == null) continue;

                            for (int i = 0; i < edgeA.pts.Length; ++i)
                                KD_AddCircle(o + edgeA.pts[i], ea % 2 == 0);

                            for (int eb = ea + 1; eb < 4; ++eb)
                            {
                                var path = edgeA.pathsToEdge[eb];
                                if (path != null)
                                    KD_AddPath(path.pts.Select(pt => pt + o));
                            }
                        }
                    }
                }*/


        public Nav__Unfinished(Vec2I size, Func<Vec2I, bool> passFn)
        {
            BB.Assert(size.x % aDim == 0);
            BB.Assert(size.y % aDim == 0);

            //this.size = size;
            this.passFn = passFn;

            this.asize = new Vec2I(size.x / aDim, size.y / aDim);
            this.atiles = new ATile[asize.x, asize.y];
            for (int x = 0; x < asize.x; ++x)
                for (int y = 0; y < asize.y; ++y)
                    atiles[x, y] = new ATile(passFn, x, y);

            this.cacheInternal = AStar.CreateSearchCache(new Vec2I(aDim, aDim));
            //this.cacheAbstract = AStar.CreateSearchCache(asize);

            ComputeAbstract(); // ???
        }

        private void ComputeAbstract()
        {
            ComputeEdgeData();

            for (int x = 0; x < asize.x; ++x)
                for (int y = 0; y < asize.y; ++y)
                    ComputeInterPaths(x, y);
        }

        private void ComputeInterPaths(int x, int y)
        {
            ATile tile = atiles[x, y];
            for (int ea = 0; ea < 3; ++ea)
            {
                Edge edgeA = tile.edges[ea];
                if (edgeA == null)
                    continue;

                for (int eb = ea + 1; eb < 4; ++eb)
                {
                    Edge edgeB = tile.edges[eb];
                    if (edgeB == null)
                        continue;

                    AStar.Path shortestPath = null;
                    for (int a = 0; a < edgeA.pts.Length; ++a)
                    {
                        Vec2I ptA = edgeA.pts[a];
                        for (int b = 0; b < edgeB.pts.Length; ++b)
                        {
                            Vec2I ptB = edgeB.pts[b];
                            var path = AStar.FindPath(
                                cacheInternal, ptA, tile.passFn, pt => pt == ptB, pt => Vec2I.Distance(ptB, pt));
                            if (shortestPath == null || path.g < shortestPath.g)
                                shortestPath = path;
                        }
                    }

                    tile.edges[ea].pathsToEdge[eb] = shortestPath;
                    tile.edges[eb].pathsToEdge[ea] = shortestPath.Reversed();
                }
            }
        }

        bool KD_log = false;

        private void ComputeEdgeData()
        {
            for (int x = 0; x < asize.x - 1; ++x)
            {
                for (int y = 0; y < asize.y - 1; ++y)
                {
                    var edgeVert = ComputeEdgeVert(x, y);
                    atiles[x, y].edges[Edge.Right] = edgeVert;
                    atiles[x + 1, y].edges[Edge.Left] = edgeVert == null ? null :
                        new Edge(edgeVert.pts.Select(pt => new Vec2I(0, pt.y)).ToArray());

                    if (y == 0 && x == 2)
                        KD_log = true;
                    else
                        KD_log = false;

                    var edgeHori = ComputeEdgeHori(x, y);
                    atiles[x, y].edges[Edge.Up] = edgeHori;
                    atiles[x, y + 1].edges[Edge.Down] = edgeHori == null ? null :
                        new Edge(edgeHori.pts.Select(pt => new Vec2I(pt.x, 0)).ToArray());
                }
            }
        }

        private Edge ComputeEdgeHori(int xa, int ya)
        {
            int y = ya * aDim + (aDim - 1);
            int x = xa * aDim;

            var pts = GenEdgePoints(i => passFn(new Vec2I(x + i, y)) && passFn(new Vec2I(x + i, y + 1)));
            if (KD_log)
            {
                string log = "pts = ";
                foreach (int i in pts)
                    log += i.ToString() + ", ";
                BB.LogInfo(log);
            }
            return pts == null ? null : new Edge(pts.Select(i => new Vec2I(i, aDim - 1)).ToArray());
        }

        private Edge ComputeEdgeVert(int xa, int ya)
        {
            int y = ya * aDim;
            int x = xa * aDim + (aDim - 1);

            var pts = GenEdgePoints(i => passFn(new Vec2I(x, y + i)) && passFn(new Vec2I(x + 1, y + i)));
            return pts == null ? null : new Edge(pts.Select(i => new Vec2I(aDim - 1, i)).ToArray());
        }

        private List<int> GenEdgePoints(Func<int, bool> PassFn)
        {
            List<int> pts = new List<int>();
            bool inSequence = false;
            int seqStart = -1;

            for (int i = 0; i <= aDim; ++i)
            {
                if (i != aDim && PassFn(i))
                {
                    if (!inSequence)
                    {
                        inSequence = true;
                        seqStart = i;
                    }
                }
                else
                {
                    if (inSequence)
                    {
                        inSequence = false;
                        AddPointsForSeq(seqStart, i - seqStart, pts);
                    }
                }
            }

            return pts.Any() ? pts : null;
        }

        private void AddPointsForSeq(int start, int len, List<int> pts)
        {
            if (KD_log) BB.LogInfo("found sequence: " + start + ", " + len);
            if (len > maxSeq)
            {
                if (KD_log) BB.LogInfo("splitting sequence");
                int lenSplit = len / 2;
                AddPointsForSeq(start, lenSplit, pts);
                AddPointsForSeq(start + lenSplit, len - lenSplit, pts);
            }
            else if (len == maxSeq)
            {
                if (KD_log) BB.LogInfo("max sequence, adding 2");
                pts.Add(start);
                pts.Add(start + len - 1);
            }
            else
            {
                int mid = (len - 1) / 2;
                pts.Add(start + mid);
                if (KD_log) BB.LogInfo("mid = " + mid + " => " + (start + mid));
            }

            // 4, 4     0 - - 0|0 - - 0
            // 3, 4,    - 0 -|0 - - 0
            // 3, 3,    - 0 -|- 0 -
            // 2, 3,    0 -|- 0 -
            // 4        0 - - 0
            // 3        - 0 -
            // 2        0 -
            // 1        0
        }
    }
}