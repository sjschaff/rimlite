using System.Collections.Generic;
using System;

namespace BB
{

    public class CacheNonNullable<TKey, TValue>
    {
        private readonly Func<TKey, TValue> createFn;
        private readonly Dictionary<TKey, TValue> map
            = new Dictionary<TKey, TValue>();

        public CacheNonNullable(Func<TKey, TValue> createFn) => this.createFn = createFn;

        public TValue Get(TKey key)
        {
            if (!map.TryGetValue(key, out var val))
            {
                val = createFn(key);
                map.Add(key, val);
            }

            return val;
        }
    }

    public class Cache<TKey, TValue> where TValue : class
    {
        private readonly CacheNonNullable<TKey, TValue> cache;

        public Cache(Func<TKey, TValue> createFn)
            => this.cache = new CacheNonNullable<TKey, TValue>(createFn);

        public TValue Get(TKey key)
            => key == null ? null : cache.Get(key);
    }

}