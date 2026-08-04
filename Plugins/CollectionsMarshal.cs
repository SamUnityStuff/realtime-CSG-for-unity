// SAM: TO FIX "UNSAFE DOES NOT EXIST" ISSUE AFTER IMPORTING ENTITIES PACKAGE,
//      REMOVED LOTS OF STUFF AND SWITCHED SYSTEM.RUNTIME.COMPILERSERVICES.UNSAFE TO UNITY.COLLECTIONS.LOWLEVEL.UNSAFE.UNSAFEUTILITY

#if NET5_0_OR_GREATER
// The CollectionsMarshal type is internal in .NET 5.0 and later, so it's not necessary to define it.
//#elif NETSTANDARD2_1_OR_GREATER
#else
// The Span<T> and ReadOnlySpan<T> types are internal in .NET Standard 2.1 and later.
#pragma warning disable CS8632, CS8500

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace System.Runtime.InteropServices
{
    internal record ListDataHelper<T>
    {
        public T[] _items;
        public int _size;
        public int _version;
    }


#region CollectionsMarshal
    /// <summary>
    /// An unsafe class that provides a set of methods to access the underlying data representations of collections.
    /// </summary>
    public static class CollectionsMarshal
    {
        /// <summary>
        /// Get a <see cref="Span{T}"/> view over a <see cref="List{T}"/>'s data.
        /// Items should not be added or removed from the <see cref="List{T}"/> while the <see cref="Span{T}"/> is in use.
        /// </summary>
        /// <param name="list">The list to get the data view over.</param>
        /// <typeparam name="T">The type of the elements in the list.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpan<T>(List<T>? list)
        {
            Span<T> span = default;
            if (list is not null)
            {
                // int size = list._size;
                // T[] items = list._items;
                var listData = UnsafeUtility.As<List<T>, ListDataHelper<T>>(ref list);
                int size = listData._size;
                T[] items = listData._items;
                Debug.Assert(items is not null, "Implementation depends on List<T> always having an array.");

                if ((uint)size > (uint)items.Length)
                {
                    // List<T> was erroneously mutated concurrently with this call, leading to a count larger than its array.
                    throw new InvalidOperationException("Concurrent operations are not supported.");
                }

                Debug.Assert(typeof(T[]) == items.GetType(), "Implementation depends on List<T> always using a T[] and not U[] where U : T.");
                span = new Span<T>(items, 0, size);
            }

            return span;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> AsSpanUnchecked<T>(this List<T>? list) {
            var listData = UnsafeUtility.As<List<T>, ListDataHelper<T>>(ref list);
            int size = listData._size;
            T[] items = listData._items;
            return new Span<T>(items, 0, size);
        }

        /// <summary>
        /// Sets the count of the <see cref="List{T}"/> to the specified value.
        /// </summary>
        /// <param name="list">The list to set the count of.</param>
        /// <param name="count">The value to set the list's count to.</param>
        /// <typeparam name="T">The type of the elements in the list.</typeparam>
        /// <exception cref="NullReferenceException">
        /// <paramref name="list"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="count"/> is negative.
        /// </exception>
        /// <remarks>
        /// When increasing the count, uninitialized data is being exposed.
        /// </remarks>
        public static void SetCount<T>(List<T> list, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Non-negative number required.");
            }

            // list._version++;
            ref var listData = ref UnsafeUtility.As<List<T>, ListDataHelper<T>>(ref list);
            ref int version = ref listData._version;
            version++;

            ref T[] items = ref listData._items;
            ref int size = ref listData._size;

            if (count > list.Capacity)
            {
                list.Grow(count);
            }
            else if (count < /* list._size */ size && RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(/* list._items */ items, count, /* list._size */ size - count);
            }

            // list._size = count;
            size = count;
        }
    }
#endregion

#region ListExtensions
    internal static class ListExtensions
    {
        /// <summary>
        /// Increase the capacity of this list to at least the specified <paramref name="capacity"/>.
        /// </summary>
        /// <param name="capacity">The minimum capacity to ensure.</param>
        internal static void Grow<T>(this List<T> list, int capacity)
        {
            list.Capacity = list.GetNewCapacity(capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetNewCapacity<T>(this List<T> list, int capacity)
        {
            const int DefaultCapacity = 4;
            var listData = UnsafeUtility.As<List<T>, ListDataHelper<T>>(ref list);
            T[] _items = listData._items;
            Debug.Assert(_items.Length < capacity);

            int newCapacity = _items.Length == 0 ? DefaultCapacity : 2 * _items.Length;

            // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
            // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
            if ((uint)newCapacity > /* Array.MaxLength */ 0X7FFFFFC7) newCapacity = /* Array.MaxLength */ 0X7FFFFFC7;

            // If the computed capacity is still less than specified, set to the original argument.
            // Capacities exceeding Array.MaxLength will be surfaced as OutOfMemoryException by Array.Resize.
            if (newCapacity < capacity) newCapacity = capacity;

            return newCapacity;
        }
    }
#endregion

}

#endif