namespace IngameScript
{
    /// <summary>
    /// Stores an item's ID.
    /// </summary>
    struct ItemId
    {
        public string type, subType;

        public ItemId(string t, string s)
        {
            type = t;
            subType = s;
        }
    }
}
