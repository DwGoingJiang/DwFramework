﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DwFramework.Core.Extensions
{
    public static class IEnumerableExtension
    {
        /// <summary>
        /// 遍历
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="action"></param>
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var item in enumerable) action(item);
        }

        /// <summary>
        /// 遍历
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="action"></param>
        public static Task ForEachAsync<T>(this IEnumerable<T> enumerable, Action<T> action) => Task.Run(() => ForEach(enumerable, action));

        /// <summary>
        /// 遍历（并行）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="action"></param>
        public static void ParallelForEach<T>(this IEnumerable<T> enumerable, Action<T> action) => Parallel.ForEach(enumerable, action);

        /// <summary>
        /// 遍历（并行）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static Task ParallelForEachAsync<T>(this IEnumerable<T> enumerable, Action<T> action) => Task.Run(() => Parallel.ForEach(enumerable, action));

        /// <summary>
        /// 按字段去重
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="source"></param>
        /// <param name="keySelector"></param>
        /// <returns></returns>
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            var hash = new HashSet<TKey>();
            return source.Where(p => hash.Add(keySelector(p)));
        }

        /// <summary>
        /// 添加多个元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="values"></param>
        public static void AddRange<T>(this ICollection<T> collection, params T[] values)
        {
            foreach (var item in values) collection.Add(item);
        }

        /// <summary>
        /// 添加符合条件的多个元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="predicate"></param>
        /// <param name="values"></param>
        public static void AddRangeIf<T>(this ICollection<T> collection, Func<T, bool> predicate, params T[] values)
        {
            foreach (var item in values) if (predicate(item)) collection.Add(item);
        }

        /// <summary>
        /// 添加不重复的元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="values"></param>
        public static void AddRangeIfNotContains<T>(this ICollection<T> collection, params T[] values)
        {
            foreach (T item in values) if (!collection.Contains(item)) collection.Add(item);
        }

        /// <summary>
        /// 移除符合条件的元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="predicate"></param>
        public static void RemoveWhere<T>(this ICollection<T> collection, Func<T, bool> predicate)
        {
            foreach (var item in collection.Where(predicate).ToList()) collection.Remove(item);
        }

        /// <summary>
        /// 在元素之后添加元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="predicate">条件</param>
        /// <param name="value">值</param>
        public static void InsertAfter<T>(this IList<T> list, Func<T, bool> predicate, T value)
        {
            foreach (var item in list.Select((item, index) => new { item, index }).Where(p => predicate(p.item)).OrderByDescending(p => p.index))
            {
                if (item.index + 1 == list.Count)
                {
                    list.Add(value);
                }
                else
                {
                    list.Insert(item.index + 1, value);
                }
            }
        }

        /// <summary>
        /// 在元素之后添加元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="index">索引位置</param>
        /// <param name="value">值</param>
        public static void InsertAfter<T>(this IList<T> list, int index, T value)
        {
            foreach (var item in list.Select((v, i) => new { Value = v, Index = i }).Where(p => p.Index == index).OrderByDescending(p => p.Index))
            {
                if (item.Index + 1 == list.Count)
                {
                    list.Add(value);
                }
                else
                {
                    list.Insert(item.Index + 1, value);
                }
            }
        }

        /// <summary>
        /// 转HashSet
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="source"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static HashSet<TResult> ToHashSet<T, TResult>(this IEnumerable<T> source, Func<T, TResult> selector)
        {
            var set = new HashSet<TResult>();
            set.UnionWith(source.Select(selector));
            return set;
        }
    }
}