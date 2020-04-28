using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program
    {
        static bool IsConnected(IMyMechanicalConnectionBlock joint) =>
            joint.IsAttached;
    }
}
