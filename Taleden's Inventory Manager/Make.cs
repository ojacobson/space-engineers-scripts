using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    static partial class Make
    {
        public static Func<Ship> Ship(IMyCubeGrid grid) =>
            () => new Ship(grid);
    }
}
