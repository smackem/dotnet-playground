using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SingleThreadedServer
{
    public class ResizeArray<T> : IReadOnlyCollection<T>
    {
        T[] _array;

        public int Count { get; private set; }
        public T[] ReadOnlyArray => _array ?? Array.Empty<T>();

        public void Add(T item)
        {
            EnsureCapacity(Count + 1);
            _array[Count] = item;
            Count++;
        }

        public void Clear()
        {
            Count = 0;

            if (_array != null)
                Array.Clear(_array, 0, Count);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ReadOnlyArray.Take(Count).GetEnumerator();
        }

        ///////////////////////////////////////////////////////////////////////

        void EnsureCapacity(int capacity)
        {
            if (_array == null)
            {
                _array = new T[Math.Max(capacity, 1024)];
            }
            else if (_array.Length < capacity)
            {
                var extra = Math.Min(capacity / 2, 16 * 1024);
                var newCapacity = Math.Max(1024, capacity + extra);
                var newArray = new T[newCapacity];
                Array.Copy(_array, newArray, Count);
                _array = newArray;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
