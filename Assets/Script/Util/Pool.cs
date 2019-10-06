using System;
using System.Collections.Generic;

namespace BB
{
    class Pool<T>
    {
        private readonly Func<T> allocator;
        private readonly Queue<T> pool
            = new Queue<T>();

        public Pool(Func<T> allocator)
            => this.allocator = allocator;

        public T Get()
        {
            if (pool.Count > 0)
                return pool.Dequeue();

            return allocator();
        }

        public void Return(T t) => pool.Enqueue(t);
    }
}
