using System;
using System.Collections.Generic;
using System.Text;

namespace IngameScript
{
    static class HashSetExtensions
    {
        public static bool Empty<T>(this HashSet<T> set) =>
            set.Count == 0;
    }
}
