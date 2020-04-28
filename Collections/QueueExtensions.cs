using System;
using System.Collections.Generic;
using System.Text;

namespace IngameScript
{
    static class QueueExtensions
    {
        public static bool Empty<T>(this Queue<T> queue) =>
            queue.Count == 0;
    }
}
