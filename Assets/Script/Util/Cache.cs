using System.Collections.Generic;
using System;

namespace BB {

public class Cache<TKey, TValue>
{
    private readonly Func<TKey, TValue> createFn;
    private readonly Dictionary<TKey, TValue> map
        = new Dictionary<TKey, TValue>();

    public Cache(Func<TKey, TValue> createFn) => this.createFn = createFn;

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

}