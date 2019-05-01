using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Torch.Commands;
using Torch.Commands.Permissions;
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
    }
}
