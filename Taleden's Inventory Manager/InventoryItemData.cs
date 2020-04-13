using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using VRage.Game;

namespace IngameScript
{
    partial class Program
    {
        class InventoryItemData
        {
            public string subType, label;
            public MyDefinitionId blueprint;
            public long amount, avail, locked, quota, minimum;
            public float ratio;
            public int qpriority, hold, jam;
            public Dictionary<IMyInventory, long> invenTotal;
            public Dictionary<IMyInventory, int> invenSlot;
            public HashSet<IMyFunctionalBlock> producers;
            public Dictionary<string, double> prdSpeed;

            /// <summary>
            /// Initialises the item with base data.
            /// </summary>
            /// <param name="itemType">Item Type ID.</param>
            /// <param name="itemSubType">Item SubType ID.</param>
            /// <param name="minimum"></param>
            /// <param name="ratio"></param>
            /// <param name="label"></param>
            /// <param name="blueprint"></param>
            public static void InitItem(string itemType, string itemSubType, long minimum = 0L, float ratio = 0.0f, string label = "", string blueprint = "")
            {
                string itypelabel = itemType, isublabel = itemSubType;
                itemType = itemType.ToUpper();
                itemSubType = itemSubType.ToUpper();

                // new type?
                if (!typeSubs.ContainsKey(itemType))
                {
                    types.Add(itemType);
                    typeLabel[itemType] = itypelabel;
                    typeSubs[itemType] = new List<string>();
                    typeAmount[itemType] = 0L;
                    typeSubData[itemType] = new Dictionary<string, InventoryItemData>();
                }

                // new subtype?
                if (!subTypes.ContainsKey(itemSubType))
                {
                    subs.Add(itemSubType);
                    subLabel[itemSubType] = isublabel;
                    subTypes[itemSubType] = new List<string>();
                }

                // new type/subtype pair?
                if (!typeSubData[itemType].ContainsKey(itemSubType))
                {
                    foundNewItem = true;
                    typeSubs[itemType].Add(itemSubType);
                    subTypes[itemSubType].Add(itemType);
                    typeSubData[itemType][itemSubType] = new InventoryItemData(itemSubType, minimum, ratio, label == "" ? isublabel : label, blueprint == "" ? isublabel : blueprint);
                    if (blueprint != null)
                        blueprintItem[typeSubData[itemType][itemSubType].blueprint] = new ItemId(itemType, itemSubType);
                }
            }

            private InventoryItemData(string isub, long minimum, float ratio, string label, string blueprint)
            {
                subType = isub;
                this.label = label;
                this.blueprint = blueprint == null ? default(MyDefinitionId) : MyDefinitionId.Parse("MyObjectBuilder_BlueprintDefinition/" + blueprint);
                amount = avail = locked = quota = 0L;
                this.minimum = (long)(minimum * 1000000.0 + 0.5);
                this.ratio = ratio / 100.0f;
                qpriority = -1;
                hold = jam = 0;
                invenTotal = new Dictionary<IMyInventory, long>();
                invenSlot = new Dictionary<IMyInventory, int>();
                producers = new HashSet<IMyFunctionalBlock>();
                prdSpeed = new Dictionary<string, double>();
            }
        }
    }
}
