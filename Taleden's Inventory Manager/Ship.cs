using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    class Ship
    {
        public IEnumerable<IMyCubeGrid> Grids
        {
            get
            {
                return ShipGrids;
            }
        }
        private HashSet<IMyCubeGrid> ShipGrids = new HashSet<IMyCubeGrid>();

        public string Name
        {
            get;
            private set;
        }
        private int LargestSizeSeen = int.MinValue;

        public Ship(IMyCubeGrid grid)
        {
            Add(grid);
        }

        public void Add(IMyCubeGrid grid)
        {
            if (ShipGrids.Add(grid))
            {
                int size = GridSize(grid);
                if (size > LargestSizeSeen)
                {
                    LargestSizeSeen = size;
                    Name = grid.CustomName;
                }
            }
        }

        int GridSize(IMyCubeGrid grid) =>
            (grid.Max - grid.Min + Vector3I.One).Size;
    }
}
