using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.Utilities;

namespace Utility
{
    public class SingletonList<T> : IList<T>
    {
        public T Element { get; }

        public SingletonList(T value)
        {
            Element = value;
        }

        public IEnumerator<T> GetEnumerator()
        {
            yield return Element;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            throw new InvalidOperationException("SingletonList is immutable");
        }

        public void Clear()
        {
            throw new InvalidOperationException("SingletonList is immutable");
        }

        public bool Contains(T item)
        {
            return Equals(item, Element);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            array[arrayIndex] = Element;
        }

        public bool Remove(T item)
        {
            throw new InvalidOperationException("SingletonList is immutable");
        }

        public int Count => 1;
        public bool IsReadOnly => true;
        
        public int IndexOf(T item)
        {
            if (Contains(item))
                return 0;
            return -1;
        }

        public void Insert(int index, T item)
        {
            throw new InvalidOperationException("SingletonList is immutable");
        }

        public void RemoveAt(int index)
        {
            throw new InvalidOperationException("SingletonList is immutable");
        }

        public T this[int index]
        {
            get => Element;
            set => throw new InvalidOperationException("SingletonList is immutable");
        }
    }
}