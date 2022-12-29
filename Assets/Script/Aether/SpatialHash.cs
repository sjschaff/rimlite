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

    public IEnumerable<T> GetNeighbors(Vec3 pos, float range) {
      var extent = Vec3.one * range;
      var max = GridPos(pos + extent);
      var min = GridPos(pos - extent);

      for (int x = min.x; x <= max.x; ++x) {
        for (int y = min.y; y <= max.y; ++y) {
          for (int z = min.z; z <= max.z; ++z) {
            List<T> bucket;
            if (hash.TryGetValue(new Vec3I(x, y, z), out bucket)) {
              foreach (var t in bucket)
                yield return t;
            }
          }
        }
      }
    }

  }
}
