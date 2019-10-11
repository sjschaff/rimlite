using System.Collections.Generic;
using System;

namespace BB
{
    class DeferredSet<T>
    {
        private readonly Action<T> removeFn;
        private readonly HashSet<T> ts
            = new HashSet<T>();
        private readonly List<T> tsToAdd
            = new List<T>();
        private readonly List<T> TsToRemove
            = new List<T>();
        private bool iterating = false;

        public DeferredSet(Action<T> removeFn = null)
            => this.removeFn = removeFn;

        public void Add(T t)
        {
            if (iterating)
                tsToAdd.Add(t);
            else
                ts.Add(t);
        }

        public void Remove(T t)
        {
            if (iterating)
                TsToRemove.Add(t);
            else
            {
                ts.Remove(t);
                removeFn?.Invoke(t);
            }
        }

        public bool ForEach(Action<T> fn)
        {
            iterating = true;
            foreach (var t in ts)
                fn(t);

            bool didChange = (tsToAdd.Count + TsToRemove.Count) > 0;
            foreach (var t in tsToAdd)
                ts.Add(t);
            foreach (var t in TsToRemove)
            {
                ts.Remove(t);
                removeFn?.Invoke(t);
            }

            tsToAdd.Clear();
            TsToRemove.Clear();
            iterating = false;

            return didChange;
        }
    }
}
