using System;
using System.Collections;
using System.Collections.Generic;

namespace SpeedCalc.Core.Runtime
{
    public sealed class RuntimeArray<T> : IEnumerable<T>
    {
        const int DefaultArrayCapacity = 8;

        T[] data;

        public int Count { get; private set; }

        public int Capacity { get; private set; }

        public T this[int index]
        {
            get { return data[index]; }
            set { data[index] = value; }
        }

        void Grow()
        {
            var newCapacity = Capacity * 2;

            Array.Resize(ref data, newCapacity);
            Capacity = newCapacity;
        }

        public void Write(T value)
        {
            if (Capacity < Count + 1)
                Grow();
            this[Count++] = value;
        }
        
        public IEnumerator<T> GetEnumerator() => new ArraySegment<T>(data, 0, Count).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => new ArraySegment<T>(data, 0, Count).GetEnumerator();

        public RuntimeArray()
        {
            Count = 0;
            Capacity = DefaultArrayCapacity;
            data = new T[Capacity];
        }
    }
}
