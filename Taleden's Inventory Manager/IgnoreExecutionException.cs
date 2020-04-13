using System;

namespace IngameScript
{
    /// <summary>
    /// Thrown when the script should abort all execution.
    /// If caught, then <c>processStep</c> should be reset to 0.
    /// </summary>
    class IgnoreExecutionException : Exception
    {
    }
}
