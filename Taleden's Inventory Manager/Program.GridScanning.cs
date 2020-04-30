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

        // True iff the grid the target block is part of is under the control of this script, taking dock tags into account.
        // Only as fresh as the last call to ScanGrids().
        bool DockedTo(IMyTerminalBlock block) =>
            DockedGrids.Contains(block.CubeGrid);

        // Update the result of `DockedTo` based on the current set of reachable grids.
        void ScanGrids()
        {
            var linkedGrids = ScanMechanicalLinks();
            var connectors = ScanConnectors();
            var shipsByGrid = ScanShips(linkedGrids, connectors);
            var dockedShipsByShip = ScanShipConnectors(shipsByGrid, connectors);
            ScanDockedGrids(shipsByGrid, dockedShipsByShip);
        }

        /*
         * Some theory:
         *  - A _grid_ is an IMyCubeGrid, i.e., a discrete physical simulation in the game world with solid-body
         *    physics, built out of adjacently-connected solid blocks. Each grid has a name.
         *  - A _mechanical link_ is an IMyMechanicalConnectionBlock, i.e., a block joining two grids quasi-permanently
         *    via a physics constraint. This does not include connectors, but does include pistons, rotors, and custom
         *    joint blocks.
         *  - A _ship_ is a set of grids that are connected to one another via mechanical links. Each ship has a name,
         *    derived from the name of the largest grid (by extent) in the ship. This includes stations.
         *  - Two ships are _docked_ if they share a connector pair which are either untagged, or which share at least
         *    one DOCK tag.
         *  
         *  The goal here is to identify the set of grids docked to "this" grid (the one the script is running from). TIM
         *  will only manage inventories that are part of those grids; inventories on other grids will not be affected.
         *  This builds up to that set in stages. The final set is stored in the DockedGrids property of this class and
         *  can be queried using the DockedTo predicate.
         *  
         *  1. First, it builds up a graph of connected grids by scanning mechanical connections.
         *  2. Then, it groups connected components of that graph, any grids containing a connector, and the grid
         *     containing this script, into ships.
         *  3. Then, it identifies links between those ships using connector pairs, ruling out tagged connectors with
         *     no matching tags.
         *  4. Finally, it walks the graph from the current grid's ship, identifying only ships connected transitively
         *     to it.
         *  
         *  Note that the game allows ships to contain "interior" connectors, which link the ship to itself (between
         *  grids or on the same grid). This algorithm takes some care to avoid getting suckered into an infinite
         *  loop by ships that happen to include these. (I got bit by this testing the algorithm!)
         *  
         *  Allocation and garbage collection hit Space Engineers unusually hard, so this code makes heavy use of
         *  properties to reuse previously-allocated objects. This makes the code highly stateful, and very much
         *  non-reentrant. For example, calling ScanMechanicalLinks() twice will return the same list both times,
         *  although the contents may change from call to call. To make the code easier to follow, data dependencies
         *  are expressed through function parameters and return values, even though these methods could use the
         *  properties to access one anothers' data.
         */
        
        List<IMyMechanicalConnectionBlock> MechanicalConnections = new List<IMyMechanicalConnectionBlock>();
        Dictionary<IMyCubeGrid, HashSet<IMyCubeGrid>> LinkedGrids = new Dictionary<IMyCubeGrid, HashSet<IMyCubeGrid>>();
        Dictionary<IMyCubeGrid, HashSet<IMyCubeGrid>> ScanMechanicalLinks()
        {
            LinkedGrids.Clear();

            GridTerminalSystem.GetBlocksOfType(MechanicalConnections, IsConnected);
            foreach (var joint in MechanicalConnections)
            {
                var fromGrid = joint.CubeGrid;
                var toGrid = joint.TopGrid;

                LinkedGrids.GetOrAdd(fromGrid, Make.HashSet<IMyCubeGrid>).Add(toGrid);
                LinkedGrids.GetOrAdd(toGrid, Make.HashSet<IMyCubeGrid>).Add(fromGrid);
            }

            return LinkedGrids;
        }

        List<IMyShipConnector> Connectors = new List<IMyShipConnector>();
        List<IMyShipConnector> ScanConnectors()
        {
            GridTerminalSystem.GetBlocksOfType(Connectors);
            return Connectors;
        }


        Dictionary<IMyCubeGrid, Ship> ShipsByGrid = new Dictionary<IMyCubeGrid, Ship>();
        Dictionary<IMyCubeGrid, Ship> ScanShips(Dictionary<IMyCubeGrid, HashSet<IMyCubeGrid>> linkedGrids, List<IMyShipConnector> connectors)
        {
            ShipsByGrid.Clear();

            // each connected component of mechanical links is a "ship"
            foreach (var gridEntry in linkedGrids)
            {
                var grid = gridEntry.Key;
                var connectedGrids = gridEntry.Value;

                var ship = ShipsByGrid.GetOrAdd(grid, Make.Ship(grid));

                foreach (var connectedGrid in connectedGrids)
                {
                    ShipsByGrid[connectedGrid] = ship;
                    ship.Add(connectedGrid);
                }
            }

            // every connector is on a ship, as well, and those ships may not include mechanical links
            foreach (var connector in connectors)
            {
                var grid = connector.CubeGrid;
                ShipsByGrid.GetOrAdd(grid, Make.Ship(grid));
            }

            // ensure that this ship is always in the set of connected grids, even if it has no connectors or mechanical joints
            var myGrid = Me.CubeGrid;
            ShipsByGrid.GetOrAdd(myGrid, Make.Ship(myGrid));

            return ShipsByGrid;
        }

        HashSet<string> NearConnectorTags = new HashSet<string>();
        HashSet<string> FarConnectorTags = new HashSet<string>();
        Dictionary<Ship, HashSet<Ship>> DockedShipsByShip = new Dictionary<Ship, HashSet<Ship>>();
        Dictionary<Ship, HashSet<Ship>> ScanShipConnectors(Dictionary<IMyCubeGrid, Ship> shipsByGrid, List<IMyShipConnector> connectors)
        {
            DockedShipsByShip.Clear();

            // connectors require at least one shared dock tag, or no tags on either
            foreach (var nearConnector in connectors)
            {
                if (nearConnector.Status != MyShipConnectorStatus.Connected)
                    continue;

                var farConnector = nearConnector.OtherConnector;
                // Only consider each pair of connectors once (regardless of the order they're encountered in), to avoid
                // doing the same regular expression matching twice. Instead, handle both sides of the connector at once
                // when they're presented in the canonical ordering, and ignore them the other time they're presented.
                if (nearConnector.EntityId > farConnector.EntityId)
                    continue;

                ParseConnectorTags(nearConnector, NearConnectorTags);
                ParseConnectorTags(farConnector, FarConnectorTags);

                if (!MatchedConnectorTags(NearConnectorTags, FarConnectorTags))
                    continue;

                var nearGrid = nearConnector.CubeGrid;
                var farGrid = farConnector.CubeGrid;

                var nearShip = shipsByGrid[nearGrid];
                var farShip = shipsByGrid[farGrid];

                DockedShipsByShip
                    .GetOrAdd(nearShip, Make.HashSet<Ship>)
                    .Add(farShip);
                DockedShipsByShip
                    .GetOrAdd(farShip, Make.HashSet<Ship>)
                    .Add(nearShip);
            }

            return DockedShipsByShip;
        }

        bool MatchedConnectorTags(HashSet<string> nearConnectorTags, HashSet<String> farConnectorTags)
        {
            if (nearConnectorTags.Empty() && farConnectorTags.Empty())
            {
                return true;
            }

            return nearConnectorTags.Overlaps(farConnectorTags);
        }

        void ParseConnectorTags(IMyShipConnector connector, HashSet<string> intoTags)
        {
            intoTags.Clear();
            var match = tagRegex.Match(connector.CustomName);
            if (match.Success)
            {
                foreach (var attr in match.Groups[1].Captures[0].Value.Split(SPACECOMMA, REE))
                {
                    if (attr.StartsWith("DOCK:", OIC))
                    {
                        var dockTags = attr.Substring(5).ToUpper().Split(COLON, REE);
                        intoTags.UnionWith(dockTags);
                    }
                }
            }
        }

        Queue<Ship> ShipScanQueue = new Queue<Ship>();
        HashSet<Ship> ShipsScanned = new HashSet<Ship>();
        void ScanDockedGrids(Dictionary<IMyCubeGrid, Ship> shipsByGrid, Dictionary<Ship, HashSet<Ship>> dockedShipsByGrid)
        {
            DockedGrids.Clear();

            ShipScanQueue.Clear();
            ShipsScanned.Clear();

            // starting "here", traverse all docked ships
            var myGrid = Me.CubeGrid;
            var myShip = shipsByGrid[myGrid];
            ShipScanQueue.Enqueue(myShip);

            while (!ShipScanQueue.Empty())
            {
                var ship = ShipScanQueue.Dequeue();

                ShipsScanned.Add(ship);
                DockedGrids.UnionWith(ship.Grids);
                if (dockedShipsByGrid.ContainsKey(ship))
                {
                    foreach (var dockedShip in dockedShipsByGrid[ship])
                    {
                        if (!ShipsScanned.Contains(dockedShip))
                        {
                            Debug($"{ship.Name} docked to {dockedShip.Name}");
                            ShipScanQueue.Enqueue(dockedShip);
                        }
                    }
                }
            }
        }
    }
}
