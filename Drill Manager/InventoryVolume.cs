using VRage;

namespace IngameScript
{
    struct InventoryVolume
    {
        public static readonly InventoryVolume Zero = new InventoryVolume(0, 0);

        public InventoryVolume(MyFixedPoint max, MyFixedPoint used)
        {
            Max = max;
            Used = used;
        }

        public MyFixedPoint Max { get; }
        public MyFixedPoint Used { get; }
 
        public double Ratio
        {
            get
            {
                if (Max > 0)
                    return (double)Used / (double)Max;

                // Blocks with no inventory are always treated as "full".
                return 1.0;
            }
        }

        public static InventoryVolume operator +(InventoryVolume a, InventoryVolume b)
        {
            return new InventoryVolume(a.Max + b.Max, a.Used + b.Used);
        }
    }
}
