using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IngameScript
{
    partial class Program
    {
        List<IMyProgrammableBlock> TimBlocks = new List<IMyProgrammableBlock>();
        void CheckStandby()
        {
            // search for other TIMs
            GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(TimBlocks, IsTimBlock);

            // check to see if this block is the first available TIM.
            // This exploits an undocumented behaviour of the SE scripting API: blocks within a grid are always
            // returned in construction/load order, so the ordering of the blocks is consistent from run to run
            // and from script to script. Docked ships are also presented in a consistent ordering.
            //
            // Thus, multiple TIM blocks will see the same value for firstAvailableIndex for the same collection
            // of docked ships, and see their own position consistently within the list as well.
            var firstAvailableIndex = TimBlocks.FindIndex(block => block.IsFunctional & block.IsWorking);
            var selfIndex = TimBlocks.IndexOf(Me);

            // update custom name based on current index
            var indexSuffix = "";
            if (TimBlocks.Count > 1)
                indexSuffix = $" #{selfIndex + 1}";

            var nameTag = $"{argTagOpen}{argTagPrefix}{indexSuffix}{argTagClose}";
            if (tagRegex.IsMatch(Me.CustomName))
                Me.CustomName = tagRegex.Replace(Me.CustomName, nameTag, 1);
            else
                Me.CustomName = $"{Me.CustomName} {nameTag}";

            // if there are other programmable blocks of lower index, then they will execute and we won't
            if (selfIndex != firstAvailableIndex)
            {
                Echo("TIM #" + (firstAvailableIndex + 1) + " is on duty. Standing by.");

                var activeConfiguration = TimBlocks[firstAvailableIndex].CustomData.Trim();
                var myConfiguration = Me.CustomData.Trim();
                if (activeConfiguration != myConfiguration)
                    Echo("WARNING: Custom data does not match TIM #" + (firstAvailableIndex + 1) + ".");

                throw new IgnoreExecutionException();
            }
        }

        bool IsTimBlock(IMyProgrammableBlock block)
        {
            if (block == Me)
                return true;

            if (!DockedTo(block))
                return false;

            return tagRegex.IsMatch(block.CustomName);
        }
    }
}
