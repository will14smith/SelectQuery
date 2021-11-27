using System;
using System.Buffers;

namespace SelectQuery.Evaluation.Slots
{
    public static class PooledList
    {
        public static PooledList<T> With<T>(T v1)
        {
            var list = new PooledList<T>(1);
            list.Add(v1);
            return list;
        } 
        public static PooledList<T> With<T>(T v1, T v2)
        {
            var list = new PooledList<T>(2);
            list.Add(v1);
            list.Add(v2);
            return list;
        }
    }
    
    public ref struct PooledList<T>
    {
        private T[] _arr;
        public int Count { get; private set; }

        public PooledList(int capacity)
        {
            _arr = ArrayPool<T>.Shared.Rent(capacity);
            Count = 0;
        }

        public void Add(T item)
        {
            EnsureCapacity(1);
            _arr[Count++] = item;
        }
        
        public void Add(T item1, T item2)
        {
            EnsureCapacity(2);
            _arr[Count++] = item1;
            _arr[Count++] = item2;
        }

        private void EnsureCapacity(int count)
        {
            var newSize = Count + count;

            if (_arr == null)
            {
                _arr = ArrayPool<T>.Shared.Rent(newSize);
                return;
            }
            
            if (newSize < _arr.Length)
            {
                return;
            }
            
            var newArr = ArrayPool<T>.Shared.Rent(newSize);
            var oldArr = _arr;
            Array.Copy(oldArr, 0, newArr, 0, Count);
            
            _arr = newArr;
            ArrayPool<T>.Shared.Return(oldArr);
        }

        public T this[int index] => _arr[index];

        public void Dispose()
        {
            var arr = _arr;
            _arr = null;
            if (arr != null)
            {
                ArrayPool<T>.Shared.Return(arr);
            }
        }
    }
}