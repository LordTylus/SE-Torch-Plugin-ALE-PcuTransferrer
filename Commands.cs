using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Groups;

namespace ALE_GridManager {

    public class Commands : CommandModule {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public GridManagerPlugin Plugin => (GridManagerPlugin) Context.Plugin;


        [Command("transfer", "Transfers PCU and Ownership of a ship over to a specified player.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void Transfer() {
            TransferInternal(true, true, false);
        }

        [Command("forcetransfer", "Transfers PCU and Ownership of a ship over to a specified player.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void ForceTransfer() {
            TransferInternal(true, true, true);
        }

        [Command("transferpcu", "Transfers PCU of a ship over to a specified player.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void TransferPcu() {
            TransferInternal(true, false, false);
        }

        [Command("forcetransferpcu", "Transfers PCU of a ship over to a specified player ignoring limits.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void ForceTransferPcu() {
            TransferInternal(true, false, true);
        }

        [Command("transferowner", "Transfers Owner of a ship over to a specified player.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void TransferOwner() {
            TransferInternal(false, true, false);
        }

        public void TransferInternal(bool pcu, bool ownership, bool force) {

            List<String> args = Context.Args;

            if (args.Count == 1) {

                TransferLookedAt(args[0], pcu, ownership, force);

            } else if(args.Count == 2) { 

                TransferGridName(args[1], args[0], pcu, ownership, force);

            } else {

                if (args.Count != 2)
                    Context.Respond("Correct Usage is !transfer <playerName> [gridName]");
            }
        }

        public void TransferGridName(string gridName, string playerName, bool pcu, bool ownership, bool force) {

            long playerId = 0L;

            if (Context.Player != null)
                playerId = Context.Player.IdentityId;

            MyPlayer author = GetPlayerByNameOrId(playerName);

            if (!checkConformation(playerId, author, gridName, null, pcu, force))
                return;

            try {

                Plugin.transfer(gridName, author, Context, pcu, ownership, force);

            } catch (Exception e) {
                Log.Error("Error on transferring ship", e);
            }
        }

        public void TransferLookedAt(string playerName, bool pcu, bool ownership, bool force) {

            IMyPlayer player = Context.Player;

            long playerId = 0L;

            if (player == null) {

                Context.Respond("Console has no Character so cannot use this command. Use !transfer <playerName> <gridname> instead!");
                return;

            } else {
                playerId = player.IdentityId;
            }

            IMyCharacter character = player.Character;

            if (character == null) {
                Context.Respond("You have no Character currently. Make sure to spawn and be out of cockpit!");
                return;
            }

            MyPlayer author = GetPlayerByNameOrId(playerName);

            if (!checkConformation(playerId, author, "nogrid_" + pcu + "_" + ownership, character, pcu, force))
                return;

            try {

                Plugin.transfer(character, author, Context, pcu, ownership, force);

            } catch (Exception e) {
                Log.Error("Error on transferring ship", e);
            }
        }

        [Command("repair", "Repairs grid to full integrity.")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void Repair() {

            List<String> args = Context.Args;

            if (args.Count == 0) {

                RepairLookedAt();

            } else if (args.Count == 1) {

                RepairGridName(args[0]);

            } else {

                if (args.Count != 2)
                    Context.Respond("Correct Usage is !repair [gridName]");
            }
        }

        public void RepairGridName(string gridName) {

            long playerId = 0L;

            if (Context.Player != null)
                playerId = Context.Player.IdentityId;

            if (!checkConformation(playerId, null, gridName, null, false, false))
                return;

            try {

                Plugin.repair(gridName, Context);

            } catch (Exception e) {
                Log.Error("Error on transferring ship", e);
            }
        }

        public void RepairLookedAt() {

            IMyPlayer player = Context.Player;

            long playerId = 0L;

            if (player == null) {

                Context.Respond("Console has no Character so cannot use this command. Use !repair <gridname> instead!");
                return;

            } else {
                playerId = player.IdentityId;
            }

            IMyCharacter character = player.Character;

            if (character == null) {
                Context.Respond("You have no Character currently. Make sure to spawn and be out of cockpit!");
                return;
            }

            if (!checkConformation(playerId, null, "nogridrepair", character, false, false))
                return;

            try {

                Plugin.repair(character, Context);

            } catch (Exception e) {
                Log.Error("Error on transferring ship", e);
            }
        }

        [Command("checkowner", "Checks the owner of the grid.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void CheckOwner() {

            List<String> args = Context.Args;

            if (args.Count == 0) {

                CheckOwnerLookedAt();

            } else if (args.Count == 1) {

                CheckOwnerGridName(args[0]);

            } else {

                if (args.Count != 2)
                    Context.Respond("Correct Usage is !checkowner [gridName]");
            }
        }

        public void CheckOwnerGridName(string gridName) {

            try {

                Plugin.checkOwner(gridName, Context);

            } catch (Exception e) {
                Log.Error("Error on transferring ship", e);
            }
        }

        public void CheckOwnerLookedAt() {

            IMyPlayer player = Context.Player;

            if (player == null) {

                Context.Respond("Console has no Character so cannot use this command. Use !checkowner <gridname> instead!");
                return;
            } 

            IMyCharacter character = player.Character;

            if (character == null) {
                Context.Respond("You have no Character currently. Make sure to spawn and be out of cockpit!");
                return;
            }

            try {

                Plugin.checkOwner(character, Context);

            } catch (Exception e) {
                Log.Error("Error on transferring ship", e);
            }
        }

        [Command("checkauthor", "Checks the author (PCU) of the grid.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void CheckAuthor() {

            List<String> args = Context.Args;

            if (args.Count == 0) {

                CheckAuthorLookedAt();

            } else if (args.Count == 1) {

                CheckAuthorGridName(args[0]);

            } else {

                if (args.Count != 2)
                    Context.Respond("Correct Usage is !checkauthor [gridName]");
            }
        }

        public void CheckAuthorGridName(string gridName) {

            try {

                Plugin.checkAuthor(gridName, Context);

            } catch (Exception e) {
                Log.Error("Error on transferring ship", e);
            }
        }

        public void CheckAuthorLookedAt() {

            IMyPlayer player = Context.Player;

            if (player == null) {

                Context.Respond("Console has no Character so cannot use this command. Use !checkauthor <gridname> instead!");
                return;
            }

            IMyCharacter character = player.Character;

            if (character == null) {
                Context.Respond("You have no Character currently. Make sure to spawn and be out of cockpit!");
                return;
            }

            try {

                Plugin.checkAuthor(character, Context);

            } catch (Exception e) {
                Log.Error("Error on transferring ship", e);
            }
        }

        [Command("listgridsowner", "Checks which grids the given player has ownership on.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void InspectOwner() {

            string playerName = null;
            bool gps = false;
            
            List<string> args = Context.Args;

            if (args.Count == 1) {

                playerName = args[0];

            } else if (args.Count == 2) {

                playerName = args[0];

                if (args[1] == "-gps")
                    gps = true;

            } else {

                Context.Respond("Correct Usage is !listgridsowner <playerName> [-gps]");
                return;
            }

            long id = GetPlayerIdByName(playerName);

            StringBuilder sb = new StringBuilder();

            List<MyCubeGrid> bigOwnerGrids = new List<MyCubeGrid>();
            List<MyCubeGrid> smallOwnerGrids = new List<MyCubeGrid>();

            foreach (MyEntity entity in MyEntities.GetEntities()) {

                MyCubeGrid grid = entity as MyCubeGrid;

                if (grid == null)
                    continue;

                if (grid.BigOwners.Contains(id))
                    bigOwnerGrids.Add(grid);
                else if (grid.SmallOwners.Contains(id))
                    smallOwnerGrids.Add(grid);
            }

            sb.AppendLine("More then 50 % Ownership");
            sb.AppendLine("---------------------------------------");

            foreach (MyCubeGrid grid in bigOwnerGrids)
                AddGridToSb(grid, sb, gps, id, false);

            sb.AppendLine("");
            sb.AppendLine("Less then 50 % Ownership");
            sb.AppendLine("---------------------------------------");

            foreach (MyCubeGrid grid in smallOwnerGrids) 
                AddGridToSb(grid, sb, gps, id, false);

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

            string playerName = null;
            bool gps = false;

            List<string> args = Context.Args;

            if (args.Count == 1) {

                playerName = args[0];

            } else if (args.Count == 2) {

                playerName = args[0];

                if (args[1] == "-gps")
                    gps = true;

            } else {

                Context.Respond("Correct Usage is !listgridsauthor <playerName> [-gps]");
                return;
            }

            long id = GetPlayerIdByName(playerName);

            StringBuilder sb = new StringBuilder();

            foreach (MyEntity entity in MyEntities.GetEntities()) {

                MyCubeGrid grid = entity as MyCubeGrid;

                if (grid == null)
                    continue;

                AddGridToSb(grid, sb, gps,id, true);
            }

            if (Context.Player == null) {

                Context.Respond($"Grids built by {playerName}");
                Context.Respond(sb.ToString());

            } else {

                ModCommunication.SendMessageTo(new DialogMessage("List Grids Authorships", $"Grids built by {playerName}", sb.ToString()), Context.Player.SteamUserId);
            }
        }


        private void AddGridToSb(MyCubeGrid grid, StringBuilder sb, bool gps, long playerId, bool pcu) {

            HashSet<MySlimBlock> blocks = new HashSet<MySlimBlock>(grid.GetBlocks());

            int value = 0;

            foreach(MySlimBlock block in blocks) {

                if (block == null || block.CubeGrid == null || block.IsDestroyed)
                    continue;

                if (!pcu) {

                    MyCubeBlock cubeBlock = block.FatBlock;
                    if (cubeBlock != null && cubeBlock.OwnerId == playerId)
                        value++;

                    continue;
                } 

                if (block.BuiltBy == playerId) {

                    int pcuValue = 1;
                    if (block.ComponentStack.IsFunctional)
                        pcuValue = block.BlockDefinition.PCU;

                    value += pcuValue;
                }
            }

            if (value == 0)
                return;

            if(pcu)
                sb.AppendLine($"{grid.DisplayName} - {value} PCU - Position {grid.PositionComp.GetPosition().ToString()}");
            else
                sb.AppendLine($"{grid.DisplayName} - {value} blocks - Position {grid.PositionComp.GetPosition().ToString()}");

            if (gps) {

                var gridGPS = MyAPIGateway.Session?.GPS.Create(grid.DisplayName, ($"{grid.DisplayName} - {grid.GridSizeEnum} - {grid.BlocksCount} blocks"), grid.PositionComp.GetPosition(), true);

                if (Context.Player != null)
                    MyAPIGateway.Session?.GPS.AddGps(Context.Player.IdentityId, gridGPS);
            }
        }

        private bool checkConformation(long executingPlayerId, MyPlayer author, string gridName, IMyCharacter character, bool pcu, bool force) {

            if (author == null && pcu) {
                Context.Respond("Player not Found!.");
                return false;
            }

            long authorId = 0L;
            if (author != null)
                authorId = author.Identity.IdentityId;

            string command = gridName + "_" + authorId;

            var confirmationCooldownMap = Plugin.ConfirmationsMap;

            CurrentCooldown confirmationCooldown = null;

            if (confirmationCooldownMap.TryGetValue(executingPlayerId, out confirmationCooldown)) {

                long remainingSeconds = confirmationCooldown.getRemainingSeconds(command);

                if (remainingSeconds == 0) {

                    if (!checkGridFound(author, gridName, character, pcu, force))
                        return false;

                    Context.Respond("Are you sure you want to continue? Enter the command again within 30 seconds to confirm.");
                    confirmationCooldown.startCooldown(command);
                    return false;
                }

            } else {

                if (!checkGridFound(author, gridName, character, pcu, force))
                    return false;

                confirmationCooldown = new CurrentCooldown(Plugin.CooldownConfirmation);
                confirmationCooldownMap.Add(executingPlayerId, confirmationCooldown);

                Context.Respond("Are you sure you want to continue? Enter the command again within 30 seconds to confirm.");

                confirmationCooldown.startCooldown(command);
                return false;
            }

            confirmationCooldownMap.Remove(executingPlayerId);
            return true;
        }

        private bool checkGridFound(MyPlayer player, string gridName, IMyCharacter character, bool pcu, bool force) {

            ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups;

            if (character == null)
                groups = GridManagerPlugin.findGridGroups(gridName);
            else
                groups = GridManagerPlugin.FindLookAtGridGroup(character);

            MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group = null;

            if (!GridManagerPlugin.checkGroups(groups, out group, Context, player, pcu, force))
                return false;

            return true;
        }

        public static MyPlayer GetPlayerByNameOrId(string nameOrPlayerId) {

            if (!long.TryParse(nameOrPlayerId, out long id)) {
                foreach (var identity in MySession.Static.Players.GetAllIdentities()) {
                    if (identity.DisplayName == nameOrPlayerId) {
                        id = identity.IdentityId;
                    }
                }
            }

            if (MySession.Static.Players.TryGetPlayerId(id, out MyPlayer.PlayerId playerId)) {
                if (MySession.Static.Players.TryGetPlayerById(playerId, out MyPlayer player)) {
                    return player;
                }
            }

            return null;
        }

        public static long GetPlayerIdByName(string playerName) {

            foreach (var identity in MySession.Static.Players.GetAllIdentities())
                if (identity.DisplayName == playerName)
                    return identity.IdentityId;

            return 0;
        }
    }
}
