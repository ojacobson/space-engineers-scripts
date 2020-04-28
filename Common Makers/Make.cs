using System.Collections.Generic;

namespace IngameScript
{
    static partial class Make
    {
        public static Dictionary<K, V> Dictionary<K, V>() =>
            new Dictionary<K, V>();

        public static List<T> List<T>() =>
            new List<T>();

        public static HashSet<T> HashSet<T>() =>
            new HashSet<T>();
    }
}
