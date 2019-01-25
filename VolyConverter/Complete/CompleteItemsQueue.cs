using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace VolyConverter.Complete
{
    /// <summary>
    /// Stores a limited collection of completed and errored  items.
    /// </summary>
    public class CompleteItems<T> : ICompleteItems<T>
    {
        private readonly int itemLimit;
        readonly ConcurrentQueue<T> complete = new ConcurrentQueue<T>();

        public CompleteItems(int itemLimit = 100)
        {
            if (itemLimit < 1)
            {
                throw new ArgumentOutOfRangeException("itemLimit", "Item limit must be greater than zero.");
            }

            this.itemLimit = itemLimit;
        }

        public void Add(T item)
        {
            lock (complete)
            {
                complete.Enqueue(item);
                while (complete.Count > itemLimit)
                {
                    complete.TryDequeue(out T outObj);
                }
            }
        }

        public int Count => complete.Count;

        public IEnumerator<T> GetEnumerator()
        {
            return complete.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return complete.GetEnumerator();
        }
    }
}
