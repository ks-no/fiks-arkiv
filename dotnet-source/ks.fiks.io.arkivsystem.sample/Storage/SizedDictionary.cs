using System;
using System.Collections.Generic;

namespace ks.fiks.io.arkivsystem.sample.Storage
{
    public sealed class SizedDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        private int maxSize;
        private Queue<TKey> keys;

        public SizedDictionary(int size)
        {
            maxSize = size;
            keys = new Queue<TKey>();
        }

        public new void Add(TKey key, TValue value)
        {
            if (key == null)
            {
                throw new ArgumentNullException();
            }
            base.Add(key, value);
            keys.Enqueue(key);
            if (keys.Count > maxSize)
            {
                base.Remove(keys.Dequeue());
            }
        }

        public new bool Remove(TKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException();
            }
            if (!keys.Contains(key)) return false;
            var newQueue = new Queue<TKey>();
            while (keys.Count > 0)
            {
                var thisKey = keys.Dequeue();
                if (!thisKey.Equals(key))
                {
                    newQueue.Enqueue(thisKey);
                }
            }

            keys = newQueue;
            return base.Remove(key);
        }
    }
}