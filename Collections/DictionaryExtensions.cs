using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IngameScript
{
    static class DictionaryExtensions
    {
        public static V GetValueOrDefault<K, V>(this Dictionary<K, V> dictionary, K key, Func<V> defaultFactory)
        {
            V value;
            if (!dictionary.TryGetValue(key, out value))
            {
                dictionary.Add(key, value = defaultFactory());
            }
            return value;
        }
    }
}
