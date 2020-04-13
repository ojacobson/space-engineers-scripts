using System;

namespace IngameScript
{
    /// <summary>
    /// Thrown when we detect that we have taken up too much processing time
    /// and need to put off the rest of the exection until the next call.
    /// </summary>
    class PutOffExecutionException : Exception
    {
    }
}
