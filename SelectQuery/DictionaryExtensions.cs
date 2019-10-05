using System.Collections.Generic;
using System.Linq;

namespace SelectQuery
{
    internal static class DictionaryExtensions
    {
        public static IReadOnlyDictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs)
        {
            return keyValuePairs.ToDictionary(x => x.Key, x => x.Value);
        }
    }
}
