using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Linq;
using System;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        HashSet<IMyCubeGrid> DockedGrids = new HashSet<IMyCubeGrid>();

        // True iff the target grid is docked to the grid under control of this script, taking dock tags into account.
        // Only as fresh as the last call to ScanGrids().
        bool DockedTo(IMyTerminalBlock block) =>
            DockedGrids.Contains(block.CubeGrid);

        // Update the result of `DockedTo` based on the current set of reachable grids.
        void ScanGrids()
        {
            Dictionary<IMyCubeGrid, int> gridShip = new Dictionary<IMyCubeGrid, int>();
            List<HashSet<IMyCubeGrid>> shipGrids = new List<HashSet<IMyCubeGrid>>();
            List<string> shipName = new List<string>();
            Dictionary<int, Dictionary<int, List<string>>> shipShipDocks = new Dictionary<int, Dictionary<int, List<string>>>();

            var gridLinks = ScanMechanicalLinks();
            ScanShips(gridLinks, gridShip, shipGrids, shipName);
            ScanShipConnectors(gridShip, shipGrids, shipName, shipShipDocks);
            ScanDockedGrids(gridShip, shipGrids, shipName, shipShipDocks);
        }

        Queue<int> ShipScanQueue = new Queue<int>();
        HashSet<int> ShipsScanned = new HashSet<int>();
        void ScanDockedGrids(Dictionary<IMyCubeGrid, int> gridShip, List<HashSet<IMyCubeGrid>> shipGrids, List<string> shipName, Dictionary<int, Dictionary<int, List<string>>> shipShipDocks)
        {
            // starting "here", traverse all docked ships
            ShipScanQueue.Clear();
            ShipsScanned.Clear();

            DockedGrids.Clear();
            DockedGrids.Add(Me.CubeGrid);
            int ship;
            if (!gridShip.TryGetValue(Me.CubeGrid, out ship))
                return;
            ShipsScanned.Add(ship);
            DockedGrids.UnionWith(shipGrids[ship]);
            ShipScanQueue.Enqueue(ship);
            while (ShipScanQueue.Count > 0)
            {
                var s1 = ShipScanQueue.Dequeue();
                Dictionary<int, List<string>> shipDocks;
                if (!shipShipDocks.TryGetValue(s1, out shipDocks))
                    continue;
                foreach (int ship2 in shipDocks.Keys)
                {
                    if (ShipsScanned.Add(ship2))
                    {
                        DockedGrids.UnionWith(shipGrids[ship2]);
                        ShipScanQueue.Enqueue(ship2);
                        debugText.Add(shipName[ship2] + " docked to " + shipName[s1] + " at " + String.Join(", ", shipDocks[ship2]));
                    }
                }
            }
        }

        List<IMyShipConnector> Connectors = new List<IMyShipConnector>();
        HashSet<string> LeftDockTags = new HashSet<string>();
        HashSet<string> RightDockTags = new HashSet<string>();
        void ScanShipConnectors(Dictionary<IMyCubeGrid, int> gridShip, List<HashSet<IMyCubeGrid>> shipGrids, List<string> shipName, Dictionary<int, Dictionary<int, List<string>>> shipShipDocks)
        {
            // connectors require at least one shared dock tag, or no tags on either

            GridTerminalSystem.GetBlocksOfType(Connectors);
            foreach (var block in Connectors)
            {
                var conn2 = block.OtherConnector;
                if (conn2 != null && block.EntityId < conn2.EntityId & block.Status == MyShipConnectorStatus.Connected)
                {
                    LeftDockTags.Clear();
                    RightDockTags.Clear();
                    var blockNameMatch = tagRegex.Match(block.CustomName);
                    if (blockNameMatch.Success)
                    {
                        foreach (string attr in blockNameMatch.Groups[1].Captures[0].Value.Split(SPACECOMMA, REE))
                        {
                            if (attr.StartsWith("DOCK:", OIC))
                                LeftDockTags.UnionWith(attr.Substring(5).ToUpper().Split(COLON, REE));
                        }
                    }
                    var targetNameMatch = tagRegex.Match(conn2.CustomName);
                    if ((targetNameMatch = tagRegex.Match(conn2.CustomName)).Success)
                    {
                        foreach (string attr in targetNameMatch.Groups[1].Captures[0].Value.Split(SPACECOMMA, REE))
                        {
                            if (attr.StartsWith("DOCK:", OIC))
                                RightDockTags.UnionWith(attr.Substring(5).ToUpper().Split(COLON, REE));
                        }
                    }
                    if ((LeftDockTags.Count > 0 | RightDockTags.Count > 0) & !LeftDockTags.Overlaps(RightDockTags))
                        continue;
                    var g1 = block.CubeGrid;
                    var g2 = conn2.CubeGrid;
                    int s1;
                    if (!gridShip.TryGetValue(g1, out s1))
                    {
                        gridShip[g1] = s1 = shipGrids.Count;
                        shipGrids.Add(new HashSet<IMyCubeGrid> { g1 });
                        shipName.Add(g1.CustomName);
                    }
                    int s2;
                    if (!gridShip.TryGetValue(g2, out s2))
                    {
                        gridShip[g2] = s2 = shipGrids.Count;
                        shipGrids.Add(new HashSet<IMyCubeGrid> { g2 });
                        shipName.Add(g2.CustomName);
                    }
                    Dictionary<int, List<string>> shipDocks;
                    List<string> docks;
                    ((shipShipDocks.TryGetValue(s1, out shipDocks) ? shipDocks : shipShipDocks[s1] = new Dictionary<int, List<string>>()).TryGetValue(s2, out docks) ? docks : shipShipDocks[s1][s2] = new List<string>()).Add(block.CustomName);
                    ((shipShipDocks.TryGetValue(s2, out shipDocks) ? shipDocks : shipShipDocks[s2] = new Dictionary<int, List<string>>()).TryGetValue(s1, out docks) ? docks : shipShipDocks[s2][s1] = new List<string>()).Add(conn2.CustomName);
                }
            }
        }

        List<IMyCubeGrid> GridScanQueue = new List<IMyCubeGrid>(); // actual Queue lacks AddRange
        void ScanShips(Dictionary<IMyCubeGrid, HashSet<IMyCubeGrid>> gridLinks, Dictionary<IMyCubeGrid, int> gridShip, List<HashSet<IMyCubeGrid>> shipGrids, List<string> shipName)
        {
            GridScanQueue.Clear();

            // each connected component of mechanical links is a "ship"
            foreach (IMyCubeGrid grid in gridLinks.Keys)
            {
                if (!gridShip.ContainsKey(grid))
                {
                    var s1 = (grid.Max - grid.Min + Vector3I.One).Size;
                    var g1 = grid;
                    gridShip[grid] = shipGrids.Count;
                    var grids = new HashSet<IMyCubeGrid> { grid };
                    GridScanQueue.Clear();
                    GridScanQueue.AddRange(gridLinks[grid]);
                    for (var q = 0; q < GridScanQueue.Count; q++)
                    {
                        var g2 = GridScanQueue[q];
                        if (!grids.Add(g2))
                            continue;
                        var s2 = (g2.Max - g2.Min + Vector3I.One).Size;
                        g1 = s2 > s1 ? g2 : g1;
                        s1 = s2 > s1 ? s2 : s1;
                        gridShip[g2] = shipGrids.Count;
                        GridScanQueue.AddRange(gridLinks[g2].Except(grids));
                    }
                    shipGrids.Add(grids);
                    shipName.Add(g1.CustomName);
                }
            }
        }

        List<IMyMechanicalConnectionBlock> MechanicalConnections = new List<IMyMechanicalConnectionBlock>();
        Dictionary<IMyCubeGrid, HashSet<IMyCubeGrid>> GridLinks = new Dictionary<IMyCubeGrid, HashSet<IMyCubeGrid>>();
        Dictionary<IMyCubeGrid, HashSet<IMyCubeGrid>> ScanMechanicalLinks()
        {
            GridLinks.Clear();

            // find mechanical links
            GridTerminalSystem.GetBlocksOfType(MechanicalConnections);
            foreach (var block in MechanicalConnections)
            {
                var g1 = block.CubeGrid;
                var g2 = block.TopGrid;
                if (g2 == null)
                    continue;
                HashSet<IMyCubeGrid> grids;
                (GridLinks.TryGetValue(g1, out grids) ? grids : GridLinks[g1] = new HashSet<IMyCubeGrid>()).Add(g2);
                (GridLinks.TryGetValue(g2, out grids) ? grids : GridLinks[g2] = new HashSet<IMyCubeGrid>()).Add(g1);
            }

            return GridLinks;
        }
    }
}
