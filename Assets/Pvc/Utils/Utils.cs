using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PVC
{
    public static class Utils
    {
        /// <summary>
        /// Enumerates each element's index paired with its value.
        /// </summary>
        public static IEnumerable<(int, T)> Counted<T>(this IEnumerable<T> source)
        {
            int i = 0;
            foreach (T t in source)
                yield return (i++, t);
        }

        /// <summary>
        /// A read-only dictionary that casts the values of another dictionary.
        /// </summary>
        public class CastedReadOnlyDictionary<K, VTrue, VPublic, Dict> : IReadOnlyDictionary<K, VPublic>
            where Dict : class, IReadOnlyDictionary<K, VTrue>
            where VTrue : VPublic
        {
            public readonly Dict Source;
            public CastedReadOnlyDictionary(Dict source) { Source = source; }

            public int Count => Source.Count;
            public VPublic this[K key] => Source[key];

            public IEnumerable<K> Keys => Source.Keys;
            public IEnumerable<VPublic> Values => Source.Values.Cast<VPublic>();

            public bool ContainsKey(K key) => Source.ContainsKey(key);
            public IEnumerator<KeyValuePair<K, VPublic>> GetEnumerator()
            {
                foreach (var kvp in Source)
                    yield return new KeyValuePair<K, VPublic>(kvp.Key, kvp.Value);
            }

            public bool TryGetValue(K key, out VPublic value)
            {
                var result = Source.TryGetValue(key, out var valueTrue);
                value = valueTrue;
                return result;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
