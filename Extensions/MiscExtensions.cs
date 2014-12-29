using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace uNet2.Extensions
{
    internal static class MiscExtensions
    {
        private unsafe delegate void memcpyimpl(byte* src, byte* dest, int len);
        private static readonly memcpyimpl _memcpyimpl = (memcpyimpl)Delegate.CreateDelegate(
                typeof(memcpyimpl), typeof(Buffer).GetMethod("memcpyimpl",
                    BindingFlags.Static | BindingFlags.NonPublic));

        /// <summary>
        /// Slice an array to get a specific part of it
        /// </summary>
        /// <typeparam name="T">Array type</typeparam>
        /// <param name="arr">The array</param>
        /// <param name="idx">Index to initiate slicing from</param>
        /// <param name="count">Amount of elements to slice</param>
        /// <returns>Returns the sliced part of the array</returns>
        public static T[] Slice<T>(this T[] arr, int idx, int count)
        {
            return _Slice(arr, idx, count).ToArray();
        }

        private static IEnumerable<T> _Slice<T>(IList<T> arr, int idx, int count)
        {
            for (var i = idx; i < idx + count; i++)
                yield return arr[i];
        }

        public static T[] CreateBuffer<T>(this T item)
        {
            return new [] {item};
        }
    }
}
