namespace IngameScript
{
    /// <summary>
    /// Stores the work information for an item in a producer block.
    /// </summary>
    struct ProducerWork
    {
        public ItemId item;
        public double quantity;

        public ProducerWork(ItemId i, double q)
        {
            item = i;
            quantity = q;
        }
    }
}
