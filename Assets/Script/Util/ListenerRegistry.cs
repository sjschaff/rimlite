using System.Collections.Generic;
using System;

namespace BB
{
    class ListenerRegistry<TListener>
    {
        private readonly HashSet<TListener> listeners
            = new HashSet<TListener>();
        private readonly List<TListener> listenersToAdd
            = new List<TListener>();
        private readonly List<TListener> listenersToRemove
            = new List<TListener>();
        private bool iterating = false;

        public ListenerRegistry() { }

        public void Register(TListener listener)
        {
            if (iterating)
                listenersToAdd.Add(listener);
            else
                listeners.Add(listener);
        }

        public void Unregister(TListener listener)
        {
            if (iterating)
                listenersToRemove.Add(listener);
            else
                listeners.Remove(listener);
        }

        public void MessageAll(Action<TListener> fn)
        {
            iterating = true;
            foreach (var listener in listeners)
                fn(listener);

            foreach (var listener in listenersToAdd)
                listeners.Add(listener);
            foreach (var listener in listenersToRemove)
                listeners.Remove(listener);

            listenersToAdd.Clear();
            listenersToRemove.Clear();
            iterating = false;
        }
    }
}
