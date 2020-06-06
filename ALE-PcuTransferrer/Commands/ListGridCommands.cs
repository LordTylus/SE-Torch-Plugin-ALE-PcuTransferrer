using ALE_Core.Utils;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Text;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace ALE_GridManager.Commands {

    public class ListGridCommands : CommandModule {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public GridManagerPlugin Plugin => (GridManagerPlugin)Context.Plugin;

        [Command("listgridsowner", "Checks which grids the given player has ownership on.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void InspectOwner() {

            string playerName;
            bool gps = false;
            bool position = false;
            bool showOwner = false;
            bool showId = false;

            List<string> args = Context.Args;

            if (args.Count == 1) {

                playerName = args[0];

            } else if (args.Count > 1) {

                playerName = args[0];

                for (int i = 1; i < args.Count; i++) {

                    if (args[i] == "-gps")
                        gps = true;

                    if (args[i] == "-position")
                        position = true;

                    if (args[i] == "-owner")
                        showOwner = true;

                    if (args[i] == "-id")
                        showId = true;
                }

            } else {

                Context.Respond("Correct Usage is !listgridsowner <playerName> [-gps] [-position] [-owner] [-id]");
                return;
            }

            IMyIdentity identity = PlayerUtils.GetIdentityByName(playerName);

            if(identity == null) {

                Context.Respond("Player not found!");
                return;
            }

            long id = identity.IdentityId;

            StringBuilder sb = new StringBuilder();

            List<MyCubeGrid> bigOwnerGrids = new List<MyCubeGrid>();
            List<MyCubeGrid> smallOwnerGrids = new List<MyCubeGrid>();

            foreach (MyEntity entity in MyEntities.GetEntities()) {

                if (!(entity is MyCubeGrid grid))
                    continue;

                if (grid.Physics == null)
                    continue;

                if (grid.BigOwners.Contains(id))
                    bigOwnerGrids.Add(grid);
                else if (grid.SmallOwners.Contains(id))
                    smallOwnerGrids.Add(grid);
            }

            sb.AppendLine("More then 50 % Ownership");
            sb.AppendLine("---------------------------------------");

            foreach (MyCubeGrid grid in bigOwnerGrids)
                AddGridToSb(grid, sb, position, gps, showOwner, id, false, showId);

            sb.AppendLine("");
            sb.AppendLine("Less then 50 % Ownership");
            sb.AppendLine("---------------------------------------");

            foreach (MyCubeGrid grid in smallOwnerGrids)
                AddGridToSb(grid, sb, position, gps, showOwner, id, false, showId);

            if (Context.Player == null) {

                Context.Respond($"Grids owned by {playerName}");
                Context.Respond(sb.ToString());

            } else {

                ModCommunication.SendMessageTo(new DialogMessage("List Grids Ownership", $"Grids owned by {playerName}", sb.ToString()), Context.Player.SteamUserId);
            }
        }

        [Command("listgridsauthor", "Checks which grids the given player has authorship on.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void InspectAuthor() {

            string playerName;
            bool gps = false;
            bool position = false;
            bool showOwner = false;
            bool showId = false;

            List<string> args = Context.Args;

            if (args.Count == 1) {

                playerName = args[0];

            } else if (args.Count > 1) {

                playerName = args[0];

                for (int i = 1; i < args.Count; i++) {

                    if (args[i] == "-gps")
                        gps = true;

                    if (args[i] == "-position")
                        position = true;
                    
                    if (args[i] == "-owner")
                        showOwner = true;

                    if (args[i] == "-id")
                        showId = true;
                }

            } else {

                Context.Respond("Correct Usage is !listgridsauthor <playerName> [-gps] [-position] [-owner] [-id]");
                return;
            }

            IMyIdentity identity = PlayerUtils.GetIdentityByName(playerName);

            if (identity == null) {

                Context.Respond("Player not found!");
                return;
            }

            long id = identity.IdentityId;

            StringBuilder sb = new StringBuilder();

            foreach (MyEntity entity in MyEntities.GetEntities()) {

                if (!(entity is MyCubeGrid grid))
                    continue;

                if (grid.Physics == null)
                    continue;

                AddGridToSb(grid, sb, position, gps, showOwner, id, true, showId);
            }

            if (Context.Player == null) {

                Context.Respond($"Grids built by {playerName}");
                Context.Respond(sb.ToString());

            } else {

                ModCommunication.SendMessageTo(new DialogMessage("List Grids Authorships", $"Grids built by {playerName}", sb.ToString()), Context.Player.SteamUserId);
            }
        }

        private void AddGridToSb(MyCubeGrid grid, StringBuilder sb, bool showPosition, bool showGps, bool showOwner, long playerId, bool pcu, bool showId) {

            HashSet<MySlimBlock> blocks = new HashSet<MySlimBlock>(grid.GetBlocks());

            int value = 0;

            foreach (MySlimBlock block in blocks) {

                if (block == null || block.CubeGrid == null || block.IsDestroyed)
                    continue;

                if (!pcu) {

                    MyCubeBlock cubeBlock = block.FatBlock;
                    if (cubeBlock != null && cubeBlock.OwnerId == playerId)
                        value++;

                    continue;
                }

                if (block.BuiltBy == playerId) 
                    value += BlockUtils.GetPcu(block);
            }

            if (value == 0)
                return;

            if (pcu)
                sb.AppendLine($"{grid.DisplayName} - {value} PCU");
            else
                sb.AppendLine($"{grid.DisplayName} - {value} blocks");

            if (showId) 
                sb.AppendLine("   Id: " + grid.EntityId);
             
            if (showOwner) {

                long ownerId = OwnershipUtils.GetOwner(grid);
                string ownerName = PlayerUtils.GetPlayerNameById(ownerId);

                string factionTag = FactionUtils.GetPlayerFactionTag(ownerId);
                if (factionTag != "")
                    factionTag = "[" + factionTag + "]";

                sb.AppendLine("   Owned by: " + ownerName + " " + factionTag);
            }


            if (showPosition) { 

                var position = grid.PositionComp.GetPosition();

                sb.AppendLine($"   X: {position.X.ToString("#,##0.00")}, Y: {position.Y.ToString("#,##0.00")}, Z: {position.Z.ToString("#,##0.00")}");
            }

            if (showGps && Context.Player != null) {

                var gridGPS = MyAPIGateway.Session?.GPS.Create("--"+grid.DisplayName, ($"{grid.DisplayName} - {grid.GridSizeEnum} - {grid.BlocksCount} blocks"), grid.PositionComp.GetPosition(), true);

                MyAPIGateway.Session?.GPS.AddGps(Context.Player.IdentityId, gridGPS);
            }
        }

        [Command("listgrids", "Lists all grids you have in your world and allows some filters for them.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void ListGrids() {

            List<string> args = Context.Args;

            string factionTag = null;
            string playerName = null;
            string orderby = "blocks";
            bool showId = false;

            for (int i = 0; i < args.Count; i++) {

                if (args[i].StartsWith("-faction="))
                    factionTag = args[i].Replace("-faction=", "");

                if (args[i].StartsWith("-player="))
                    playerName = args[i].Replace("-player=", "");

                if (args[i].StartsWith("-orderby="))
                    orderby = args[i].Replace("-orderby=", "");

                if (args[i] == "-id")
                    showId = true;
            }

            if (orderby != "blocks" && orderby != "pcu" && orderby != "name" && orderby != "faction" && orderby != "owner") {
                Context.Respond("You can only order by 'pcu', 'name', 'faction', 'owner' or 'blocks'! Will use blocks as default.");
                orderby = "blocks";
            }

            ListGrids(factionTag, playerName, orderby, showId);
        }

        private void ListGrids(string factionTag, string playerName, string orderby, bool showId) {

            List<MyCubeGrid> grids = new List<MyCubeGrid>();
            /* Very cheaply. shame on me */
            Dictionary<long, string> ownerNames = new Dictionary<long, string>();
            Dictionary<long, string> factionNames = new Dictionary<long, string>();

            HashSet<long> identities = null;

            string title = "Grids in World";

            if (playerName != null) {

                MyIdentity player = PlayerUtils.GetIdentityByName(playerName);
                if (player == null) {

                    Context.Respond("Player not found!");
                    return;
                }

                title = "Grids of Player " + playerName;

                identities = new HashSet<long> {
                    player.IdentityId
                };

            } else if (factionTag != null) {

                IMyFaction faction = FactionUtils.GetIdentityByTag(factionTag);

                if (faction == null) {

                    Context.Respond("Faction not found!");
                    return;
                }

                title = "Grids of Faction " + factionTag;

                identities = new HashSet<long>();
                foreach (long identityId in faction.Members.Keys)
                    identities.Add(identityId);
            }

            foreach (MyEntity entity in MyEntities.GetEntities()) {

                if (!(entity is MyCubeGrid grid))
                    continue;

                if (grid.Physics == null)
                    continue;

                if(identities != null && !identities.Overlaps(grid.BigOwners)) 
                    continue;

                var gridOwnerList = grid.BigOwners;
                var gridOwner = gridOwnerList.Count > 0 ? gridOwnerList[0] : 0L;

                grids.Add(grid);

                long entityId = grid.EntityId;

                ownerNames.Add(entityId, PlayerUtils.GetPlayerNameById(gridOwner));
                factionNames.Add(entityId, FactionUtils.GetPlayerFactionTag(gridOwner));
            }

            grids.Sort(delegate (MyCubeGrid grid1, MyCubeGrid grid2) {

                if (orderby == "name")
                    return grid1.DisplayName.CompareTo(grid2.DisplayName);

                if (orderby == "pcu")
                    return grid2.BlocksPCU.CompareTo(grid1.BlocksPCU);

                if (orderby == "owner")
                    return ownerNames[grid1.EntityId].CompareTo(ownerNames[grid2.EntityId]);

                if (orderby == "faction")
                    return factionNames[grid1.EntityId].CompareTo(factionNames[grid2.EntityId]);

                return grid2.BlocksCount.CompareTo(grid1.BlocksCount);
            });

            StringBuilder sb = new StringBuilder();

            long totalPCU = 0;
            long totalValue = 0;

            foreach (MyCubeGrid grid in grids) {

                string pcuString = "";

                long pcu = grid.BlocksPCU;
                pcuString = " " + pcu.ToString("#,##0") + " PCU";
                totalPCU += pcu;

                long blocks = grid.BlocksCount;

                sb.AppendLine(blocks.ToString("#,##0").PadRight(7) + grid.DisplayName);

                if (showId)
                    sb.AppendLine("".PadRight(10) + "Id: " + grid.EntityId);

                sb.AppendLine("".PadRight(10) + factionNames[grid.EntityId].PadRight(5) +" - "+ownerNames[grid.EntityId] + pcuString);

                totalValue += blocks;
            }

            sb.AppendLine();
            sb.AppendLine("Total: " + totalValue.ToString("#,##0") + " Blocks");
            sb.AppendLine("Total: " + totalPCU.ToString("#,##0") + " PCU");

            sb.AppendLine();
            sb.AppendLine(grids.Count.ToString("#,##0") + " Grids checked");


            if (Context.Player == null) {

                Context.Respond(title);
                Context.Respond(sb.ToString());

            } else {

                string subtitle = "All Grids";

                ModCommunication.SendMessageTo(new DialogMessage(title, subtitle, sb.ToString()), Context.Player.SteamUserId);
            }
        }

        [Command("listgridsrange", "Lists all grids which are in the given range (m) of your character.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void ListGridsRange(int radius) {

            if(Context.Player == null) {
                Context.Respond("This command is not for console!");
                return;
            }

            bool showGps = false;
            bool showPosition = false;
            bool showId = false;

            List<string> args = Context.Args;
            for (int i = 1; i < args.Count; i++) {

                if (args[i] == "-gps")
                    showGps = true;

                if (args[i] == "-position")
                    showPosition = true;

                if (args[i] == "-id")
                    showId = true;
            }

            var position = Context.Player.GetPosition();
            double distance = radius;

            StringBuilder sb = new StringBuilder();

            HashSet<MyCubeGrid> grids = new HashSet<MyCubeGrid>();

            foreach (MyEntity entity in MyEntities.GetEntities()) {

                if (!(entity is MyCubeGrid grid))
                    continue;

                if (grid.Physics == null)
                    continue;

                grids.Add(grid);
            }

            foreach(MyCubeGrid grid in grids) {

                var gridPosition = grid.PositionComp.GetPosition();

                double distancePlayerGrid = Vector3D.Distance(position, gridPosition);

                if (distance < distancePlayerGrid)
                    continue;

                double distancePlayerGridKm = distancePlayerGrid / 1000;

                sb.AppendLine($"{distancePlayerGridKm.ToString("#,##0.00")} km - {grid.DisplayName} - {grid.BlocksCount} blocks");

                if (showId)
                    sb.AppendLine("   Id: " + grid.EntityId);

                if (showPosition)
                    sb.AppendLine($"   X: {gridPosition.X.ToString("#,##0.00")}, Y: {gridPosition.Y.ToString("#,##0.00")}, Z: {gridPosition.Z.ToString("#,##0.00")}");

                if (showGps && Context.Player != null) {

                    var gridGPS = MyAPIGateway.Session?.GPS.Create("--" + grid.DisplayName, ($"{grid.DisplayName} - {grid.GridSizeEnum} - {grid.BlocksCount} blocks"), grid.PositionComp.GetPosition(), true);

                    MyAPIGateway.Session?.GPS.AddGps(Context.Player.IdentityId, gridGPS);
                }
            }

            ModCommunication.SendMessageTo(new DialogMessage("Grids in Range", radius.ToString("#,##0") + "m", sb.ToString()), Context.Player.SteamUserId);
        }

        [Command("listnoauthor", "Lists all grids which contain blocks not owned by a player or npc.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void ListNoAuthor() {

            bool showGps = false;
            bool showPosition = false;
            bool showId = false;

            List<string> args = Context.Args;
            for (int i = 0; i < args.Count; i++) {

                if (args[i] == "-gps")
                    showGps = true;

                if (args[i] == "-position")
                    showPosition = true;

                if (args[i] == "-id")
                    showId = true;
            }

            StringBuilder sb = new StringBuilder();

            HashSet<MyCubeGrid> grids = new HashSet<MyCubeGrid>();

            foreach (MyEntity entity in MyEntities.GetEntities()) {

                if (!(entity is MyCubeGrid grid))
                    continue;

                if (grid.Physics == null)
                    continue;

                grids.Add(grid);
            }

            HashSet<long> identities = new HashSet<long>();

            foreach (var identity in MySession.Static.Players.GetAllIdentities())
                identities.Add(identity.IdentityId);

            foreach (MyCubeGrid grid in grids) {

                int numberNobodyBlocks = 0;

                foreach(var block in grid.GetBlocks()) {

                    long buildBy = block.BuiltBy;

                    if (!identities.Contains(buildBy))
                        numberNobodyBlocks++;
                }

                if (numberNobodyBlocks == 0)
                    continue;

                var gridPosition = grid.PositionComp.GetPosition();
                var ownerId = OwnershipUtils.GetOwner(grid);
                var ownerName = PlayerUtils.GetPlayerNameById(ownerId);

                sb.AppendLine($"{grid.DisplayName} - Owned by: {ownerName} - {numberNobodyBlocks} blocks");

                if (showId)
                    sb.AppendLine("   Id: " + grid.EntityId);

                if (showPosition)
                    sb.AppendLine($"   X: {gridPosition.X.ToString("#,##0.00")}, Y: {gridPosition.Y.ToString("#,##0.00")}, Z: {gridPosition.Z.ToString("#,##0.00")}");

                if (showGps && Context.Player != null) {

                    var gridGPS = MyAPIGateway.Session?.GPS.Create("--" + grid.DisplayName, ($"{grid.DisplayName} - {grid.GridSizeEnum} - {grid.BlocksCount} blocks"), grid.PositionComp.GetPosition(), true);

                    MyAPIGateway.Session?.GPS.AddGps(Context.Player.IdentityId, gridGPS);
                }
            }

            string title = "Grids without author";

            if (Context.Player == null) {

                Context.Respond(title);
                Context.Respond(sb.ToString());

            } else {

                ModCommunication.SendMessageTo(new DialogMessage(title,
                    "Blocks are build by nobody or a deleted identity", sb.ToString()), 
                    Context.Player.SteamUserId);
            }
        }
    }
}