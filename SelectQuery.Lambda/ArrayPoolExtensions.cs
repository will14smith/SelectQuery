using System;
using System.Buffers;

namespace SelectQuery.Lambda
{
    public static class ArrayPoolExtensions
    {
        public static void Resize<T>(this ArrayPool<T> pool, ref T[] array, int newSize)
        {
            var newArray = pool.Rent(newSize);

            var length = newSize < array.Length ? newSize : array.Length;
            Array.Copy(array, 0, newArray, 0, length);

            pool.Return(array);
            array = newArray;
        }
    }
}
