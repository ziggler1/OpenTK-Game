﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public static class LinqEx
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <remarks>Original code found here: http://stackoverflow.com/a/14160879 </remarks>
        public static TValue GetOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
        {
            TValue val;
            dict.TryGetValue(key, out val);
            return val;
        }

        /// <summary>
        /// Finds the index of the first match.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static int IndexOfFirst<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate)
        {
            var enumerator = enumerable.GetEnumerator();
            var index = 0;

            while (enumerator.MoveNext())
            {
                if (predicate(enumerator.Current))
                {
                    return index;
                }
                index++;
            }
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Finds the index of the first match. If no match exists, null is returned.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static int? IndexOfFirstOrNull<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate)
        {
            var enumerator = enumerable.GetEnumerator();
            var index = 0;

            while (enumerator.MoveNext())
            {
                if (predicate(enumerator.Current))
                {
                    return index;
                }
                index++;
            }
            return null;
        }

        /// <summary>
        /// Tests if each item and the next item meet a condition. Always returns true if there is 1 or fewer items.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="pairCondition">The first parameter is this item and the second parameter is the next item.</param>
        /// <returns></returns>
        public static bool PairwiseAll<T>(this IEnumerable<T> enumerable, Func<T, T, bool> pairCondition)
        {
            T previous = enumerable.FirstOrDefault();
            foreach (T item in enumerable.Skip(1))
            {
                if (!pairCondition(previous, item))
                {
                    return false;
                }
                previous = item;
            }
            return true;
        }

        /// <summary>
        /// Returns the first pair of items that meets a given condition.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="pairCondition">The first parameter is this item and the second parameter is the next item.</param>
        /// <returns></returns>
        public static (T first, T second) PairwiseFirst<T>(this IEnumerable<T> enumerable, Func<T, T, bool> pairCondition)
        {
            T previous = enumerable.FirstOrDefault();
            foreach (T item in enumerable.Skip(1))
            {
                if (pairCondition(previous, item))
                {
                    return (previous, item);
                }
                previous = item;
            }
            throw new InvalidOperationException("No pair of items met specified the condition.");
        }

        /// <summary>
        /// Returns the first pair of items that meets a given condition or null if the condition is never met.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="pairCondition">The first parameter is this item and the second parameter is the next item.</param>
        /// <returns></returns>
        public static (T first, T second)? PairwiseFirstOrDefault<T>(this IEnumerable<T> enumerable, Func<T, T, bool> pairCondition)
        {
            T previous = enumerable.FirstOrDefault();
            foreach (T item in enumerable.Skip(1))
            {
                if (pairCondition(previous, item))
                {
                    return (previous, item);
                }
                previous = item;
            }
            return null;
        }

        public static TResult? MinOrNull<T, TResult>(this IEnumerable<T> enumerable, Func<T, TResult> selector) where TResult : struct
        {
            return enumerable.Any() ?
                (TResult?)enumerable.Min(selector) :
                null;
        }
    }
}
