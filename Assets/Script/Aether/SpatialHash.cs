using BB;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;
using Vec3 = UnityEngine.Vector3;
using Vec3I = UnityEngine.Vector3Int;

namespace BB {
  class SpatialHash<T> {

    private readonly float grid_size;
    private readonly Dictionary<Vec3I, List<T>> hash = new Dictionary<Vec3I, List<T>>();
    private readonly List<List<T>> list_pool = new List<List<T>>();

    public SpatialHash(float grid_size) {
      this.grid_size = grid_size;
    }

    public void Clear() {
      octree = null;
      foreach (var l in hash.Values) {
        l.Clear();
        list_pool.Add(l);
      }
      hash.Clear();
    }

    private List<T> GetNewBucket() {
      var ct = list_pool.Count;
      if (ct > 0) {
        var i = ct - 1;
        var bucket = list_pool[i];
        list_pool.RemoveAt(i);
        return bucket;
      }

      return new List<T>();
    }

    private List<T> GetBucket(Vec3I pos) {
      List<T> bucket;
      if (!hash.TryGetValue(pos, out bucket)) {
        bucket = GetNewBucket();
        hash.Add(pos, bucket);
      }

      return bucket;
    }

    private Vec3I GridPos(Vec3 pos) {
      return Vec3I.FloorToInt(pos / grid_size);
    }

    public void Add(Vec3 pos, T item) {
      var bucket = GetBucket(GridPos(pos));
      bucket.Add(item);
    }

    private OctreeNode octree;

    public IEnumerable<T> GetNeighbors(Vec3 pos, float range, bool D_use_octree) {
      if (D_use_octree && octree == null)
        octree = ConstructOctree();

      var extent = Vec3.one * range;
      var max = GridPos(pos + extent);
      var min = GridPos(pos - extent);

      for (int x = min.x; x <= max.x; ++x) {
        for (int y = min.y; y <= max.y; ++y) {
          for (int z = min.z; z <= max.z; ++z) {
            var p = new Vec3I(x, y, z);

            if (D_use_octree) {
              var bucket = octree.GetBucket(p);
              if (bucket != null)
                foreach (var t in bucket)
                  yield return t;

            } else {
              List<T> bucket;
              if (hash.TryGetValue(p, out bucket)) {
                foreach (var t in bucket)
                  yield return t;
              }
            }
          }
        }
      }
    }

    public class OctreeNode {
      Vec3I origin;
      int size;

      OctreeNode[,,] children; // x, y, z
      List<T> values;

      public static void D_Assert(bool cond) {
        if (!cond) {
          var x = 4;
        }
        Debug.Assert(cond);
      }

      public OctreeNode(Vec3I origin, List<T> values) {
        this.origin = origin;
        this.size = 1;

        this.values = values;
      }

      public OctreeNode(Vec3I origin, int size) {
        D_Assert(size > 1);
        this.origin = origin;
        this.size = size;

        this.children = new OctreeNode[2,2,2];
      }

      public void D_Print() {
        Debug.Log(D_Dump(0));
      }

      private string D_Dump(int indent) {
        string idt = new String(' ', indent*4);
        string s = $"node {size}: {origin}";
        if (size == 1) {
          s += $"\n{idt}- {values}";
        }
        else {
          for (int x = 0; x < 2; ++x) {
            for (int y = 0; y < 2; ++y) {
              for (int z = 0; z < 2; ++z) {
                var n = children[x, y, z];
                if (n != null) {
                  s += $"\n{idt}  -[{x}, {y}, {z}] {n.D_Dump(indent+1)}";
                }
              }
            }
          }
        }
        return s;
      }

      public bool Contains(Vec3I pos) {
        return
          (pos.x >= origin.x && pos.x < origin.x + size) &&
          (pos.y >= origin.y && pos.y < origin.y + size) &&
          (pos.z >= origin.z && pos.z < origin.z + size);
      }

      private Vec3I GetChildIndex(Vec3I pos) {
        int half_size = size >> 1;
        Vec3I center_ofs = Vec3I.one * half_size;
        Vec3I center = origin + center_ofs;
        int x = (pos.x < center.x) ? 0 : 1;
        int y = (pos.y < center.y) ? 0 : 1;
        int z = (pos.z < center.z) ? 0 : 1;
        return new Vec3I(x, y, z);
      }

      public void Insert(Vec3I pos, List<T> values) {
        var ci = GetChildIndex(pos);

        var child = children[ci.x, ci.y, ci.z];
        if (size == 2) {
          D_Assert(child == null);
          children[ci.x, ci.y, ci.z] = new OctreeNode(pos, values);
        } else {
          if (child == null) {
            var half_size = size >> 1;
            var child_origin = origin + ci * half_size;
            child = new OctreeNode(child_origin, half_size);
            children[ci.x, ci.y, ci.z] = child;
          }
          child.Insert(pos, values);
        }
      }

      // private void AddChild(OctreeNode node) {
      //   D_Assert(size > 1);
      //   D_Assert(node.size < size);

      //   var ci = GetChildIndex(node.origin);
      //   var half_size = size >> 1;
      //   if (half_size == node.size) {
      //     D_Assert(children[ci.x, ci.y, ci.z] == null);
      //     children[ci.x, ci.y, ci.z] = node;
      //   } else {
      //     var child = children[ci.x, ci.y, ci.z];
      //     if (child == null) {
      //       Vec3I child_center = node.origin + ci * half_size;
      //       child = new OctreeNode(child_center, half_size);
      //       children[ci.x, ci.y, ci.z] = child;
      //       child.AddChild(node);
      //     }
      //   }
      // }

      // public OctreeNode InsertNode(OctreeNode node) {
      //   if (Contains(node.origin)) {
      //     AddChild(node);
      //     return this;
      //   }

      //   // grow root
      //   var p_size = size * 2;
      //   var p_origin = Vec3I.FloorToInt((Vec3)(origin) / p_size) * p_size;
      //   if ()

      //   var parent = new OctreeNode(origin, size * 2);
      //   parent.AddChild(node);
      //   return parent;
      // }


      // Testing...
      public List<T> GetBucket(Vec3I v) {
        if (size == 1)
          return values;
        else if (!Contains(v))
          return null;

        var ci = GetChildIndex(v);
        var child = children[ci.x, ci.y, ci.z];
        if (child == null)
          return null;
        return child.GetBucket(v);
      }

      public static OctreeNode CreateFromBounds(Vec3I min, Vec3I max) {
        max = max + Vec3I.one;
        Vec3I range = max - min;
        var min_size = Math.Max(Math.Max(range.x, Math.Max(range.y, range.z)), 2);

        var size = 1;
        while (size < min_size)
          size *= 2;

        return new OctreeNode(min, size);
      }
    }

    public OctreeNode ConstructOctree() {
      if (hash.Count == 0)
        return new OctreeNode(Vec3I.zero, new List<T>());

      Vec3I min = Vec3I.zero, max = Vec3I.zero;
      foreach (var kvp in hash) {
        min = max = kvp.Key;
        break;
      }

      foreach (var kvp in hash) {
        min = Vec3I.Min(min, kvp.Key);
        max = Vec3I.Max(max, kvp.Key);
      }

      var root = OctreeNode.CreateFromBounds(min, max);
      foreach (var kvp in hash)
        root.Insert(kvp.Key, kvp.Value);

      return root;
    }

    public void SerializeToOctree() {

    }

  }
}
