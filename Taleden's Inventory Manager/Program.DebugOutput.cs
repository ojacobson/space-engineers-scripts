using System;
using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    partial class Program
    {
        readonly string[] VALID_DEBUG_FLAGS = { "quotas", "sorting", "refineries", "assemblers" };

        HashSet<string> DebugFlags = new HashSet<string>();
        List<string> DebugMessages = new List<string>();

        void ResetDebugFlags() =>
            DebugFlags.Clear();

        void EnableDebugFlag(string flag)
        {
            if (!VALID_DEBUG_FLAGS.Contains(flag))
            {
                throw new ArgumentException($"Invalid debug type '{flag}', must be one of: {String.Join(", ", VALID_DEBUG_FLAGS)}");
            }

            DebugFlags.Add(flag);
        }

        bool ShouldDebug(string flag) =>
            DebugFlags.Contains(flag);

        void ClearDebugMessages() =>
            DebugMessages.Clear();

        void Debug(string message) =>
            DebugMessages.Add(message);
    }
}
