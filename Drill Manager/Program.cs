using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System;
using VRage.Game.ModAPI.Ingame;
using VRage.ObjectBuilders;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        enum Goal
        {
            STOP,
            CONTINUE,
            START
        }

        void EnableAction(IMyFunctionalBlock drill)
        {
            drill.Enabled = true;
        }

        void DisableAction(IMyFunctionalBlock drill)
        {
            drill.Enabled = false;
        }

        void ContinueAction(IMyFunctionalBlock drill) {
        }

        public void Main(string argument, UpdateType updateSource)
        {
            var goal = InventoryGoal();

            ApplyGoal(goal);
        }

        List<IMyFunctionalBlock> controlledBlocks = new List<IMyFunctionalBlock>();
        void ApplyGoal(Goal goal)
        {
            GridTerminalSystem.GetBlocksOfType(controlledBlocks, IsControlled);
            Action<IMyFunctionalBlock> goalAction = GoalAction(goal);
            controlledBlocks.ForEach(goalAction);
        }

        Action<IMyFunctionalBlock> GoalAction(Goal goal)
        {
            Action<IMyFunctionalBlock> goalAction = ContinueAction;
            switch (goal)
            {
                case Goal.START:
                    goalAction = EnableAction;
                    break;
                case Goal.STOP:
                    goalAction = DisableAction;
                    break;
            }

            return goalAction;
        }

        bool IsControlled(IMyFunctionalBlock block) =>
            IsLocal(block) && block.IsFunctional && IsControlTagged(block);

        const string CONTROL_TAG = "[DM CONTROL]";
        bool IsControlTagged(IMyTerminalBlock block) => block.CustomName.Contains(CONTROL_TAG);

        List<IMyTerminalBlock> measuredBlocks = new List<IMyTerminalBlock>();
        Goal InventoryGoal()
        {
            GridTerminalSystem.GetBlocksOfType(measuredBlocks, IsMonitored);

            var inventoryRatio = measuredBlocks
                .Select(ContainerUsage)
                .Aggregate(InventoryVolume.Zero, (a, b) => a + b)
                .Ratio;

            if (inventoryRatio < 0.1)
                return Goal.START;
            if (inventoryRatio < 0.9)
                return Goal.CONTINUE;
            return Goal.STOP;
        }

        InventoryVolume ContainerUsage(IMyTerminalBlock container)
        {
            InventoryVolume usage = InventoryVolume.Zero;

            int inventories = container.InventoryCount;
            for (var i = 0; i < inventories; ++i)
            {
                var inventory = container.GetInventory(i);
                var inventoryUsage = InventoryUsage(inventory);

                usage += inventoryUsage;
            }

            return usage;
        }

        InventoryVolume InventoryUsage(IMyInventory inventory) =>
            new InventoryVolume(inventory.MaxVolume, inventory.CurrentVolume);

        bool IsLocal(IMyTerminalBlock block) =>
            Me.IsSameConstructAs(block);

        bool IsMonitored(IMyTerminalBlock block) =>
            IsLocal(block) && block.IsFunctional && IsMonitorTagged(block);

        const string MONITOR_TAG = "[DM MONITOR]";
        bool IsMonitorTagged(IMyTerminalBlock block) => block.CustomName.Contains(MONITOR_TAG);
    }
}
